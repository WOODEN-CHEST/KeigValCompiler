using KeigValCompiler.Semantician.Member.Code;

namespace KeigValCompiler.Semantician.Bound;

/* A statement of a kind the compiler cannot bind yet, kept as written. Nothing in it is looked at, and it
 * counts as having errors, so that it is never treated as valid. It exists only while function bodies are
 * being built up, and goes once every kind of statement is bound. */
internal sealed class BoundNotYetSupportedStatement : BoundStatement
{
    // Internal fields.
    internal override IEnumerable<BoundNode> Children => Array.Empty<BoundNode>();


    // Protected fields.
    protected override bool IsErroneous => true;


    // Constructors.
    internal BoundNotYetSupportedStatement(Statement syntax) : base(syntax) { }
}
