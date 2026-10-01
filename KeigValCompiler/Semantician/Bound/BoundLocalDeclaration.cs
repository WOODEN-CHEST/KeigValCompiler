using KeigValCompiler.Semantician.Member.Code;

namespace KeigValCompiler.Semantician.Bound;

/* One local declared, with the value it starts with, converted to its type, if it is given one. */
internal sealed class BoundLocalDeclaration : BoundStatement
{
    // Internal fields.
    internal LocalSymbol Local { get; private init; }
    internal BoundExpression? Initializer { get; private init; }
    internal override IEnumerable<BoundNode> Children =>
        (Initializer == null) ? Array.Empty<BoundNode>() : new BoundNode[] { Initializer };


    // Constructors.
    internal BoundLocalDeclaration(Statement syntax, LocalSymbol local, BoundExpression? initializer)
        : base(syntax)
    {
        Local = local ?? throw new ArgumentNullException(nameof(local));
        Initializer = initializer;
    }
}
