using KeigValCompiler.Error;
using KeigValCompiler.Semantician.Member;
using KeigValCompiler.Semantician.Types;

namespace KeigValCompiler.Semantician.Resolver;

/* Resolves one type as it is written, wherever that is: in a declaration, or inside a function body. It points
 * the type's identifier at the type or generic parameter it means; a type keyword such as "int" resolves to
 * the library type it stands for. A qualified name, as "KGVL.Collections.List<int>" or "Outer<int>.Inner", is
 * resolved from its first name on, each naming a namespace or a type in which the next is looked for; a name
 * standing for a namespace resolves to its full name. A name which cannot be resolved, or names a type which
 * cannot be used there, is reported to the target it is given, and left unresolved.
 *
 * What the types around a name derive from comes from a source of bases: SignatureResolver while signatures
 * are being resolved, which resolves a base list when a lookup first needs it, and ResolvedBaseTypes once
 * every base list is.
 *
 * A type found through bases keeps the way the lookup went to the base declaring it, which the type model
 * reads that base along, as the lookup saw it: a base list being resolved counts as none, which reading
 * afterwards could not tell. For an interface, that way comes from looking again among the interfaces it
 * derives from as the type model reads them, since the lookup tells them apart by declaration only. */
internal class TypeNameResolver
{
    // Private fields.
    private readonly PackResolutionContext _context;
    private readonly IBaseTypeSource _bases;


    // Constructors.
    internal TypeNameResolver(PackResolutionContext context, IBaseTypeSource bases)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _bases = bases ?? throw new ArgumentNullException(nameof(bases));
    }


    // Internal methods.
    /* Resolves a type written where the scope is, as the member it is written in, or a type for the types
     * written outside its body, as its base list. What cannot be resolved is reported to the target. Type
     * arguments come first, so that each is reported on its own even when the type around it is not found
     * either. */
    internal void Resolve(TypeTargetIdentifier type, PackMember scope, MessageTarget target)
    {
        ArgumentNullException.ThrowIfNull(type, nameof(type));
        ArgumentNullException.ThrowIfNull(scope, nameof(scope));

        ResolveTypes(type.TypeArguments, scope, target);
        if (IsKeywordType(type))
        {
            ResolveKeywordType(type, target);
            return;
        }

        TypeSearchResult? Result = Search(type, scope, target);
        if (Result == null)
        {
            return;
        }

        if (Result.IsFound)
        {
            SetFound(type, Result);
        }
        else if (Result.IsNameSpace)
        {
            target.AddError(_context.ErrorCreator.NameSpaceUsedAsType.CreateOptions(type.ToString(),
                Result.NameSpaceName!));
        }
        else
        {
            ReportNotFound(type, Result, false, scope, target);
        }
    }


    // Private methods.
    private void ResolveTypes(IEnumerable<TypeTargetIdentifier> types,
        PackMember scope,
        MessageTarget target)
    {
        foreach (TypeTargetIdentifier Type in types)
        {
            Resolve(Type, scope, target);
        }
    }

    /* What comes before a '.' in a type's name is resolved as a type is, but it may also name a namespace,
     * and it cannot be a generic parameter, through which no type can be named. The parser never lets a type
     * keyword stand there, as C# does not. Null when it names nothing, which has been reported. */
    private TypeSearchResult? ResolveQualifier(TypeTargetIdentifier qualifier,
        PackMember scope,
        MessageTarget target)
    {
        ResolveTypes(qualifier.TypeArguments, scope, target);
        TypeSearchResult? Result = Search(qualifier, scope, target);
        if (Result == null)
        {
            return null;
        }

        if (Result.IsNameSpace)
        {
            SetNameSpaceTarget(qualifier.MainTarget, Result.NameSpaceName!);
            return Result;
        }
        if (Result.Target is GenericTypeParameter)
        {
            target.AddError(_context.ErrorCreator.GenericParameterQualifier.CreateOptions(qualifier.ToString()));
            return null;
        }
        if (Result.IsFound)
        {
            SetFound(qualifier, Result);
            return Result;
        }
        ReportNotFound(qualifier, Result, true, scope, target);
        return null;
    }

    /* Looks a name up where it is written, or when it has a qualifier, in what the qualifier names, and for a
     * type found through bases, works out the way to it. Null when the qualifier names nothing, or the name
     * is ambiguous among inherited interfaces, which has been reported. */
    private TypeSearchResult? Search(TypeTargetIdentifier type, PackMember scope, MessageTarget target)
    {
        TypeSearchResult? Result = SearchWhereWritten(type, scope, target);
        if ((Result == null) || !Result.IsFound || (Result.InheritedThrough == null))
        {
            return Result;
        }
        if (Result.InheritedThrough is PackInterface)
        {
            return FindInterfacePath(type, Result, scope, target);
        }
        return Result.WithInheritedPath(FindBaseClassPath(Result.InheritedThrough,
            MemberRelations.GetHoldingMember((PackMember)Result.Target!)!));
    }

    private TypeSearchResult? SearchWhereWritten(TypeTargetIdentifier type,
        PackMember scope,
        MessageTarget target)
    {
        string Name = type.MainTarget.SourceCodeName;
        int TypeArgumentCount = type.TypeArguments.Length;
        if (type.Qualifier == null)
        {
            return _context.TypeSearcher.Search(Name, TypeArgumentCount, scope, _bases);
        }

        TypeSearchResult? Qualifier = ResolveQualifier(type.Qualifier, scope, target);
        if (Qualifier == null)
        {
            return null;
        }
        if (Qualifier.IsNameSpace)
        {
            return _context.TypeSearcher.SearchNameSpace(Qualifier.NameSpaceName!, Name, TypeArgumentCount, scope);
        }

        TypeSearchResult Nested = _context.TypeSearcher.SearchNested((PackMember)Qualifier.Target!, Name,
            TypeArgumentCount, scope, _bases);
        if (Nested.IsFound || (Qualifier.ClashingNameSpaceName == null))
        {
            return Nested;
        }

        /* A name found in neither the type nor the namespace of the clash is left to the clash, which has been
         * reported. */
        TypeSearchResult InNameSpace = _context.TypeSearcher.SearchNameSpace(Qualifier.ClashingNameSpaceName, Name,
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
        MessageTarget target)
    {
        PackMember InheritedThrough = result.InheritedThrough!;
        DeclaredType? Through = (type.Qualifier == null) ? _context.TypeReader.GetInstanceType(InheritedThrough)
            : _context.TypeReader.Read(type.Qualifier) as DeclaredType;
        if (Through == null)
        {
            return result;
        }

        string Name = type.MainTarget.SourceCodeName;
        HashSet<PackMember> SelfDeriving = MemberRelations.FindSelfDerivingTypes(InheritedThrough, _bases);
        Dictionary<SemanticType, (DeclaredType Derived, TypeTargetIdentifier WrittenBase)> ReachedBy = new();
        List<(DeclaredType Holder, PackMember Nested)> Found = new();
        foreach (DeclaredType Inherited in GetInheritedInterfaces(new DeclaredType[] { Through }, SelfDeriving,
            ReachedBy))
        {
            PackMember? Nested = _context.TypeSearcher.FindDeclaredType(Inherited.Declaration, Name,
                type.TypeArguments.Length, scope, _bases);
            if (Nested != null)
            {
                Found.Add((Inherited, Nested));
            }
        }

        HashSet<SemanticType> Hidden = new(GetInheritedInterfaces(Found.Select(found => found.Holder),
            SelfDeriving, null));
        List<(DeclaredType Holder, PackMember Nested)> Unhidden = Found.Where(
            found => !Hidden.Contains(found.Holder)).ToList();
        if (Unhidden.Count > 1)
        {
            string Candidates = string.Join(", ", Unhidden.Select(found =>
                $"\"{found.Holder}{KGVL.MEMBER_ACCESS}{found.Nested.SelfIdentifier.SourceCodeName}\""));
            target.AddError(_context.ErrorCreator.AmbiguousInheritedType.CreateOptions(Name,
                InheritedThrough.SelfIdentifier.SourceCodeName, Candidates));
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
        Dictionary<SemanticType, (DeclaredType Derived, TypeTargetIdentifier WrittenBase)>? reachedBy)
    {
        List<DeclaredType> Inherited = new();
        HashSet<SemanticType> Seen = new();
        Queue<DeclaredType> ToSearch = new(types);
        while (ToSearch.Count > 0)
        {
            DeclaredType Current = ToSearch.Dequeue();

            /* Asking the source for a type's bases resolves its base list first, when that is still to be done,
             * so that the type model can read the bases it names. */
            _bases.GetBaseTypes(Current.Declaration);
            if (selfDeriving.Contains(Current.Declaration) || _bases.IsResolvingBases(Current.Declaration))
            {
                continue;
            }
            foreach ((TypeTargetIdentifier WrittenBase, DeclaredType Base) in _context.TypeReader
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
        MessageTarget target)
    {
        ErrorRepository ErrorCreator = _context.ErrorCreator;
        string Name = type.MainTarget.SourceCodeName;
        if (result.CircularType != null)
        {
            PackMember Circular = result.CircularType;
            target.AddError(ErrorCreator.CircularBaseLookup.CreateOptions(Name,
                MemberRelations.GetKindName(Circular), MemberRelations.GetDisplayName(Circular)));
        }
        else if (result.IsAmbiguous && (result.InheritedThrough != null))
        {
            string Candidates = string.Join(", ", result.AmbiguousTypes.Select(candidate =>
                $"\"{MemberRelations.GetHoldingMember(candidate)!.SelfIdentifier.SourceCodeName}" +
                $"{KGVL.NAMESPACE_SEPARATOR}{candidate.SelfIdentifier.SourceCodeName}\""));
            target.AddError(ErrorCreator.AmbiguousInheritedType.CreateOptions(Name,
                result.InheritedThrough.SelfIdentifier.SourceCodeName, Candidates));
        }
        else if (result.IsAmbiguous)
        {
            string Candidates = string.Join(", ", result.AmbiguousTypes.Select(
                candidate => $"\"{candidate.SelfIdentifier.ResolvedName}\""));
            target.AddError(ErrorCreator.AmbiguousType.CreateOptions(Name, Candidates));
        }
        else if ((result.InaccessibleType != null)
            && (MemberRelations.GetHoldingMember(result.InaccessibleType) is PackMember Holder))
        {
            PackMember Nested = result.InaccessibleType;
            string NestedName = Holder.SelfIdentifier.SourceCodeName + KGVL.NAMESPACE_SEPARATOR
                + Nested.SelfIdentifier.SourceCodeName;
            target.AddError(ErrorCreator.NestedTypeInaccessible.CreateOptions(NestedName,
                MemberRelations.GetKindName(scope), MemberRelations.GetDisplayName(scope),
                ModifierKeywords.FormatAccess(MemberRelations.GetEffectiveAccess(Nested)),
                Holder.SelfIdentifier.SourceCodeName));
        }
        else if (result.InaccessibleType != null)
        {
            PackMember Inaccessible = result.InaccessibleType;
            target.AddError(ErrorCreator.TypeInaccessible.CreateOptions(type.ToString(),
                Inaccessible.NameSpace.SelfIdentifier.SourceCodeName + KGVL.NAMESPACE_SEPARATOR
                + Inaccessible.SelfIdentifier.SourceCodeName));
        }
        else if (result.OtherGenericParameterCount != null)
        {
            target.AddError(ErrorCreator.WrongTypeArgumentCount.CreateOptions(type.ToString(),
                type.TypeArguments.Length, result.OtherGenericParameterCount.Value));
        }
        else if (result.NameSpaceWithArguments != null)
        {
            target.AddError(ErrorCreator.NameSpaceWithTypeArguments.CreateOptions(type.ToString(),
                result.NameSpaceWithArguments));
        }
        else if ((type.Qualifier == null) && isQualifier)
        {
            target.AddError(ErrorCreator.TypeOrNameSpaceNotFound.CreateOptions(Name));
        }
        else if (type.Qualifier == null)
        {
            target.AddError(ErrorCreator.TypeNotFound.CreateOptions(Name));
        }
        else if (type.Qualifier.MainTarget.Target is PackMember)
        {
            target.AddError(ErrorCreator.NestedTypeNotFound.CreateOptions(type.Qualifier.ToString(), Name));
        }
        else
        {
            target.AddError(ErrorCreator.TypeNotInNameSpace.CreateOptions(type.Qualifier.MainTarget.ResolvedName!,
                Name));
        }
    }

    private bool IsKeywordType(TypeTargetIdentifier type)
    {
        return (type.Qualifier == null) && KGVL.TYPE_KEYWORDS.Contains(type.MainTarget.SourceCodeName);
    }

    /* A keyword whose type the library does not declare is left unresolved without an error of its
     * own, since the missing library type has already been reported. */
    private void ResolveKeywordType(TypeTargetIdentifier type, MessageTarget target)
    {
        if (type.TypeArguments.Length > 0)
        {
            target.AddError(_context.ErrorCreator.WrongTypeArgumentCount.CreateOptions(type.ToString(),
                type.TypeArguments.Length, 0));
            return;
        }

        PackMember? KeywordType = _context.Registry.GetTypeByKeyword(type.MainTarget.SourceCodeName);
        if (KeywordType != null)
        {
            SetTarget(type.MainTarget, KeywordType);
        }
    }

    /* A type found through the bases of the type it was looked for in keeps that type, and the way to the base
     * holding it, along which the type model reads that base. It is read at once, while each base on the way
     * has just been read in turn, so that reading it later never goes down a long chain of such ways. */
    private void SetFound(TypeTargetIdentifier type, TypeSearchResult result)
    {
        SetTarget(type.MainTarget, result.Target!);
        type.InheritedThrough = result.InheritedThrough;
        type.InheritedPath = result.InheritedPath;
        if (type.InheritedPath != null)
        {
            _context.TypeReader.Read(type);
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
    private void SetNameSpaceTarget(Identifier identifier, string nameSpaceName)
    {
        identifier.Target = _context.Pack.TryGetNamespace(nameSpaceName);
        identifier.ResolvedName = nameSpaceName;
        identifier.SelfName = identifier.SourceCodeName;
    }
}
