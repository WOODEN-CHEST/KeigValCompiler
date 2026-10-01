using KeigValCompiler.Semantician.Member.Code;
using KeigValCompiler.Semantician.Types;

namespace KeigValCompiler.Semantician.Bound;

/* A value given to a variable, a field or a property with '='. The value has been converted to the target's
 * type, which is also the type of the assignment, as in C#. */
internal sealed class BoundAssignment : BoundExpression
{
    // Internal fields.
    internal BoundExpression Target { get; private init; }
    internal BoundExpression Value { get; private init; }
    internal override IEnumerable<BoundNode> Children => new BoundNode[] { Target, Value };


    // Constructors.
    internal BoundAssignment(Statement syntax, BoundExpression target, BoundExpression value, SemanticType type)
        : base(syntax, type ?? throw new ArgumentNullException(nameof(type)), null)
    {
        Target = target ?? throw new ArgumentNullException(nameof(target));
        Value = value ?? throw new ArgumentNullException(nameof(value));
    }
}
