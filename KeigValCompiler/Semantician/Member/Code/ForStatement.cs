using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KeigValCompiler.Semantician.Member.Code;

internal class ForStatement : Statement
{
    // Internal fields.
    internal VariableAssignmentStatement Assignment { get; set; }
    internal Statement Condition { get; set; }
    internal VariableAssignmentStatement Increment { get; set; }
    internal StatementCollection Body { get; set; }
}