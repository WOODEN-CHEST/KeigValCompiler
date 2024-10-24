using System.Collections;
using System.Linq;

namespace KeigValCompiler.Semantician.Member;

internal class IdentifiableCollection<T> : IEnumerable<T> where T : IIdentifiable
{
    // Internal fields.
    internal T? this[Identifier identifier] =>
        _items.Where(item => item.SelfIdentifier.Equals(identifier)).FirstOrDefault();

    internal T? this[int index] => _items[index];

    internal int Count => _items.Count;


    // Private fields.
    private readonly List<T> _items = new();


    // Methods.
    public void AddItem(T item)
    {
        ArgumentNullException.ThrowIfNull(item, nameof(item));
        _items.Add(item);
    }

    public void RemoveItem(T item)
    {
        ArgumentNullException.ThrowIfNull(item, nameof(item));
        _items.Remove(item);
    }

    public void ClearItems()
    {
        _items.Clear();
    }


    // Inherited methods.
    public IEnumerator<T> GetEnumerator()
    {
        foreach (T Item in _items)
        {
            yield return Item;
        }
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    public override bool Equals(object? obj)
    {
        if (obj is not IdentifiableCollection<T> TypeCollection)
        {
            return false;
        }

        return this.ToArray().SequenceEqual(TypeCollection.ToArray());
    }

    public override int GetHashCode()
    {
        return this.ToArray().Select(param => param.GetHashCode()).Sum();
    }
}