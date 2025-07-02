using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KeigValCompiler.Semantician.Member.Code;

internal class SwitchCase
{
    // Fields.
    internal Statement CaseCondition { get; set; }
    internal StatementCollection Body { get; } = new();
    internal bool IsBroken = false;


    // Constructors.
    internal SwitchCase(Statement condition)
    {
        CaseCondition = condition;
    }
}