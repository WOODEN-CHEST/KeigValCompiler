using KeigValCompiler.Semantician.Member;

namespace KeigValCompiler.Semantician.Resolver;

/* Names every member which is not a type, now that the types in their signatures are resolved: a
 * function's name carries its parameter types, so that overloads stay apart. Accessors and operator
 * functions are named by what holds them, a property or indexer, or the type declaring the operator. */
internal class MemberIdentifierResolver : IPackResolver
{
    // Private methods.
    private void ResolveHolder(object holder, PackResolutionContext context)
    {
        if (holder is IPackFieldHolder FieldHolder)
        {
            foreach (PackField Field in FieldHolder.Fields)
            {
                SetIdentifier(Field, context.IdentifierGenerator.GetFullResolvedIdentifier(Field));
            }
        }
        if (holder is IPackEventHolder EventHolder)
        {
            foreach (PackEvent Event in EventHolder.Events)
            {
                SetIdentifier(Event, context.IdentifierGenerator.GetFullResolvedIdentifier(Event));
            }
        }
        if (holder is IPackFunctionHolder FunctionHolder)
        {
            ResolveFunctionHolder(FunctionHolder, context);
        }
        if ((holder is IOperatorOverloadHolder OverloadHolder) && (holder is IIdentifiable OverloadOwner))
        {
            foreach (OperatorOverload Overload in OverloadHolder.OperatorOverloads)
            {
                SetFunctionIdentifier(Overload.Function, context.IdentifierGenerator
                    .GetOperatorOverloadFunctionName(Overload, OverloadOwner.SelfIdentifier));
            }
        }
        if (holder is IPackTypeHolder TypeHolder)
        {
            foreach (PackMember Type in TypeHolder.Types)
            {
                ResolveHolder(Type, context);
            }
        }
    }

    private void ResolveFunctionHolder(IPackFunctionHolder holder, PackResolutionContext context)
    {
        foreach (PackFunction Function in holder.Functions)
        {
            SetFunctionIdentifier(Function,
                context.IdentifierGenerator.GetFullyResolvedFunctionIdentifier(Function));
        }

        foreach (PackProperty Property in holder.Properties)
        {
            SetIdentifier(Property, context.IdentifierGenerator.GetFullResolvedIdentifier(Property));
            foreach (PackFunction? Accessor in new PackFunction?[]
                { Property.GetFunction, Property.SetFunction, Property.InitFunction })
            {
                if (Accessor != null)
                {
                    SetFunctionIdentifier(Accessor,
                        context.IdentifierGenerator.GetPropertyFunctionIdentifier(Property, Accessor));
                }
            }
        }

        foreach (PackIndexer Indexer in holder.Indexers)
        {
            SetIdentifier(Indexer, context.IdentifierGenerator.GetFullyResolvedIndexerIdentifier(Indexer));
            SetParameterIdentifiers(Indexer.Parameters);
            foreach (PackFunction? Accessor in new PackFunction?[] { Indexer.GetFunction, Indexer.SetFunction })
            {
                if (Accessor != null)
                {
                    SetFunctionIdentifier(Accessor,
                        context.IdentifierGenerator.GetIndexerFunctionIdentifier(Indexer, Accessor));
                }
            }
        }
    }

    private void SetFunctionIdentifier(PackFunction function, string resolvedName)
    {
        SetIdentifier(function, resolvedName);
        SetParameterIdentifiers(function.Parameters);
    }

    /* A parameter is only ever looked up inside its own function, so its name alone is enough. */
    private void SetParameterIdentifiers(FunctionParameterCollection parameters)
    {
        foreach (FunctionParameter Parameter in parameters)
        {
            Parameter.SelfIdentifier.ResolvedName = Parameter.SelfIdentifier.SourceCodeName;
            Parameter.SelfIdentifier.SelfName = Parameter.SelfIdentifier.SourceCodeName;
            Parameter.SelfIdentifier.Target = Parameter;
        }
    }

    private void SetIdentifier(PackMember member, string resolvedName)
    {
        member.SelfIdentifier.ResolvedName = resolvedName;
        member.SelfIdentifier.SelfName = member.SelfIdentifier.SourceCodeName;
        member.SelfIdentifier.Target = member;
    }


    // Inherited methods.
    public void ResolvePack(PackResolutionContext context)
    {
        ArgumentNullException.ThrowIfNull(context, nameof(context));

        foreach (PackNameSpace NameSpace in context.Pack.NameSpaces)
        {
            ResolveHolder(NameSpace, context);
        }
    }
}
