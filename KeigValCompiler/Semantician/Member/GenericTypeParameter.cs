namespace KeigValCompiler.Semantician.Member;

internal class GenericTypeParameter : IIdentifiable
{
    // Fields.
    public Identifier SelfIdentifier { get; set; }
    public GenericConstraint[] Constraints { get; set; }


    // Constructors.
    internal GenericTypeParameter(Identifier identifier, GenericConstraint[]? constraints)
    {
        SelfIdentifier = identifier;
        Constraints = constraints?.ToArray() ?? Array.Empty<GenericConstraint>();
    }


    // Inherited methods.
    public override bool Equals(object? obj)
    {
        if (obj is not GenericTypeParameter GenericParameter)
        {
            return false;
        }
        return SelfIdentifier.Equals(GenericParameter.SelfIdentifier)
            && Constraints.SequenceEqual(GenericParameter.Constraints);
    }

    public override int GetHashCode()
    {
        return SelfIdentifier.GetHashCode() * Constraints.Select(constraint => constraint.GetHashCode()).Sum();
    }

    public override string ToString()
    {
        return $"{SelfIdentifier} : {string.Join(", ", (IEnumerable<GenericConstraint>)Constraints)}";
    }
}