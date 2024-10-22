using KeigValCompiler.Semantician.Member;
using System.Globalization;
using System.Collections.Generic;
using System.Linq;

namespace KeigValCompiler.Semantician.Resolver;

internal class KGVLPackResolver : IPackResolver
{
    // Private methods.
    /* Resolving namespaces. */
    private void ResolveNameSpaceIdentifiers(PackNameSpace[] nameSpaces)
    {
        foreach (PackNameSpace NameSpace in nameSpaces)
        {
            string Name = NameSpace.SelfIdentifier.SourceCodeName;
            NameSpace.SelfIdentifier.Target = NameSpace;

            NameSpace.SelfIdentifier.ResolvedName = Name;
            NameSpace.SelfIdentifier.SelfName = Name.Substring(Name.LastIndexOf(KGVL.NAMESPACE_SEPARATOR) + 1);
        }
    }

    private void ResolveMemberNameSpaces(PackNameSpace[] nameSpaces)
    {
        foreach (PackNameSpace NameSpace in nameSpaces)
        {
            foreach (PackMember Member in NameSpace.AllMembers)
            {
                ResolveMemberNameSpaces(Member, NameSpace);
            }
        }
    }

    private void ResolveFunctionNameSpaces(IPackFunctionHolder functionHolder, PackNameSpace nameSpace)
    {
        foreach (PackFunction Function in functionHolder.Functions)
        {
            Function.NameSpace = nameSpace;
        }
        foreach (PackProperty Property in functionHolder.Properties)
        {
            Property.NameSpace = nameSpace;
            if (Property.SetFunction != null)
            {
                Property.SetFunction.NameSpace = nameSpace;
            }
            if (Property.GetFunction != null)
            {
                Property.GetFunction.NameSpace = nameSpace;
            }
            if (Property.InitFunction != null)
            {
                Property.InitFunction.NameSpace = nameSpace;
            }
        }
        foreach (PackIndexer Indexer in functionHolder.Indexers)
        {
            Indexer.NameSpace = nameSpace;
            if (Indexer.SetFunction != null)
            {
                Indexer.SetFunction.NameSpace = nameSpace;
            }
            if (Indexer.GetFunction != null)
            {
                Indexer.GetFunction.NameSpace = nameSpace;
            }
        }
    }

    private void ResolveFieldNameSpaces(IPackFieldHolder fieldHoler, PackNameSpace nameSpace)
    {
        foreach (PackField Field in fieldHoler.Fields)
        {
            Field.NameSpace = nameSpace;
        }
    }

    private void ResolveTypeHolderNameSpaces(IPackTypeHolder typeHolder, PackNameSpace nameSpace)
    {
        foreach (PackStruct Struct in typeHolder.Structs)
        {
            ResolveMemberNameSpaces(Struct, nameSpace);
        }
        foreach (PackClass Class in typeHolder.Classes)
        {
            ResolveMemberNameSpaces(Class, nameSpace);
        }
        foreach (PackInterface Interface in typeHolder.Interfaces)
        {
            ResolveMemberNameSpaces(Interface, nameSpace);
        }
    }

    private void ResolveOperatorOverloadNameSpaces(IOperatorOverloadHolder overloadHolder, PackNameSpace nameSpace)
    {
        foreach (OperatorOverload Overload in overloadHolder.OperatorOverloads)
        {
            Overload.Function.NameSpace = nameSpace;
        }
    }

    private void ResolveMemberNameSpaces(PackMember member, PackNameSpace nameSpace)
    {
        member.NameSpace = nameSpace;
        if (member is IPackFieldHolder FieldHolder)
        {
            ResolveFieldNameSpaces(FieldHolder, nameSpace);
        }
        if (member is IPackFunctionHolder FunctionHolder)
        {
            ResolveFunctionNameSpaces(FunctionHolder, nameSpace);
        }
        if (member is IPackTypeHolder TypeHolder)
        {
            ResolveTypeHolderNameSpaces(TypeHolder, nameSpace);
        }
        if (member is IOperatorOverloadHolder OperatorOverloadHolder)
        {
            ResolveOperatorOverloadNameSpaces(OperatorOverloadHolder, nameSpace);
        }
    }


    /* Resolving parent items. */
    private void ResolveParentItems(DataPack pack)
    {

    }



    /* Resolving identifiers. */
    private void ResolveTypeIdentifiers(DataPack pack)
    {
        foreach (PackMember TypeMember in pack.Types)
        {
            TypeMember.SelfIdentifier.ResolvedName = $"{TypeMember.NameSpace.SelfIdentifier.ResolvedName}" +
                $"{KGVL.NAMESPACE_SEPARATOR}{TypeMember.SelfIdentifier.SourceCodeName}";
            TypeMember.SelfIdentifier.SelfName = TypeMember.SelfIdentifier.SourceCodeName;
            TypeMember.SelfIdentifier.Target = TypeMember;
        }
    }

    private void ResolveMemberTypeIdentifiers(DataPack pack)
    {
        foreach (PackField Field in pack.Fields)
        {
            ResolveFieldType(Field);
        }
        foreach (PackFunction Function in pack.Functions.Concat(
            pack.Classes.Select(item => item.OperatorOverloads)
            .Concat(pack.Structs.Select(item => item.OperatorOverloads)).SelectMany(overload => overload)
            .Select(overload => overload.Function)))
        {
            ResolveFunctionType(Function);
        }
        foreach (PackProperty Property in pack.Properties)
        {
            ResolvePropertyType(Property);
        }
        foreach (PackIndexer Indexer in pack.Indexers)
        {
            ResolveIndexerType(Indexer);
        }
    }

    private PackMember? SearchSourceFileForTypeByCodeName(Identifier type, PackSourceFile sourceFile)
    {
        foreach (PackNameSpace WorkingNameSpace in sourceFile.Namespaces)
        {
            foreach (PackNameSpace FilterNameSpace in sourceFile.NamespaceImports.Concat(new PackNameSpace[] { WorkingNameSpace }))
            {
                PackMember? FoundType = SearchNameSpaceForTypeByCodeName(type, FilterNameSpace);
                if (FoundType != null)
                {
                    return FoundType;
                }
            }
        }
        return null;
    }

    private PackMember? SearchNameSpaceForTypeByCodeName(Identifier type, PackNameSpace nameSpace)
    {
        foreach (PackMember TypeMember in nameSpace.Types)
        {
            if ((TypeMember.SelfIdentifier.SelfName == type.SourceCodeName) 
                || (TypeMember.SelfIdentifier.ResolvedName == type.SourceCodeName))
            {
                return TypeMember;
            }
        }
        return null;
    }

    private void ResolveFunctionStatementTypes(PackFunction function)
    {

    }

    private void ResolveFunctionType(PackFunction function)
    {

    }

    private void ResolveFieldType(PackField field)
    {
        PackMember TargetType = SearchSourceFileForTypeByCodeName(field.Type, field.SourceFile) 
            ?? throw new PackContentException($"Found no suitable type for field with identifier \"{field.SelfIdentifier.SourceCodeName}\"" +
            $"for field \"{field.SelfIdentifier.SourceCodeName}\" in type \"{field.ParentItem.ResolvedName}\" in " +
            $"file \"{field.SourceFile.Path}\".");

        field.Type.Target = TargetType;
        field.Type.ResolvedName = TargetType.SelfIdentifier.ResolvedName;
        field.Type.SelfName = TargetType.SelfIdentifier.SelfName;
    }

    private void ResolvePropertyType(PackProperty property)
    {

    }

    private void ResolveIndexerType(PackIndexer indexer)
    {

    }



    // Inherited methods.
    public void ResolvePack(DataPack pack)
    {
        ArgumentNullException.ThrowIfNull(pack, nameof(pack));
        PackNameSpace[] NameSpaces = pack.NameSpaces;

        // Order of method calls here must NOT be changed, or else this will incorrectly resolve things.
        ResolveNameSpaceIdentifiers(NameSpaces);
        ResolveMemberNameSpaces(NameSpaces);
        ResolveParentItems(pack);
        ResolveTypeIdentifiers(pack);
        ResolveMemberTypeIdentifiers(pack);
    }
}