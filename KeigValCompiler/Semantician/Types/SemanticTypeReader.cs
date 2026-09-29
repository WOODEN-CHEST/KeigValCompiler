using KeigValCompiler.Semantician.Library;
using KeigValCompiler.Semantician.Member;

namespace KeigValCompiler.Semantician.Types;

/* Builds the SemanticType a resolved TypeTargetIdentifier names, and the types a declaration implies:
 * the type it is inside its own declaration, and the types it derives from. It is only usable once types
 * are resolved and the library's known types found. A type which did not resolve, or which needs a known
 * type the library does not declare, reads as null, since that has been reported already. */
internal class SemanticTypeReader
{
    // Private fields.
    private readonly BuiltInTypeRegistry _registry;


    // Constructors.
    internal SemanticTypeReader(BuiltInTypeRegistry registry)
    {
        _registry = registry ?? throw new ArgumentNullException(nameof(registry));
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

        if (type.Declaration is not IPackMemberExtender Extender)
        {
            yield break;
        }

        /* A base written as an array or with '?' is reported by InheritanceChecker and no base at all. */
        TypeSubstitution Substitution = TypeSubstitution.Of(type);
        foreach (TypeTargetIdentifier WrittenBase in Extender.ExtendedMembers.Where(
            written => !written.IsArrayOrNullable))
        {
            if (Read(WrittenBase)?.Substitute(Substitution) is DeclaredType BaseType)
            {
                yield return BaseType;
            }
        }
    }


    // Private methods.
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
                return new DeclaredType(Declaration, TypeArguments, GetContainingType(Declaration),
                    _registry.GetLibraryType(Declaration), false);

            default:
                return null;
        }
    }

    /* A nested type can only be named from inside the types around it, so what holds it is always the
     * holding type as its own declaration sees it. */
    private DeclaredType? GetContainingType(PackMember declaration)
    {
        return (declaration.ParentItem?.Target is PackMember Holder) ? GetInstanceType(Holder) : null;
    }

    private DeclaredType? ReadKnownType(LibraryType knownType, SemanticType typeArgument)
    {
        PackMember? Declaration = _registry.GetDeclaredType(knownType);
        if (Declaration == null)
        {
            return null;
        }
        return new(Declaration, new SemanticType[] { typeArgument }, null, knownType, false);
    }
}
