using KeigValCompiler.Semantician.Member.Code;
using KeigValCompiler.Semantician.Types;

namespace KeigValCompiler.Semantician.Bound;

/* "base" before a '.', the object a member runs on as its base class sees it, whose members are reached
 * without overriding taking part. Its type is the base class. */
internal sealed class BoundBaseReference : BoundExpression
{
    // Internal fields.
    internal override IEnumerable<BoundNode> Children => Array.Empty<BoundNode>();


    // Constructors.
    internal BoundBaseReference(Statement syntax, DeclaredType type)
        : base(syntax, type ?? throw new ArgumentNullException(nameof(type)), null) { }
}
