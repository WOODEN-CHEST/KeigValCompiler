using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KeigValCompiler.Semantician.Member;

[Flags]
internal enum FunctionParameterModifier
{
    None = 0,
    In = 1,
    Out = 2,
}