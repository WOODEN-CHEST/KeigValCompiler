using KeigValCompiler.Semantician.Member.Code;
using KeigValCompiler.Semantician.Types;

namespace KeigValCompiler.Semantician.Bound;

/* A type's default value, as "default(T)" or "default" gives it. Written alone, it has no type until it is
 * converted to one. For a numeric type, bool, char, string or any other reference type it is a constant, as
 * in C#: zero, false, the char zero, or null. */
internal sealed class BoundDefault : BoundExpression
{
    // Internal fields.
    internal override IEnumerable<BoundNode> Children => Array.Empty<BoundNode>();


    // Constructors.
    internal BoundDefault(Statement syntax, SemanticType? type, ConstantValue? constantValue)
        : base(syntax, type, constantValue) { }
}
