using KeigValCompiler.Semantician.Member;

namespace KeigValCompiler.Semantician.Resolver;

/* Resolves every type written in a declaration, as opposed to inside a function body: base types,
 * generic constraints, and the types of fields, properties, indexers, events, delegates, functions and
 * their parameters, together with the interface an explicit implementation names. Each is resolved by
 * pointing its identifier at the type or generic parameter it means; a type keyword such as "int"
 * resolves to the library type it stands for. A qualified name, as "KGVL.Collections.List<int>" or
 * "Outer<int>.Inner", is resolved from its first name on, each naming a namespace or a type in which the
 * next is looked for; a name standing for a namespace resolves to its full name. A name which cannot be
 * resolved is reported and left unresolved, and resolution carries on with the next. So is a type declared
 * inside another and named after it which the declaration cannot use, as C# reports it, which is decided
 * once every base type is resolved, since a protected type can be used from what derives from its holder. */
internal class SignatureResolver : IPackResolver
{
    // Private methods.
    private void ResolveMember(PackMember member, PackResolutionContext context)
    {
        switch (member)
        {
            case IPackMemberExtender Extender:
                ResolveTypes(Extender.ExtendedMembers, member, context);
                break;

            case PackField Field:
                ResolveType(Field.Type, member, context);
                break;

            case PackProperty Property:
                ResolveType(Property.Type, member, context);
                ResolveExplicitInterface(Property.ExplicitInterface, member, context);
                break;

            case PackIndexer Indexer:
                ResolveType(Indexer.Type, member, context);
                ResolveParameters(Indexer.Parameters, member, context);
                ResolveExplicitInterface(Indexer.ExplicitInterface, member, context);
                break;

            case PackEvent Event:
                ResolveType(Event.Type, member, context);
                break;

            case PackDelegate Delegate:
                ResolveOptionalType(Delegate.ReturnType, member, context);
                ResolveParameters(Delegate.Parameters, member, context);
                break;

            case PackFunction Function:
                ResolveFunctionGenericParameterNames(Function, context);
                ResolveOptionalType(Function.ReturnType, member, context);
                ResolveParameters(Function.Parameters, member, context);
                ResolveExplicitInterface(Function.ExplicitInterface, member, context);
                break;
        }

        if (member is IGenericParameterHolder GenericsHolder)
        {
            ResolveGenericConstraints(GenericsHolder, member, context);
        }
    }

    /* A function's generic parameters are named here rather than with the types', since only types are
     * named before signatures are resolved. */
    private void ResolveFunctionGenericParameterNames(PackFunction function, PackResolutionContext context)
    {
        string OwnerName = context.IdentifierGenerator.GetFullResolvedIdentifier(function);
        foreach (GenericTypeParameter Parameter in function.GenericParameters)
        {
            Parameter.SelfIdentifier.ResolvedName = context.IdentifierGenerator
                .GetGenericParameterIdentifier(OwnerName, Parameter);
            Parameter.SelfIdentifier.SelfName = Parameter.SelfIdentifier.SourceCodeName;
            Parameter.SelfIdentifier.Target = Parameter;
            Parameter.Owner = function;
        }
    }

    private void ResolveGenericConstraints(IGenericParameterHolder holder,
        PackMember member,
        PackResolutionContext context)
    {
        foreach (GenericTypeParameter Parameter in holder.GenericParameters)
        {
            foreach (GenericConstraint Constraint in Parameter.Constraints)
            {
                ResolveOptionalType(Constraint.ConstrainedItemName, member, context);
            }
        }
    }

    private void ResolveParameters(FunctionParameterCollection parameters,
        PackMember member,
        PackResolutionContext context)
    {
        foreach (FunctionParameter Parameter in parameters)
        {
            ResolveOptionalType(Parameter.Type, member, context);
        }
    }

    private void ResolveExplicitInterface(TypeTargetIdentifier? explicitInterface,
        PackMember member,
        PackResolutionContext context)
    {
        if (explicitInterface == null)
        {
            return;
        }

        ResolveType(explicitInterface, member, context);
        IIdentifiable? Target = explicitInterface.MainTarget.Target;
        if ((Target != null) && (Target is not PackInterface))
        {
            context.AddError(context.ErrorCreator.ExplicitInterfaceNotInterface.CreateOptions(
                member.SelfIdentifier.SourceCodeName, explicitInterface.ToString()), member);
        }
        else if (explicitInterface.IsArrayOrNullable)
        {
            context.AddError(context.ErrorCreator.ExplicitInterfaceWithMarkers.CreateOptions(
                member.SelfIdentifier.SourceCodeName, explicitInterface.ToString()), member);
        }
    }

    private void ResolveTypes(IEnumerable<TypeTargetIdentifier> types,
        PackMember scope,
        PackResolutionContext context)
    {
        foreach (TypeTargetIdentifier Type in types)
        {
            ResolveType(Type, scope, context);
        }
    }

    private void ResolveOptionalType(TypeTargetIdentifier? type, PackMember scope, PackResolutionContext context)
    {
        if (type != null)
        {
            ResolveType(type, scope, context);
        }
    }

    /* Type arguments first, so that each is reported on its own even when the type around it is not
     * found either. */
    private void ResolveType(TypeTargetIdentifier type, PackMember scope, PackResolutionContext context)
    {
        ResolveTypes(type.TypeArguments, scope, context);
        if (IsKeywordType(type))
        {
            ResolveKeywordType(type, scope, context);
            return;
        }

        TypeSearchResult? Result = Search(type, scope, context);
        if (Result == null)
        {
            return;
        }

        if (Result.IsFound)
        {
            SetTarget(type.MainTarget, Result.Target!);
        }
        else if (Result.IsNameSpace)
        {
            context.AddError(context.ErrorCreator.NameSpaceUsedAsType.CreateOptions(type.ToString(),
                Result.NameSpaceName!), scope);
        }
        else
        {
            ReportNotFound(type, Result, false, scope, context);
        }
    }

    /* What comes before a '.' in a type's name is resolved as a type is, but it may also name a namespace,
     * and it cannot be a generic parameter, through which no type can be named. The parser never lets a type
     * keyword stand there, as C# does not. Null when it names nothing, which has been reported. */
    private TypeSearchResult? ResolveQualifier(TypeTargetIdentifier qualifier,
        PackMember scope,
        PackResolutionContext context)
    {
        ResolveTypes(qualifier.TypeArguments, scope, context);
        TypeSearchResult? Result = Search(qualifier, scope, context);
        if (Result == null)
        {
            return null;
        }

        if (Result.IsNameSpace)
        {
            SetNameSpaceTarget(qualifier.MainTarget, Result.NameSpaceName!, context);
            return Result;
        }
        if (Result.Target is GenericTypeParameter)
        {
            context.AddError(context.ErrorCreator.GenericParameterQualifier.CreateOptions(qualifier.ToString()),
                scope);
            return null;
        }
        if (Result.IsFound)
        {
            SetTarget(qualifier.MainTarget, Result.Target!);
            return Result;
        }
        ReportNotFound(qualifier, Result, true, scope, context);
        return null;
    }

    /* Looks a name up where it is written, or when it has a qualifier, in what the qualifier names. Null
     * when the qualifier names nothing, which has been reported. */
    private TypeSearchResult? Search(TypeTargetIdentifier type, PackMember scope, PackResolutionContext context)
    {
        string Name = type.MainTarget.SourceCodeName;
        int TypeArgumentCount = type.TypeArguments.Length;
        if (type.Qualifier == null)
        {
            return context.TypeSearcher.Search(Name, TypeArgumentCount, scope);
        }

        TypeSearchResult? Qualifier = ResolveQualifier(type.Qualifier, scope, context);
        if (Qualifier == null)
        {
            return null;
        }
        if (Qualifier.IsNameSpace)
        {
            return context.TypeSearcher.SearchNameSpace(Qualifier.NameSpaceName!, Name, TypeArgumentCount, scope);
        }

        TypeSearchResult Nested = context.TypeSearcher.SearchNested((PackMember)Qualifier.Target!, Name,
            TypeArgumentCount);
        if (Nested.IsFound || (Qualifier.ClashingNameSpaceName == null))
        {
            return Nested;
        }

        /* A name found in neither the type nor the namespace of the clash is left to the clash, which has been
         * reported. */
        TypeSearchResult InNameSpace = context.TypeSearcher.SearchNameSpace(Qualifier.ClashingNameSpaceName, Name,
            TypeArgumentCount, scope);
        return (InNameSpace.IsFound || InNameSpace.IsNameSpace) ? InNameSpace : null;
    }

    /* Why a name found no type, as precisely as what was seen allows: where it was looked for, and whether a
     * type of the name was seen with another number of generic parameters, one which cannot be used, or a
     * namespace which a name with type arguments cannot mean. A qualifier could as well have named a
     * namespace. */
    private void ReportNotFound(TypeTargetIdentifier type,
        TypeSearchResult result,
        bool isQualifier,
        PackMember scope,
        PackResolutionContext context)
    {
        string Name = type.MainTarget.SourceCodeName;
        if (result.IsAmbiguous)
        {
            string Candidates = string.Join(", ", result.AmbiguousTypes.Select(
                candidate => $"\"{candidate.SelfIdentifier.ResolvedName}\""));
            context.AddError(context.ErrorCreator.AmbiguousType.CreateOptions(Name, Candidates), scope);
        }
        else if (result.InaccessibleType != null)
        {
            PackMember Inaccessible = result.InaccessibleType;
            context.AddError(context.ErrorCreator.TypeInaccessible.CreateOptions(type.ToString(),
                Inaccessible.NameSpace.SelfIdentifier.SourceCodeName + KGVL.NAMESPACE_SEPARATOR
                + Inaccessible.SelfIdentifier.SourceCodeName), scope);
        }
        else if (result.OtherGenericParameterCount != null)
        {
            context.AddError(context.ErrorCreator.WrongTypeArgumentCount.CreateOptions(type.ToString(),
                type.TypeArguments.Length, result.OtherGenericParameterCount.Value), scope);
        }
        else if (result.NameSpaceWithArguments != null)
        {
            context.AddError(context.ErrorCreator.NameSpaceWithTypeArguments.CreateOptions(type.ToString(),
                result.NameSpaceWithArguments), scope);
        }
        else if ((type.Qualifier == null) && isQualifier)
        {
            context.AddError(context.ErrorCreator.TypeOrNameSpaceNotFound.CreateOptions(Name), scope);
        }
        else if (type.Qualifier == null)
        {
            context.AddError(context.ErrorCreator.TypeNotFound.CreateOptions(Name), scope);
        }
        else if (type.Qualifier.MainTarget.Target is PackMember)
        {
            context.AddError(context.ErrorCreator.NestedTypeNotFound.CreateOptions(type.Qualifier.ToString(),
                Name), scope);
        }
        else
        {
            context.AddError(context.ErrorCreator.TypeNotInNameSpace.CreateOptions(
                type.Qualifier.MainTarget.ResolvedName!, Name), scope);
        }
    }

    private bool IsKeywordType(TypeTargetIdentifier type)
    {
        return (type.Qualifier == null) && KGVL.TYPE_KEYWORDS.Contains(type.MainTarget.SourceCodeName);
    }

    /* A keyword whose type the library does not declare is left unresolved without an error of its
     * own, since the missing library type has already been reported. */
    private void ResolveKeywordType(TypeTargetIdentifier type, PackMember scope, PackResolutionContext context)
    {
        if (type.TypeArguments.Length > 0)
        {
            context.AddError(context.ErrorCreator.WrongTypeArgumentCount.CreateOptions(type.ToString(),
                type.TypeArguments.Length, 0), scope);
            return;
        }

        PackMember? KeywordType = context.Registry.GetTypeByKeyword(type.MainTarget.SourceCodeName);
        if (KeywordType != null)
        {
            SetTarget(type.MainTarget, KeywordType);
        }
    }

    /* The types a declaration names through the types holding them, which it may not be able to use. The types
     * around a use, and a namespace's types, are only ever found where the use can see them. */
    private void CheckUsableTypes(PackMember member, PackResolutionContext context)
    {
        IEnumerable<TypeTargetIdentifier> OwnBases = (member as IPackMemberExtender)?.ExtendedMembers
            ?? Enumerable.Empty<TypeTargetIdentifier>();
        foreach (TypeTargetIdentifier Written in MemberRelations.GetWrittenTypes(member))
        {
            CheckUsable(Written, member, OwnBases.Contains(Written), context);
        }
    }

    /* Whether a written type, or one before its '.', is a type the declaration cannot use, which leaves the
     * written type unresolved. Only the first such name before a '.' is reported, as the names after it are
     * looked for in it, but each type argument is a name of its own. */
    private bool CheckUsable(TypeTargetIdentifier written,
        PackMember member,
        bool isInOwnBaseList,
        PackResolutionContext context)
    {
        bool IsUnusable = (written.Qualifier != null)
            && CheckUsable(written.Qualifier, member, isInOwnBaseList, context);
        foreach (TypeTargetIdentifier Argument in written.TypeArguments)
        {
            CheckUsable(Argument, member, isInOwnBaseList, context);
        }

        if (!IsUnusable && (written.Qualifier?.MainTarget.Target is PackMember Holder)
            && (written.MainTarget.Target is PackMember Nested)
            && !AccessDomains.IsAccessibleFrom(Nested, member, isInOwnBaseList, context))
        {
            string NestedName = Holder.SelfIdentifier.SourceCodeName + KGVL.NAMESPACE_SEPARATOR
                + Nested.SelfIdentifier.SourceCodeName;
            context.AddError(context.ErrorCreator.NestedTypeInaccessible.CreateOptions(NestedName,
                MemberRelations.GetKindName(member), MemberRelations.GetDisplayName(member),
                ModifierKeywords.FormatAccess(MemberRelations.GetEffectiveAccess(Nested)),
                Holder.SelfIdentifier.SourceCodeName), member);
            IsUnusable = true;
        }
        if (IsUnusable)
        {
            written.MainTarget.Target = null;
            written.MainTarget.ResolvedName = null;
        }
        return IsUnusable;
    }

    private void SetTarget(Identifier identifier, IIdentifiable target)
    {
        identifier.Target = target;
        identifier.ResolvedName = target.SelfIdentifier.ResolvedName;
        identifier.SelfName = identifier.SourceCodeName;
    }

    /* A name standing for a namespace, and the namespace itself where a file declares it, which one existing
     * only for the namespaces inside it is not. */
    private void SetNameSpaceTarget(Identifier identifier, string nameSpaceName, PackResolutionContext context)
    {
        identifier.Target = context.Pack.TryGetNamespace(nameSpaceName);
        identifier.ResolvedName = nameSpaceName;
        identifier.SelfName = identifier.SourceCodeName;
    }


    // Inherited methods.
    public void ResolvePack(PackResolutionContext context)
    {
        ArgumentNullException.ThrowIfNull(context, nameof(context));

        foreach (PackMember Member in context.Pack.Members)
        {
            ResolveMember(Member, context);
        }
        foreach (PackMember Member in context.Pack.Members)
        {
            CheckUsableTypes(Member, context);
        }
    }
}
