using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KeigValCompiler.Error;

internal abstract class CompilerMessageDefinition
{
    // Fields.
    public int Code { get; init; }
    public string RawMessage { get; init; }
    public CompilerMessageCategory Category { get; init; }


    // Constructors.
    public CompilerMessageDefinition(int code, string rawMessage, CompilerMessageCategory category)
    {
        Code = code;
        RawMessage = rawMessage ?? throw new ArgumentNullException(nameof(rawMessage));
        Category = category ?? throw new ArgumentNullException(nameof(category));
    }
}