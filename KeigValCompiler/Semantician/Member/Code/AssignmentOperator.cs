namespace KeigValCompiler.Semantician.Member.Code;

internal enum AssignmentOperator
{
    Assign,

    AddAssign,
    SubtractAssign,
    MultiplyAssign,
    DivideAssign,
    ModuloAssign,

    BitwiseAndAssign,
    BitwiseOrAssign,
    BitwiseXorAssign,

    LeftShiftAssign,
    RightShiftAssign,
    UnsignedRightShiftAssign,

    NullCoalesceAssign
}
