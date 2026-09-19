namespace KeigValCompiler.Semantician.Member.Code;

/* Access to the enclosing instance via the "this" keyword. */
internal class ThisStatement : Statement
{
    // Inherited fields.
    internal override IEnumerable<Statement> Children => Array.Empty<Statement>();
}
