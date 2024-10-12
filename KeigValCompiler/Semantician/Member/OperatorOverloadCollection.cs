using System.Collections;

namespace KeigValCompiler.Semantician.Member;

internal class OperatorOverloadCollection : IEnumerable<OperatorOverload>
{
    // Internal fields.
    internal OperatorOverload[] this[Operator targetOperator]
    {
        get
        {
            _overloads.TryGetValue(targetOperator, out OperatorOverload[]? overloads);
            return overloads ?? Array.Empty<OperatorOverload>();
        }
    }


    // Private fields.
    private readonly Dictionary<Operator, OperatorOverload[]> _overloads = new();


    // Constructors.
    public OperatorOverloadCollection(OperatorOverload[] items)
    {
        ArgumentNullException.ThrowIfNull(items, nameof(items));

        foreach (Operator TargetOperator in Enum.GetValues(typeof(Operator)))
        {
            _overloads[TargetOperator] = items.Where(item => item.OverloadedOperator == TargetOperator).ToArray();
        }
    }


    // Inherited methods.
    public IEnumerator<OperatorOverload> GetEnumerator()
    {
        foreach (OperatorOverload Overload in _overloads.Values.SelectMany(value => value))
        {
            yield return Overload;
        }
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    public override bool Equals(object? obj)
    {
        if (obj is not OperatorOverloadCollection OverloadCollection)
        {
            return false;
        }

        return this.ToArray().SequenceEqual(OverloadCollection.ToArray());
    }

    public override int GetHashCode()
    {
        return this.ToArray().Select(overload => overload.GetHashCode()).Sum();
    }
}