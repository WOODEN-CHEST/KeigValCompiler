using KeigValCompiler.Error;
using KeigValCompiler.Semantician.Member;

namespace KeigValCompiler.Semantician.Resolver;

/* Resolves every type written in a declaration, as opposed to inside a function body: base types,
 * generic constraints, and the types of fields, properties, indexers, events, delegates, functions and
 * their parameters, together with the interface an explicit implementation names. Each is resolved as
 * TypeNameResolver describes, and a name which cannot be resolved, or names a type the declaration cannot
 * use, is reported at its member's line and left unresolved, and resolution carries on with the next.
 *
 * Every base list is resolved first, and each when first needed: a name may be found among the types a
 * type around it inherits, or a qualifier inherits, and whether a nested type can be used may depend on
 * what derives from what, so a lookup asks for the bases it needs, through IBaseTypeSource, before the
 * pass over every type has reached them. While a type's own base list is being resolved, it has no bases,
 * as in C#, which also ends any loop; a lookup which then needs what a class or a structure inherits is an
 * error, as TypeSearcher explains. Everything else is resolved once every base is known. */
internal class SignatureResolver : IPackResolver, IBaseTypeSource
{
    // Static fields.
    /* How deep resolving one base list may need others resolved first, each waiting on the next. Real
     * programs stay far below it; far enough past it, the waiting lists would run out of stack. */
    private const int MAX_BASE_LIST_DEPTH = 100;


    // Private fields.
    /* Whether each type's base list is resolved, false while it is being resolved. */
    private readonly Dictionary<PackMember, bool> _isBaseListResolved = new(ReferenceEqualityComparer.Instance);
    private PackResolutionContext? _context = null;
    private TypeNameResolver? _typeNames = null;
    private int _baseListDepth = 0;


    // Private methods.
    /* Past the deepest base lists may wait on each other, resolution stops, since what it would go on to
     * report would only follow from the base lists left unresolved. */
    private void ResolveBaseList(PackMember type)
    {
        if ((type is not IPackMemberExtender Extender) || _isBaseListResolved.ContainsKey(type))
        {
            return;
        }
        if (_baseListDepth == MAX_BASE_LIST_DEPTH)
        {
            _context!.AddError(_context.ErrorCreator.BaseListsTooDeep.CreateOptions(MemberRelations.GetKindName(type),
                MemberRelations.GetDisplayName(type), MAX_BASE_LIST_DEPTH), type);
            throw new ResolutionStoppedException();
        }

        _baseListDepth++;
        _isBaseListResolved.Add(type, false);
        ResolveTypes(Extender.ExtendedMembers, type, _context!);
        _isBaseListResolved[type] = true;
        _baseListDepth--;
    }

    private void ResolveMember(PackMember member, PackResolutionContext context)
    {
        switch (member)
        {
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
                ResolveParameters(Function.Parameters, GetParameterScope(Function), context);
                ResolveExplicitInterface(Function.ExplicitInterface, member, context);
                break;
        }

        if (member is IGenericParameterHolder GenericsHolder)
        {
            ResolveGenericConstraints(GenericsHolder, member, context);
        }
    }

    /* A record's parameter list is written after its name, outside the record, as its base list is, so it is
     * looked up from the record itself, which sees only its generic parameters there, as in C#. */
    private PackMember GetParameterScope(PackFunction function)
    {
        return ((function is PackConstructor Constructor) && Constructor.IsPrimary)
            ? MemberRelations.GetHoldingMember(function)! : function;
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

    /* Each type is reported at the line of the member it is written in. */
    private void ResolveType(TypeTargetIdentifier type, PackMember scope, PackResolutionContext context)
    {
        _typeNames!.Resolve(type, scope, new MessageTarget(context.Messages, context.GetLocation(scope)));
    }


    // Inherited methods.
    public void ResolvePack(PackResolutionContext context)
    {
        ArgumentNullException.ThrowIfNull(context, nameof(context));

        _context = context;
        _typeNames = new(context, this);
        foreach (PackMember Type in context.Pack.Types)
        {
            ResolveBaseList(Type);
        }
        foreach (PackMember Member in context.Pack.Members)
        {
            ResolveMember(Member, context);
        }
    }

    /* The bases a lookup asks for are resolved first when they are not yet, and taken as none while they are
     * being resolved. A class's base class comes first, when it has one. */
    public IEnumerable<PackMember> GetBaseTypes(PackMember type)
    {
        ArgumentNullException.ThrowIfNull(type, nameof(type));

        ResolveBaseList(type);
        return IsResolvingBases(type) ? Enumerable.Empty<PackMember>() : MemberRelations.GetBaseDeclarations(type);
    }

    public bool IsResolvingBases(PackMember type)
    {
        ArgumentNullException.ThrowIfNull(type, nameof(type));

        return _isBaseListResolved.TryGetValue(type, out bool IsResolved) && !IsResolved;
    }
}
