using KeigValCompiler.Semantician.Member.Code;

namespace KeigValCompiler.Semantician.Bound;

/* A local variable or constant, named. A constant's value is the expression's. */
internal sealed class BoundLocal : BoundExpression
{
    // Internal fields.
    internal LocalSymbol Local { get; private init; }
    internal override IEnumerable<BoundNode> Children => Array.Empty<BoundNode>();


    // Constructors.
    internal BoundLocal(Statement syntax, LocalSymbol local)
        : base(syntax, local?.Type, local?.ConstantValue)
    {
        Local = local ?? throw new ArgumentNullException(nameof(local));
    }
}
