using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KeigValCompiler.Semantician.Member.Code;

internal class OperatorStatement : Statement
{
    // Fields.
    internal Operator TargetOperator { get; private init; }
    internal Statement LeftStatement { get; private init; }
    internal Statement? RightStatement { get; private init; }


    // Constructors.
    public OperatorStatement(Operator targetOperator, Statement leftStatement, Statement? rightStatement)
    {
        TargetOperator = targetOperator;
        LeftStatement = leftStatement ?? throw new ArgumentNullException(nameof(leftStatement));
        RightStatement = rightStatement;
    }
}