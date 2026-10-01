namespace KeigValCompiler.Semantician.Member.Code;

/* A braced run of statements standing as a statement of its own, as in "{ int a = 1; }", whose locals
 * belong to it alone, as in C#. */
internal class BlockStatement : Statement
{
    // Fields.
    internal StatementCollection Body { get; } = new();


    // Inherited fields.
    internal override IEnumerable<Statement> Children => Body;


    // Inherited methods.
    internal override void TransformChildren(Func<Statement, Statement> transform)
    {
        Body.TransformAll(transform);
    }
}
