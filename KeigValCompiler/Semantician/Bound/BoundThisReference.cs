using KeigValCompiler.Semantician.Member.Code;
using KeigValCompiler.Semantician.Types;

namespace KeigValCompiler.Semantician.Bound;

/* The object a member runs on: "this" as written, or as a member named on its own reaches it. */
internal sealed class BoundThisReference : BoundExpression
{
    // Internal fields.
    /* Not written, but what a member of the object named on its own is reached through. */
    internal bool IsImplicit { get; private init; }
    internal override IEnumerable<BoundNode> Children => Array.Empty<BoundNode>();


    // Constructors.
    internal BoundThisReference(Statement syntax, DeclaredType type, bool isImplicit)
        : base(syntax, type ?? throw new ArgumentNullException(nameof(type)), null)
    {
        IsImplicit = isImplicit;
    }
}
