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
    // Private fields.
    private readonly IdentifierGenerator _identifierGenerator;


    // Constructors.
    public IdentifierResolver(IdentifierGenerator identifierGenerator)
    {
        _identifierGenerator = identifierGenerator ?? throw new ArgumentNullException(nameof(identifierGenerator));
    }


    // Private methods.
    /* Resolving types. */
    private void ResolveTypeIdentifiers(DataPack pack)
    {
        foreach (PackMember TypeMember in pack.Types)
        {
            TypeMember.SelfIdentifier.ResolvedName = _identifierGenerator.GetFullResolvedIdentifier(TypeMember);
            TypeMember.SelfIdentifier.SelfName = _identifierGenerator.GetSelfName(TypeMember.SelfIdentifier);
            TypeMember.SelfIdentifier.Target = TypeMember;
        }
    }


    /* Resolving member type identifiers. */
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
        Builder.Append($" in file \"{member.SourceFile.Path}\" on line {member.SourceFileOrigin.Line}.");

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

        if (property.GetFunction != null)
        {
            property.GetFunction.ReturnType = new(TargetType.SelfIdentifier);
        }
    }

    private void ResolveIndexerType(PackIndexer indexer, PackResolutionContext context)
    {
        PackMember TargetType = context.IdentifierSearcher.GetTypeFromCodeName(
            indexer.Type.SourceCodeName, indexer.SourceFile, context.Registry)
            ?? throw new PackContentException(GetNoSuitableTypeMessage("indexer", indexer));

        indexer.Type.ResolveFrom(TargetType.SelfIdentifier);

        if (indexer.GetFunction != null)
        {
            indexer.GetFunction.ReturnType = new(TargetType.SelfIdentifier);
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
        if (function.ReturnType != null)
        {
            PackMember ReturnType = context.IdentifierSearcher.GetTypeFromCodeName(
            function.ReturnType.SourceCodeName, function.SourceFile, context.Registry)
            ?? throw new PackContentException(GetNoSuitableTypeMessage("function", function));

            function.ReturnType.ResolveFrom(ReturnType.SelfIdentifier);
        }
    }


    /* Resolving field and property identifiers. */
    private void ResolveFieldAndPropertydIdentifiers(PackResolutionContext context)
    {
        foreach (PackMember Member in Enumerable.Empty<PackMember>()
            .Concat(context.Pack.Fields).Concat(context.Pack.Properties))
        {
            Member.SelfIdentifier.ResolvedName = _identifierGenerator.GetFullResolvedIdentifier(Member);
            Member.SelfIdentifier.SelfName = _identifierGenerator.GetSelfName(Member.SelfIdentifier);
            Member.SelfIdentifier.Target = Member;
        }
    }


    /* Resolving function identifiers. */
    private void ResolveFunctionIdentifiers(PackResolutionContext context)
    {
        foreach (PackFunction Function in context.Pack.Functions)
        {
            Function.SelfIdentifier.ResolvedName = _identifierGenerator.GetFullyResolvedFunctionIdentifier(Function);
            Function.SelfIdentifier.SelfName = _identifierGenerator.GetSelfName(Function.SelfIdentifier);
            Function.SelfIdentifier.Target = Function;
        }
        foreach ()
    }


    /* Code identifier resolving. */
    private void ResolveCodeIdentifiers(PackResolutionContext context)
    {

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
        // Woo!
        if (statement is CastStatement TargetCastStatement)
        {
            ResolveCastStatementType(function, TargetCastStatement, context);
        }
        else if (statement is ConstructorCallStatement TargetConstructorStatement)
        {
            ResolveConstructorCallStatement(function, TargetConstructorStatement, context);
        }
        else if (statement is ForStatement TargetForStatement)
        {
            ResolveForStatement(function, TargetForStatement, context);
        }
        else if (statement is IfStatement TargetIfStatement)
        {
            ResolveIfStatement(function, TargetIfStatement, context);
        }
        else if (statement is NamedFunctionCallStatement TargetFunctionCallStatement)
        {
            ResolveNamedFunctionCallStatement(function, TargetFunctionCallStatement, context);
        }
        else if (statement is PrimitiveValueStatement TargetValueStatement)
        {
            ResolvePrimitiveValueStatementType(function, TargetValueStatement, context);
        }
        else if (statement is OperatorStatement TargetOperatorStatement)
        {
            ResolveOperatorStatement(function, TargetOperatorStatement, context);
        }
        else if (statement is TernaryStatement TargetTernaryStatement)
        {
            ResolveTernaryStatement(function, TargetTernaryStatement, context);
        }
        else if (statement is VariableAssignmentStatement TargetAssignmentStatement)
        {
            ResolveVariableAssignmentStatement(function, TargetAssignmentStatement, context);
        }
        else if (statement is VariableRetrieveStatement TargetRetrieveStatement)
        {
            ResolveVariableRetrieveStatement(function, TargetRetrieveStatement, context);
        }
        else if (statement is WhileStatement TargetWhileStatement)
        {
            ResolveWhileStatement(function, TargetWhileStatement, context);
        }
        else
        {
            throw new PackContentException($"internal error, invalid statement type: \"{statement.GetType().FullName}\"");
        }

        foreach (Statement SubStatement in statement.SubStatements)
        {
            ResolveTypeOfStatement(function, SubStatement, context);
        }
    }

    private void ResolveCastStatementType(PackFunction function,
        CastStatement statement,
        PackResolutionContext context)
    {
        PackMember ReturnType = context.IdentifierSearcher.GetTypeFromCodeName(
                statement.TargetCastType.SelfName, function.SourceFile, context.Registry)
                ?? throw new PackContentException(GetNoSuitableTypeMessage("cast statement", function));

        statement.TargetCastType.ResolveFrom(ReturnType.SelfIdentifier);
        statement.StatementReturnType = new(ReturnType.SelfIdentifier);
    }

    private void ResolveConstructorCallStatement(PackFunction function,
        ConstructorCallStatement statement,
        PackResolutionContext context)
    {
        PackMember ReturnType = context.IdentifierSearcher.GetTypeFromCodeName(
            statement.ObjectType.SelfName, function.SourceFile, context.Registry)
            ?? throw new PackContentException(GetNoSuitableTypeMessage("constructor statement", function));

        statement.ObjectType.ResolveFrom(ReturnType.SelfIdentifier);
        statement.StatementReturnType = new(ReturnType.SelfIdentifier);
    }

    private void ResolveForStatement(PackFunction function,
        ForStatement statement,
        PackResolutionContext context)
    { }

    private void ResolveIfStatement(PackFunction function,
        IfStatement statement,
        PackResolutionContext context)
    { }

    private void ResolveNamedFunctionCallStatement(PackFunction function,
        NamedFunctionCallStatement statement,
        PackResolutionContext context)
    {

    }

    private void ResolveOperatorStatement(PackFunction function,
        OperatorStatement statement,
        PackResolutionContext context)
    {

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

    private void ResolveTernaryStatement(PackFunction function,
        TernaryStatement statement,
        PackResolutionContext context)
    {

    }

    private void ResolveVariableAssignmentStatement(PackFunction function,
        VariableAssignmentStatement statement,
        PackResolutionContext context)
    {

    }

    private void ResolveVariableRetrieveStatement(PackFunction function,
        VariableRetrieveStatement statement,
        PackResolutionContext context)
    {

    }

    private void ResolveWhileStatement(PackFunction function,
        WhileStatement statement,
        PackResolutionContext context)
    {

    }

    private void ResolveFunctionArgumentTypes(PackFunction function,
        FunctionCallStatement statement,
        PackResolutionContext context)
    {
        foreach (Statement ArgStatement in statement.Arguments)
        {
            ResolveTypeOfStatement(function, statement, context);
        }
    }



    // Inherited methods.
    public void ResolvePack(PackResolutionContext context)
    {
        ResolveTypeIdentifiers(context.Pack);
        ResolveMemberTypeIdentifiers(context);
        ResolveFieldAndPropertydIdentifiers(context);
        ResolveFunctionIdentifiers(context);
        ResolveCodeIdentifiers(context);
    }
}