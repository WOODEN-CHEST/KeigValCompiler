namespace KeigValCompiler.Semantician.Library.Bindings;

/* KGVL.Nullable<T>, the type behind T? for a value type. Operators on nullable values are the
 * language's lifting of T's own, so none are here. */
internal class NullableBindings : ILibraryBindingProvider
{
    // Static fields.
    private const string NAME_HAS_VALUE = "HasValue";
    private const string NAME_VALUE = "Value";


    // Inherited methods.
    public void AddBindings(LibraryBindingTable table)
    {
        ArgumentNullException.ThrowIfNull(table, nameof(table));
        SignatureBuilder Member = new(LibraryTypes.Nullable);
        SignatureType Value = Member.TypeParameter(0);

        table.AddIntrinsic(Member.Constructor(Value), IntrinsicOperation.NewNullable);
        table.AddIntrinsic(Member.PropertyGetter(NAME_HAS_VALUE, LibraryTypes.Boolean),
            IntrinsicOperation.HasValue);
        table.AddIntrinsic(Member.PropertyGetter(NAME_VALUE, Value), IntrinsicOperation.GetValue);

        table.AddIntrinsic(Member.Conversion(true, Value, Member.Self), IntrinsicOperation.Convert);
        table.AddIntrinsic(Member.Conversion(false, Member.Self, Value), IntrinsicOperation.Convert);
    }
}
