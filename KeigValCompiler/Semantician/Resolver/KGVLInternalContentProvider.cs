using KeigValCompiler.Semantician.Member;

namespace KeigValCompiler.Semantician.Resolver;

internal class KGVLInternalContentProvider : IInternalContentProvider
{
    // Private fields.
    private const string INTERNAL_SOURCE_FILE_PATH = "{internal}";
    private const string INTERNAL_NAMESPACE = "{internal}";

    // Private methods.
    private PackStruct CreateBoolStruct(DataPack pack, PackSourceFile sourceFile, PackNameSpace nameSpace)
    {
        Identifier TargetIdentifier = new(KGVL.KEYWORD_BOOL);

        PackStruct Struct = new(TargetIdentifier, PackMemberModifiers.Sealed | PackMemberModifiers.BuiltIn, sourceFile,
            nameSpace, nameSpace.SelfIdentifier, null, null, null);
        TargetIdentifier.Target = Struct;

        return Struct;
    }

    private PackStruct CreateByteStruct(DataPack pack, PackSourceFile sourceFile, PackNameSpace nameSpace)
    {
        Identifier TargetIdentifier = new(KGVL.KEYWORD_BYTE);

        PackStruct Struct = new(TargetIdentifier, PackMemberModifiers.Sealed | PackMemberModifiers.BuiltIn, sourceFile,
            nameSpace, nameSpace.SelfIdentifier, null, null, null);
        TargetIdentifier.Target = Struct;

        return Struct;
    }

    private PackStruct CreateUByteStruct(DataPack pack, PackSourceFile sourceFile, PackNameSpace nameSpace)
    {
        Identifier TargetIdentifier = new(KGVL.KEYWORD_UBYTE);

        PackStruct Struct = new(TargetIdentifier, PackMemberModifiers.Sealed | PackMemberModifiers.BuiltIn, sourceFile,
            nameSpace, nameSpace.SelfIdentifier, null, null, null);
        TargetIdentifier.Target = Struct;

        return Struct;
    }

    private PackStruct CreateShortStruct(DataPack pack, PackSourceFile sourceFile, PackNameSpace nameSpace)
    {
        Identifier TargetIdentifier = new(KGVL.KEYWORD_SHORT);

        PackStruct Struct = new(TargetIdentifier, PackMemberModifiers.Sealed | PackMemberModifiers.BuiltIn, sourceFile,
            nameSpace, nameSpace.SelfIdentifier, null, null, null);
        TargetIdentifier.Target = Struct;

        return Struct;
    }

    private PackStruct CreateUShortStruct(DataPack pack, PackSourceFile sourceFile, PackNameSpace nameSpace)
    {
        Identifier TargetIdentifier = new(KGVL.KEYWORD_USHORT);

        PackStruct Struct = new(TargetIdentifier, PackMemberModifiers.Sealed | PackMemberModifiers.BuiltIn, sourceFile,
            nameSpace, nameSpace.SelfIdentifier, null, null, null);
        TargetIdentifier.Target = Struct;

        return Struct;
    }

    private PackStruct CreateIntStruct(DataPack pack, PackSourceFile sourceFile, PackNameSpace nameSpace)
    {
        Identifier TargetIdentifier = new(KGVL.KEYWORD_INT);

        PackStruct Struct = new(TargetIdentifier, PackMemberModifiers.Sealed | PackMemberModifiers.BuiltIn, sourceFile,
            nameSpace, nameSpace.SelfIdentifier, null, null, null);
        TargetIdentifier.Target = Struct;

        return Struct;
    }

    private PackStruct CreateUIntStruct(DataPack pack, PackSourceFile sourceFile, PackNameSpace nameSpace)
    {
        Identifier TargetIdentifier = new(KGVL.KEYWORD_UINT);

        PackStruct Struct = new(TargetIdentifier, PackMemberModifiers.Sealed | PackMemberModifiers.BuiltIn, sourceFile,
            nameSpace, nameSpace.SelfIdentifier, null, null, null);
        TargetIdentifier.Target = Struct;

        return Struct;
    }


    // Inherited methods.
    public void AddInternalContent(DataPack pack)
    {
        ArgumentNullException.ThrowIfNull(pack, nameof(pack));
        PackNameSpace TargetNameSpace = new(new Identifier(INTERNAL_NAMESPACE));
        PackSourceFile SourceFile = new(pack, INTERNAL_SOURCE_FILE_PATH);
        SourceFile.AddNamespace(TargetNameSpace);
        pack.AddSourceFile(SourceFile);

        TargetNameSpace.AddStruct(CreateByteStruct(pack, SourceFile, TargetNameSpace));
        TargetNameSpace.AddStruct(CreateUByteStruct(pack, SourceFile, TargetNameSpace));
        TargetNameSpace.AddStruct(CreateShortStruct(pack, SourceFile, TargetNameSpace));
        TargetNameSpace.AddStruct(CreateUShortStruct(pack, SourceFile, TargetNameSpace));
        TargetNameSpace.AddStruct(CreateIntStruct(pack, SourceFile, TargetNameSpace));
        TargetNameSpace.AddStruct(CreateUIntStruct(pack, SourceFile, TargetNameSpace));
        TargetNameSpace.AddStruct(CreateBoolStruct(pack, SourceFile, TargetNameSpace));
    }
}