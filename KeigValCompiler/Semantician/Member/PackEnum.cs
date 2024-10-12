using System.Collections;

namespace KeigValCompiler.Semantician.Member;

internal class PackEnum : PackMember, IEnumerable<KeyValuePair<string, int>>
{
    // Fields.
    public Identifier SelfIdentifier => throw new NotImplementedException();


    // Internal fields.
    internal string[] Names => _values.Keys.ToArray();
    internal int[] Values => _values.Values.Distinct().ToArray();
    internal int this[string name] => _values[name];


    // Private fields.
    private readonly Dictionary<string, int> _values = new();


    // Constructors.
    public PackEnum(Identifier identifier,
        PackMemberModifiers modifiers,
        PackSourceFile sourceFile,
        PackNameSpace nameSpace,
        Identifier parentItem) : base(identifier, modifiers, sourceFile, nameSpace, parentItem) { }


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