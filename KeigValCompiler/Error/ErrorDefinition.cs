using KeigValCompiler.Source.Parser;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KeigValCompiler.Error;

internal class ErrorDefinition : CompilerMessageDefinition
{
    // Constructors.
    public ErrorDefinition(int code, string rawMessage) : base(code, rawMessage) { }


    // Methods.
    internal ErrorCreateOptions CreateOptions(params object[] args)
    {
        return new(this, args);
    }
}