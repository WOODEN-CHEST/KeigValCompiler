using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KeigValCompiler.Semantician.Member.Code;

internal class CompositeAccessStatement : Statement
{
    // Fields.
    internal IEnumerable<Statement> Components => _statements;


    // Private fields.
    private readonly List<Statement> _statements = new();


    // Methods.
    internal void AddStatement(Statement statement)
    {
        _statements.Add(statement);
    }

    internal void RemoveStatement(Statement statement)
    {
        _statements.Remove(statement);
    }

    internal void RemoveStatementAt(int index)
    {
        _statements.RemoveAt(index);
    }

    internal void InsertStatementAt(Statement statement, int index)
    {
        _statements.Insert(index, statement);
    }

    internal void ClearStatements()
    {
        _statements.Clear();
    }
}