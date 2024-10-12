using System.Collections;

namespace KeigValCompiler.Semantician.Member;

internal class IdentifiableCollection<T> : IEnumerable<T> where T : IIdentifiable
{
    // Internal fields.
    internal T? this[Identifier identifier] =>
        _items.Where(item => item.SelfIdentifier.Equals(identifier)).FirstOrDefault();

    internal int Count => _items.Length;


    // Private fields.
    private readonly T[] _items;


    // Constructors.
    public IdentifiableCollection(T[] items)
    {
        _items = items?.ToArray() ?? throw new ArgumentNullException(nameof(items));
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
}