using KeigValCompiler.Semantician.Member;
using KeigValCompiler.Semantician.Member.Code;
using System.Diagnostics;

namespace KeigValCompiler.Semantician.Resolver;

internal class DefaultInternalContentProvider : IInternalContentProvider
{
    // Private fields.
    private const string TYPE_BOOL_NAME = "Boolean";
    private const string TYPE_BYTE_NAME = "Int8";
    private const string TYPE_UBYTE_NAME = "UInt8";
    private const string TYPE_SHORT_NAME = "Int16";
    private const string TYPE_USHORT_NAME = "UInt16";
    private const string TYPE_INT_NAME = "Int32";
    private const string TYPE_UINT_NAME = "UInt32";
    private const string TYPE_LONG_NAME = "Int64";
    private const string TYPE_ULONG_NAME = "UInt64";
    private const string TYPE_DECIMAL_NAME = "TwoIntDecimal";
    private const string TYPE_NULL_NAME = "Null";
    private const string TYPE_STRING_NAME = "String";

    private const string INTERNAL_SOURCE_FILE_PATH = "/\\internal\\/";
    private const string NAMESPACE_KGVL = "KGVL";
    private const string MIN_VALUE = "MIN_VALUE";
    private const string MAX_VALUE = "MAX_VALUE";
    private const string BOOLEAN_FALSE_CONST = "FALSE";
    private const string BOOLEAN_TRUE_CONST = "TRUE";


    // Private methods.
    /* Primitives. */
    private PackStruct CreateStruct(string name, PackSourceFile sourceFile)
    {
        PackStruct Struct = new(new(name), sourceFile);
        Struct.Modifiers |= (PackMemberModifiers.Sealed | PackMemberModifiers.BuiltIn);
        return Struct;
    }

    private PackClass CreateClass(string name, PackSourceFile sourceFile)
    {
        PackClass Class = new(new(name), sourceFile);
        Class.Modifiers |= (PackMemberModifiers.Sealed | PackMemberModifiers.BuiltIn);
        return Class;
    }

    private PackStruct CreateBoolStruct(PackSourceFile sourceFile, BuiltInTypeRegistry registry)
    {
        PackStruct Struct = CreateStruct(TYPE_BOOL_NAME, sourceFile);
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
        PackStruct Struct = CreateStruct(TYPE_BYTE_NAME, sourceFile);
        registry.AddShorthandType(KGVL.KEYWORD_BYTE, Struct);
        registry.TypeInt8 = Struct;

        return Struct;
    }

    private PackStruct CreateUByteStruct(PackSourceFile sourceFile, BuiltInTypeRegistry registry)
    {
        PackStruct Struct = CreateStruct(TYPE_UBYTE_NAME, sourceFile);
        registry.AddShorthandType(KGVL.KEYWORD_UBYTE, Struct);
        registry.TypeUInt8 = Struct;

        return Struct;
    }

    private PackStruct CreateShortStruct(PackSourceFile sourceFile, BuiltInTypeRegistry registry)
    {
        PackStruct Struct = CreateStruct(TYPE_SHORT_NAME, sourceFile);
        registry.AddShorthandType(KGVL.KEYWORD_SHORT, Struct);
        registry.TypeInt16 = Struct;

        return Struct;
    }

    private PackStruct CreateUShortStruct(PackSourceFile sourceFile, BuiltInTypeRegistry registry)
    {
        PackStruct Struct = CreateStruct(TYPE_USHORT_NAME, sourceFile);
        registry.AddShorthandType(KGVL.KEYWORD_USHORT, Struct);
        registry.TypeUInt16 = Struct;

        return Struct;
    }

    private PackStruct CreateIntStruct(PackSourceFile sourceFile, BuiltInTypeRegistry registry)
    {
        PackStruct Struct = CreateStruct(TYPE_INT_NAME, sourceFile);
        registry.AddShorthandType(KGVL.KEYWORD_INT, Struct);
        registry.TypeInt32 = Struct;

        return Struct;
    }

    private PackStruct CreateUIntStruct(PackSourceFile sourceFile, BuiltInTypeRegistry registry)
    {
        PackStruct Struct = CreateStruct(TYPE_UINT_NAME, sourceFile);
        registry.AddShorthandType(KGVL.KEYWORD_UINT, Struct);
        registry.TypeUInt32 = Struct;

        return Struct;
    }

    private PackStruct CreateLongStruct(PackSourceFile sourceFile, BuiltInTypeRegistry registry)
    {
        PackStruct Struct = CreateStruct(TYPE_LONG_NAME, sourceFile);
        registry.AddShorthandType(KGVL.KEYWORD_LONG, Struct);
        registry.TypeInt64 = Struct;

        return Struct;
    }

    private PackStruct CreateULongStruct(PackSourceFile sourceFile, BuiltInTypeRegistry registry)
    {
        PackStruct Struct = CreateStruct(TYPE_ULONG_NAME, sourceFile);
        registry.AddShorthandType(KGVL.KEYWORD_ULONG, Struct);
        registry.TypeUInt64 = Struct;

        return Struct;
    }

    private PackStruct CreateDecimalStruct(PackSourceFile sourceFile, BuiltInTypeRegistry registry)
    {
        PackStruct Struct = CreateStruct(TYPE_DECIMAL_NAME, sourceFile);
        registry.AddShorthandType(KGVL.KEYWORD_DECIMAL, Struct);
        registry.TypeDecimal = Struct;

        return Struct;
    }

    private PackStruct CreateNullStruct(PackSourceFile sourceFile, BuiltInTypeRegistry registry)
    {
        PackStruct Struct = CreateStruct(TYPE_NULL_NAME, sourceFile);
        registry.AddShorthandType(KGVL.KEYWORD_NULL, Struct);
        registry.TypeNull = Struct;

        return Struct;
    }

    private PackClass CreateStringClass(PackSourceFile sourceFile, BuiltInTypeRegistry registry)
    {
        PackClass Class = CreateClass(TYPE_STRING_NAME, sourceFile);
        registry.AddShorthandType(KGVL.KEYWORD_STRING, Class);
        registry.TypeString = Class;

        return Class;
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
        TargetNameSpace.AddClass(CreateStringClass(sourceFile, registry));

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