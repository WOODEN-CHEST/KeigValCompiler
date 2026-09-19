namespace KeigValCompiler.Semantician.Member.Code;

/* The "nameof" operator, which resolves to the source code name of its target at compile time. */
internal class NameOfStatement : Statement
{
    // Fields.
    internal Identifier Target { get; set; }


    // Constructors.
    internal NameOfStatement(Identifier target)
    {
        Target = target ?? throw new ArgumentNullException(nameof(target));
    }


    // Inherited fields.
    internal override IEnumerable<Statement> Children => Array.Empty<Statement>();
}
