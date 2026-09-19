using System.Collections;

namespace KeigValCompiler.Semantician.Member.Code;

internal class StatementCollection : IEnumerable<Statement>
{
    // Fields.
    internal int Count => _statements.Count;
    internal Statement this[int index] => _statements[index];
    internal bool IsEmpty => (Count <= 0) || _statements.All(statement => statement is EmptyStatement);

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

    public void ClearStatements()
    {
        _statements.Clear();
    }

    public void SetFrom(IEnumerable<Statement> body)
    {
        ClearStatements();
        _statements.AddRange(body);
    }

    /* Replaces every statement in the collection with the result of running it through the given
     * function, in place. Used by Statement.TransformChildren for bodies. */
    public void TransformAll(Func<Statement, Statement> transform)
    {
        ArgumentNullException.ThrowIfNull(transform, nameof(transform));

        for (int i = 0; i < _statements.Count; i++)
        {
            _statements[i] = transform(_statements[i]);
        }
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