namespace KeigValCompiler.Semantician.Member;

/* What kind of operator each overloadable operator is, the one table of them, which the checks of
 * operators and the naming of them share. "+" and "-" are unary or binary as the parser decides from their
 * parameters, so Addition and Subtraction are binary and UnaryPlus and Negation unary. */
internal static class OverloadableOperatorKinds
{
    // Internal static methods.
    internal static bool IsUnary(OverloadableOperator overloadedOperator)
    {
        return overloadedOperator is OverloadableOperator.UnaryPlus or OverloadableOperator.Negation
            or OverloadableOperator.LogicalNot or OverloadableOperator.BitwiseComplement
            or OverloadableOperator.Increment or OverloadableOperator.Decrement;
    }

    internal static bool IsShift(OverloadableOperator overloadedOperator)
    {
        return overloadedOperator is OverloadableOperator.LeftShift or OverloadableOperator.RightShift
            or OverloadableOperator.UnsignedRightShift;
    }

    internal static bool IsConversion(OverloadableOperator overloadedOperator)
    {
        return overloadedOperator is OverloadableOperator.ImplicitCast or OverloadableOperator.ExplicitCast;
    }

    /* How many parameters an operator takes: one for a unary operator or a conversion, two for the rest. */
    internal static int GetOperandCount(OverloadableOperator overloadedOperator)
    {
        return (IsUnary(overloadedOperator) || IsConversion(overloadedOperator)) ? 1 : 2;
    }
}
