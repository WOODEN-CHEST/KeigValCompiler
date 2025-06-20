using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KeigValCompiler.Semantician.Member.Code;

internal abstract class ConditionalStatement : Statement
{
    // Internal fields.
    internal override IEnumerable<Statement> SubStatements => IfBody.Concat(ElseBody).Append(Condition);
    internal Statement Condition { get; set; }
    internal StatementCollection IfBody { get; } = new();
    internal StatementCollection ElseBody { get; } = new();


    // Constructors.
    internal ConditionalStatement(Statement condition)
    {
        Condition = condition ?? throw new ArgumentNullException(nameof(condition));
    }
}