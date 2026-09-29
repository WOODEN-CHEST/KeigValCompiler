namespace KeigValCompiler.Semantician.Resolver;

/* What kind of member a DeclaredSignature describes, which decides what it can be compared with. */
internal enum DeclaredSignatureKind
{
    Method,
    Constructor,
    Property,
    Indexer,
    Event,
    Field,
    Operator,
    Conversion
}
