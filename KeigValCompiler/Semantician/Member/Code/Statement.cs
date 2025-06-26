namespace KeigValCompiler.Semantician.Member.Code;

internal abstract class Statement
{
    // Fields.
    internal virtual IEnumerable<Statement> SubStatements => Enumerable.Empty<Statement>();
    internal virtual TypeTargetIdentifier? StatementReturnType { get; set; }
    internal virtual SourceFileOrigin Origin { get; set; }
}