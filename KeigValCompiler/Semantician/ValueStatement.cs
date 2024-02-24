using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KeigValCompiler.Semantician;

// A statement in code which returns a value.
internal class ValueStatement
{
    // Internal fields.
    internal string ReturnType { get; private init; }


    // Constructors.
    internal ValueStatement(string returnType)
    {
        ReturnType = returnType ?? throw new ArgumentNullException(nameof(ReturnType));
    }
}
