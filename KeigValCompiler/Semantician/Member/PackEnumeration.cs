using System.Collections;

namespace KeigValCompiler.Semantician.Member;

internal class PackEnumeration : PackMember, IEnumerable<KeyValuePair<string, int>>, IPackType
{
    // Fields.
    public IEnumerable<PackFunction> Constructors => Enumerable.Empty<PackFunction>();


    // Internal fields.
    internal string[] Names => _values.Keys.ToArray();
    internal int[] Values => _values.Values.Distinct().ToArray();
    internal int this[string name] => _values[name];


    // Private fields.
    private readonly Dictionary<string, int> _values = new();


    // Constructors.
    public PackEnumeration(Identifier identifier, PackSourceFile sourceFile) : base(identifier, sourceFile) { }


    // Inherited methods.
    public IEnumerator<KeyValuePair<string, int>> GetEnumerator()
    {
        return _values.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}