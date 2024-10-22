namespace KeigValCompiler.Semantician.Member;

internal class FunctionParameterCollection : IdentifiableCollection<FunctionParameter>
{
    // Inherited methods.
    public override string ToString()
    {
        return string.Join(", ", this);
    }
}