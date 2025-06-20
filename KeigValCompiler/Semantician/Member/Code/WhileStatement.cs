using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KeigValCompiler.Semantician.Member.Code;

internal class WhileStatement : Statement
{
    // Internal fields.
    internal override IEnumerable<Statement> SubStatements => Condition == null ? Body : Body.Append(Condition);
    internal Statement? Condition { get; set; }
    internal StatementCollection Body { get; } = new();


    // Constructors.
    internal WhileStatement(Statement? condition)
    {
        Condition = condition;
    }
}