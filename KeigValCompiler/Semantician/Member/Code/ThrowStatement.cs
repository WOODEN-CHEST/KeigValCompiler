namespace KeigValCompiler.Semantician.Member.Code;

internal class ThrowStatement : Statement
{
    // Fields.
    internal Statement StatementToThrow { get; set; }


    // Constructors.
    internal ThrowStatement(Statement statementToThrow)
    {
        StatementToThrow = statementToThrow ?? throw new ArgumentNullException(nameof(statementToThrow));
    }


    // Inherited fields.
    internal override IEnumerable<Statement> Children => new Statement[] { StatementToThrow };


    // Inherited methods.
    internal override void TransformChildren(Func<Statement, Statement> transform)
    {
        StatementToThrow = transform(StatementToThrow);
    }
}
