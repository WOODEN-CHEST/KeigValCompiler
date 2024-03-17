using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KeigValCompiler.Semantician;

internal class PackClass : PackMember
{
    // Internal fields.
    internal PackClass? BaseClass { get; private set; } = null;
    internal PackInterface[] ImplementedInterfaces { get; private set; } = Array.Empty<PackInterface>();


    // Private fields.
    private readonly string[] _extensions;
    private readonly Dictionary<string, PackFunction> _functions = new();
    private readonly Dictionary<string, PackField> _fields = new();



    // Constructors.
    internal PackClass(string identifier, PackMemberModifiers modifiers, PackClass parentClass, string[] extendedItems)
        : base(identifier, modifiers, parentClass)
    {
        _extensions = extendedItems ?? throw new ArgumentNullException(nameof(extendedItems));
        VerifyDefinition();
    }

    internal PackClass(string identifier, PackMemberModifiers modifiers, string parentNamespace, PackSourceFile sourceFile, string[] extendedItems)
        : base(identifier, modifiers, parentNamespace, sourceFile)
    {
        _extensions = extendedItems ?? throw new ArgumentNullException(nameof(extendedItems));
        VerifyDefinition();
    }


    // Private methods.
    private void VerifyDefinition()
    {
        if (IsStatic && _extensions.Length > 0)
        {
            throw new PackContentException("Static classes may not extend any other classes or implement interfaces.");
        }

        if (HasAnyModifiersExcept(PackMemberModifiers.Private, PackMemberModifiers.Protected,
            PackMemberModifiers.Public, PackMemberModifiers.Static))
        {
            throw new PackContentException("Classes may only have the following modifiers: " +
                $"{KGVL.KEYWORD_STATIC}, {KGVL.KEYWORD_PRIVATE}, {KGVL.KEYWORD_PROTECTED}, {KGVL.KEYWORD_PUBLIC}");
        }
    }
}