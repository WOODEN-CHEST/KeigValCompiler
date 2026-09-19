using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KeigValCompiler.Semantician.Member.Code;

internal class WhileStatement : Statement
{
    // Internal fields.
    internal Statement Condition { get; set; }
    internal StatementCollection Body { get; } = new();
    internal bool IsPairedWithDoStatement { get; set; } = false;


    // Constructors.
    internal WhileStatement(Statement condition)
    {
        Condition = condition;
    }


    // Inherited fields.
    internal override IEnumerable<Statement> Children => new Statement[] { Condition }.Concat(Body);


    // Inherited methods.
    internal override void TransformChildren(Func<Statement, Statement> transform)
    {
        Condition = transform(Condition);
        Body.TransformAll(transform);
    }
}
