using KeigValCompiler.Semantician.Library.Bindings;

namespace KeigValCompiler.Semantician.Library;

/* The bindings for the whole standard library. A new binding file is added to the list here. */
internal static class DefaultLibraryBindings
{
    // Internal static methods.
    internal static LibraryBindingTable CreateTable()
    {
        ILibraryBindingProvider[] Providers = new ILibraryBindingProvider[]
        {
            new ObjectBindings(),
            new BooleanBindings(),
            new CharBindings(),
            new IntegerBindings(),
            new DecimalBindings(),
            new NumericConversionBindings(),
            new StringBindings(),
            new ArrayBindings(),
            new NullableBindings()
        };

        LibraryBindingTable Table = new();
        foreach (ILibraryBindingProvider Provider in Providers)
        {
            Provider.AddBindings(Table);
        }
        return Table;
    }
}
