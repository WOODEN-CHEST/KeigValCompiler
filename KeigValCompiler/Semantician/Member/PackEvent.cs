using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KeigValCompiler.Semantician.Member;

internal class PackEvent : PackMember
{
    // Internal fields.
    internal Identifier Type { get; set; }
    internal Identifier DelegateIdentifier { get; private init; }


    // Constructors.
    public PackEvent(Identifier identifier, 
        Identifier delegateIdentifier,
        PackSourceFile sourceFile) 
        : base(identifier, sourceFile)
    {
        DelegateIdentifier = delegateIdentifier ?? throw new ArgumentNullException(nameof(delegateIdentifier));
    }
}