using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KeigValCompiler.Semantician.Member.Code;

internal class SwitchStatement : Statement
{
    // Fields.
    internal IEnumerable<SwitchCase> Cases => _cases;
    internal int CaseCount => _cases.Count;


    // Private fields.
    private readonly List<SwitchCase> _cases = new();


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
}