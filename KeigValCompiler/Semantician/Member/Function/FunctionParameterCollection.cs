namespace KeigValCompiler.Semantician.Member.Function;

internal class FunctionParameterCollection
{
    // Internal static fields.
    internal static FunctionParameterCollection Empty { get; } = new();


    // Internal fields.
    internal FunctionParameter[] Parameters => _paramaters.ToArray();
    internal int Count => _paramaters.Count;


    // Private fields.
    private readonly List<FunctionParameter> _paramaters = new();


    // Constructors.
    internal FunctionParameterCollection() { }

    internal FunctionParameterCollection(params FunctionParameter[] paramaters)
    {
        ArgumentNullException.ThrowIfNull(paramaters, nameof(paramaters));
        _paramaters.AddRange(paramaters);
    }


    // Internal methods.
    internal FunctionParameter? GetParameter(Identifier identifier)
    {
        return _paramaters.Where(param => param.SelfIdentifier.Equals(identifier)).FirstOrDefault();
    }


    // Inherited methods.
    public override bool Equals(object? obj)
    {
        if (obj is not FunctionParameterCollection ParamCollection)
        {
            return false;
        }

        return ParamCollection.Parameters.SequenceEqual(Parameters);
    }

    public override int GetHashCode()
    {
        return Parameters.Select(param => param.GetHashCode()).Sum();
    }

    public override string ToString()
    {
        return string.Join<FunctionParameter>(", ", Parameters);
    }
}