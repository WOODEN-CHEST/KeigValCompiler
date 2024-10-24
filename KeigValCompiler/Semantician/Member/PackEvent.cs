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


    // Constructors.
    public PackEvent(Identifier identifier, PackSourceFile sourceFile) : base(identifier, sourceFile) { }
}