using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KeigValCompiler.Error;

internal readonly struct ErrorCreateOptions
{
    // Fields.
    public ErrorDefinition Definition { get; private init; }
    public object[] Arguments { get; private init; }


    // Constructors.
    public ErrorCreateOptions(ErrorDefinition definition, params object[] args)
    {
        Definition = definition;
        Arguments = args;
    }


    // Methods.
    public string CreateMessage()
    {
        return $"KGVL{Definition.Code}: " 
            + string.Format(CultureInfo.InvariantCulture, Definition.RawMessage, Arguments);
    }
}