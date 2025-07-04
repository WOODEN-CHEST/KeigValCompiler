using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KeigValCompiler.Semantician.Member.Code;

internal class OperatorStatement : Statement
{
    // Fields.
    internal StatementOperator TargetOperator { get; set; }
    internal Statement MainStatement { get; set; }
    internal Statement? AdditionalStatement { get; set; } = null;


    // Constructors.
    public OperatorStatement(StatementOperator targetOperator, 
        Statement mainStatement, 
        Statement? additionalStatement)
    {
        TargetOperator = targetOperator;
        MainStatement = mainStatement ?? throw new ArgumentNullException(nameof(mainStatement));
        AdditionalStatement = additionalStatement;
    }
}