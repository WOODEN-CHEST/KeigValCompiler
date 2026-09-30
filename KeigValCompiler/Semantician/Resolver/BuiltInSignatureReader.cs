using KeigValCompiler.Semantician.Library;
using KeigValCompiler.Semantician.Member;

namespace KeigValCompiler.Semantician.Resolver;

/* Reads the signature of one resolved builtin member, for matching against those the compiler
 * implements. Every type in it has to be one the compiler knows by name. When one is not, reading
 * fails and the first such type is kept for the error; when one is not even resolved, reading fails
 * quietly, since that has already been reported. */
internal class BuiltInSignatureReader
{
    // Internal fields.
    /* The first type in the signature which the compiler does not know by name, if reading failed on
     * one. */
    internal TypeTargetIdentifier? UnknownType { get; private set; } = null;


    // Private fields.
    private readonly BuiltInTypeRegistry _registry;
    private readonly LibraryType _declaringType;
    private readonly IGenericParameterHolder? _declaringGenerics;


    // Constructors.
    internal BuiltInSignatureReader(BuiltInTypeRegistry registry,
        LibraryType declaringType,
        PackMember declaringMember)
    {
        _registry = registry ?? throw new ArgumentNullException(nameof(registry));
        _declaringType = declaringType ?? throw new ArgumentNullException(nameof(declaringType));
        ArgumentNullException.ThrowIfNull(declaringMember, nameof(declaringMember));
        _declaringGenerics = declaringMember as IGenericParameterHolder;
    }


    // Internal methods.
    /* A method, a constructor, or an operator, which is a function the type's operator list names. */
    internal MemberSignature? ReadFunction(PackFunction function, OverloadableOperator? overloadedOperator)
    {
        ArgumentNullException.ThrowIfNull(function, nameof(function));

        MemberSignatureKind Kind = (function is PackConstructor) ? MemberSignatureKind.Constructor
            : (overloadedOperator != null) ? MemberSignatureKind.Operator : MemberSignatureKind.Method;
        SignatureType? ReturnType = (function.ReturnType == null) ? null : ReadType(function.ReturnType, function);
        SignatureParameter[]? Parameters = ReadParameters(function.Parameters, function);

        if (((function.ReturnType != null) && (ReturnType == null)) || (Parameters == null))
        {
            return null;
        }
        return new()
        {
            DeclaringType = _declaringType,
            Kind = Kind,
            Name = (Kind == MemberSignatureKind.Method) ? function.SelfIdentifier.SourceCodeName : null,
            Operator = overloadedOperator,
            IsStatic = function.HasModifier(PackMemberModifiers.Static),
            Type = ReturnType,
            Parameters = Parameters,
            GenericParameterCount = function.GenericParameters.Count
        };
    }

    internal MemberSignature? ReadPropertyAccessor(PackProperty property, bool isGetter)
    {
        ArgumentNullException.ThrowIfNull(property, nameof(property));

        SignatureType? Type = ReadType(property.Type, null);
        if (Type == null)
        {
            return null;
        }
        return new()
        {
            DeclaringType = _declaringType,
            Kind = isGetter ? MemberSignatureKind.PropertyGetter : MemberSignatureKind.PropertySetter,
            Name = property.SelfIdentifier.SourceCodeName,
            IsStatic = property.HasModifier(PackMemberModifiers.Static),
            Type = Type,
            IsInit = !isGetter && (property.SetFunction == null) && (property.InitFunction != null)
        };
    }

    internal MemberSignature? ReadIndexerAccessor(PackIndexer indexer, bool isGetter)
    {
        ArgumentNullException.ThrowIfNull(indexer, nameof(indexer));

        SignatureType? Type = ReadType(indexer.Type, null);
        SignatureParameter[]? Parameters = ReadParameters(indexer.Parameters, null);
        if ((Type == null) || (Parameters == null))
        {
            return null;
        }
        return new()
        {
            DeclaringType = _declaringType,
            Kind = isGetter ? MemberSignatureKind.IndexerGetter : MemberSignatureKind.IndexerSetter,
            Type = Type,
            Parameters = Parameters,
            IsInit = !isGetter && (indexer.SetFunction == null) && (indexer.InitFunction != null)
        };
    }


    // Private methods.
    private SignatureParameter[]? ReadParameters(FunctionParameterCollection parameters, PackFunction? function)
    {
        List<SignatureParameter> Read = new();
        foreach (FunctionParameter Parameter in parameters)
        {
            SignatureType? Type = (Parameter.Type == null) ? null : ReadType(Parameter.Type, function);
            if (Type == null)
            {
                return null;
            }
            Read.Add(new(Type, Parameter.Modifiers));
        }
        return Read.ToArray();
    }

    /* The type's own name and arguments first, then its array levels and nullable markers from the
     * innermost out. A '?' makes a value type a Nullable, and is only an annotation on anything else. */
    private SignatureType? ReadType(TypeTargetIdentifier type, PackFunction? function)
    {
        SignatureType? Type = ReadNamedType(type, function);
        if (Type == null)
        {
            return null;
        }

        for (int Level = 0; Level <= type.ArrayRank; Level++)
        {
            if (Level > 0)
            {
                Type = SignatureType.ArrayOf(Type);
            }
            if (type.GetNullabilityAtLevel(Level) && (Level == 0) && IsValueType(type.MainTarget.Target))
            {
                Type = SignatureType.NullableOf(Type);
            }
        }
        return Type;
    }

    private SignatureType? ReadNamedType(TypeTargetIdentifier type, PackFunction? function)
    {
        IIdentifiable? Target = type.MainTarget.Target;

        if (Target is GenericTypeParameter Parameter)
        {
            int MethodPosition = IndexOf(function?.GenericParameters, Parameter);
            if (MethodPosition >= 0)
            {
                return SignatureType.MethodParameter(MethodPosition);
            }
            int TypePosition = IndexOf(_declaringGenerics?.GenericParameters, Parameter);
            if (TypePosition >= 0)
            {
                return SignatureType.TypeParameter(TypePosition);
            }
        }
        else if ((Target is PackMember Member) && (_registry.GetLibraryType(Member) is LibraryType KnownType))
        {
            List<SignatureType> Arguments = new();
            foreach (TypeTargetIdentifier Argument in type.TypeArguments)
            {
                SignatureType? ReadArgument = ReadType(Argument, function);
                if (ReadArgument == null)
                {
                    return null;
                }
                Arguments.Add(ReadArgument);
            }
            return SignatureType.Of(KnownType, Arguments.ToArray());
        }

        if (Target != null)
        {
            UnknownType ??= type;
        }
        return null;
    }

    private int IndexOf(GenericTypeParameterCollection? parameters, GenericTypeParameter parameter)
    {
        if (parameters == null)
        {
            return -1;
        }

        int Position = 0;
        foreach (GenericTypeParameter Candidate in parameters)
        {
            if (ReferenceEquals(Candidate, parameter))
            {
                return Position;
            }
            Position++;
        }
        return -1;
    }

    /* Structs and enums are value types, and so is a generic parameter constrained to be a struct. */
    private bool IsValueType(IIdentifiable? target)
    {
        return target switch
        {
            PackStruct or PackEnumeration => true,
            GenericTypeParameter Parameter => Parameter.Constraints.Any(
                constraint => constraint.SpecialConstraint == SpecialGenericConstraint.Struct),
            _ => false
        };
    }
}
