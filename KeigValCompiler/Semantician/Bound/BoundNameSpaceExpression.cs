using KeigValCompiler.Semantician.Member.Code;

namespace KeigValCompiler.Semantician.Bound;

/* A namespace named before a '.', through which what it holds is reached. It is no value, and has no type. */
internal sealed class BoundNameSpaceExpression : BoundExpression
{
    // Internal fields.
    /* The namespace's full name. */
    internal string Name { get; private init; }
    internal override IEnumerable<BoundNode> Children => Array.Empty<BoundNode>();


    // Constructors.
    internal BoundNameSpaceExpression(Statement syntax, string name) : base(syntax, null, null)
    {
        Name = name ?? throw new ArgumentNullException(nameof(name));
    }
}
