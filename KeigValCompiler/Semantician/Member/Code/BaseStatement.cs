namespace KeigValCompiler.Semantician.Member.Code;

/* Access to the inherited type's members via the "base" keyword. */
internal class BaseStatement : Statement
{
    // Inherited fields.
    internal override IEnumerable<Statement> Children => Array.Empty<Statement>();
}
