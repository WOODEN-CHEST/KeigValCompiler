using System.Collections;

namespace KeigValCompiler.Semantician.Member;

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