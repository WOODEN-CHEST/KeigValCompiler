using KeigValCompiler.Semantician.Member;

namespace KeigValCompiler.Semantician.Library.Bindings;

/* Which operation each binary operator is, in the groups the binding files use. An operator is the
 * same operation on every type declaring it; the operand types tell the backend which kind. */
internal static class OperatorIntrinsics
{
    // Static fields.
    internal static IReadOnlyList<OperatorIntrinsic> Arithmetic { get; } = new OperatorIntrinsic[]
    {
        new(OverloadableOperator.Addition, IntrinsicOperation.Add),
        new(OverloadableOperator.Subtraction, IntrinsicOperation.Subtract),
        new(OverloadableOperator.Multiplication, IntrinsicOperation.Multiply),
        new(OverloadableOperator.Division, IntrinsicOperation.Divide),
        new(OverloadableOperator.Modulo, IntrinsicOperation.Remainder)
    };

    internal static IReadOnlyList<OperatorIntrinsic> Bitwise { get; } = new OperatorIntrinsic[]
    {
        new(OverloadableOperator.BitwiseAnd, IntrinsicOperation.And),
        new(OverloadableOperator.BitwiseOr, IntrinsicOperation.Or),
        new(OverloadableOperator.BitwiseXor, IntrinsicOperation.Xor)
    };

    internal static IReadOnlyList<OperatorIntrinsic> Shifts { get; } = new OperatorIntrinsic[]
    {
        new(OverloadableOperator.LeftShift, IntrinsicOperation.ShiftLeft),
        new(OverloadableOperator.RightShift, IntrinsicOperation.ShiftRight),
        new(OverloadableOperator.UnsignedRightShift, IntrinsicOperation.ShiftRightUnsigned)
    };

    internal static IReadOnlyList<OperatorIntrinsic> Equality { get; } = new OperatorIntrinsic[]
    {
        new(OverloadableOperator.Equals, IntrinsicOperation.Equal),
        new(OverloadableOperator.NotEquals, IntrinsicOperation.NotEqual)
    };

    internal static IReadOnlyList<OperatorIntrinsic> Ordering { get; } = new OperatorIntrinsic[]
    {
        new(OverloadableOperator.LessThan, IntrinsicOperation.LessThan),
        new(OverloadableOperator.LargerThan, IntrinsicOperation.GreaterThan),
        new(OverloadableOperator.LessThanOrEqual, IntrinsicOperation.LessThanOrEqual),
        new(OverloadableOperator.LargerOrEqual, IntrinsicOperation.GreaterThanOrEqual)
    };
}
