using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KeigValCompiler.Semantician.Member.Code;

internal class TryStatement : Statement
{
    // Fields.
    internal StatementCollection TryBody { get; } = new();
    internal List<CatchClause> CatchClauses { get; } = new();
    internal StatementCollection? FinallyBody { get; set; } = null;


    // Inherited fields.
    internal override IEnumerable<Statement> Children => TryBody
        .Concat(CatchClauses.SelectMany(clause => clause.WhenCondition == null
            ? clause.Body.AsEnumerable()
            : new Statement[] { clause.WhenCondition }.Concat(clause.Body)))
        .Concat(FinallyBody ?? Enumerable.Empty<Statement>());


    // Inherited methods.
    internal override void TransformChildren(Func<Statement, Statement> transform)
    {
        TryBody.TransformAll(transform);
        foreach (CatchClause Clause in CatchClauses)
        {
            if (Clause.WhenCondition != null)
            {
                Clause.WhenCondition = transform(Clause.WhenCondition);
            }
            Clause.Body.TransformAll(transform);
        }
        FinallyBody?.TransformAll(transform);
    }
}
