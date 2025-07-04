using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KeigValCompiler.Semantician.Member.Code;

internal class ReturnStatement : Statement
{
    internal Statement? ReturnValue { get; set; } = null;
}