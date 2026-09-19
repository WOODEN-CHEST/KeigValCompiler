namespace KeigValCompiler.Semantician.Member.Code;

/* An assignment to an already existing target, as opposed to a declaration of a new variable,
 * which is a VariableDeclarationStatement instead. The target is any statement rather than a plain
 * identifier so that member accesses ("obj.Field = 5") and index accesses ("array[i] = 5") can be
 * assigned to, not just simple variable names. */
internal class AssignmentStatement : Statement
{
    // Fields.
    internal Statement Target { get; set; }
    internal AssignmentOperator Operator { get; set; }
    internal Statement Value { get; set; }


    // Constructors.
    internal AssignmentStatement(Statement target, AssignmentOperator targetOperator, Statement value)
    {
        Target = target ?? throw new ArgumentNullException(nameof(target));
        Operator = targetOperator;
        Value = value ?? throw new ArgumentNullException(nameof(value));
    }


    // Inherited fields.
    internal override IEnumerable<Statement> Children => new Statement[] { Target, Value };


    // Inherited methods.
    internal override void TransformChildren(Func<Statement, Statement> transform)
    {
        Target = transform(Target);
        Value = transform(Value);
    }
}
