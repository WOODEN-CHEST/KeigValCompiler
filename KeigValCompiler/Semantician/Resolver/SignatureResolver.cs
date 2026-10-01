using KeigValCompiler.Semantician.Member;
using KeigValCompiler.Semantician.Types;

namespace KeigValCompiler.Semantician.Resolver;

/* Resolves every type written in a declaration, as opposed to inside a function body: base types,
 * generic constraints, and the types of fields, properties, indexers, events, delegates, functions and
 * their parameters, together with the interface an explicit implementation names. Each is resolved by
 * pointing its identifier at the type or generic parameter it means; a type keyword such as "int"
 * resolves to the library type it stands for. A qualified name, as "KGVL.Collections.List<int>" or
 * "Outer<int>.Inner", is resolved from its first name on, each naming a namespace or a type in which the
 * next is looked for; a name standing for a namespace resolves to its full name. A name which cannot be
 * resolved, or names a type the declaration cannot use, is reported and left unresolved, and resolution
 * carries on with the next.
 *
 * Every base list is resolved first, and each when first needed: a name may be found among the types a
 * type around it inherits, or a qualifier inherits, and whether a nested type can be used may depend on
 * what derives from what, so a lookup asks for the bases it needs, through IBaseTypeSource, before the
 * pass over every type has reached them. While a type's own base list is being resolved, it has no bases,
 * as in C#, which also ends any loop; a lookup which then needs what a class or a structure inherits is an
 * error, as TypeSearcher explains. Everything else is resolved once every base is known.
 *
 * A type found through bases keeps the way the lookup went to the base declaring it, which the type model
 * reads that base along, as the lookup saw it: a base list being resolved counts as none, which reading
 * afterwards could not tell. For an interface, that way comes from looking again among the interfaces it
 * derives from as the type model reads them, since the lookup tells them apart by declaration only. */
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
            SetFound(type, Result, context);
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
            SetFound(qualifier, Result, context);
            return Result;
        }
        ReportNotFound(qualifier, Result, true, scope, context);
        return null;
    }

    /* Looks a name up where it is written, or when it has a qualifier, in what the qualifier names, and for a
     * type found through bases, works out the way to it. Null when the qualifier names nothing, or the name
     * is ambiguous among inherited interfaces, which has been reported. */
    private TypeSearchResult? Search(TypeTargetIdentifier type, PackMember scope, PackResolutionContext context)
    {
        TypeSearchResult? Result = SearchWhereWritten(type, scope, context);
        if ((Result == null) || !Result.IsFound || (Result.InheritedThrough == null))
        {
            return Result;
        }
        if (Result.InheritedThrough is PackInterface)
        {
            return FindInterfacePath(type, Result, scope, context);
        }
        return Result.WithInheritedPath(FindBaseClassPath(Result.InheritedThrough,
            MemberRelations.GetHoldingMember((PackMember)Result.Target!)!));
    }

    private TypeSearchResult? SearchWhereWritten(TypeTargetIdentifier type,
        PackMember scope,
        PackResolutionContext context)
    {
        string Name = type.MainTarget.SourceCodeName;
        int TypeArgumentCount = type.TypeArguments.Length;
        if (type.Qualifier == null)
        {
            return context.TypeSearcher.Search(Name, TypeArgumentCount, scope, this);
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
            TypeArgumentCount, scope, this);
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

    /* The base classes written on the way from a class to one of its base classes, which is the one way. */
    private IReadOnlyList<TypeTargetIdentifier>? FindBaseClassPath(PackMember type, PackMember baseClass)
    {
        List<TypeTargetIdentifier> Path = new();
        HashSet<PackMember> Seen = new(ReferenceEqualityComparer.Instance);
        for (PackMember Current = type; !ReferenceEquals(Current, baseClass);
            Current = (PackMember)Path[^1].MainTarget.Target!)
        {
            TypeTargetIdentifier? WrittenBase = MemberRelations.GetWrittenBaseClassName(Current);
            if ((WrittenBase == null) || !Seen.Add(Current))
            {
                return null;
            }
            Path.Add(WrittenBase);
        }
        return Path;
    }

    /* The lookup tells the interfaces an interface derives from apart by declaration, but one reached with two
     * sets of type arguments is two interfaces, as IA<int> and IA<string> are, each with its own nested types,
     * and one declared in an interface deriving from IA<int> hides only IA<int>'s. So a type found through an
     * interface's bases is looked for again among them as the type model reads them, which also gives the
     * way to the one declaring it, as the lookup went. When more than one declares a type of the name, and
     * none hides the others, that is reported, and the result is null. The way is left unknown when a base on
     * it cannot be read. */
    private TypeSearchResult? FindInterfacePath(TypeTargetIdentifier type,
        TypeSearchResult result,
        PackMember scope,
        PackResolutionContext context)
    {
        PackMember InheritedThrough = result.InheritedThrough!;
        DeclaredType? Through = (type.Qualifier == null) ? context.TypeReader.GetInstanceType(InheritedThrough)
            : context.TypeReader.Read(type.Qualifier) as DeclaredType;
        if (Through == null)
        {
            return result;
        }

        string Name = type.MainTarget.SourceCodeName;
        HashSet<PackMember> SelfDeriving = MemberRelations.FindSelfDerivingTypes(InheritedThrough, this);
        Dictionary<SemanticType, (DeclaredType Derived, TypeTargetIdentifier WrittenBase)> ReachedBy = new();
        List<(DeclaredType Holder, PackMember Nested)> Found = new();
        foreach (DeclaredType Inherited in GetInheritedInterfaces(new DeclaredType[] { Through }, SelfDeriving,
            ReachedBy, context))
        {
            PackMember? Nested = context.TypeSearcher.FindDeclaredType(Inherited.Declaration, Name,
                type.TypeArguments.Length, scope, this);
            if (Nested != null)
            {
                Found.Add((Inherited, Nested));
            }
        }

        HashSet<SemanticType> Hidden = new(GetInheritedInterfaces(Found.Select(found => found.Holder),
            SelfDeriving, null, context));
        List<(DeclaredType Holder, PackMember Nested)> Unhidden = Found.Where(
            found => !Hidden.Contains(found.Holder)).ToList();
        if (Unhidden.Count > 1)
        {
            string Candidates = string.Join(", ", Unhidden.Select(found =>
                $"\"{found.Holder}{KGVL.MEMBER_ACCESS}{found.Nested.SelfIdentifier.SourceCodeName}\""));
            context.AddError(context.ErrorCreator.AmbiguousInheritedType.CreateOptions(Name,
                InheritedThrough.SelfIdentifier.SourceCodeName, Candidates), scope);
            return null;
        }
        if ((Unhidden.Count == 0) || !ReferenceEquals(Unhidden[0].Nested, result.Target))
        {
            return result;
        }

        List<TypeTargetIdentifier> Path = new();
        for (DeclaredType Step = Unhidden[0].Holder; !Step.Equals(Through); Step = ReachedBy[Step].Derived)
        {
            Path.Add(ReachedBy[Step].WrittenBase);
        }
        Path.Reverse();
        return result.WithInheritedPath(Path);
    }

    /* Every interface some interfaces derive from, however far back, as the type model reads them, each once,
     * but not the interfaces themselves. As for the lookup, an interface deriving from itself derives from
     * nothing, and one whose base list is being resolved has no bases yet. Where each was reached from is
     * noted, when asked for. */
    private List<DeclaredType> GetInheritedInterfaces(IEnumerable<DeclaredType> types,
        HashSet<PackMember> selfDeriving,
        Dictionary<SemanticType, (DeclaredType Derived, TypeTargetIdentifier WrittenBase)>? reachedBy,
        PackResolutionContext context)
    {
        List<DeclaredType> Inherited = new();
        HashSet<SemanticType> Seen = new();
        Queue<DeclaredType> ToSearch = new(types);
        while (ToSearch.Count > 0)
        {
            DeclaredType Current = ToSearch.Dequeue();
            ResolveBaseList(Current.Declaration);
            if (selfDeriving.Contains(Current.Declaration) || IsResolvingBases(Current.Declaration))
            {
                continue;
            }
            foreach ((TypeTargetIdentifier WrittenBase, DeclaredType Base) in context.TypeReader
                .GetWrittenBases(Current))
            {
                if ((Base.Declaration is PackInterface) && Seen.Add(Base))
                {
                    reachedBy?.Add(Base, (Current, WrittenBase));
                    Inherited.Add(Base);
                    ToSearch.Enqueue(Base);
                }
            }
        }
        return Inherited;
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
        if (result.CircularType != null)
        {
            PackMember Circular = result.CircularType;
            context.AddError(context.ErrorCreator.CircularBaseLookup.CreateOptions(Name,
                MemberRelations.GetKindName(Circular), MemberRelations.GetDisplayName(Circular)), scope);
        }
        else if (result.IsAmbiguous && (result.InheritedThrough != null))
        {
            string Candidates = string.Join(", ", result.AmbiguousTypes.Select(candidate =>
                $"\"{MemberRelations.GetHoldingMember(candidate)!.SelfIdentifier.SourceCodeName}" +
                $"{KGVL.NAMESPACE_SEPARATOR}{candidate.SelfIdentifier.SourceCodeName}\""));
            context.AddError(context.ErrorCreator.AmbiguousInheritedType.CreateOptions(Name,
                result.InheritedThrough.SelfIdentifier.SourceCodeName, Candidates), scope);
        }
        else if (result.IsAmbiguous)
        {
            string Candidates = string.Join(", ", result.AmbiguousTypes.Select(
                candidate => $"\"{candidate.SelfIdentifier.ResolvedName}\""));
            context.AddError(context.ErrorCreator.AmbiguousType.CreateOptions(Name, Candidates), scope);
        }
        else if ((result.InaccessibleType != null)
            && (MemberRelations.GetHoldingMember(result.InaccessibleType) is PackMember Holder))
        {
            PackMember Nested = result.InaccessibleType;
            string NestedName = Holder.SelfIdentifier.SourceCodeName + KGVL.NAMESPACE_SEPARATOR
                + Nested.SelfIdentifier.SourceCodeName;
            context.AddError(context.ErrorCreator.NestedTypeInaccessible.CreateOptions(NestedName,
                MemberRelations.GetKindName(scope), MemberRelations.GetDisplayName(scope),
                ModifierKeywords.FormatAccess(MemberRelations.GetEffectiveAccess(Nested)),
                Holder.SelfIdentifier.SourceCodeName), scope);
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

    /* A type found through the bases of the type it was looked for in keeps that type, and the way to the base
     * holding it, along which the type model reads that base. It is read at once, while each base on the way
     * has just been read in turn, so that reading it later never goes down a long chain of such ways. */
    private void SetFound(TypeTargetIdentifier type, TypeSearchResult result, PackResolutionContext context)
    {
        SetTarget(type.MainTarget, result.Target!);
        type.InheritedThrough = result.InheritedThrough;
        type.InheritedPath = result.InheritedPath;
        if (type.InheritedPath != null)
        {
            context.TypeReader.Read(type);
        }
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

        _context = context;
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
