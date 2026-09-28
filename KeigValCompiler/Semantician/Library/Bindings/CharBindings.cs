using KeigValCompiler.Semantician.Member;

namespace KeigValCompiler.Semantician.Library.Bindings;

/* KGVL.Char. Its conversions are in NumericConversionBindings, and its other arithmetic is int's. */
internal class CharBindings : ILibraryBindingProvider
{
    // Inherited methods.
    public void AddBindings(LibraryBindingTable table)
    {
        ArgumentNullException.ThrowIfNull(table, nameof(table));
        SignatureBuilder Member = new(LibraryTypes.Char);

        table.AddIntrinsic(Member.UnaryOperator(OverloadableOperator.Increment), IntrinsicOperation.Increment);
        table.AddIntrinsic(Member.UnaryOperator(OverloadableOperator.Decrement), IntrinsicOperation.Decrement);
        table.AddIntrinsic(Member.Method(LibraryMemberNames.TO_STRING, LibraryTypes.String),
            IntrinsicOperation.ToText);
    }
}
