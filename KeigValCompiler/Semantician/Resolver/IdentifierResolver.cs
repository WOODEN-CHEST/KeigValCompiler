using KeigValCompiler.Semantician.Member;
using KeigValCompiler.Semantician.Member.Code;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Runtime.InteropServices.JavaScript.JSType;

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
    /* First step is for all types to resolve their self identifiers. */
    private void ResolveTypeSelfIdentifiers(DataPack pack)
    {
        foreach (PackNameSpace NameSpace in pack.NameSpaces)
        {
            foreach (IPackType Type in NameSpace.Members)
            {
                ResolveIdentifiersForSingleType(Type);
            }
        }

        foreach (PackMember TypeMember in pack.Types)
        {
            TypeMember.SelfIdentifier.ResolvedName = _identifierGenerator.GetFullResolvedIdentifier(TypeMember);
            TypeMember.SelfIdentifier.SelfName = _identifierGenerator.GetSelfName(TypeMember.SelfIdentifier);
            TypeMember.SelfIdentifier.Target = TypeMember;
        }
    }

    private void ResolveIdentifiersForSingleType(IPackType type)
    {
        if (type is not PackMember Member)
        {
            return;
        }

        Member.SelfIdentifier.ResolvedName = _identifierGenerator.GetFullResolvedIdentifier(Member);
        Member.SelfIdentifier.SelfName = _identifierGenerator.GetSelfName(Member.SelfIdentifier);
        Member.SelfIdentifier.Target = Member;

        if (Member is IPackTypeHolder TypeHolderMember)
        {
            foreach (PackMember SubType in TypeHolderMember.Types)
            {
                ResolveIdentifiersForSingleType((IPackType)SubType);
            }
        }
    }


    /* Second step is o resolve the identifiers for members which point to a type. */
    private void ResolveMemberTypeIdentifiers(PackResolutionContext context)
    {
        foreach (PackField Field in context.Pack.Fields)
        {
            ResolveFieldType(Field, context);
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
        foreach (PackFunction Function in context.Pack.Functions)
        {
            ResolveFunctionType(Function, context);
        }
        foreach (PackDelegate Delegate in  context.Pack.Delegates)
        {
            ResolveDelegateType(Delegate, context);
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
        if (property.SetFunction != null)
        {
            property.SetFunction.ReturnType = null;
        }
        if (property.InitFunction != null)
        {
            property.InitFunction.ReturnType = null;
        }
    }

    private void ResolveIndexerType(PackIndexer indexer, PackResolutionContext context)
    {
        PackMember TargetType = context.IdentifierSearcher.GetTypeFromCodeName(
            indexer.Type.SourceCodeName, indexer.SourceFile, context.Registry)
            ?? throw new PackContentException(GetNoSuitableTypeMessage("indexer", indexer));

        indexer.Type.ResolveFrom(TargetType.SelfIdentifier);

        ResolveParameterTypes(indexer.Parameters, indexer, "indexer parameter", context);

        if (indexer.GetFunction != null)
        {
            indexer.GetFunction.ReturnType = new(TargetType.SelfIdentifier);
            indexer.GetFunction.Parameters.SetFrom(indexer.Parameters);
        }
        if (indexer.SetFunction != null)
        {
            indexer.SetFunction.ReturnType = null;
            indexer.SetFunction.Parameters.SetFrom(indexer.Parameters);
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

        ResolveParameterTypes(function.Parameters, function, "function parameter", context);
    }

    private void ResolveDelegateType(PackDelegate targetDelegate, PackResolutionContext context)
    {
        if (targetDelegate.ReturnType != null)
        {
            PackMember ReturnType = context.IdentifierSearcher.GetTypeFromCodeName(
                targetDelegate.ReturnType.SourceCodeName, targetDelegate.SourceFile, context.Registry)
                ?? throw new PackContentException(GetNoSuitableTypeMessage("delegate", targetDelegate));

            targetDelegate.ReturnType.ResolveFrom(ReturnType.SelfIdentifier);
        }

        ResolveParameterTypes(targetDelegate.Parameters, targetDelegate, "delegate parameter", context);
    }

    private void ResolveParameterTypes(FunctionParameterCollection parameters,
        PackMember member,
        string noSuitableTypeMessageName,
        PackResolutionContext context)
    {
        foreach (FunctionParameter Parameter in parameters)
        {
            PackMember ParameterType = context.IdentifierSearcher.GetTypeFromCodeName(
                Parameter.Type.SourceCodeName, member.SourceFile, context.Registry)
                ?? throw new PackContentException(GetNoSuitableTypeMessage(noSuitableTypeMessageName, member));

            Parameter.Type.ResolveFrom(ParameterType.SelfIdentifier);
        }
    }


    /* Resolving generic member identifiers. */
    private void ResolveGenericMemberIdentifiers(PackResolutionContext context)
    {
        foreach (PackMember Member in Enumerable.Empty<PackMember>()
            .Concat(context.Pack.Fields).Concat(context.Pack.Properties).Concat(context.Pack.Events))
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
            ResolveParameterSelfIdentifiers(Function.Parameters);
        }

        foreach (PackProperty Property in context.Pack.Properties)
        {
            foreach (PackFunction? Function in new PackFunction?[] 
                { Property.GetFunction, Property.SetFunction, Property.InitFunction })
            {
                ResolvePropertyFunctionName(Property, Function);
                ResolveParameterSelfIdentifiers(Function?.Parameters);
            }
        }

        foreach (PackIndexer Indexer in context.Pack.Indexers)
        {
            foreach (PackFunction? Function in new PackFunction?[] { Indexer.GetFunction, Indexer.SetFunction })
            {
                ResolveIndexerFunctionName(Indexer, Function);
                ResolveParameterSelfIdentifiers(Function?.Parameters);
            }
        }

        foreach (PackMember Member in context.Pack.Members)
        {
            if (Member is not IOperatorOverloadHolder OverloadHolder)
            {
                continue;
            }

            ResolveOperatorOverloadFunctions(Member, OverloadHolder.OperatorOverloads);
        }
    }

    private void ResolvePropertyFunctionName(PackProperty property, PackFunction? function)
    {
        if (function == null)
        {
            return;
        }

        function.SelfIdentifier.ResolvedName = _identifierGenerator.GetPropertyFunctionIdentifier(property, function);
        function.SelfIdentifier.SelfName = _identifierGenerator.GetSelfName(function.SelfIdentifier);
        function.SelfIdentifier.Target = function;
    }

    private void ResolveIndexerFunctionName(PackIndexer indexer, PackFunction? function)
    {
        if (function == null)
        {
            return;
        }

        function.SelfIdentifier.ResolvedName = _identifierGenerator.GetIndexerFunctionIdentifier(indexer, function);
        function.SelfIdentifier.SelfName = _identifierGenerator.GetSelfName(function.SelfIdentifier);
        function.SelfIdentifier.Target = function;
    }

    private void ResolveParameterSelfIdentifiers(FunctionParameterCollection? parameters)
    {
        if (parameters == null)
        {
            return;
        }

        foreach (FunctionParameter Parameter in parameters)
        {
            Parameter.SelfIdentifier.SelfName = Parameter.SelfIdentifier.SourceCodeName;
            Parameter.SelfIdentifier.ResolvedName = Parameter.SelfIdentifier.SelfName;
            Parameter.SelfIdentifier.Target = Parameter;
        }
    }

    private void ResolveOperatorOverloadFunctions(PackMember member, OperatorOverloadCollection overloads)
    {
        foreach (OperatorOverload Overload in overloads)
        {
            Identifier FunctionIdentifier = Overload.Function.SelfIdentifier;

            FunctionIdentifier.ResolvedName = _identifierGenerator
                .GetOperatorOverloadFunctionName(Overload, member.SelfIdentifier);
            FunctionIdentifier.SelfName = _identifierGenerator.GetSelfName(FunctionIdentifier);
            FunctionIdentifier.Target = Overload.Function;
        }
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
        else if (statement is MemberRetrieveStatement TargetRetrieveStatement)
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
        MemberRetrieveStatement statement,
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
        ResolveTypeSelfIdentifiers(context.Pack);
        ResolveMemberTypeIdentifiers(context);
        ResolveGenericMemberIdentifiers(context);
        ResolveFunctionIdentifiers(context);
    }
}