namespace KeigValCompiler.Semantician.Member.Code;

/* One arm of a switch expression. Unlike a SwitchCase, an arm produces a single value rather than
 * running a body of statements. */
internal class SwitchExpressionArm
{
    // Fields.
    /* Null marks the discard arm, which matches anything that no earlier arm matched. */
    internal Statement? Pattern { get; set; }
    internal Statement? WhenCondition { get; set; } = null;
    internal Statement Value { get; set; }


    // Constructors.
    internal SwitchExpressionArm(Statement? pattern, Statement value)
    {
        Pattern = pattern;
        Value = value ?? throw new ArgumentNullException(nameof(value));
    }
}
