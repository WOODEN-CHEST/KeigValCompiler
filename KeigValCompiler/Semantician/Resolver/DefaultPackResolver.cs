using KeigValCompiler.Semantician.Member;
using System.Globalization;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System;
using KeigValCompiler.Semantician.Member.Code;
using Microsoft.Win32;

namespace KeigValCompiler.Semantician.Resolver;

internal class DefaultPackResolver : IPackResolver
{



    // Private methods.
    /* Resolving namespaces. */
    private void ResolveNameSpaceIdentifiers(IEnumerable<PackNameSpace> nameSpaces)
    {
        foreach (PackNameSpace NameSpace in nameSpaces)
        {
            string Name = NameSpace.SelfIdentifier.SourceCodeName;
            NameSpace.SelfIdentifier.Target = NameSpace;

            NameSpace.SelfIdentifier.ResolvedName = Name;
            NameSpace.SelfIdentifier.SelfName = Name.Substring(Name.LastIndexOf(KGVL.NAMESPACE_SEPARATOR) + 1);
        }
    }

    private void ResolveMemberNameSpaces(IEnumerable<PackNameSpace> nameSpaces)
    {
        foreach (PackNameSpace NameSpace in nameSpaces)
        {
            foreach (PackMember Member in NameSpace.AllMembers)
            {
                Member.NameSpace = NameSpace;
            }
        }
    }


    /* Resolving parent items. */
    private void ResolveParentItems(IEnumerable<PackNameSpace> nameSpaces)
    {
        foreach (PackNameSpace NameSpace in nameSpaces)
        {
            foreach (PackMember Member in NameSpace.Members)
            {
                ResolveParentItemsOfMember(Member, null);
            }
        }
    }

    private void ResolveParentItemsOfMember(PackMember member, Identifier? parentItem)
    {
        member.ParentItem = parentItem;

        foreach (PackMember SubMember in member.SubMembers)
        {
            ResolveParentItemsOfMember(SubMember, member.SelfIdentifier);
        }
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

    private void ResolveMemberTypeIdentifiers(DataPack pack, BuiltInTypeRegistry registry)
    {
        foreach (PackField Field in pack.Fields)
        {
            ResolveFieldType(Field, registry);
        }
        foreach (PackFunction Function in pack.Functions)
        {
            ResolveFunctionType(Function, registry);
        }
        foreach (PackProperty Property in pack.Properties)
        {
            ResolvePropertyType(Property, registry);
        }
        foreach (PackIndexer Indexer in pack.Indexers)
        {
            ResolveIndexerType(Indexer, registry);
        }
        foreach (PackEvent Event in pack.Events)
        {
            ResolveEventType(Event, registry);
        }
    }



    private string GetNoSuitableTypeMessage(string memberTypeName, PackMember member)
    {
        StringBuilder Builder = new();

        Builder.Append($"Found no suitable type for {memberTypeName}" +
            $" with identifier \"{member.SelfIdentifier.SourceCodeName}\" in ");
        if (member.ParentItem?.Target is IPackType)
        {
            Builder.Append($"type {member.ParentItem.Target.SelfIdentifier.ResolvedName}");
        }
        else
        {
            Builder.Append($"namespace {member.NameSpace.SelfIdentifier.ResolvedName}");
        }
        Builder.Append($" in file \"{member.SourceFile.Path}\" on line {member.SourceFileOrigin.Line.ToString()}.");

        return Builder.ToString();
    }

    private void ResolveFieldType(PackField field, BuiltInTypeRegistry registry)
    {
        PackMember TargetType = SearchSourceFileForTypeByCodeName(field.Type, field.SourceFile, registry)
            ?? throw new PackContentException(GetNoSuitableTypeMessage("field", field));

        field.Type.ResolveFrom(TargetType.SelfIdentifier);
    }

    private void ResolvePropertyType(PackProperty property, BuiltInTypeRegistry registry)
    {
        PackMember TargetType = SearchSourceFileForTypeByCodeName(property.Type, property.SourceFile, registry)
            ?? throw new PackContentException(GetNoSuitableTypeMessage("property", property));

        property.Type.ResolveFrom(TargetType.SelfIdentifier);

        foreach (PackFunction? Function in new PackFunction?[] { property.GetFunction, property.SetFunction, property.InitFunction })
        {
            if (Function != null)
            {
                Function.ReturnType.ResolveFrom(TargetType.SelfIdentifier);
                ResolveFunctionStatementTypes(Function, registry);
            }
        }
    }

    private void ResolveIndexerType(PackIndexer indexer, BuiltInTypeRegistry registry)
    {
        PackMember TargetType = SearchSourceFileForTypeByCodeName(indexer.Type, indexer.SourceFile, registry)
            ?? throw new PackContentException(GetNoSuitableTypeMessage("indexer", indexer));

        indexer.Type.ResolveFrom(TargetType.SelfIdentifier);

        foreach (PackFunction? Function in new PackFunction?[] { indexer.GetFunction, indexer.SetFunction })
        {
            if (Function != null)
            {
                Function.ReturnType.ResolveFrom(TargetType.SelfIdentifier);
                ResolveFunctionStatementTypes(Function, registry);
            }
        }
    }

    private void ResolveEventType(PackEvent packEvent, BuiltInTypeRegistry registry)
    {
        PackMember TargetType = SearchSourceFileForTypeByCodeName(packEvent.Type, packEvent.SourceFile, registry)
            ?? throw new PackContentException(GetNoSuitableTypeMessage("event", packEvent));

        packEvent.Type.ResolveFrom(TargetType.SelfIdentifier);
    }

    private void ResolveFunctionType(PackFunction function, BuiltInTypeRegistry registry)
    {
        PackMember TargetType = SearchSourceFileForTypeByCodeName(function.ReturnType, function.SourceFile, registry)
            ?? throw new PackContentException(GetNoSuitableTypeMessage("function", function));

        function.ReturnType.ResolveFrom(TargetType.SelfIdentifier);
    }

    private void ResolveFunctionStatementTypes(PackFunction function, BuiltInTypeRegistry registry)
    {
        foreach (Statement FunctionStatement in function.Statements)
        {
            ResolveTypeOfStatement(FunctionStatement, registry);
        }
    }

    private void ResolveTypeOfStatement(Statement statement, BuiltInTypeRegistry registry)
    {
        statement.StatementReturnType

        if (statement is PrimitiveValueStatement ValueStatement)
        {
            ValueStatement.StatementReturnType                                                                                                                                          
        }
    }


    // Inherited methods.
    public void ResolvePack(DataPack pack, BuiltInTypeRegistry registry)
    {
        ArgumentNullException.ThrowIfNull(pack, nameof(pack));
        IEnumerable<PackNameSpace> NameSpaces = pack.NameSpaces;

        // Order of method calls here must NOT be changed, or else this will incorrectly resolve things.
        ResolveNameSpaceIdentifiers(NameSpaces);
        ResolveMemberNameSpaces(NameSpaces);
        ResolveParentItems(NameSpaces);
        ResolveTypeIdentifiers(pack);
        ResolveMemberTypeIdentifiers(pack, registry);
    }
}