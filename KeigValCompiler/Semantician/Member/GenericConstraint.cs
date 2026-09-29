namespace KeigValCompiler.Semantician.Member;

internal class GenericConstraint
{
    // Fields.
    internal TypeTargetIdentifier? ConstrainedItemName { get; set; }
    internal SpecialGenericConstraint SpecialConstraint { get; set; } = SpecialGenericConstraint.None;


    // Constructors.
    internal GenericConstraint(TypeTargetIdentifier constrainedItemName)
    {
        ConstrainedItemName = constrainedItemName ?? throw new ArgumentNullException(nameof(constrainedItemName));
    }

    internal GenericConstraint(SpecialGenericConstraint constraint)
    {
        SpecialConstraint = constraint;
    }


    // Private methods.
    /* The constraining type as its resolved names spell it, or as source code does before resolution,
     * since TypeTargetIdentifier itself only compares by reference. Null for a special constraint. */
    private string? GetTypeName()
    {
        return ConstrainedItemName?.Format(identifier => identifier.ResolvedName ?? identifier.SourceCodeName);
    }


    // Inherited methods.
    public override bool Equals(object? obj)
    {
        if (obj is not GenericConstraint Constraint)
        {
            return false;
        }
        return (SpecialConstraint == Constraint.SpecialConstraint) && (GetTypeName() == Constraint.GetTypeName());
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(SpecialConstraint, GetTypeName());
    }

    public override string ToString()
    {
        return ConstrainedItemName?.ToString() ?? SpecialConstraint.ToString();
    }
}