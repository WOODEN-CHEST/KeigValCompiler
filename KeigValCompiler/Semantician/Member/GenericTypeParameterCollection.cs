namespace KeigValCompiler.Semantician.Member;

internal class GenericTypeParameterCollection : IdentifiableCollection<GenericTypeParameter>
{
    // Inherited methods.
    public override string ToString()
    {
        return $"Generic Params: {{{string.Join(", ", (IEnumerable<GenericTypeParameter>)this.ToArray())}}}";
    }
}