namespace KeigValCompiler.Semantician.Member;

internal class GenericTypeParameter : IIdentifiable
{
    // Fields.
    public Identifier SelfIdentifier { get; }
    public GenericConstraint[] Constraints => _constraints.ToArray();


    // Private fields.
    private readonly GenericConstraint[] _constraints;


    // Constructors.
    internal GenericTypeParameter(Identifier identifier, GenericConstraint[] constraints)
    {
        SelfIdentifier = identifier ?? throw new ArgumentNullException(nameof(identifier));
        _constraints = constraints?.ToArray() ?? throw new ArgumentNullException(nameof(constraints));
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