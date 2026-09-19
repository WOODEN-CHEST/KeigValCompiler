namespace KeigValCompiler.Semantician.Member.Code;

/* An operator with a single operand, such as "!flag", "-number" or "index++". */
internal class UnaryOperatorStatement : Statement
{
    // Fields.
    internal StatementOperator Operator { get; set; }
    internal Statement Operand { get; set; }

    /* Distinguishes "++index" from "index++", which differ in the value the statement returns.
     * Always true for operators which can only be written before their operand, such as '!'. */
    internal bool IsPrefix { get; set; } = true;


    // Constructors.
    internal UnaryOperatorStatement(StatementOperator targetOperator, Statement operand, bool isPrefix)
    {
        Operator = targetOperator;
        Operand = operand ?? throw new ArgumentNullException(nameof(operand));
        IsPrefix = isPrefix;
    }


    // Inherited fields.
    internal override IEnumerable<Statement> Children => new Statement[] { Operand };


    // Inherited methods.
    internal override void TransformChildren(Func<Statement, Statement> transform)
    {
        Operand = transform(Operand);
    }
}
