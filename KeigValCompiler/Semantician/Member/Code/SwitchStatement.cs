using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KeigValCompiler.Semantician.Member.Code;

internal class SwitchStatement : Statement
{
    // Fields.
    /* The value being switched over, as in the "x" of "switch (x) { ... }". */
    internal Statement Target { get; set; }
    internal IEnumerable<SwitchCase> Cases => _cases;
    internal int CaseCount => _cases.Count;
    internal StatementCollection DefaultCase { get; } = new();


    // Private fields.
    private readonly List<SwitchCase> _cases = new();


    // Constructors.
    internal SwitchStatement(Statement target)
    {
        Target = target ?? throw new ArgumentNullException(nameof(target));
    }


    // Methods.
    public void AddCase(SwitchCase switchCase)
    {
        _cases.Add(switchCase);
    }

    public void RemoveCase(SwitchCase switchCase)
    {
        _cases.Remove(switchCase);
    }

    public void RemoveCaseAt(int index)
    {
        _cases.RemoveAt(index);
    }

    public void InsertCaseAt(int index, SwitchCase switchCase)
    {
        _cases.Insert(index, switchCase);
    }

    public void ClearCases()
    {
        _cases.Clear();
    }


    // Inherited fields.
    internal override IEnumerable<Statement> Children => new Statement[] { Target }
        .Concat(_cases.SelectMany(switchCase => switchCase.CaseConditions.Concat(switchCase.Body)))
        .Concat(DefaultCase);


    // Inherited methods.
    internal override void TransformChildren(Func<Statement, Statement> transform)
    {
        Target = transform(Target);
        foreach (SwitchCase Case in _cases)
        {
            Case.CaseConditions.TransformAll(transform);
            Case.Body.TransformAll(transform);
        }
        DefaultCase.TransformAll(transform);
    }
}
