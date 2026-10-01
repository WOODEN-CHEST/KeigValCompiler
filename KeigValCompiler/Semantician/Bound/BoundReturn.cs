using KeigValCompiler.Semantician.Member.Code;

namespace KeigValCompiler.Semantician.Bound;

/* Leaving the function, with the value it returns, converted to its return type, if it returns one. */
internal sealed class BoundReturn : BoundStatement
{
    // Internal fields.
    internal BoundExpression? Value { get; private init; }
    internal override IEnumerable<BoundNode> Children =>
        (Value == null) ? Array.Empty<BoundNode>() : new BoundNode[] { Value };


    // Constructors.
    internal BoundReturn(Statement syntax, BoundExpression? value) : base(syntax)
    {
        Value = value;
    }
}
