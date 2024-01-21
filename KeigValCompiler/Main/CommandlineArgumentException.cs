using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KeigValCompiler.Main;

internal class CommandlineArgumentException : Exception
{
    // Constructors.
    internal CommandlineArgumentException(string message) : base(message) { }
}
