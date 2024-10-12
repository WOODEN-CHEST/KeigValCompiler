namespace KeigValCompiler.Semantician.Member;

internal class GenericConstraint
{
    // Fields.
    internal Identifier ContrainedItemName { get; private init; }


    // Constructors.
    internal GenericConstraint(Identifier constraintIdentifier)
    {
        ContrainedItemName = constraintIdentifier ?? throw new ArgumentNullException(nameof(constraintIdentifier));
    }


    // Inherited methods.
    public override bool Equals(object? obj)
    {
        if (obj is not GenericConstraint Constraint)
        {
            return false;
        }
        return Constraint.ContrainedItemName.Equals(Constraint);
    }

    public override int GetHashCode()
    {
        return ContrainedItemName.GetHashCode();
    }

    public override string ToString()
    {
        return ContrainedItemName.ToString();
    }
}