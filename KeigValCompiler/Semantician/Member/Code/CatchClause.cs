using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KeigValCompiler.Semantician.Member.Code;

internal class CatchClause
{
    // Fields.
    internal TypeTargetIdentifier ExceptionType { get; set; }
    internal Statement? WhenCondition { get; set; }
    internal Identifier? ExceptionIdentifier { get; set; } = null;
    internal StatementCollection Body { get; } = new();


    // Constructors.
    internal CatchClause(TypeTargetIdentifier exceptionType)
    {
        ExceptionType = exceptionType ?? throw new ArgumentNullException(nameof(exceptionType));
    }
}