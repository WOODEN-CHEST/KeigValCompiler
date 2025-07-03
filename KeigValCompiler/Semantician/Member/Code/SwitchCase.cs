using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KeigValCompiler.Semantician.Member.Code;

internal class SwitchCase
{
    // Fields.
    internal IEnumerable<Statement> CaseConditions => _caseConditions;
    internal int CaseConditionCount => _caseConditions.Count;
    internal StatementCollection Body { get; } = new();
    internal bool IsBroken = false;


    // Private fields.
    private readonly List<Statement> _caseConditions = new();


    // Methods.
    public void AddCondition(Statement condition)
    {
        _caseConditions.Add(condition);
    }

    public void RemoveCondition(Statement condition)
    {
        _caseConditions.Remove(condition);
    }

    public void RemoveConditionAt(int index)
    {
        _caseConditions.RemoveAt(index);
    }

    public void InsertConditionAt(int index, Statement condition)
    {
        _caseConditions.Insert(index, condition);
    }

    public void ClearCondition()
    {
        _caseConditions.Clear();
    }
}