using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KeigValCompiler.Semantician;

internal class PackInterface : PackMember
{
    // Constructors.
    internal PackInterface(string identifier, PackMemberModifiers modifiers, PackClass parentClass)
        : base(identifier, modifiers, parentClass)
    {
    }

    internal PackInterface(string identifier, PackMemberModifiers modifiers, string parentNamespace, PackSourceFile sourceFile)
        : base(identifier, modifiers, parentNamespace, sourceFile)
    {
    }
}