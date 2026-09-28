namespace KeigValCompiler.Semantician.Member;

internal enum OverloadableOperator
{
    /* Overloadable. */
    Addition,
    Subtraction,
    Multiplication,
    Division,
    Modulo,

    UnaryPlus,
    Negation,
    LogicalNot,
    BitwiseComplement,

    Increment,
    Decrement,

    BitwiseAnd,
    BitwiseOr,
    BitwiseXor,
    LeftShift,
    RightShift,
    UnsignedRightShift,

    Equals,
    NotEquals,
    LargerThan,
    LessThan,
    LargerOrEqual,
    LessThanOrEqual,

    ImplicitCast,
    ExplicitCast
}
