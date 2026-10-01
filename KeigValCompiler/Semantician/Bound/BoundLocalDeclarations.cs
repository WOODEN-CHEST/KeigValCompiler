using KeigValCompiler.Semantician.Member.Code;

namespace KeigValCompiler.Semantician.Bound;

/* Every local one declaration statement declares, as "int a = 1, b = 2;" declares two, in order. */
internal sealed class BoundLocalDeclarations : BoundStatement
{
    // Internal fields.
    internal IReadOnlyList<BoundLocalDeclaration> Declarations { get; private init; }
    internal override IEnumerable<BoundNode> Children => Declarations;


    // Constructors.
    internal BoundLocalDeclarations(Statement syntax, IReadOnlyList<BoundLocalDeclaration> declarations)
        : base(syntax)
    {
        Declarations = declarations ?? throw new ArgumentNullException(nameof(declarations));
    }
}
