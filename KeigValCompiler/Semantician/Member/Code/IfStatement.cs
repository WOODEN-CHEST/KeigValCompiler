using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KeigValCompiler.Semantician.Member.Code;

internal class IfStatement : Statement
{
    // Internal fields.
    internal Statement Condition { get; private init; }
    internal StatementCollection IfBody { get; } = new();
    internal StatementCollection ElseBody { get; } = new();


    // Constructors.
    internal IfStatement(Statement condition)
    {
        Condition = condition ?? throw new ArgumentNullException(nameof(condition));
    }
}