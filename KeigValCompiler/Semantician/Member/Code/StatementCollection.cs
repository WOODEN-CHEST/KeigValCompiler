using System.Collections;

namespace KeigValCompiler.Semantician.Member.Code;

internal class StatementCollection : IEnumerable<Statement>
{
    // Fields.
    public int Count => _statements.Count;
    public Statement this[int index] => _statements[index];



    // Private fields.
    private readonly List<Statement> _statements;


    // Constructors.
    internal StatementCollection() { }


    // Methods.
    public void AddStatement(Statement statement)
    {
        _statements.Add(statement);
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