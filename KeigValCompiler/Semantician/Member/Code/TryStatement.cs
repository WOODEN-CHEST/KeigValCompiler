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
}