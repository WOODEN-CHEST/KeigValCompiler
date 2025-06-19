using KeigValCompiler.Semantician.Member.Code;
using KeigValCompiler.Semantician.Member;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KeigValCompiler.Semantician.Resolver;

internal class IdentifierResolver : IPackResolver
{
    // Private methods.
    /* Resolving types. */
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


    /* Resolving Type identifiers. */
    private void ResolveMemberTypeIdentifiers(PackResolutionContext context)
    {
        foreach (PackField Field in context.Pack.Fields)
        {
            ResolveFieldType(Field, context);
        }
        foreach (PackFunction Function in context.Pack.Functions)
        {
            ResolveFunctionType(Function, context);
        }
        foreach (PackProperty Property in context.Pack.Properties)
        {
            ResolvePropertyType(Property, context);
        }
        foreach (PackIndexer Indexer in context.Pack.Indexers)
        {
            ResolveIndexerType(Indexer, context);
        }
        foreach (PackEvent Event in context.Pack.Events)
        {
            ResolveEventType(Event, context);
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

    private string GetNoPrimitiveStatementTypeMessage(PackFunction function, SourceFileOrigin origin)
    {
        StringBuilder Builder = new();

        Builder.Append($"Found no suitable type for primitive statement" +
            $"in function \"{function.SelfIdentifier.SourceCodeName}\" in ");
        if (function.ParentItem?.Target is IPackType)
        {
            Builder.Append($"type {function.ParentItem.Target.SelfIdentifier.ResolvedName}");
        }
        else
        {
            Builder.Append($"namespace {function.NameSpace.SelfIdentifier.ResolvedName}");
        }
        Builder.Append($" in file \"{function.SourceFile.Path}\" on line {origin.Line.ToString()}.");

        return Builder.ToString();
    }

    private void ResolveFieldType(PackField field, PackResolutionContext context)
    {
        PackMember TargetType = context.IdentifierSearcher.GetTypeFromCodeName(
            field.Type.SourceCodeName, field.SourceFile, context.Registry)
            ?? throw new PackContentException(GetNoSuitableTypeMessage("field", field));

        field.Type.ResolveFrom(TargetType.SelfIdentifier);
    }

    private void ResolvePropertyType(PackProperty property, PackResolutionContext context)
    {
        PackMember TargetType = context.IdentifierSearcher.GetTypeFromCodeName(
            property.Type.SourceCodeName, property.SourceFile, context.Registry)
            ?? throw new PackContentException(GetNoSuitableTypeMessage("property", property));

        property.Type.ResolveFrom(TargetType.SelfIdentifier);

        foreach (PackFunction? Function in new PackFunction?[] { property.GetFunction, property.SetFunction, property.InitFunction })
        {
            if (Function != null)
            {
                Function.ReturnType.ResolveFrom(TargetType.SelfIdentifier);
                ResolveFunctionStatementTypes(Function, context);
            }
        }
    }

    private void ResolveIndexerType(PackIndexer indexer, PackResolutionContext context)
    {
        PackMember TargetType = context.IdentifierSearcher.GetTypeFromCodeName(
            indexer.Type.SourceCodeName, indexer.SourceFile, context.Registry)
            ?? throw new PackContentException(GetNoSuitableTypeMessage("indexer", indexer));

        indexer.Type.ResolveFrom(TargetType.SelfIdentifier);

        foreach (PackFunction? Function in new PackFunction?[] { indexer.GetFunction, indexer.SetFunction })
        {
            if (Function != null)
            {
                Function.ReturnType.ResolveFrom(TargetType.SelfIdentifier);
                ResolveFunctionStatementTypes(Function, context);
            }
        }
    }

    private void ResolveEventType(PackEvent packEvent, PackResolutionContext context)
    {
        PackMember TargetType = context.IdentifierSearcher.GetTypeFromCodeName(
            packEvent.Type.SourceCodeName, packEvent.SourceFile, context.Registry)
            ?? throw new PackContentException(GetNoSuitableTypeMessage("event", packEvent));

        packEvent.Type.ResolveFrom(TargetType.SelfIdentifier);
    }

    private void ResolveFunctionType(PackFunction function, PackResolutionContext context)
    {
        PackMember TargetType = context.IdentifierSearcher.GetTypeFromCodeName(
            function.ReturnType.SourceCodeName, function.SourceFile, context.Registry)
            ?? throw new PackContentException(GetNoSuitableTypeMessage("function", function));

        function.ReturnType.ResolveFrom(TargetType.SelfIdentifier);
    }

    private void ResolveFunctionStatementTypes(PackFunction function, PackResolutionContext context)
    {
        foreach (Statement FunctionStatement in function.Statements)
        {
            ResolveTypeOfStatement(function, FunctionStatement, context);
        }
    }

    private void ResolveTypeOfStatement(PackFunction function, Statement statement, PackResolutionContext context)
    {
        if (statement is PrimitiveValueStatement ValueStatement)
        {
            ResolvePrimitiveValueStatementType(function, ValueStatement, context);
        }
    }

    private void ResolvePrimitiveValueStatementType(PackFunction function,
        PrimitiveValueStatement statement,
        PackResolutionContext context)
    {
        PackMember? TargetType = context.PrimitiveResolver.GetTypeOfValue(statement.Value, context.Pack, context.Registry);
        if (TargetType == null)
        {
            throw new PackContentException(GetNoPrimitiveStatementTypeMessage(function, statement.Origin));
        }
        statement.StatementReturnType = new(TargetType.SelfIdentifier);
    }


    /* Resolving identifiers. */
    private void ResolveMemberIdentifiers(DataPack pack)
    {
        foreach (PackProperty Property in pack.Properties)
        {
            ResolvePropertyIdentifiers(Property);
        }

        foreach (PackMember Member in pack.Members)
        {
            if (Member is IPackType)
            {
                continue;
            }


        }
    }

    private void ResolvePropertyIdentifiers(PackProperty Property)
    {

    }

    private void ResolveIndexerIdentifiers(PackProperty Property)
    {

    }

    private void ResolveFieldIdentifiers(PackProperty Property)
    {

    }

    private void ResolveFunctionIdentifiers(PackProperty Property)
    {

    }


    // Inherited methods.
    public void ResolvePack(PackResolutionContext context)
    {
        ResolveTypeIdentifiers(context.Pack);
        ResolveMemberTypeIdentifiers(context);
        ResolveMemberIdentifiers(context.Pack);
    }
}