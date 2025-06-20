using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KeigValCompiler.Semantician.Member.Code;

internal class CastStatement : Statement
{
    // Fields.
    internal override IEnumerable<Statement> SubStatements => new Statement[] { StatementToCast };
    internal Identifier TargetCastType { get; set; }
    internal Statement StatementToCast { get; set; }


    // Constructors.
    internal CastStatement(Identifier targetCastType, Statement statementToCast)
    {
        TargetCastType = targetCastType ?? throw new ArgumentNullException(nameof(targetCastType));
        StatementToCast = statementToCast;
    }
}