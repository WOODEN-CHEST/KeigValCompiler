namespace KeigValCompiler.Semantician.Member.Code;

/* An inline function such as "x => x + 1" or "(a, b) => { return a + b; }". An expression bodied
 * lambda holds its single expression as the only statement in Body, with IsExpressionBodied set so
 * that the implicit return is not lost. */
internal class LambdaStatement : Statement
{
    // Fields.
    internal FunctionParameterCollection Parameters { get; } = new();
    internal StatementCollection Body { get; } = new();

    /* Null until inference determines it; lambdas never declare a return type in source. */
    internal TypeTargetIdentifier? ReturnType { get; set; } = null;

    internal bool IsExpressionBodied { get; set; } = false;


    // Inherited fields.
    internal override IEnumerable<Statement> Children => Body;


    // Inherited methods.
    internal override void TransformChildren(Func<Statement, Statement> transform)
    {
        Body.TransformAll(transform);
    }
}
