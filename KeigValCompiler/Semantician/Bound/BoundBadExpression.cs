using KeigValCompiler.Semantician.Member.Code;
using KeigValCompiler.Semantician.Types;

namespace KeigValCompiler.Semantician.Bound;

/* A value which could not be bound because of a mistake which has been reported, with whatever of it could
 * be bound below it. It has the error type, so that nothing using it is reported. */
internal sealed class BoundBadExpression : BoundExpression
{
    // Internal fields.
    internal IReadOnlyList<BoundExpression> BoundParts { get; private init; }
    internal override IEnumerable<BoundNode> Children => BoundParts;


    // Constructors.
    internal BoundBadExpression(Statement syntax, params BoundExpression[] boundParts)
        : base(syntax, ErrorType.Instance, null)
    {
        BoundParts = boundParts ?? throw new ArgumentNullException(nameof(boundParts));
    }
}
