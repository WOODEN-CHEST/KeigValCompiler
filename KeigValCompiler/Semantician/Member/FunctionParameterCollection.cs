namespace KeigValCompiler.Semantician.Member;

internal class FunctionParameterCollection : IdentifiableCollection<FunctionParameter>
{
    // Methods.
    public void SetFrom(FunctionParameterCollection parameters)
    {
        ClearItems();
        foreach (FunctionParameter Parameter in parameters)
        {
            AddItem(Parameter);
        }
    }


    // Inherited methods.
    public override string ToString()
    {
        return string.Join(", ", this);
    }
}