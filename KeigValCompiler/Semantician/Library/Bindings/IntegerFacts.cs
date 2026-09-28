namespace KeigValCompiler.Semantician.Library.Bindings;

/* What IntegerBindings needs to know about one integer type to bind its members. */
internal sealed record IntegerFacts(LibraryType Type, bool IsSigned, int BitCount);
