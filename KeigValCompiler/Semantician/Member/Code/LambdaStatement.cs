namespace KeigValCompiler.Semantician.Member.Code;

internal class LambdaStatement : Statement
{
    // Fields.
    internal FunctionParameterCollection Parameters { get; } = new();
    internal StatementCollection Body { get; } = new();
}