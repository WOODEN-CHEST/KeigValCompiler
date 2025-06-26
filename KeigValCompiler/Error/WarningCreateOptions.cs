using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KeigValCompiler.Error;

internal readonly struct WarningCreateOptions
{
    // Fields.
    public WarningDefinition Definition { get; private init; }
    public object[] Arguments { get; private init; }


    // Constructors.
    public WarningCreateOptions(WarningDefinition definition, params object[] args)
    {
        Definition = definition;
        Arguments = args;
    }


    // Methods.
    public string CreateMessage()
    {
        return $"KGVL Warning {Definition.Code}{SeverityToString()}: " 
            + string.Format(CultureInfo.InvariantCulture, Definition.RawMessage, Arguments);
    }


    // Private methods.
    private string? SeverityToString()
    {
        return Definition.Severity switch
        {
            WarningSeverity.Minor => " (Minor)",
            WarningSeverity.Normal => string.Empty,
            WarningSeverity.Severe => " (Severe)",
            _ => " (Unknown severity)"
        };
    }
}