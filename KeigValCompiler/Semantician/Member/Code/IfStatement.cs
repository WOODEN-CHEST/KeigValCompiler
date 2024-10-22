using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KeigValCompiler.Semantician.Member.Code;

internal class IfStatement : Statement
{
    // Internal fields.
    internal Statement Condition { get; set; }
    internal StatementCollection IfBody { get; set; }
    internal StatementCollection ElseBody { get; set; }
}