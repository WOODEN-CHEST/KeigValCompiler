using KeigValCompiler.Source.Parser;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KeigValCompiler.Error;

internal class WarningDefinition : CompilerMessageDefinition
{
    // Fields.
    public WarningSeverity Severity { get; private init; }


    // Constructors.
    public WarningDefinition(int code, WarningSeverity severity, string rawMessage)
        : base(code, rawMessage)
    {
        Severity = severity;
    }


    // Methods.
    internal WarningCreateOptions CreateOptions(params object[] args)
    {
        return new(this, args);
    }
}