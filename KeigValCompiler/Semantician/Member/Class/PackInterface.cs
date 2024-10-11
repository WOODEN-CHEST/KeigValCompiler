namespace KeigValCompiler.Semantician.Member.Class;

internal class PackInterface : PackMember
{
    // Internal fields.
    internal MemberContainer Members { get; } = new(canContainFields: false, canContainClasses: false);
    internal PackInterface[] ImplementedInterfaces { get; private set; } = Array.Empty<PackInterface>();


    // Private fields.
    private readonly Identifier[] _extensionIdentifiers;


    // Constructors.
    internal PackInterface(string identifier, PackMemberModifiers modifiers, PackClass parentClass, Identifier[] extendedIdentifiers)
        : base(identifier, modifiers, parentClass)
    {
        _extensionIdentifiers = extendedIdentifiers ?? throw new ArgumentNullException(nameof(extendedIdentifiers));
    }

    internal PackInterface(string identifier,
        PackMemberModifiers modifiers,
        string parentNamespace,
        PackSourceFile sourceFile,
         Identifier[] extendedIdentifiers)
        : base(identifier, modifiers, parentNamespace, sourceFile)
    {
        _extensionIdentifiers = extendedIdentifiers ?? throw new ArgumentNullException(nameof(extendedIdentifiers));
    }


    // Private methods.
    private void VerifyDefinition()
    {
        PackMemberModifiers[] AllowedModifiers = new PackMemberModifiers[]
        {
            PackMemberModifiers.Public,
        };
        if (HasAnyModifiersExcept(AllowedModifiers))
        {
            throw new PackContentException("Fields may only have the following modifiers:" +
                $" {string.Join(", ", AllowedModifiers.Select(Modifier => Modifier.ToString().ToLower()))}");
        }
    }
}