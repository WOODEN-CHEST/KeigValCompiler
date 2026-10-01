using KeigValCompiler.Semantician.Member.Code;
using KeigValCompiler.Semantician.Types;

namespace KeigValCompiler.Semantician.Bound;

/* A type named before a '.', through which its static members and nested types are reached. It is no value
 * itself. */
internal sealed class BoundTypeExpression : BoundExpression
{
    // Internal fields.
    internal override IEnumerable<BoundNode> Children => Array.Empty<BoundNode>();


    // Constructors.
    internal BoundTypeExpression(Statement syntax, SemanticType type)
        : base(syntax, type ?? throw new ArgumentNullException(nameof(type)), null) { }
}
