namespace KeigValCompiler.Semantician.Member.Code;

internal abstract class Statement
{
    // Fields.
    internal virtual TypeTargetIdentifier? StatementReturnType { get; set; } = null;
    internal virtual SourceFileOrigin Origin { get; set; }

    /* Every statement directly held by this one, in the order they appear in source, including the
     * contents of statement bodies. Abstract rather than defaulting to empty so that adding a new
     * statement type without declaring its children is a compile error rather than a traversal
     * that silently skips it. */
    internal abstract IEnumerable<Statement> Children { get; }


    // Methods.
    /* Replaces each direct child with the result of running it through the given function. Each
     * statement reassigns its own fields, which keeps the rewrite typed and means callers never
     * depend on the order children happen to be enumerated in. Statements with no children need
     * no override. */
    internal virtual void TransformChildren(Func<Statement, Statement> transform) { }

    /* Every statement in this subtree, this one included, in depth first source order. */
    internal IEnumerable<Statement> EnumerateSelfAndDescendants()
    {
        yield return this;

        foreach (Statement Child in Children)
        {
            foreach (Statement Descendant in Child.EnumerateSelfAndDescendants())
            {
                yield return Descendant;
            }
        }
    }
}
