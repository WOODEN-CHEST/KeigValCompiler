namespace KeigValCompiler.Semantician.Member;

internal class PackField : PackMember
{
    // Internal fields.
    internal Identifier Type { get; private init; }
    internal Statement? InitialValue { get; private init; }


    // Constructors.
    public PackField(Identifier identifier,
        PackMemberModifiers modifiers,
        PackSourceFile sourceFile,
        PackNameSpace nameSpace,
        Identifier parentItem,
        Identifier type,
        Statement initialValue) : base(identifier, modifiers, sourceFile, nameSpace, parentItem)
    {
        Type = type ?? throw new ArgumentNullException(nameof(type));
        InitialValue = initialValue ?? throw new ArgumentNullException(nameof(initialValue));
    }
}