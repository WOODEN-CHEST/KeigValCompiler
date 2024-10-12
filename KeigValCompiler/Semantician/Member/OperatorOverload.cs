namespace KeigValCompiler.Semantician.Member;

internal class OperatorOverload
{
    // Internal fields.
    internal Operator OverloadedOperator { get; private init; }
    internal PackFunction Function { get; private init; }


    // Constructors.
    internal OperatorOverload(Operator operatorType, PackFunction function)
    {
        OverloadedOperator = operatorType;
        Function = function ?? throw new ArgumentNullException(nameof(function));
    }


    // Inherited methods.
    public override bool Equals(object? obj)
    {
        if (obj is not OperatorOverload Overload)
        {
            return false;
        }

        return OverloadedOperator == Overload.OverloadedOperator && Function.Parameters.Equals(Overload.Function.Parameters);
    }

    public override int GetHashCode()
    {
        return OverloadedOperator.GetHashCode() + Function.GetHashCode();
    }
}