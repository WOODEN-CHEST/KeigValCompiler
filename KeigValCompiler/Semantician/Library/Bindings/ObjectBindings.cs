namespace KeigValCompiler.Semantician.Library.Bindings;

/* KGVL.Object: what every object can do before a type overrides it. */
internal class ObjectBindings : ILibraryBindingProvider
{
    // Static fields.
    private const string NAME_REFERENCE_EQUALS = "ReferenceEquals";


    // Inherited methods.
    public void AddBindings(LibraryBindingTable table)
    {
        ArgumentNullException.ThrowIfNull(table, nameof(table));
        SignatureBuilder Member = new(LibraryTypes.Object);

        table.AddIntrinsic(Member.StaticMethod(NAME_REFERENCE_EQUALS, LibraryTypes.Boolean,
            LibraryTypes.Object, LibraryTypes.Object), IntrinsicOperation.SameReference);
        table.AddIntrinsic(Member.Method(LibraryMemberNames.EQUALS, LibraryTypes.Boolean, LibraryTypes.Object),
            IntrinsicOperation.ObjectEquals);
        table.AddIntrinsic(Member.Method(LibraryMemberNames.GET_HASH_CODE, LibraryTypes.Int32),
            IntrinsicOperation.HashCode);
        table.AddIntrinsic(Member.Method(LibraryMemberNames.TO_STRING, LibraryTypes.String),
            IntrinsicOperation.ToText);
    }
}
