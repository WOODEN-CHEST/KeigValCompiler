namespace KeigValCompiler.Semantician.Library.Bindings;

/* KGVL.Array<T>, the type behind T[]. Making one is the language's "new T[length]", not a member. */
internal class ArrayBindings : ILibraryBindingProvider
{
    // Static fields.
    private const string NAME_LENGTH = "Length";


    // Inherited methods.
    public void AddBindings(LibraryBindingTable table)
    {
        ArgumentNullException.ThrowIfNull(table, nameof(table));
        SignatureBuilder Member = new(LibraryTypes.Array);
        SignatureType Element = Member.TypeParameter(0);

        table.AddIntrinsic(Member.PropertyGetter(NAME_LENGTH, LibraryTypes.Int32), IntrinsicOperation.Length);
        table.AddIntrinsic(Member.IndexerGetter(Element, LibraryTypes.Int32), IntrinsicOperation.GetElement);
        table.AddIntrinsic(Member.IndexerSetter(Element, LibraryTypes.Int32), IntrinsicOperation.SetElement);
    }
}
