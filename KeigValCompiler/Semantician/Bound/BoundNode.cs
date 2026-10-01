using KeigValCompiler.Semantician.Member.Code;

namespace KeigValCompiler.Semantician.Bound;

/* A node of the bound tree, which resolving a function body builds from its parse tree, leaving that as it
 * was written. In it every name is already what it names, and every conversion is written out; later stages
 * and the backend see only this tree. Each node keeps the statement or value of the parse tree it was made
 * from, whose line messages about it point at, but for a function's whole body, which the parse tree holds as
 * a run of statements rather than as a statement.
 *
 * A node has errors when a mistake in it, or in a node below it, has been reported, or when binding it is not
 * written yet. Nothing more is reported about such a node, so that one mistake makes one message. */
internal abstract class BoundNode
{
    // Internal fields.
    internal Statement? Syntax { get; private init; }

    /* Every node directly below this one, in the order they run. Abstract rather than empty by default, so
     * that a new kind of node which forgets its children does not compile. */
    internal abstract IEnumerable<BoundNode> Children { get; }

    internal bool HasErrors => _hasErrors ??= IsErroneous || Children.Any(child => child.HasErrors);


    // Protected fields.
    /* Whether a mistake in this node itself has been reported, or binding it is not written yet. */
    protected virtual bool IsErroneous => false;


    // Private fields.
    private bool? _hasErrors = null;


    // Constructors.
    internal BoundNode(Statement? syntax)
    {
        Syntax = syntax;
    }
}
