namespace KeigValCompiler.Semantician.Member.Code;

internal abstract class FunctionCallStatement : Statement
{
    // Internal fields.
    internal IEnumerable<FunctionArgument> Arguments => _arguments;
    internal int ArgumentCount => _arguments.Count;


    // Private fields.
    private readonly List<FunctionArgument> _arguments = new();


    // Constructors.
    internal FunctionCallStatement(params Statement[]? arguments)
    {
        if (arguments != null)
        {
            _arguments.AddRange(arguments.Select(argument => new FunctionArgument(argument)));
        }
    }


    // Methods.
    public FunctionArgument GetArgument(int index)
    {
        return _arguments[index];
    }

    public void AddArgument(FunctionArgument argument)
    {
        _arguments.Add(argument ?? throw new ArgumentNullException(nameof(argument)));
    }

    public void AddArgument(Statement argument)
    {
        AddArgument(new FunctionArgument(argument ?? throw new ArgumentNullException(nameof(argument))));
    }

    public void RemoveArgument(FunctionArgument argument)
    {
        _arguments.Remove(argument ?? throw new ArgumentNullException(nameof(argument)));
    }

    public void RemoveArgumentAt(int index)
    {
        _arguments.RemoveAt(index);
    }

    public void InsertArgument(int index, FunctionArgument argument)
    {
        _arguments.Insert(index, argument ?? throw new ArgumentNullException(nameof(argument)));
    }

    public void ClearArguments()
    {
        _arguments.Clear();
    }


    // Inherited fields.
    internal override IEnumerable<Statement> Children => _arguments.Select(argument => argument.Value);


    // Inherited methods.
    internal override void TransformChildren(Func<Statement, Statement> transform)
    {
        foreach (FunctionArgument Argument in _arguments)
        {
            Argument.Value = transform(Argument.Value);
        }
    }
}
