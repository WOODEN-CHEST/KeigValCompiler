using System.Collections;

namespace KeigValCompiler.Semantician.Member;

internal class OperatorOverloadCollection : IEnumerable<OperatorOverload>
{
    // Internal fields.
    internal IEnumerable<OperatorOverload> this[OverloadableOperator targetOperator]
    {
        get
        {
            _overloads.TryGetValue(targetOperator, out List<OperatorOverload>? overloads);
            return overloads ?? Enumerable.Empty<OperatorOverload>();
        }
    }


    // Private fields.
    private readonly Dictionary<OverloadableOperator, List<OperatorOverload>> _overloads = new();


    // Constructors.
    public OperatorOverloadCollection()
    {
        foreach (OverloadableOperator TargetOperator in Enum.GetValues(typeof(OverloadableOperator)))
        {
            _overloads.Add(TargetOperator, new());
        }
    }


    // Methods.
    public void AddOverload(OperatorOverload overload)
    {
        ArgumentNullException.ThrowIfNull(overload, nameof(overload));
        _overloads[overload.OverloadedOperator].Add(overload);
    }

    public void RemoveOverload(OperatorOverload overload)
    {
        ArgumentNullException.ThrowIfNull(overload, nameof(overload));
        _overloads[overload.OverloadedOperator].Remove(overload);
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