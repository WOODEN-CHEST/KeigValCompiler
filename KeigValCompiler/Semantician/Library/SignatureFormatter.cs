using KeigValCompiler.Semantician.Member;
using System.Text;

namespace KeigValCompiler.Semantician.Library;

/* Writes signatures out close to how the library declares them, for messages about the library, as
 * in "KGVL.Int32: static int Parse(string)". Nothing compares what it writes, so the wording can
 * change freely. */
internal static class SignatureFormatter
{
    // Static fields.
    private const string SEPARATOR = ", ";
    private const char SPACE = ' ';
    private const string METHOD_GENERIC_PARAMETER_PREFIX = "M";
    private const string TYPE_GENERIC_PARAMETER_PREFIX = "T";


    // Internal static methods.
    internal static string Format(MemberSignature signature)
    {
        ArgumentNullException.ThrowIfNull(signature, nameof(signature));

        StringBuilder Builder = new();
        Builder.Append(FormatDeclaringType(signature.DeclaringType)).Append(KGVL.COLON).Append(SPACE);
        if (signature.IsStatic)
        {
            Builder.Append(KGVL.KEYWORD_STATIC).Append(SPACE);
        }

        switch (signature.Kind)
        {
            case MemberSignatureKind.Constructor:
                Builder.Append(signature.DeclaringType.Name);
                AppendParameters(Builder, signature, KGVL.OPEN_PARENTHESIS, KGVL.CLOSE_PARENTHESIS);
                break;

            case MemberSignatureKind.Operator:
                AppendOperator(Builder, signature);
                break;

            case MemberSignatureKind.PropertyGetter:
            case MemberSignatureKind.PropertySetter:
                Builder.Append(FormatType(signature.Type!, signature)).Append(SPACE).Append(signature.Name);
                AppendAccessor(Builder, signature.Kind == MemberSignatureKind.PropertyGetter);
                break;

            case MemberSignatureKind.IndexerGetter:
            case MemberSignatureKind.IndexerSetter:
                Builder.Append(FormatType(signature.Type!, signature)).Append(SPACE).Append(KGVL.KEYWORD_THIS);
                AppendParameters(Builder, signature, KGVL.OPEN_SQUARE_BRACKET, KGVL.CLOSE_SQUARE_BRACKET);
                AppendAccessor(Builder, signature.Kind == MemberSignatureKind.IndexerGetter);
                break;

            default:
                Builder.Append((signature.Type == null) ? KGVL.KEYWORD_VOID
                    : FormatType(signature.Type, signature));
                Builder.Append(SPACE).Append(signature.Name);
                AppendMethodGenericParameters(Builder, signature.GenericParameterCount);
                AppendParameters(Builder, signature, KGVL.OPEN_PARENTHESIS, KGVL.CLOSE_PARENTHESIS);
                break;
        }

        return Builder.ToString();
    }

    /* A generic parameter is written by its name in the declaring type when there is one to take it
     * from, and by its position otherwise. Arrays and nullable values are written as their syntax. */
    internal static string FormatType(SignatureType type, MemberSignature? signature)
    {
        ArgumentNullException.ThrowIfNull(type, nameof(type));

        if (type.IsGenericParameter)
        {
            if (type.IsMethodGenericParameter)
            {
                return METHOD_GENERIC_PARAMETER_PREFIX + type.GenericParameterPosition;
            }
            IReadOnlyList<string>? Names = signature?.DeclaringType.GenericParameterNames;
            return ((Names != null) && (type.GenericParameterPosition < Names.Count))
                ? Names[type.GenericParameterPosition]
                : TYPE_GENERIC_PARAMETER_PREFIX + type.GenericParameterPosition;
        }

        LibraryType Type = type.Type!;
        if (Type == LibraryTypes.Array)
        {
            return FormatType(type.TypeArguments[0], signature)
                + KGVL.OPEN_SQUARE_BRACKET + KGVL.CLOSE_SQUARE_BRACKET;
        }
        if (Type == LibraryTypes.Nullable)
        {
            return FormatType(type.TypeArguments[0], signature) + KGVL.TYPE_NULLABLE_INDICATOR;
        }
        if (Type.Keyword != null)
        {
            return Type.Keyword;
        }
        if (type.TypeArguments.Count == 0)
        {
            return Type.FullName;
        }
        return Type.FullName + KGVL.GENERIC_TYPE_START
            + string.Join(SEPARATOR, type.TypeArguments.Select(argument => FormatType(argument, signature)))
            + KGVL.GENERIC_TYPE_END;
    }

    /* How source code spells an operator, the same for its unary and binary forms. */
    internal static string GetOperatorSpelling(OverloadableOperator overloadedOperator)
    {
        return overloadedOperator switch
        {
            OverloadableOperator.Addition => KGVL.OPERATOR_ADD,
            OverloadableOperator.UnaryPlus => KGVL.OPERATOR_ADD,
            OverloadableOperator.Subtraction => KGVL.OPERATOR_SUBTRACT,
            OverloadableOperator.Negation => KGVL.OPERATOR_SUBTRACT,
            OverloadableOperator.Multiplication => KGVL.OPERATOR_MULTIPLY,
            OverloadableOperator.Division => KGVL.OPERATOR_DIVIDE,
            OverloadableOperator.Modulo => KGVL.OPERATOR_MODULO,
            OverloadableOperator.LogicalNot => KGVL.OPERATOR_NOT,
            OverloadableOperator.BitwiseComplement => KGVL.OPERATOR_BITWISE_COMPLEMENT,
            OverloadableOperator.Increment => KGVL.OPERATOR_INCREMENT,
            OverloadableOperator.Decrement => KGVL.OPERATOR_DECREMENT,
            OverloadableOperator.BitwiseAnd => KGVL.OPERATOR_BITWISE_AND,
            OverloadableOperator.BitwiseOr => KGVL.OPERATOR_BITWISE_OR,
            OverloadableOperator.BitwiseXor => KGVL.OPERATOR_BITWISE_XOR,
            OverloadableOperator.LeftShift => KGVL.OPERATOR_LEFT_SHIFT,
            OverloadableOperator.RightShift => KGVL.OPERATOR_RIGHT_SHIFT,
            OverloadableOperator.UnsignedRightShift => KGVL.OPERATOR_UNSIGNED_RIGHT_SHIFT,
            OverloadableOperator.Equals => KGVL.OPERATOR_EQUALS,
            OverloadableOperator.NotEquals => KGVL.OPERATOR_NOT_EQUALS,
            OverloadableOperator.LargerThan => KGVL.OPERATOR_LARGER_THAN,
            OverloadableOperator.LessThan => KGVL.OPERATOR_LESS_THAN,
            OverloadableOperator.LargerOrEqual => KGVL.OPERATOR_LARGER_OR_EQUAL,
            OverloadableOperator.LessThanOrEqual => KGVL.OPERATOR_LESS_OR_EQUAL,
            _ => overloadedOperator.ToString()
        };
    }


    // Private static methods.
    private static string FormatDeclaringType(LibraryType type)
    {
        if (type.GenericParameterNames.Count == 0)
        {
            return type.FullName;
        }
        return type.FullName + KGVL.GENERIC_TYPE_START + string.Join(SEPARATOR, type.GenericParameterNames)
            + KGVL.GENERIC_TYPE_END;
    }

    private static void AppendOperator(StringBuilder builder, MemberSignature signature)
    {
        OverloadableOperator Operator = signature.Operator!.Value;
        string TypeText = FormatType(signature.Type!, signature);

        if ((Operator == OverloadableOperator.ImplicitCast) || (Operator == OverloadableOperator.ExplicitCast))
        {
            string Keyword = Operator == OverloadableOperator.ImplicitCast
                ? KGVL.KEYWORD_IMPLICIT : KGVL.KEYWORD_EXPLICIT;
            builder.Append(Keyword).Append(SPACE).Append(KGVL.KEYWORD_OPERATOR).Append(SPACE).Append(TypeText);
        }
        else
        {
            builder.Append(TypeText).Append(SPACE).Append(KGVL.KEYWORD_OPERATOR).Append(SPACE)
                .Append(GetOperatorSpelling(Operator));
        }
        AppendParameters(builder, signature, KGVL.OPEN_PARENTHESIS, KGVL.CLOSE_PARENTHESIS);
    }

    private static void AppendParameters(StringBuilder builder, MemberSignature signature, char open, char close)
    {
        builder.Append(open);
        builder.Append(string.Join(SEPARATOR, signature.Parameters.Select(
            parameter => GetModifierPrefix(parameter.Modifier) + FormatType(parameter.Type, signature))));
        builder.Append(close);
    }

    private static void AppendAccessor(StringBuilder builder, bool isGetter)
    {
        builder.Append(SPACE).Append(KGVL.OPEN_CURLY_BRACKET).Append(SPACE)
            .Append(isGetter ? KGVL.KEYWORD_GET : KGVL.KEYWORD_SET).Append(KGVL.SEMICOLON)
            .Append(SPACE).Append(KGVL.CLOSE_CURLY_BRACKET);
    }

    private static void AppendMethodGenericParameters(StringBuilder builder, int count)
    {
        if (count == 0)
        {
            return;
        }
        builder.Append(KGVL.GENERIC_TYPE_START)
            .Append(string.Join(SEPARATOR, Enumerable.Range(0, count)
                .Select(position => METHOD_GENERIC_PARAMETER_PREFIX + position)))
            .Append(KGVL.GENERIC_TYPE_END);
    }

    private static string GetModifierPrefix(FunctionParameterModifier modifier)
    {
        string Keyword = modifier switch
        {
            FunctionParameterModifier.In => KGVL.KEYWORD_IN,
            FunctionParameterModifier.Out => KGVL.KEYWORD_OUT,
            FunctionParameterModifier.Ref => KGVL.KEYWORD_REF,
            FunctionParameterModifier.Params => KGVL.KEYWORD_PARAMS,
            _ => string.Empty
        };
        return Keyword.Length == 0 ? Keyword : Keyword + SPACE;
    }
}
