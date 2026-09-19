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

    /* The types named on a closed generic type, as in the "int" of "Cache<int>.Value". A static
     * member belongs to one instantiation rather than to the generic type as a whole, so these
     * are part of naming which member is meant. Empty for an ordinary name. */
    internal TypeTargetIdentifier[] GenericArguments { get; set; } = Array.Empty<TypeTargetIdentifier>();


    // Constructors.
    internal IdentifiableAccessStatement(Identifier memberIdentifier)
    {
        MemberIdentifier = memberIdentifier ?? throw new ArgumentNullException(nameof(memberIdentifier));
    }

    internal IdentifiableAccessStatement(string identifierName) : this(new Identifier(identifierName)) { }

    // Inherited fields.
    internal override IEnumerable<Statement> Children => Array.Empty<Statement>();
}
