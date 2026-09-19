using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KeigValCompiler.Semantician.Member.Code;

internal class ReturnStatement : Statement
{
    internal Statement? ReturnValue { get; set; } = null;


    // Inherited fields.
    internal override IEnumerable<Statement> Children =>
        ReturnValue == null ? Array.Empty<Statement>() : new Statement[] { ReturnValue };


    // Inherited methods.
    internal override void TransformChildren(Func<Statement, Statement> transform)
    {
        if (ReturnValue != null)
        {
            ReturnValue = transform(ReturnValue);
        }
    }
}
