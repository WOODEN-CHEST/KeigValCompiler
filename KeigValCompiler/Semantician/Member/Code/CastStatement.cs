using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KeigValCompiler.Semantician.Member.Code;

internal class CastStatement : Statement
{
    // Fields.
    internal Identifier TargetCastType { get; }
    internal Statement StatementToCast
    {
        get => _statementToCast;
        set => _statementToCast = value ?? throw new ArgumentNullException(nameof(value));
    }


    // Private fields.
    private Statement _statementToCast;


    // Constructors.
    internal CastStatement(Identifier targetCastType, Statement statementToCast)
    {
        TargetCastType = targetCastType ?? throw new ArgumentNullException(nameof(targetCastType));
        StatementToCast = statementToCast;
    }
}