using System.Collections;

namespace KeigValCompiler.Semantician.Member.Code;

internal class StatementCollection : IEnumerable<Statement>
{
    // Fields.
    internal int Count => _statements.Count;
    internal Statement this[int index] => _statements[index];

    // Private fields.
    private readonly List<Statement> _statements = new();


    // Constructors.
    internal StatementCollection() { }


    // Methods.
    public void AddStatement(Statement statement)
    {
        _statements.Add(statement);
    }

    public void AddStatements(StatementCollection collection)
    {
        _statements.AddRange(collection);
    }

    public void InsertStatement(Statement statement, int index)
    {
        _statements.Insert(index, statement);
    }

    public void RemoveStatement(Statement statement)
    {
        _statements.Remove(statement);
    }

    public void RemoveStatementAt(int index)
    {
        _statements.RemoveAt(index);
    }

    public void ClearStataements()
    {
        _statements.Clear();
    }

    public void SetFrom(IEnumerable<Statement> body)
    {
        ClearStataements();
        _statements.AddRange(body);
    }

    public IEnumerator<Statement> GetEnumerator()
    {
        return _statements.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}