namespace KeigValCompiler.Semantician.Library;

/* What a builtin member does, which is what the backend will switch on to emit it. One operation
 * serves every type it applies to, and the bound signature says which: Add is the same entry for
 * all integer widths and for decimal, and Convert for every conversion, its source and target
 * taken from the signature. */
internal enum IntrinsicOperation
{
    /* Arithmetic. */
    Add,
    Subtract,
    Multiply,
    Divide,
    Remainder,
    Plus,
    Negate,
    Increment,
    Decrement,

    /* Logic and bits. */
    Not,
    And,
    Or,
    Xor,
    Complement,
    ShiftLeft,
    ShiftRight,
    ShiftRightUnsigned,

    /* Comparison. */
    Equal,
    NotEqual,
    LessThan,
    GreaterThan,
    LessThanOrEqual,
    GreaterThanOrEqual,
    Compare,

    /* Conversion and text. */
    Convert,
    Parse,
    TryParse,
    ToText,

    /* What every object can do. */
    SameReference,
    ObjectEquals,
    HashCode,

    /* Decimal rounding. */
    Floor,
    Ceiling,
    Truncate,
    Round,
    RoundToDecimals,

    /* Decimal approximations, each with or without an iteration count, which the signature tells. */
    Sqrt,
    Cbrt,
    RootN,
    Pow,
    Exp,
    Exp10,
    Log,
    LogBase,
    Log2,
    Log10,
    Sin,
    Cos,
    Tan,
    Asin,
    Acos,
    Atan,
    Atan2,
    Sinh,
    Cosh,
    Tanh,

    /* Strings and arrays. */
    NewRepeatedString,
    NewStringFromChars,
    Length,
    GetElement,
    SetElement,
    Concat,
    Substring,
    IndexOf,

    /* Nullable values. */
    NewNullable,
    HasValue,
    GetValue
}
