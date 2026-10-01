using KeigValCompiler.Semantician.Member.Code;
using KeigValCompiler.Semantician.Types;

namespace KeigValCompiler.Semantician.Bound;

/* A literal: a number, a char, a string, true or false, or null, which has no type of its own. */
internal sealed class BoundLiteral : BoundExpression
{
    // Internal fields.
    internal override IEnumerable<BoundNode> Children => Array.Empty<BoundNode>();


    // Constructors.
    internal BoundLiteral(Statement syntax, SemanticType? type, ConstantValue constantValue)
        : base(syntax, type, constantValue ?? throw new ArgumentNullException(nameof(constantValue))) { }
}
