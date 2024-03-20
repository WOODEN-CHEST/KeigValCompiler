using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KeigValCompiler.Semantician.Member;

internal class PackField : PackMember
{
    // Internal fields.
    internal string Type { get; private init; }
    internal string? InitialValue { get; private init; }


    // Constructors.
    internal PackField(string identifier, PackMemberModifiers modifiers, PackClass packClass, string type, string? initialValue)
        : base(identifier, modifiers, packClass)
    {
        Type = type ?? throw new ArgumentNullException(nameof(modifiers));
        InitialValue = initialValue;
    }

    internal PackField(string identifier,
        PackMemberModifiers modifiers,
        string nameSpace,
        PackSourceFile sourceFile,
        string type,
        string? initialValue)
        : base(identifier, modifiers, nameSpace, sourceFile)
    {
        Type = type ?? throw new ArgumentNullException(nameof(modifiers));
        InitialValue = initialValue;
    }
}