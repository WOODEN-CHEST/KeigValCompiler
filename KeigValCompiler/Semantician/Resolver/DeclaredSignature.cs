using KeigValCompiler.Semantician.Library;
using KeigValCompiler.Semantician.Member;
using KeigValCompiler.Semantician.Types;
using System.Text;

namespace KeigValCompiler.Semantician.Resolver;

/* A member as the declaration checks compare it with another: its kind, its name, its own generic
 * parameters, its parameters' types and how each is passed, and its type, all as seen through a type which
 * holds it, so that the "Equals" of IEquatable<int> takes an int. Two functions' own generic parameters are
 * compared by position, as in C#, so "Foo<T>(T)" and "Foo<U>(U)" take the same parameters. */
internal sealed class DeclaredSignature
{
    // Static fields.
    private const FunctionParameterModifier BY_REFERENCE = FunctionParameterModifier.Ref
        | FunctionParameterModifier.Out | FunctionParameterModifier.In;
    private const char SPACE = ' ';
    private const string SEPARATOR = ", ";


    // Internal fields.
    internal required PackMember Member { get; init; }
    internal required DeclaredSignatureKind Kind { get; init; }

    /* The member's own name, which for an indexer is "this", and for an operator or a conversion is left
     * to Operator. */
    internal required string Name { get; init; }
    internal required OverloadableOperator? Operator { get; init; }
    internal required GenericTypeParameterCollection GenericParameters { get; init; }
    internal required IReadOnlyList<SemanticType> ParameterTypes { get; init; }
    internal required IReadOnlyList<FunctionParameterModifier> ParameterModifiers { get; init; }

    /* What a function returns, or null when it returns nothing or is a constructor; for any other member,
     * its type. */
    internal required SemanticType? Type { get; init; }

    /* Whether every type in the signature was read. A type which did not resolve has been reported, and a
     * signature missing one is left out of comparisons, which could only report what follows from it. */
    internal required bool IsComplete { get; init; }


    // Internal methods.
    /* Whether another member takes the same parameters. Which parameters are passed by reference counts,
     * and, when exactly, by which of "ref", "out" and "in" as well. */
    internal bool HasSameParameters(DeclaredSignature other, bool isPassingExact)
    {
        ArgumentNullException.ThrowIfNull(other, nameof(other));

        if (!HasSameParameterTypes(other))
        {
            return false;
        }
        for (int Index = 0; Index < ParameterTypes.Count; Index++)
        {
            if (!IsSamePassing(ParameterModifiers[Index], other.ParameterModifiers[Index], isPassingExact))
            {
                return false;
            }
        }
        return true;
    }

    /* Whether another member takes parameters of the same types, however each is passed, and has as many
     * generic parameters. */
    internal bool HasSameParameterTypes(DeclaredSignature other)
    {
        ArgumentNullException.ThrowIfNull(other, nameof(other));

        if ((GenericParameters.Count != other.GenericParameters.Count)
            || (ParameterTypes.Count != other.ParameterTypes.Count))
        {
            return false;
        }

        TypeSubstitution Positions = TypeSubstitution.ByPosition(other.GenericParameters, GenericParameters);
        for (int Index = 0; Index < ParameterTypes.Count; Index++)
        {
            if (!ParameterTypes[Index].Equals(other.ParameterTypes[Index].Substitute(Positions)))
            {
                return false;
            }
        }
        return true;
    }

    /* Whether another member has the same type, its own generic parameters standing for these. */
    internal bool HasSameType(DeclaredSignature other)
    {
        ArgumentNullException.ThrowIfNull(other, nameof(other));

        if ((Type == null) || (other.Type == null))
        {
            return (Type == null) && (other.Type == null);
        }
        return Type.Equals(MapFromOther(other, other.Type));
    }

    /* A type written in another member's signature, with that member's own generic parameters replaced by
     * these, by position. */
    internal SemanticType MapFromOther(DeclaredSignature other, SemanticType type)
    {
        ArgumentNullException.ThrowIfNull(other, nameof(other));
        ArgumentNullException.ThrowIfNull(type, nameof(type));

        return type.Substitute(TypeSubstitution.ByPosition(other.GenericParameters, GenericParameters));
    }


    /* The member as messages name it, with its parameters' types as the signature has them, as in
     * "Equals(int)", "this[string]" or "operator +(Money, Money)", so that overloads, and a member seen
     * through a type which gives its generic parameters types, are told apart. */
    internal string GetDisplayName()
    {
        StringBuilder Builder = new();
        switch (Kind)
        {
            case DeclaredSignatureKind.Method or DeclaredSignatureKind.Constructor:
                Builder.Append(Name);
                if (GenericParameters.Count > 0)
                {
                    Builder.Append(KGVL.GENERIC_TYPE_START).Append(string.Join(SEPARATOR, GenericParameters
                        .Select(parameter => parameter.SelfIdentifier.SourceCodeName)))
                        .Append(KGVL.GENERIC_TYPE_END);
                }
                AppendParameters(Builder, KGVL.OPEN_PARENTHESIS, KGVL.CLOSE_PARENTHESIS);
                break;

            case DeclaredSignatureKind.Indexer:
                Builder.Append(KGVL.KEYWORD_THIS);
                AppendParameters(Builder, KGVL.OPEN_SQUARE_BRACKET, KGVL.CLOSE_SQUARE_BRACKET);
                break;

            case DeclaredSignatureKind.Operator:
                Builder.Append(KGVL.KEYWORD_OPERATOR).Append(SPACE)
                    .Append(SignatureFormatter.GetOperatorSpelling(Operator!.Value));
                AppendParameters(Builder, KGVL.OPEN_PARENTHESIS, KGVL.CLOSE_PARENTHESIS);
                break;

            case DeclaredSignatureKind.Conversion:
                Builder.Append((Operator == OverloadableOperator.ImplicitCast) ? KGVL.KEYWORD_IMPLICIT
                    : KGVL.KEYWORD_EXPLICIT).Append(SPACE).Append(KGVL.KEYWORD_OPERATOR).Append(SPACE)
                    .Append(Type?.ToString() ?? KGVL.KEYWORD_VOID);
                AppendParameters(Builder, KGVL.OPEN_PARENTHESIS, KGVL.CLOSE_PARENTHESIS);
                break;

            default:
                Builder.Append(Name);
                break;
        }
        return Builder.ToString();
    }


    // Private methods.
    private void AppendParameters(StringBuilder builder, char open, char close)
    {
        builder.Append(open);
        for (int Index = 0; Index < ParameterTypes.Count; Index++)
        {
            if (Index > 0)
            {
                builder.Append(SEPARATOR);
            }
            string? Keyword = (ParameterModifiers[Index] & BY_REFERENCE) switch
            {
                FunctionParameterModifier.Ref => KGVL.KEYWORD_REF,
                FunctionParameterModifier.Out => KGVL.KEYWORD_OUT,
                FunctionParameterModifier.In => KGVL.KEYWORD_IN,
                _ => null
            };
            if (Keyword != null)
            {
                builder.Append(Keyword).Append(SPACE);
            }
            builder.Append(ParameterTypes[Index]);
        }
        builder.Append(close);
    }

    private bool IsSamePassing(FunctionParameterModifier first, FunctionParameterModifier second,
        bool isPassingExact)
    {
        FunctionParameterModifier FirstPassing = first & BY_REFERENCE;
        FunctionParameterModifier SecondPassing = second & BY_REFERENCE;
        bool IsFirstByReference = FirstPassing != FunctionParameterModifier.None;
        bool IsSecondByReference = SecondPassing != FunctionParameterModifier.None;
        return isPassingExact ? (FirstPassing == SecondPassing) : (IsFirstByReference == IsSecondByReference);
    }
}
