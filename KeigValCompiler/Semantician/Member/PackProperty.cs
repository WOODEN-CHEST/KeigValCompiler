namespace KeigValCompiler.Semantician.Member;

internal class PackProperty : PackMember
{
    // Internal fields.
    internal Identifier Type { get; private init; }
    internal Statement? InitialValue { get; private init; }
    internal PackFunction? GetFunction { get; private init; }
    internal PackFunction? SetFunction { get; private init; }
    internal PackFunction? InitFunction { get; private init; }


    // Constructors.
    internal PackProperty(Identifier identifier,
        PackMemberModifiers modifiers,
        PackSourceFile sourceFile,
        PackNameSpace nameSpace,
        Identifier parentItem,
        Identifier type,
        PackFunction? getFunc,
        PackFunction? setFunc,
        PackFunction? initFunc,
        Statement? initialValue)
        : base(identifier, modifiers, sourceFile, nameSpace, parentItem)
    {
        Type = type ?? throw new ArgumentNullException(nameof(type));
        GetFunction = getFunc;
        SetFunction = setFunc;
        InitFunction = initFunc;
        InitialValue = initialValue;
    }
}