using KeigValCompiler.Semantician.Member;

namespace KeigValCompiler.Semantician.Resolver;

/* Resolves every type written in a declaration, as opposed to inside a function body: base types,
 * generic constraints, and the types of fields, properties, indexers, events, delegates, functions and
 * their parameters, together with the interface an explicit implementation names. Each is resolved by
 * pointing its identifier at the type or generic parameter it means; a type keyword such as "int"
 * resolves to the library type it stands for. A name which cannot be resolved is reported and left
 * unresolved, and resolution carries on with the next. */
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

        string Name = type.MainTarget.SourceCodeName;
        if (KGVL.TYPE_KEYWORDS.Contains(Name))
        {
            ResolveKeywordType(type, scope, context);
            return;
        }

        TypeSearchResult Result = context.TypeSearcher.Search(Name, type.TypeArguments.Length, scope);
        if (Result.IsFound)
        {
            SetTarget(type.MainTarget, Result.Target!);
        }
        else if (Result.IsAmbiguous)
        {
            string Candidates = string.Join(", ", Result.AmbiguousTypes.Select(
                candidate => $"\"{candidate.SelfIdentifier.ResolvedName}\""));
            context.AddError(context.ErrorCreator.AmbiguousType.CreateOptions(Name, Candidates), scope);
        }
        else if (Result.InaccessibleType != null)
        {
            PackMember Inaccessible = Result.InaccessibleType;
            context.AddError(context.ErrorCreator.TypeInaccessible.CreateOptions(Name,
                Inaccessible.NameSpace.SelfIdentifier.SourceCodeName + KGVL.NAMESPACE_SEPARATOR
                + Inaccessible.SelfIdentifier.SourceCodeName), scope);
        }
        else if (Result.OtherGenericParameterCount != null)
        {
            context.AddError(context.ErrorCreator.WrongTypeArgumentCount.CreateOptions(type.ToString(),
                type.TypeArguments.Length, Result.OtherGenericParameterCount.Value), scope);
        }
        else
        {
            context.AddError(context.ErrorCreator.TypeNotFound.CreateOptions(Name), scope);
        }
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

    private void SetTarget(Identifier identifier, IIdentifiable target)
    {
        identifier.Target = target;
        identifier.ResolvedName = target.SelfIdentifier.ResolvedName;
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
    }
}
