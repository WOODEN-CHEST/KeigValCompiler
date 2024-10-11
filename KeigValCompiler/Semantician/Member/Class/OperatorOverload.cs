using KeigValCompiler.Semantician.Member.Function;

namespace KeigValCompiler.Semantician.Member.Class;

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
        if (obj == null || !(obj is OperatorOverload))
        {
            return false;
        }

        OperatorOverload Overload = (OperatorOverload)obj;
        return OverloadedOperator == Overload.OverloadedOperator && Function.Parameters.Equals(Overload.Function.Parameters);
    }

    public override int GetHashCode()
    {
        return OverloadedOperator.GetHashCode() + Function.GetHashCode();
    }
}