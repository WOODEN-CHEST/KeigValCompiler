using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KeigValCompiler.Semantician.Member.Code;

internal class OperatorStatement : Statement
{
    // Fields.
    internal override IEnumerable<Statement> SubStatements
    {
        get
        {
            List<Statement> Statements = new() {  MainStatement  };
            if (AdditionalStatement != null)
            {
                Statements.Add(AdditionalStatement);
            }

            return Statements;
        }
    }

    internal StatementOperator TargetOperator { get; set; }
    internal Statement MainStatement { get; set; }
    internal Statement? AdditionalStatement { get; set; }


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