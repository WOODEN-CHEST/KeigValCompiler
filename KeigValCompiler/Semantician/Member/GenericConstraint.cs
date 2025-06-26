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


    // Inherited methods.
    public override bool Equals(object? obj)
    {
        if (obj is not GenericConstraint Constraint)
        {
            return false;
        }
        return Constraint.ConstrainedItemName?.Equals(Constraint) 
            ?? (Constraint.SpecialConstraint == SpecialConstraint);
    }

    public override int GetHashCode()
    {
        return ConstrainedItemName?.GetHashCode() ?? SpecialConstraint.GetHashCode();
    }

    public override string ToString()
    {
        return ConstrainedItemName?.ToString() ?? SpecialConstraint.ToString();
    }
}