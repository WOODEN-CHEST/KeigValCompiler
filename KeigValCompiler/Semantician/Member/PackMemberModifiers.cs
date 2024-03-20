using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KeigValCompiler.Semantician.Member;

[Flags]
internal enum PackMemberModifiers
{
    None = 0,
    Static = 1,

    Readonly = 2,

    BuiltIn = 4,

    Abstract = 8,
    Virtual = 16,
    Override = 32,

    Private = 64,
    Protected = 128,
    Public = 256,

    Inline = 512
}