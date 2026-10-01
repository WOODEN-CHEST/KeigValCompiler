using KeigValCompiler.Semantician.Member.Code;

namespace KeigValCompiler.Semantician.Bound;

/* A run of statements with the locals declared directly in it, which belong to it alone: a function's body,
 * which has no statement of its own to keep, a block written on its own, or the body of a statement such as
 * "if". */
internal sealed class BoundBlock : BoundStatement
{
    // Internal fields.
    internal IReadOnlyList<LocalSymbol> Locals { get; private init; }
    internal IReadOnlyList<BoundStatement> Statements { get; private init; }
    internal override IEnumerable<BoundNode> Children => Statements;


    // Constructors.
    internal BoundBlock(Statement? syntax, IReadOnlyList<LocalSymbol> locals,
        IReadOnlyList<BoundStatement> statements) : base(syntax)
    {
        Locals = locals ?? throw new ArgumentNullException(nameof(locals));
        Statements = statements ?? throw new ArgumentNullException(nameof(statements));
    }
}
