using KeigValCompiler.Semantician.Member;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KeigValCompiler.Semantician.Member.Function;

internal class LocalField : IIdentifiable
{
    // Fields.
    public Identifier SelfIdentifier { get; private init; }


    // Internal fields.
    internal Identifier Type { get; private init; }


    // Constructors.
    internal LocalField(Identifier type, Identifier name)
    {
        Type = type ?? throw new ArgumentNullException(nameof(type));
        SelfIdentifier = name ?? throw new ArgumentNullException(nameof(name));
    }
}