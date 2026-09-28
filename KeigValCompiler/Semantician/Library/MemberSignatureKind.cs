namespace KeigValCompiler.Semantician.Library;

/* What kind of member a signature is of. A property's or an indexer's accessors are separate here,
 * since each is an operation of its own. Conversions are operators, told apart by their
 * OverloadableOperator. */
internal enum MemberSignatureKind
{
    Method,
    Constructor,
    Operator,
    PropertyGetter,
    PropertySetter,
    IndexerGetter,
    IndexerSetter
}
