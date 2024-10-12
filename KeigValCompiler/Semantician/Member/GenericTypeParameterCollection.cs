namespace KeigValCompiler.Semantician.Member;

internal class GenericTypeParameterCollection : IdentifiableCollection<GenericTypeParameter>
{
    // Constructors.
    internal GenericTypeParameterCollection(GenericTypeParameter[] parameters) : base(parameters) { }


    // Inherited methods.
    public override bool Equals(object? obj)
    {
        if (obj is not GenericTypeParameterCollection TypeCollection)
        {
            return false;
        }

        return this.ToArray().SequenceEqual(TypeCollection.ToArray());
    }

    public override int GetHashCode()
    {
        return this.ToArray().Select(param => param.GetHashCode()).Sum();
    }

    public override string ToString()
    {
        return $"Generic Params: {{{string.Join(", ", (IEnumerable<GenericTypeParameter>)this.ToArray())}}}";
    }
}