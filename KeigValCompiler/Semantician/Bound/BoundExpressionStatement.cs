using KeigValCompiler.Semantician.Member.Code;

namespace KeigValCompiler.Semantician.Bound;

/* A value worked out for what doing so does, such as an assignment or a call, its result unused. */
internal sealed class BoundExpressionStatement : BoundStatement
{
    // Internal fields.
    internal BoundExpression Expression { get; private init; }
    internal override IEnumerable<BoundNode> Children => new BoundNode[] { Expression };


    // Constructors.
    internal BoundExpressionStatement(Statement syntax, BoundExpression expression) : base(syntax)
    {
        Expression = expression ?? throw new ArgumentNullException(nameof(expression));
    }
}
