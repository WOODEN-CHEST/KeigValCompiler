using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KeigValCompiler.Semantician.Member.Code;

internal class VariableRetrieveStatement : Statement
{
    // Fields.
    internal Identifier VariableIdentifier { get; set; }


    // Constructors.
    internal VariableRetrieveStatement(string identifierName)
    {
        VariableIdentifier = new(identifierName);
    }
}