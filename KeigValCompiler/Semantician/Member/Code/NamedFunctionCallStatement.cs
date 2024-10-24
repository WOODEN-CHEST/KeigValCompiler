using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KeigValCompiler.Semantician.Member.Code;

internal class NamedFunctionCallStatement : FunctionCallStatement
{
    // Internal fields.
    internal Identifier FunctionName { get; set; }


    // Constructors.
    public NamedFunctionCallStatement(Identifier functionName)
    {
        FunctionName = functionName ?? throw new ArgumentNullException(nameof(functionName));
    }
}