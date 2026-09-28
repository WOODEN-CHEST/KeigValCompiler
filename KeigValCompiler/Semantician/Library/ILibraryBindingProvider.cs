namespace KeigValCompiler.Semantician.Library;

/* Binds the builtin members of one family of library types, such as all eight integer types. */
internal interface ILibraryBindingProvider
{
    // Methods.
    void AddBindings(LibraryBindingTable table);
}
