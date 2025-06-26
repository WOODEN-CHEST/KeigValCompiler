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
    internal TypeTargetIdentifier TargetCastType { get; set; }
    internal Statement StatementToCast { get; set; }
    internal bool IsStrict { get; set; } = true;


    // Constructors.
    internal CastStatement(TypeTargetIdentifier targetCastType, Statement statementToCast)
    {
        TargetCastType = targetCastType ?? throw new ArgumentNullException(nameof(targetCastType));
        StatementToCast = statementToCast;
    }
}