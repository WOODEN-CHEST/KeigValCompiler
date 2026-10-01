using KeigValCompiler.Semantician.Member;
using KeigValCompiler.Semantician.Member.Code;
using KeigValCompiler.Semantician.Types;

namespace KeigValCompiler.Semantician.Bound;

/* A parameter of the function the body belongs to, named: one it declares, an indexer's in its accessors, or
 * the "value" a setter is given. */
internal sealed class BoundParameter : BoundExpression
{
    // Internal fields.
    internal FunctionParameter Parameter { get; private init; }
    internal override IEnumerable<BoundNode> Children => Array.Empty<BoundNode>();


    // Constructors.
    internal BoundParameter(Statement syntax, FunctionParameter parameter, SemanticType type)
        : base(syntax, type ?? throw new ArgumentNullException(nameof(type)), null)
    {
        Parameter = parameter ?? throw new ArgumentNullException(nameof(parameter));
    }
}
