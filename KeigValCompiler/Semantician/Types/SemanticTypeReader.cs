using KeigValCompiler.Semantician.Library;
using KeigValCompiler.Semantician.Member;

namespace KeigValCompiler.Semantician.Types;

/* Builds the SemanticType a resolved TypeTargetIdentifier names, and the types a declaration implies:
 * the type it is inside its own declaration, and the types it derives from. It is only usable once types
 * are resolved and the library's known types found. A type which did not resolve, or which needs a known
 * type the library does not declare, reads as null, since that has been reported already, and so does a
 * nested type reached through a base which reads as null. */
internal class SemanticTypeReader
{
    // Private fields.
    private readonly BuiltInTypeRegistry _registry;

    /* The type holding each nested type found through bases, once read along the way the lookup went, which
     * does not change once the name is resolved, so that each way is read once. */
    private readonly Dictionary<TypeTargetIdentifier, DeclaredType?> _inheritedHolders =
        new(ReferenceEqualityComparer.Instance);


    // Constructors.
    internal SemanticTypeReader(BuiltInTypeRegistry registry)
    {
        _registry = registry ?? throw new ArgumentNullException(nameof(registry));
    }


    // Internal static methods.
    /* What stands in a class's base class's place, as C# decides it: the first base it lists which is not an
     * interface, when that names a class, even written with '?', which is reported but still names the class.
     * One which did not resolve, or is no class, has been reported, and leaves the class with no base class of
     * its own rather than letting a class written after it take its place. A base written as an array is
     * passed over. Null for any type but a class. */
    internal static TypeTargetIdentifier? GetWrittenBaseClassName(PackMember declaration)
    {
        ArgumentNullException.ThrowIfNull(declaration, nameof(declaration));

        if (declaration is not PackClass Class)
        {
            return null;
        }
        TypeTargetIdentifier? Written = Class.ExtendedMembers.FirstOrDefault(written => !written.IsArray
            && (written.MainTarget.Target is not PackInterface));
        return (Written?.MainTarget.Target is PackClass) ? Written : null;
    }


    // Internal methods.
    /* The named type with its arguments first, then its array levels and '?' markers from the innermost
     * out, as KGVL writes them: each "[]" wraps what is to its left, and each '?' marks what is just to
     * its left, making a value type Nullable and annotating anything else. */
    internal SemanticType? Read(TypeTargetIdentifier type)
    {
        ArgumentNullException.ThrowIfNull(type, nameof(type));

        SemanticType? Type = ReadNamedType(type);
        for (int Level = 0; (Type != null) && (Level <= type.ArrayRank); Level++)
        {
            if (Level > 0)
            {
                Type = ReadKnownType(LibraryTypes.Array, Type);
            }
            if ((Type != null) && type.GetNullabilityAtLevel(Level))
            {
                Type = IsNullableValue(Type) ? ReadKnownType(LibraryTypes.Nullable, Type)
                    : Type.WithNullableAnnotation(true);
            }
        }
        return Type;
    }

    /* A type as its own declaration sees it, with its generic parameters as its type arguments, as
     * Thing<T> is inside Thing<T>. */
    internal DeclaredType GetInstanceType(PackMember declaration)
    {
        ArgumentNullException.ThrowIfNull(declaration, nameof(declaration));

        SemanticType[] TypeArguments = (declaration as IGenericParameterHolder)?.GenericParameters
            .Select(parameter => (SemanticType)new GenericParameterType(parameter, false)).ToArray()
            ?? Array.Empty<SemanticType>();
        return new(declaration, TypeArguments, GetContainingType(declaration),
            _registry.GetLibraryType(declaration), false);
    }

    /* The types a type's declaration names as its bases, given in terms of the type's own type arguments,
     * so that Thing<int> derives from IEquatable<Thing<int>> where Thing<T> derives from
     * IEquatable<Thing<T>>. A base which did not resolve is left out. */
    internal IEnumerable<DeclaredType> GetWrittenBaseTypes(DeclaredType type)
    {
        ArgumentNullException.ThrowIfNull(type, nameof(type));

        return GetWrittenBases(type).Select(written => written.Base);
    }

    /* The types GetWrittenBaseTypes gives, each with the base as the declaration writes it. */
    internal IEnumerable<(TypeTargetIdentifier WrittenBase, DeclaredType Base)> GetWrittenBases(
        DeclaredType type)
    {
        ArgumentNullException.ThrowIfNull(type, nameof(type));

        if (type.Declaration is not IPackMemberExtender Extender)
        {
            yield break;
        }

        TypeSubstitution Substitution = TypeSubstitution.Of(type);
        TypeTargetIdentifier? BaseClassName = GetWrittenBaseClassName(type.Declaration);
        foreach (TypeTargetIdentifier WrittenBase in Extender.ExtendedMembers.Where(
            written => !written.IsArray))
        {
            SemanticType? Base = WrittenBase.IsNullable
                ? (IsKeptDespiteMarker(type.Declaration, WrittenBase, BaseClassName) ? ReadNamedType(WrittenBase) : null)
                : Read(WrittenBase);
            if (Base?.Substitute(Substitution) is DeclaredType BaseType)
            {
                yield return (WrittenBase, BaseType);
            }
        }
    }


    // Private methods.
    /* A base written as an array is reported by InheritanceChecker and no base at all, and so is one written
     * with '?', as Roslyn takes it, but for a class's base class and an interface's base interfaces, which it
     * still takes as the type named: only a class's or a structure's interfaces written so are none. */
    private bool IsKeptDespiteMarker(PackMember declaration,
        TypeTargetIdentifier writtenBase,
        TypeTargetIdentifier? baseClassName)
    {
        return (declaration is PackInterface) || ReferenceEquals(writtenBase, baseClassName);
    }

    /* Whether a '?' after the type makes it Nullable rather than annotating it. As in C#, a function which
     * overrides another or implements one explicitly cannot restate its generic parameters' constraints,
     * so a '?' on one of them makes it Nullable unless it is written with a "class" constraint, which is
     * what its constraint inherited from the other function would decide. */
    private bool IsNullableValue(SemanticType type)
    {
        if (type.IsValueType)
        {
            return true;
        }
        if ((type is not GenericParameterType ParameterType)
            || (ParameterType.Parameter.Owner is not PackFunction Owner))
        {
            return false;
        }

        bool IsRestating = Owner.HasModifier(PackMemberModifiers.Override) || (Owner.ExplicitInterface != null);
        return IsRestating && !ParameterType.Parameter.Constraints.Any(
            constraint => constraint.SpecialConstraint == SpecialGenericConstraint.Class);
    }

    private SemanticType? ReadNamedType(TypeTargetIdentifier type)
    {
        switch (type.MainTarget.Target)
        {
            case GenericTypeParameter Parameter:
                return new GenericParameterType(Parameter, false);

            case PackMember Declaration:
                List<SemanticType> TypeArguments = new();
                foreach (TypeTargetIdentifier Argument in type.TypeArguments)
                {
                    SemanticType? ReadArgument = Read(Argument);
                    if (ReadArgument == null)
                    {
                        return null;
                    }
                    TypeArguments.Add(ReadArgument);
                }

                if (!TryReadContainingType(type, Declaration, out DeclaredType? ContainingType))
                {
                    return null;
                }
                return new DeclaredType(Declaration, TypeArguments, ContainingType,
                    _registry.GetLibraryType(Declaration), false);

            default:
                return null;
        }
    }

    /* What holds a nested type as its own declaration sees itself, in terms of its generic parameters. */
    private DeclaredType? GetContainingType(PackMember declaration)
    {
        return (declaration.ParentItem?.Target is PackMember Holder) ? GetInstanceType(Holder) : null;
    }

    /* What holds a named nested type. One named after a type, as in "Outer<int>.Inner", is held by that type,
     * and one found through the bases of the type named, or of a type around the use, by the base declaring
     * it, as that type sees it: inside a class deriving from Outer<int>, "Inner" is Outer<int>.Inner. That
     * base is read along the way the lookup went to it. One named on its own among the types around the use is
     * held by its holder as that one sees itself, and so is any whose holder has no generic parameters, nor
     * any type around it, being the same type however it is reached. False when the type before the '.', or
     * a base on the way to the holder, cannot be read, which has been reported. */
    private bool TryReadContainingType(TypeTargetIdentifier type,
        PackMember declaration,
        out DeclaredType? containingType)
    {
        containingType = null;
        if (declaration.ParentItem?.Target is not PackMember Holder)
        {
            return true;
        }

        DeclaredType? Through = (type.InheritedThrough != null) ? GetInstanceType(type.InheritedThrough) : null;
        if (type.Qualifier?.MainTarget.Target is PackMember)
        {
            Through = Read(type.Qualifier) as DeclaredType;
            if (Through == null)
            {
                return false;
            }
        }

        if ((Through == null) || ((type.InheritedThrough != null) && !IsGenericOrInsideGeneric(Holder)))
        {
            containingType = GetInstanceType(Holder);
        }
        else
        {
            containingType = (type.InheritedThrough == null) ? Through : ReadInheritedHolder(type, Through);
        }
        return containingType != null;
    }

    private bool IsGenericOrInsideGeneric(PackMember declaration)
    {
        for (PackMember? Type = declaration; Type != null; Type = Type.ParentItem?.Target as PackMember)
        {
            if ((Type is IGenericParameterHolder GenericsHolder)
                && (GenericsHolder.GenericParameters.Count > 0))
            {
                return true;
            }
        }
        return false;
    }

    /* Each base on the way is read in terms of the type before it, starting from the type the name was found
     * through. The way reads back only bases resolved before the name was, so it never comes back round. */
    private DeclaredType? ReadInheritedHolder(TypeTargetIdentifier type, DeclaredType through)
    {
        if (_inheritedHolders.TryGetValue(type, out DeclaredType? Known))
        {
            return Known;
        }

        DeclaredType? Current = (type.InheritedPath == null) ? null : through;
        foreach (TypeTargetIdentifier WrittenBase in type.InheritedPath ?? Array.Empty<TypeTargetIdentifier>())
        {
            Current = (Current == null) ? null
                : ReadNamedType(WrittenBase)?.Substitute(TypeSubstitution.Of(Current)) as DeclaredType;
        }
        _inheritedHolders[type] = Current;
        return Current;
    }

    /* A type the compiler knows by name with one type argument, as Nullable<int>, or null when the library
     * does not declare it, which has been reported. */
    internal DeclaredType? ReadKnownType(LibraryType knownType, SemanticType typeArgument)
    {
        PackMember? Declaration = _registry.GetDeclaredType(knownType);
        if (Declaration == null)
        {
            return null;
        }
        return new(Declaration, new SemanticType[] { typeArgument }, null, knownType, false);
    }
}
