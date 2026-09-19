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
}