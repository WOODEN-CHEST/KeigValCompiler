using KeigValCompiler.Semantician.Member;
using KeigValCompiler.Semantician.Types;

namespace KeigValCompiler.Semantician.Resolver;

/* Reads the DeclaredSignature of a member which a type or namespace declares, as seen through a type
 * holding it: the substitution gives that type's arguments for the generic parameters of the member's
 * own type, and of any type around it. */
internal class DeclaredSignatureReader
{
    // Static fields.
    private static readonly GenericTypeParameterCollection _noGenericParameters = new();


    // Private fields.
    private readonly SemanticTypeReader _typeReader;


    // Constructors.
    internal DeclaredSignatureReader(SemanticTypeReader typeReader)
    {
        _typeReader = typeReader ?? throw new ArgumentNullException(nameof(typeReader));
    }


    // Internal methods.
    internal DeclaredSignature Read(PackMember member, TypeSubstitution substitution)
    {
        ArgumentNullException.ThrowIfNull(member, nameof(member));
        ArgumentNullException.ThrowIfNull(substitution, nameof(substitution));

        OperatorOverload? Overload = MemberRelations.GetOperatorOverload(member);
        DeclaredSignatureKind Kind = GetKind(member, Overload);
        FunctionParameterCollection? Parameters = member switch
        {
            PackFunction Function => Function.Parameters,
            PackIndexer Indexer => Indexer.Parameters,
            _ => null
        };
        TypeTargetIdentifier? WrittenType = member switch
        {
            PackFunction Function => Function.ReturnType,
            PackProperty Property => Property.Type,
            PackIndexer Indexer => Indexer.Type,
            PackEvent Event => Event.Type,
            PackField Field => Field.Type,
            _ => null
        };

        bool IsComplete = true;
        List<SemanticType> ParameterTypes = new();
        List<FunctionParameterModifier> ParameterModifiers = new();
        foreach (FunctionParameter Parameter in Parameters ?? Enumerable.Empty<FunctionParameter>())
        {
            SemanticType? ParameterType = ReadType(Parameter.Type, substitution);
            IsComplete &= ParameterType != null;
            if (ParameterType != null)
            {
                ParameterTypes.Add(ParameterType);
                ParameterModifiers.Add(Parameter.Modifiers);
            }
        }

        SemanticType? Type = ReadType(WrittenType, substitution);
        IsComplete &= (WrittenType == null) || (Type != null);

        return new()
        {
            Member = member,
            Kind = Kind,
            Name = (Kind == DeclaredSignatureKind.Indexer) ? KGVL.KEYWORD_THIS
                : member.SelfIdentifier.SourceCodeName,
            Operator = Overload?.OverloadedOperator,
            GenericParameters = (member as PackFunction)?.GenericParameters ?? _noGenericParameters,
            ParameterTypes = ParameterTypes,
            ParameterModifiers = ParameterModifiers,
            Type = Type,
            IsComplete = IsComplete
        };
    }


    // Private methods.
    private DeclaredSignatureKind GetKind(PackMember member, OperatorOverload? overload)
    {
        if (overload != null)
        {
            return OverloadableOperatorKinds.IsConversion(overload.OverloadedOperator)
                ? DeclaredSignatureKind.Conversion : DeclaredSignatureKind.Operator;
        }

        return member switch
        {
            PackConstructor => DeclaredSignatureKind.Constructor,
            PackFunction => DeclaredSignatureKind.Method,
            PackProperty => DeclaredSignatureKind.Property,
            PackIndexer => DeclaredSignatureKind.Indexer,
            PackEvent => DeclaredSignatureKind.Event,
            PackField => DeclaredSignatureKind.Field,
            _ => throw new ArgumentException($"{member} is not a member with a signature.", nameof(member))
        };
    }

    private SemanticType? ReadType(TypeTargetIdentifier? type, TypeSubstitution substitution)
    {
        return (type == null) ? null : _typeReader.Read(type)?.Substitute(substitution);
    }
}
