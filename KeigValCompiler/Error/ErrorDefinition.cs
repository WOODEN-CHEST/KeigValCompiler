using KeigValCompiler.Source.Parser;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KeigValCompiler.Error;

internal class ErrorDefinition
{
    // Fields.
    public int Code { get; init; }
    public string RawMessage { get; init; }


    // Constructors.
    public ErrorDefinition(int code, string rawMessage)
    {
        Code = code;
        RawMessage = rawMessage ?? throw new ArgumentNullException(nameof(rawMessage));
    }


    // Methods.
    internal ErrorCreateOptions CreateOptions(params object[] args)
    {
        return new(this, args);
    }
}