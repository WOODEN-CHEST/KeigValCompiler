using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KeigValCompiler.Semantician.Member.Code;

internal abstract class ConditionalStatement : Statement
{
    // Internal fields.
    internal Statement Condition { get; set; }
    internal StatementCollection IfBody { get; } = new();
    /* Null means the statement has no else branch at all, which is distinct from an empty one. */
    internal StatementCollection? ElseBody { get; set; } = null;


    // Constructors.
    internal ConditionalStatement(Statement condition)
    {
        Condition = condition ?? throw new ArgumentNullException(nameof(condition));
    }


    // Inherited fields.
    internal override IEnumerable<Statement> Children => new Statement[] { Condition }
        .Concat(IfBody).Concat(ElseBody ?? Enumerable.Empty<Statement>());


    // Inherited methods.
    internal override void TransformChildren(Func<Statement, Statement> transform)
    {
        Condition = transform(Condition);
        IfBody.TransformAll(transform);
        ElseBody?.TransformAll(transform);
    }
}
