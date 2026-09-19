namespace KeigValCompiler.Semantician.Member.Code;

/* Either "yield return someValue;" or "yield break;". */
internal class YieldStatement : Statement
{
    // Fields.
    /* Null exactly when IsBreak is true, since "yield break" produces no value. */
    internal Statement? Value { get; set; }
    internal bool IsBreak { get; set; } = false;


    // Constructors.
    internal YieldStatement(Statement value)
    {
        Value = value ?? throw new ArgumentNullException(nameof(value));
        IsBreak = false;
    }

    internal YieldStatement()
    {
        Value = null;
        IsBreak = true;
    }


    // Inherited fields.
    internal override IEnumerable<Statement> Children =>
        Value == null ? Array.Empty<Statement>() : new Statement[] { Value };


    // Inherited methods.
    internal override void TransformChildren(Func<Statement, Statement> transform)
    {
        if (Value != null)
        {
            Value = transform(Value);
        }
    }
}
