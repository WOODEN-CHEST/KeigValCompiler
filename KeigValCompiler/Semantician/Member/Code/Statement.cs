namespace KeigValCompiler.Semantician.Member.Code;

internal abstract class Statement
{
    // Fields.
    internal virtual TypeTargetIdentifier? StatementReturnType { get; set; } = null;
    internal virtual SourceFileOrigin Origin { get; set; }
}