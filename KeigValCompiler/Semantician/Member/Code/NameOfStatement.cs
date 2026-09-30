namespace KeigValCompiler.Semantician.Member.Code;

/* The "nameof" operator, which resolves at compile time to the source code name of what it names. That is a
 * name, or a chain of member accesses ending in one, as in "nameof(Thing.Count)" or "nameof(this.Count)",
 * whose parts may have type arguments, as in "nameof(List<int>.Count)". Its text is the last name's. */
internal class NameOfStatement : Statement
{
    // Fields.
    /* An IdentifiableAccessStatement for a name, or a CompositeAccessStatement for a chain, which may start
     * with a ThisStatement or a BaseStatement. One of those alone names nothing, which the parser reports. */
    internal Statement Target { get; set; }


    // Constructors.
    internal NameOfStatement(Statement target)
    {
        Target = target ?? throw new ArgumentNullException(nameof(target));
    }


    // Inherited fields.
    internal override IEnumerable<Statement> Children => new Statement[] { Target };


    // Inherited methods.
    internal override void TransformChildren(Func<Statement, Statement> transform)
    {
        Target = transform(Target);
    }
}
