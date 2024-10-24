using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KeigValCompiler.Semantician;

internal readonly struct SourceFileOrigin
{
    // Fields.
    internal int Line { get; }


    // Constructors.
    internal SourceFileOrigin(int line)
    {
        Line = line;
    }
}