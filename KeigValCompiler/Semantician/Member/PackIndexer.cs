namespace KeigValCompiler.Semantician.Member;

internal class PackIndexer : PackMember
{
    // Fields.
    public Identifier SelfIdentifier => throw new NotImplementedException();


    // Internal fields.
    internal Identifier Type { get; private init; }
    internal PackFunction? GetFunction { get; private init; }
    internal PackFunction? SetFunction { get; private init; }


    // Constructors.
    public PackIndexer(Identifier identifier,
        PackMemberModifiers modifiers,
        PackSourceFile sourceFile,
        PackNameSpace nameSpace,
        Identifier parentItem,
        Identifier type,
        PackFunction? getFunc,
        PackFunction? setFunc) : base(identifier, modifiers, sourceFile, nameSpace, parentItem)
    {
        Type = type ?? throw new ArgumentNullException(nameof(type));
        GetFunction = getFunc;
        SetFunction = setFunc;
    }
}