using KeigValCompiler.Semantician.Member;
using KeigValCompiler.Semantician.Member.Code;
using System.Diagnostics;

namespace KeigValCompiler.Semantician.Resolver;

internal class DefaultInternalContentProvider : IInternalContentProvider
{
    // Private fields.
    private const string INTERNAL_SOURCE_FILE_PATH = "/\\internal\\/";
    private const string NAMESPACE_KGVL = "KGVL";
    private const string MIN_VALUE = "MIN_VALUE";
    private const string MAX_VALUE = "MAX_VALUE";
    private const string BOOLEAN_FALSE_CONST = "FALSE";
    private const string BOOLEAN_TRUE_CONST = "TRUE";


    // Private methods.
    /* Primitives. */
    private PackStruct GetPrimitiveStruct(string name, PackSourceFile sourceFile)
    {
        PackStruct Struct = new(new(name), sourceFile);
        Struct.Modifiers |= (PackMemberModifiers.Sealed | PackMemberModifiers.BuiltIn);
        return Struct;
    }

    private PackStruct CreateBoolStruct(PackSourceFile sourceFile, BuiltInTypeRegistry registry)
    {
        PackStruct Struct = GetPrimitiveStruct(KGVL.TYPE_BOOL_NAME, sourceFile);
        registry.AddShorthandType(KGVL.KEYWORD_BOOL, Struct);
        registry.TypeBool = Struct;

        PackField FalseField = new(new(BOOLEAN_FALSE_CONST), sourceFile);
        FalseField.Modifiers |= (PackMemberModifiers.Readonly | PackMemberModifiers.Static);
        FalseField.Type = new(KGVL.KEYWORD_BOOL);
        FalseField.InitialValue = new PrimitiveValueStatement(KGVL.KEYWORD_FALSE);
        Struct.AddField(FalseField);

        PackField TrueField = new(new(BOOLEAN_TRUE_CONST), sourceFile);
        TrueField.Modifiers |= (PackMemberModifiers.Readonly | PackMemberModifiers.Static);
        TrueField.Type = new(KGVL.KEYWORD_BOOL);
        TrueField.InitialValue = new PrimitiveValueStatement(KGVL.KEYWORD_TRUE);
        Struct.AddField(TrueField);

        return Struct;
    }

    private PackStruct CreateByteStruct(PackSourceFile sourceFile, BuiltInTypeRegistry registry)
    {
        PackStruct Struct = GetPrimitiveStruct(KGVL.TYPE_BYTE_NAME, sourceFile);
        return Struct;
    }

    private PackStruct CreateUByteStruct(PackSourceFile sourceFile, BuiltInTypeRegistry registry)
    {
        PackStruct Struct = GetPrimitiveStruct(KGVL.TYPE_UBYTE_NAME, sourceFile);
        return Struct;
    }

    private PackStruct CreateShortStruct(PackSourceFile sourceFile, BuiltInTypeRegistry registry)
    {
        PackStruct Struct = GetPrimitiveStruct(KGVL.TYPE_SHORT_NAME, sourceFile);
        return Struct;
    }

    private PackStruct CreateUShortStruct(PackSourceFile sourceFile, BuiltInTypeRegistry registry)
    {
        PackStruct Struct = GetPrimitiveStruct(KGVL.TYPE_USHORT_NAME, sourceFile);
        return Struct;
    }

    private PackStruct CreateIntStruct(PackSourceFile sourceFile, BuiltInTypeRegistry registry)
    {
        PackStruct Struct = GetPrimitiveStruct(KGVL.TYPE_INT_NAME, sourceFile);
        return Struct;
    }

    private PackStruct CreateUIntStruct(PackSourceFile sourceFile, BuiltInTypeRegistry registry)
    {
        PackStruct Struct = GetPrimitiveStruct(KGVL.TYPE_UINT_NAME, sourceFile);
        return Struct;
    }

    private PackStruct CreateLongStruct(PackSourceFile sourceFile, BuiltInTypeRegistry registry)
    {
        PackStruct Struct = GetPrimitiveStruct(KGVL.TYPE_LONG_NAME, sourceFile);
        return Struct;
    }

    private PackStruct CreateULongStruct(PackSourceFile sourceFile, BuiltInTypeRegistry registry)
    {
        PackStruct Struct = GetPrimitiveStruct(KGVL.TYPE_ULONG_NAME, sourceFile);
        return Struct;
    }

    private PackStruct CreateDecimalStruct(PackSourceFile sourceFile, BuiltInTypeRegistry registry)
    {
        PackStruct Struct = GetPrimitiveStruct(KGVL.TYPE_DECIMAL_NAME, sourceFile);
        return Struct;
    }

    private PackNameSpace GetKGVLNameSpace(PackSourceFile sourceFile, BuiltInTypeRegistry registry)
    {
        PackNameSpace TargetNameSpace = new(new Identifier(NAMESPACE_KGVL));

        TargetNameSpace.AddStruct(CreateByteStruct(sourceFile, registry));
        TargetNameSpace.AddStruct(CreateUByteStruct(sourceFile, registry));
        TargetNameSpace.AddStruct(CreateShortStruct(sourceFile, registry));
        TargetNameSpace.AddStruct(CreateUShortStruct(sourceFile, registry));
        TargetNameSpace.AddStruct(CreateIntStruct(sourceFile, registry));
        TargetNameSpace.AddStruct(CreateUIntStruct(sourceFile, registry));
        TargetNameSpace.AddStruct(CreateLongStruct(sourceFile, registry));
        TargetNameSpace.AddStruct(CreateULongStruct(sourceFile, registry));
        TargetNameSpace.AddStruct(CreateDecimalStruct(sourceFile, registry));
        TargetNameSpace.AddStruct(CreateBoolStruct(sourceFile, registry));

        return TargetNameSpace;
    }


    // Inherited methods.
    public BuiltInTypeRegistry AddInternalContent(DataPack pack)
    {
        ArgumentNullException.ThrowIfNull(pack, nameof(pack));
        PackSourceFile SourceFile = new(pack, INTERNAL_SOURCE_FILE_PATH);
        pack.AddSourceFile(SourceFile);
        BuiltInTypeRegistry Registry = new();

        PackNameSpace PrimitiveNameSpace = GetKGVLNameSpace(SourceFile, Registry);
        SourceFile.AddNamespace(PrimitiveNameSpace);

        return Registry;
    }
}