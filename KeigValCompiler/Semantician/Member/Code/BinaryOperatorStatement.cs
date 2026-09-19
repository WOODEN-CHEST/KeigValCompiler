namespace KeigValCompiler.Semantician.Member.Code;

/* An operator with two operands, such as "a + b" or "left is Right". */
internal class BinaryOperatorStatement : Statement
{
    // Fields.
    internal StatementOperator Operator { get; set; }
    internal Statement Left { get; set; }
    internal Statement Right { get; set; }


    // Constructors.
    internal BinaryOperatorStatement(StatementOperator targetOperator, Statement left, Statement right)
    {
        Operator = targetOperator;
        Left = left ?? throw new ArgumentNullException(nameof(left));
        Right = right ?? throw new ArgumentNullException(nameof(right));
    }


    // Inherited fields.
    internal override IEnumerable<Statement> Children => new Statement[] { Left, Right };


    // Inherited methods.
    internal override void TransformChildren(Func<Statement, Statement> transform)
    {
        Left = transform(Left);
        Right = transform(Right);
    }
}
