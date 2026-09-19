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
    internal IdentifiableAccessStatement(Identifier memberIdentifier)
    {
        MemberIdentifier = memberIdentifier ?? throw new ArgumentNullException(nameof(memberIdentifier));
    }

    internal IdentifiableAccessStatement(string identifierName) : this(new Identifier(identifierName)) { }

    // Inherited fields.
    internal override IEnumerable<Statement> Children => Array.Empty<Statement>();
}
