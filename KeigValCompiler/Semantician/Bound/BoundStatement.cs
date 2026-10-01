using KeigValCompiler.Semantician.Member.Code;

namespace KeigValCompiler.Semantician.Bound;

/* A bound statement: something a body does, as opposed to a value. */
internal abstract class BoundStatement : BoundNode
{
    // Constructors.
    internal BoundStatement(Statement? syntax) : base(syntax) { }
}
