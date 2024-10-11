namespace KeigValCompiler.Semantician.Member.Class;

internal class PackClass : PackMember
{
    // Internal fields.
    internal PackInterface[] ImplementedInterfaces
    {
        get => _implementedInterfaces;
        set => _implementedInterfaces = value?.ToArray() ?? throw new ArgumentNullException(nameof(value));
    }

    internal OperatorOverload[] OperatorOverloads => _operatorOverloads.ToArray();
    internal Generi


    // Private fields.
    private PackInterface[] _implementedInterfaces = Array.Empty<PackInterface>();
    private readonly Identifier[] _extensionIdentifiers;
    private readonly List<OperatorOverload> _operatorOverloads = new();


    // Constructors.
    internal PackClass(string identifier, PackMemberModifiers modifiers, PackClass parentClass, Identifier[] extendedItems)
        : base(identifier, modifiers, parentClass)
    {
        _extensionIdentifiers = extendedItems ?? throw new ArgumentNullException(nameof(extendedItems));
        VerifyDefinition();
    }

    internal PackClass(string identifier, PackMemberModifiers modifiers, string parentNamespace, PackSourceFile sourceFile, Identifier[] extendedItems)
        : base(identifier, modifiers, parentNamespace, sourceFile)
    {
        _extensionIdentifiers = extendedItems ?? throw new ArgumentNullException(nameof(extendedItems));
        VerifyDefinition();
    }


    // Private methods.
    private void VerifyDefinition()
    {
        if (IsStatic && _extensionIdentifiers.Length > 0)
        {
            throw new PackContentException("Static classes may not extend any other classes or implement interfaces.");
        }

        PackMemberModifiers[] AllowedModifiers = new PackMemberModifiers[]
        {
            PackMemberModifiers.Private,
            PackMemberModifiers.Protected,
            PackMemberModifiers.Public,
            PackMemberModifiers.Static,
            PackMemberModifiers.Abstract
        };
        if (HasAnyModifiersExcept(AllowedModifiers))
        {
            throw new PackContentException("Classes may only have the following modifiers:" +
                $" {string.Join(", ", AllowedModifiers.Select(Modifier => Modifier.ToString().ToLower()))}");
        }
    }
}