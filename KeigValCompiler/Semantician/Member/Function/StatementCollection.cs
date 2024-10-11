using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KeigValCompiler.Semantician.Member.Function;

internal class StatementCollection : IEnumerable<Statement>
{
    // Fields.
    public int Count => _statements.Length;
    public Statement this[int index] => _statements[index]; 



    // Private fields.
    private readonly Statement[] _statements;


    // Constructors.
    internal StatementCollection(Statement[] statements)
    {
        _statements = statements?.ToArray() ?? throw new ArgumentNullException(nameof(statements));
    }


    // Inherited methods.
    public IEnumerator<Statement> GetEnumerator()
    {
        foreach (Statement TargetStatement in _statements)
        {
            yield return TargetStatement;
        }
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}