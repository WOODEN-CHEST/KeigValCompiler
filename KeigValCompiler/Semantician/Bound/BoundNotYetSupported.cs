using KeigValCompiler.Semantician.Member.Code;
using KeigValCompiler.Semantician.Types;

namespace KeigValCompiler.Semantician.Bound;

/* A value of a kind the compiler cannot bind yet, kept as written. It has the error type, so that nothing
 * around it is reported, which also means it is never treated as valid. It exists only while function bodies
 * are being built up, and goes once every kind of value and statement is bound. */
internal sealed class BoundNotYetSupported : BoundExpression
{
    // Internal fields.
    internal override IEnumerable<BoundNode> Children => Array.Empty<BoundNode>();


    // Constructors.
    internal BoundNotYetSupported(Statement syntax) : base(syntax, ErrorType.Instance, null) { }
}
