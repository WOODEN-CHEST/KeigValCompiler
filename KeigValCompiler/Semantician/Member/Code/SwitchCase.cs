using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KeigValCompiler.Semantician.Member.Code;

internal class SwitchCase
{
    // Fields.
    internal StatementCollection CaseConditions { get; } = new();
    internal StatementCollection Body { get; } = new();
    internal bool IsBrokenOutOf = false;
}