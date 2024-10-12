namespace KeigValCompiler.Semantician.Member;

internal class FunctionParameterCollection : IdentifiableCollection<FunctionParameter>
{
    // Private fields.
    private readonly List<FunctionParameter> _paramaters = new();


    // Constructors.

    internal FunctionParameterCollection(params FunctionParameter[] paramaters) : base(paramaters) { }


    // Inherited methods.
    public override bool Equals(object? obj)
    {
        if (obj is not FunctionParameterCollection ParamCollection)
        {
            return false;
        }

        return _paramaters.SequenceEqual(ParamCollection._paramaters);
    }

    public override int GetHashCode()
    {
        return _paramaters.Select(param => param.GetHashCode()).Sum();
    }

    public override string ToString()
    {
        return string.Join(", ", _paramaters);
    }
}