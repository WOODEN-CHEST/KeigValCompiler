using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KeigValCompiler.Semantician.Member.Code;

internal class TryStatement : Statement
{
    // Fields.
    internal override IEnumerable<Statement> SubStatements
    {
        get
        {
            List<Statement> Statements = new(TryBody);

            foreach (CatchClause Clause in CatchClauses)
            {
                if (Clause.WhenCondition != null)
                {
                    Statements.Add(Clause.WhenCondition);
                }
                Statements.AddRange(Clause.Body);
            }

            if (FinallyBody != null)
            {
                Statements.AddRange(FinallyBody);
            }

            return SubStatements;
        }
    }

    internal StatementCollection TryBody { get; } = new();
    internal List<CatchClause> CatchClauses { get; } = new();
    internal StatementCollection? FinallyBody { get; set; } = null;
}