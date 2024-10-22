using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KeigValCompiler.Semantician.Member.Code;

internal class PrimitiveValueStatement : Statement
{
    // Internal fields.
    internal Identifier? ExpectedType { get; set; }
    internal string Value { get; set; }
}