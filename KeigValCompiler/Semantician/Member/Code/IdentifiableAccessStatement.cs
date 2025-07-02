using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KeigValCompiler.Semantician.Member.Code;

internal class IdentifiableAccessStatement : Statement
{
    // Fields.
    internal Identifier MemberIdentifier { get; set; }


    // Constructors.
    internal IdentifiableAccessStatement(string identifierName)
    {
        MemberIdentifier = new(identifierName);
    }
}