namespace KeigValCompiler.Semantician.Bound;

/* The value of an expression known at compile time, as C# computes constants: an integer, a char, a bool, a
 * string, a decimal or null. An integer is held whole, whatever its type, which is its expression's, so that
 * whether it fits another integer type can be asked of it; a char is held as its UTF-16 code unit. A decimal
 * is the TwoIntDecimal the runtime computes with, so that a constant is what running the code would give. */
internal sealed class ConstantValue
{
    // Static fields.
    internal static ConstantValue Null { get; } = new() { Kind = ConstantValueKind.Null };
    internal static ConstantValue True { get; } = new() { Kind = ConstantValueKind.Boolean, Boolean = true };
    internal static ConstantValue False { get; } = new() { Kind = ConstantValueKind.Boolean, Boolean = false };


    // Internal fields.
    internal ConstantValueKind Kind { get; private init; }

    /* An integer's value, or a char's code unit. */
    internal Int128 Integer { get; private init; }
    internal bool Boolean { get; private init; }
    internal string? Text { get; private init; }
    internal TwoIntDecimal Decimal { get; private init; }


    // Constructors.
    private ConstantValue() { }


    // Internal static methods.
    internal static ConstantValue FromInteger(Int128 value)
    {
        return new() { Kind = ConstantValueKind.Integer, Integer = value };
    }

    internal static ConstantValue FromChar(char value)
    {
        return new() { Kind = ConstantValueKind.Char, Integer = value };
    }

    internal static ConstantValue FromBoolean(bool value)
    {
        return value ? True : False;
    }

    internal static ConstantValue FromString(string value)
    {
        return new()
        {
            Kind = ConstantValueKind.String,
            Text = value ?? throw new ArgumentNullException(nameof(value))
        };
    }

    internal static ConstantValue FromDecimal(TwoIntDecimal value)
    {
        return new() { Kind = ConstantValueKind.Decimal, Decimal = value };
    }


    // Inherited methods.
    /* The value as messages show it, as C# writes the constant. */
    public override string ToString()
    {
        return Kind switch
        {
            ConstantValueKind.Integer => Integer.ToString(),
            ConstantValueKind.Char => ((char)Integer).ToString(),
            ConstantValueKind.Boolean => Boolean ? KGVL.KEYWORD_TRUE : KGVL.KEYWORD_FALSE,
            ConstantValueKind.String => Text!,
            ConstantValueKind.Decimal => Decimal.ToString(),
            _ => KGVL.KEYWORD_NULL
        };
    }
}
