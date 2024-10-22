using KeigValCompiler.Semantician.Member;
using System.Diagnostics;

namespace KeigValCompiler.Semantician.Resolver;

internal class KGVLInternalContentProvider : IInternalContentProvider
{
    // Private fields.
    private const string INTERNAL_SOURCE_FILE_PATH = "/\\internal\\/";
    private const string NAMESPACE_KGVL = "KGVL";


    // Private methods.
    /* Primitives. */
    private PackStruct GetPrimitiveStruct(string name, PackSourceFile sourceFile)
    {
        PackStruct Struct = new(new(name), sourceFile);
        Struct.Modifiers |= (PackMemberModifiers.Sealed | PackMemberModifiers.BuiltIn);
        return Struct;
    }

    private PackStruct CreateBoolStruct(PackSourceFile sourceFile)
    {
        PackStruct Struct = GetPrimitiveStruct(KGVL.TYPE_BOOL_NAME, sourceFile);
        return Struct;
    }

    private PackStruct CreateByteStruct(PackSourceFile sourceFile)
    {
        PackStruct Struct = GetPrimitiveStruct(KGVL.TYPE_BYTE_NAME, sourceFile);
        return Struct;
    }

    private PackStruct CreateUByteStruct(PackSourceFile sourceFile)
    {
        PackStruct Struct = GetPrimitiveStruct(KGVL.TYPE_UBYTE_NAME, sourceFile);
        return Struct;
    }

    private PackStruct CreateShortStruct(PackSourceFile sourceFile)
    {
        PackStruct Struct = GetPrimitiveStruct(KGVL.TYPE_SHORT_NAME, sourceFile);
        return Struct;
    }

    private PackStruct CreateUShortStruct(PackSourceFile sourceFile)
    {
        PackStruct Struct = GetPrimitiveStruct(KGVL.TYPE_USHORT_NAME, sourceFile);
        return Struct;
    }

    private PackStruct CreateIntStruct(PackSourceFile sourceFile)
    {
        PackStruct Struct = GetPrimitiveStruct(KGVL.TYPE_INT_NAME, sourceFile);
        return Struct;
    }

    private PackStruct CreateUIntStruct(PackSourceFile sourceFile)
    {
        PackStruct Struct = GetPrimitiveStruct(KGVL.TYPE_UINT_NAME, sourceFile);
        return Struct;
    }

    private PackStruct CreateLongStruct(PackSourceFile sourceFile)
    {
        PackStruct Struct = GetPrimitiveStruct(KGVL.TYPE_LONG_NAME, sourceFile);
        return Struct;
    }

    private PackStruct CreateULongStruct(PackSourceFile sourceFile)
    {
        PackStruct Struct = GetPrimitiveStruct(KGVL.TYPE_ULONG_NAME, sourceFile);
        return Struct;
    }

    private PackStruct CreateDecimalStruct(PackSourceFile sourceFile)
    {
        PackStruct Struct = GetPrimitiveStruct(KGVL.TYPE_DECIMAL_NAME, sourceFile);
        return Struct;
    }

    private PackNameSpace GetKGVLNameSpace(PackSourceFile sourceFile)
    {
        PackNameSpace TargetNameSpace = new(new Identifier(NAMESPACE_KGVL));

        TargetNameSpace.AddStruct(CreateByteStruct(sourceFile));
        TargetNameSpace.AddStruct(CreateUByteStruct(sourceFile));
        TargetNameSpace.AddStruct(CreateShortStruct(sourceFile));
        TargetNameSpace.AddStruct(CreateUShortStruct(sourceFile));
        TargetNameSpace.AddStruct(CreateIntStruct(sourceFile));
        TargetNameSpace.AddStruct(CreateUIntStruct(sourceFile));
        TargetNameSpace.AddStruct(CreateLongStruct(sourceFile));
        TargetNameSpace.AddStruct(CreateULongStruct(sourceFile));
        TargetNameSpace.AddStruct(CreateDecimalStruct(sourceFile));
        TargetNameSpace.AddStruct(CreateBoolStruct(sourceFile));

        return TargetNameSpace;
    }


    // Inherited methods.
    public void AddInternalContent(DataPack pack)
    {
        ArgumentNullException.ThrowIfNull(pack, nameof(pack));
        PackSourceFile SourceFile = new(pack, INTERNAL_SOURCE_FILE_PATH);
        pack.AddSourceFile(SourceFile);

        PackNameSpace PrimitiveNameSpace = GetKGVLNameSpace(SourceFile);
        SourceFile.AddNamespace(PrimitiveNameSpace);
    }
}