using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KeigValCompiler.Semantician.Member;

internal class TypeTargetIdentifier
{
    // Fields.
    internal Identifier? MainTarget { get; set; }
    internal TypeTargetIdentifier[] TypeArguments { get; set; }


    // Constructors.
    public TypeTargetIdentifier(Identifier mainTarget,  TypeTargetIdentifier[]? typeArguments)
    {
        MainTarget = mainTarget;
        TypeArguments = typeArguments ?? Array.Empty<TypeTargetIdentifier>();
    }
}