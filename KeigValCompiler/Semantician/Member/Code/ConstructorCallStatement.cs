using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KeigValCompiler.Semantician.Member.Code;

internal class ConstructorCallStatement : FunctionCallStatement
{
    // Fields.
    internal Identifier? ObjectType { get; }
}