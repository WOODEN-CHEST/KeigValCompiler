namespace KeigValCompiler.Semantician.Member;

internal enum Operator
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
    ExplicitCast,

    Indexer,

    /* Non-overloadable */
}
