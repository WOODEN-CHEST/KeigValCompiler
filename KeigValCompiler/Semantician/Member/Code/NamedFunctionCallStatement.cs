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

    /* The types named at the call site, as in the "int" of "Convert<int>(value)". Empty when the
     * call names none, which includes a call to a generic function whose types are inferred. */
    internal TypeTargetIdentifier[] GenericArguments { get; set; } = Array.Empty<TypeTargetIdentifier>();


    // Constructors.
    public NamedFunctionCallStatement(Identifier functionName)
    {
        FunctionName = functionName ?? throw new ArgumentNullException(nameof(functionName));
    }
}