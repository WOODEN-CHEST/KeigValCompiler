using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KeigValCompiler.Semantician.Member.Code;

internal abstract class FunctionCallStatement : Statement
{
    // Internal fields.
    internal override IEnumerable<Statement> SubStatements => Arguments;
    internal IEnumerable<Statement> Arguments => _arguments;


    // Private fields.
    private readonly List<Statement> _arguments = new();


    // Constructors.
    internal FunctionCallStatement(params Statement[]? arguments)
    {
        if (arguments != null)
        {
            _arguments.AddRange(arguments);
        }
    }


    // Methods.
    public Statement GetArgument(int index)
    {
        return _arguments[index];
    }

    public void AddArgument(Statement argument)
    {
        _arguments.Add(argument ?? throw new ArgumentNullException(nameof(argument)));
    }

    public void RemoveArgument(Statement argument)
    {
        _arguments.Remove(argument ?? throw new ArgumentNullException(nameof(argument)));
    }

    public void AddArgumentAt(int index)
    {
        _arguments.RemoveAt(index);
    }

    public void InsertArgument(int index, Statement argument)
    {
        _arguments.Insert(index, argument ?? throw new ArgumentNullException(nameof(argument)));
    }

    public void ClearArguments()
    {
        _arguments.Clear();
    }
}