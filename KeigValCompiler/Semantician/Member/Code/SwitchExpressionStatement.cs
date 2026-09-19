namespace KeigValCompiler.Semantician.Member.Code;

/* A switch used as an expression, as in "x switch { 1 => "one", _ => "other" }", which produces a
 * value rather than running statement bodies like a SwitchStatement does. */
internal class SwitchExpressionStatement : Statement
{
    // Fields.
    internal Statement Target { get; set; }
    internal IEnumerable<SwitchExpressionArm> Arms => _arms;
    internal int ArmCount => _arms.Count;


    // Private fields.
    private readonly List<SwitchExpressionArm> _arms = new();


    // Constructors.
    internal SwitchExpressionStatement(Statement target)
    {
        Target = target ?? throw new ArgumentNullException(nameof(target));
    }


    // Methods.
    internal void AddArm(SwitchExpressionArm arm)
    {
        _arms.Add(arm ?? throw new ArgumentNullException(nameof(arm)));
    }

    internal void ClearArms()
    {
        _arms.Clear();
    }


    // Inherited fields.
    internal override IEnumerable<Statement> Children => new Statement[] { Target }
        .Concat(_arms.SelectMany(arm => new Statement?[] { arm.Pattern, arm.WhenCondition, arm.Value }
            .Where(statement => statement != null).Select(statement => statement!)));


    // Inherited methods.
    internal override void TransformChildren(Func<Statement, Statement> transform)
    {
        Target = transform(Target);
        foreach (SwitchExpressionArm Arm in _arms)
        {
            if (Arm.Pattern != null)
            {
                Arm.Pattern = transform(Arm.Pattern);
            }
            if (Arm.WhenCondition != null)
            {
                Arm.WhenCondition = transform(Arm.WhenCondition);
            }
            Arm.Value = transform(Arm.Value);
        }
    }
}
