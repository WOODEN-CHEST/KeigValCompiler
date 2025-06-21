namespace KeigValCompiler.Semantician.Member;

internal enum OverloadableOperator
{
    /* Overloadable. */
    Addition,
    Subtraction,
    Multiplication,
    Division,
    Modulo,

    Negation,

    Increment,
    Decrement,

    Equals,
    NotEquals,
    LargerThan,
    LessThan,
    LargerOrEqual,
    LessThanOrEqual,

    ImplicitCast,
    ExplicitCast
}
