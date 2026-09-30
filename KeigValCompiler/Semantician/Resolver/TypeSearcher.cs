using KeigValCompiler.Semantician.Member;

namespace KeigValCompiler.Semantician.Resolver;

/* Finds the type a name written in a declaration means, looking where C# looks and taking the first
 * place with a match. First the generic parameters of the member the name is written in and of each
 * type around it, together with the types each of those declares or inherits, from the innermost
 * outwards. A type's own base list and constraints are written outside it, so there it offers only its
 * generic parameters, as in C#. Then, at the member's namespace and at each namespace containing it, so
 * "A.B" before "A", a namespace of the name inside it, and then a type it holds; then a namespace of the
 * name at the root. Last the types of the namespaces the file imports, where two matches are ambiguous
 * rather than one winning. A namespace is found as in C#, which lets a name start a qualified one, as the
 * "KGVL" of "KGVL.Int32", and only for a name written without type arguments. A namespace exists if a
 * file declares it or one inside it; the standard library, compiled as if on its own, sees only its own,
 * and the user's code both sides'.
 *
 * Where one namespace holds both a namespace and a type of the name, Roslyn prefers what the compilation
 * declares to what it imports, so from the user's code a namespace of its own wins over a library type,
 * and a type of its own over a library namespace. Two of one side are a clash, which DeclarationNameChecker
 * reports, and the type is taken, so that nothing else is reported about them.
 *
 * A name after a qualifier is looked for only in what the qualifier names: in a namespace, a namespace
 * inside it and then a type it holds, and in a type, a type it declares or inherits.
 *
 * A type inherits, as C#'s member lookup has it, the types its base class declares and those that one
 * inherits, the nearest first; an interface those of every interface it derives from, where one declared
 * in an interface deriving from another's hides that one, and two which neither hides are ambiguous. A
 * structure, and a class, inherit nothing from the interfaces they implement. A type whose own base list
 * is still being resolved has no bases yet. For an interface the lookup then goes on, but a class or a
 * structure in which it finds nothing ends it, as C# ends it with its error for bases which depend on
 * each other, since what the type inherits could have been what the name means.
 *
 * A type matches by its name and its number of generic parameters, so "Foo<int>" never finds "Foo".
 * A type the use cannot see is passed over, as C# passes over what it cannot access, and is only given
 * back when nothing else matched, to be reported as inaccessible: a namespace's internal type on the
 * other side of the standard library's boundary, or a nested type its access keeps from the use, as
 * AccessDomains decides it. That can depend on what derives from what, so the bases it needs are asked
 * for, and resolved when first needed. */
internal class TypeSearcher
{
    // Private fields.
    private readonly DataPack _pack;
    private HashSet<string>? _allNameSpaceNames = null;
    private HashSet<string>? _libraryNameSpaceNames = null;
    private HashSet<string>? _userNameSpaceNames = null;


    // Constructors.
    internal TypeSearcher(DataPack pack)
    {
        _pack = pack ?? throw new ArgumentNullException(nameof(pack));
    }


    // Internal methods.
    internal TypeSearchResult Search(string name, int typeArgumentCount, PackMember scope, IBaseTypeSource bases)
    {
        ArgumentNullException.ThrowIfNull(name, nameof(name));
        ArgumentNullException.ThrowIfNull(scope, nameof(scope));
        ArgumentNullException.ThrowIfNull(bases, nameof(bases));

        int? OtherGenericParameterCount = null;
        PackMember? InaccessibleType = null;
        string? NameSpaceWithArguments = null;

        for (PackMember? Member = scope; Member != null; Member = Member.ParentItem?.Target as PackMember)
        {
            GenericTypeParameter? Parameter = (typeArgumentCount == 0) ? FindGenericParameter(name, Member) : null;
            if (Parameter != null)
            {
                return TypeSearchResult.Found(Parameter);
            }

            bool IsOwnHeader = ReferenceEquals(Member, scope) && MemberRelations.IsType(scope);
            TypeSearchResult? InType = IsOwnHeader ? null : SearchMemberTypes(Member, name, typeArgumentCount,
                scope, bases, ref OtherGenericParameterCount, ref InaccessibleType);
            if (InType != null)
            {
                return InType;
            }
        }

        foreach (string NameSpaceName in GetNameSpaceAndParentNames(scope.NameSpace))
        {
            TypeSearchResult? AtLevel = SearchNameSpaceLevel(NameSpaceName, name, typeArgumentCount, scope,
                ref OtherGenericParameterCount, ref InaccessibleType, ref NameSpaceWithArguments);
            if (AtLevel != null)
            {
                return AtLevel;
            }
        }
        if (IsVisibleNameSpace(name, scope))
        {
            if (typeArgumentCount == 0)
            {
                return TypeSearchResult.FoundNameSpace(name);
            }
            NameSpaceWithArguments ??= name;
        }

        List<PackMember> ImportedTypes = new();
        foreach (PackNameSpace Import in scope.SourceFile.NamespaceImports)
        {
            PackMember? ImportedType = FindType(name, typeArgumentCount, Import.Types,
                type => IsVisibleFrom(type, scope), ref OtherGenericParameterCount, ref InaccessibleType);
            if (ImportedType != null)
            {
                ImportedTypes.Add(ImportedType);
            }
        }

        return ImportedTypes.Count switch
        {
            0 => TypeSearchResult.NotFound(OtherGenericParameterCount, InaccessibleType, NameSpaceWithArguments),
            1 => TypeSearchResult.Found(ImportedTypes[0]),
            _ => TypeSearchResult.Ambiguous(ImportedTypes)
        };
    }

    /* The name after a namespace's, as the "C" of "A.B.C". */
    internal TypeSearchResult SearchNameSpace(string nameSpaceName,
        string name,
        int typeArgumentCount,
        PackMember scope)
    {
        ArgumentNullException.ThrowIfNull(nameSpaceName, nameof(nameSpaceName));
        ArgumentNullException.ThrowIfNull(name, nameof(name));
        ArgumentNullException.ThrowIfNull(scope, nameof(scope));

        int? OtherGenericParameterCount = null;
        PackMember? InaccessibleType = null;
        string? NameSpaceWithArguments = null;
        return SearchNameSpaceLevel(nameSpaceName, name, typeArgumentCount, scope, ref OtherGenericParameterCount,
                ref InaccessibleType, ref NameSpaceWithArguments)
            ?? TypeSearchResult.NotFound(OtherGenericParameterCount, InaccessibleType, NameSpaceWithArguments);
    }

    /* The name after a type's, as the "Inner" of "Outer<int>.Inner": a type it declares or inherits. */
    internal TypeSearchResult SearchNested(PackMember type,
        string name,
        int typeArgumentCount,
        PackMember scope,
        IBaseTypeSource bases)
    {
        ArgumentNullException.ThrowIfNull(type, nameof(type));
        ArgumentNullException.ThrowIfNull(name, nameof(name));
        ArgumentNullException.ThrowIfNull(scope, nameof(scope));
        ArgumentNullException.ThrowIfNull(bases, nameof(bases));

        int? OtherGenericParameterCount = null;
        PackMember? InaccessibleType = null;
        return SearchMemberTypes(type, name, typeArgumentCount, scope, bases, ref OtherGenericParameterCount,
                ref InaccessibleType)
            ?? TypeSearchResult.NotFound(OtherGenericParameterCount, InaccessibleType, null);
    }

    /* A type one type declares itself, of the name and number of generic parameters, which the use can see. */
    internal PackMember? FindDeclaredType(PackMember holder,
        string name,
        int typeArgumentCount,
        PackMember scope,
        IBaseTypeSource bases)
    {
        ArgumentNullException.ThrowIfNull(holder, nameof(holder));
        ArgumentNullException.ThrowIfNull(name, nameof(name));
        ArgumentNullException.ThrowIfNull(scope, nameof(scope));
        ArgumentNullException.ThrowIfNull(bases, nameof(bases));

        int? OtherGenericParameterCount = null;
        PackMember? InaccessibleType = null;
        return FindNestedType(holder, name, typeArgumentCount, scope, bases, ref OtherGenericParameterCount,
            ref InaccessibleType);
    }


    // Private methods.
    /* The types a type declares, and then those it inherits, which only a class or an interface does. Null
     * when there is none of the name the use can see, unless the lookup has to end at the type. */
    private TypeSearchResult? SearchMemberTypes(PackMember type,
        string name,
        int typeArgumentCount,
        PackMember scope,
        IBaseTypeSource bases,
        ref int? otherGenericParameterCount,
        ref PackMember? inaccessibleType)
    {
        PackMember? Own = FindNestedType(type, name, typeArgumentCount, scope, bases,
            ref otherGenericParameterCount, ref inaccessibleType);
        if (Own != null)
        {
            return TypeSearchResult.Found(Own);
        }
        if ((type is PackClass or PackStruct) && bases.IsResolvingBases(type))
        {
            return TypeSearchResult.Circular(type);
        }
        if (type is PackInterface Interface)
        {
            return SearchBaseInterfaces(Interface, name, typeArgumentCount, scope, bases,
                ref otherGenericParameterCount, ref inaccessibleType);
        }

        HashSet<PackMember> Seen = new(ReferenceEqualityComparer.Instance) { type };
        for (PackMember? Base = GetBaseClass(type, bases); (Base != null) && Seen.Add(Base);
            Base = GetBaseClass(Base, bases))
        {
            PackMember? Inherited = FindNestedType(Base, name, typeArgumentCount, scope, bases,
                ref otherGenericParameterCount, ref inaccessibleType);
            if (Inherited != null)
            {
                return TypeSearchResult.FoundInherited(Inherited, type);
            }
        }
        return null;
    }

    /* Every interface an interface derives from, however far back, is searched, since one found further back
     * is only hidden by one declared in an interface deriving from its own. The same type reached along two
     * paths is one, and an interface deriving from itself derives from nothing, as in C#. Interfaces are told
     * apart here by declaration, before their type arguments can be read, so SignatureResolver checks
     * afterwards whether one reached with two sets of type arguments makes the name ambiguous after all. */
    private TypeSearchResult? SearchBaseInterfaces(PackInterface type,
        string name,
        int typeArgumentCount,
        PackMember scope,
        IBaseTypeSource bases,
        ref int? otherGenericParameterCount,
        ref PackMember? inaccessibleType)
    {
        HashSet<PackMember> SelfDeriving = MemberRelations.FindSelfDerivingTypes(type, bases);
        List<PackMember> Candidates = new();
        foreach (PackMember Inherited in GetInheritedTypes(new PackMember[] { type }, SelfDeriving, bases))
        {
            PackMember? Nested = FindNestedType(Inherited, name, typeArgumentCount, scope, bases,
                ref otherGenericParameterCount, ref inaccessibleType);
            if (Nested != null)
            {
                Candidates.Add(Nested);
            }
        }

        HashSet<PackMember> Hidden = new(GetInheritedTypes(
            Candidates.Select(candidate => MemberRelations.GetHoldingMember(candidate)!), SelfDeriving, bases),
            ReferenceEqualityComparer.Instance);
        List<PackMember> Unhidden = Candidates.Where(
            candidate => !Hidden.Contains(MemberRelations.GetHoldingMember(candidate)!)).ToList();
        return Unhidden.Count switch
        {
            0 => null,
            1 => TypeSearchResult.FoundInherited(Unhidden[0], type),
            _ => TypeSearchResult.AmbiguousInherited(Unhidden, type)
        };
    }

    /* Every type some types derive from, however far back, nearest first and each once, but not the types
     * themselves, nor what a type deriving from itself derives from. */
    private List<PackMember> GetInheritedTypes(IEnumerable<PackMember> types,
        HashSet<PackMember> selfDeriving,
        IBaseTypeSource bases)
    {
        List<PackMember> Inherited = new();
        HashSet<PackMember> Seen = new(ReferenceEqualityComparer.Instance);
        Queue<PackMember> ToSearch = new(types);
        while (ToSearch.Count > 0)
        {
            PackMember Current = ToSearch.Dequeue();
            if (selfDeriving.Contains(Current))
            {
                continue;
            }
            foreach (PackMember Base in bases.GetBaseTypes(Current).Where(Seen.Add))
            {
                Inherited.Add(Base);
                ToSearch.Enqueue(Base);
            }
        }
        return Inherited;
    }

    private PackMember? GetBaseClass(PackMember type, IBaseTypeSource bases)
    {
        return bases.GetBaseTypes(type).OfType<PackClass>().FirstOrDefault();
    }

    /* A type one type declares itself, which the use can see. */
    private PackMember? FindNestedType(PackMember holder,
        string name,
        int typeArgumentCount,
        PackMember scope,
        IBaseTypeSource bases,
        ref int? otherGenericParameterCount,
        ref PackMember? inaccessibleType)
    {
        if (holder is not IPackTypeHolder TypeHolder)
        {
            return null;
        }
        return FindType(name, typeArgumentCount, TypeHolder.Types,
            nested => AccessDomains.IsAccessibleFrom(nested, scope, bases), ref otherGenericParameterCount,
            ref inaccessibleType);
    }

    /* A name inside one namespace: a namespace of the name, or a type the namespace holds, whichever wins
     * where both are declared. Null when neither is, noting what could make a better error: a type of the
     * name with another number of generic parameters, one which cannot be seen from the use, or a namespace
     * which a name written with type arguments cannot mean. */
    private TypeSearchResult? SearchNameSpaceLevel(string nameSpaceName,
        string name,
        int typeArgumentCount,
        PackMember scope,
        ref int? otherGenericParameterCount,
        ref PackMember? inaccessibleType,
        ref string? nameSpaceWithArguments)
    {
        PackNameSpace? NameSpace = _pack.TryGetNamespace(nameSpaceName);
        PackMember? Type = (NameSpace == null) ? null : FindType(name, typeArgumentCount, NameSpace.Types,
            type => IsVisibleFrom(type, scope), ref otherGenericParameterCount, ref inaccessibleType);

        string InnerName = nameSpaceName + KGVL.NAMESPACE_SEPARATOR + name;
        if (IsVisibleNameSpace(InnerName, scope))
        {
            if ((typeArgumentCount == 0) && ((Type == null) || IsNameSpacePreferred(InnerName, Type, scope)))
            {
                return TypeSearchResult.FoundNameSpace(InnerName);
            }
            if (typeArgumentCount > 0)
            {
                nameSpaceWithArguments ??= InnerName;
            }
            else if (GetNameSpaceNames(Type!.SourceFile.Kind).Contains(InnerName))
            {
                return TypeSearchResult.FoundClashing(Type, InnerName);
            }
        }
        return (Type == null) ? null : TypeSearchResult.Found(Type);
    }

    /* Where a namespace and a type of one name are both declared, whether the namespace is what the name
     * means: only for the user's code, when the namespace is its own and the type the library's. */
    private bool IsNameSpacePreferred(string fullName, PackMember type, PackMember scope)
    {
        return (scope.SourceFile.Kind == SourceFileKind.User) && (type.SourceFile.Kind == SourceFileKind.Library)
            && GetNameSpaceNames(SourceFileKind.User).Contains(fullName);
    }

    /* The standard library is looked at as its own assembly, compiled without the user's code, so from it only
     * the namespaces it declares exist. */
    private bool IsVisibleNameSpace(string fullName, PackMember scope)
    {
        SourceFileKind? Side = (scope.SourceFile.Kind == SourceFileKind.Library) ? SourceFileKind.Library : null;
        return GetNameSpaceNames(Side).Contains(fullName);
    }

    /* The namespaces declared on one side of the library's boundary, or on either when no side is given. */
    private HashSet<string> GetNameSpaceNames(SourceFileKind? kind)
    {
        return kind switch
        {
            SourceFileKind.Library => _libraryNameSpaceNames ??= _pack.GetExistingNamespaceNames(kind),
            SourceFileKind.User => _userNameSpaceNames ??= _pack.GetExistingNamespaceNames(kind),
            _ => _allNameSpaceNames ??= _pack.GetExistingNamespaceNames(null)
        };
    }

    private GenericTypeParameter? FindGenericParameter(string name, PackMember member)
    {
        if (member is not IGenericParameterHolder GenericsHolder)
        {
            return null;
        }
        return GenericsHolder.GenericParameters.FirstOrDefault(
            parameter => parameter.SelfIdentifier.SourceCodeName == name);
    }

    /* Also notes the generic parameter count of a type which has the name but not the count, and the first
     * type which matches but cannot be seen from the use. */
    private PackMember? FindType(string name,
        int genericParameterCount,
        IEnumerable<PackMember> types,
        Func<PackMember, bool> isVisible,
        ref int? otherGenericParameterCount,
        ref PackMember? inaccessibleType)
    {
        foreach (PackMember Type in types)
        {
            if (Type.SelfIdentifier.SourceCodeName != name)
            {
                continue;
            }

            int TypeGenericParameterCount = TypeDeclarationResolver.GetGenericParameterCount(Type);
            if (TypeGenericParameterCount != genericParameterCount)
            {
                otherGenericParameterCount ??= TypeGenericParameterCount;
            }
            else if (isVisible(Type))
            {
                return Type;
            }
            else
            {
                inaccessibleType ??= Type;
            }
        }
        return null;
    }

    /* A namespace's type is public or internal, and internal ones are seen on their own side of the
     * standard library's boundary only. One whose access modifier cannot be used there is reported later,
     * and counts as its own side's. The library, looked at as its own assembly, sees none of the user's. */
    private bool IsVisibleFrom(PackMember type, PackMember scope)
    {
        return (type.SourceFile.Kind == scope.SourceFile.Kind)
            || ((scope.SourceFile.Kind == SourceFileKind.User) && type.HasModifier(PackMemberModifiers.Public));
    }

    /* The namespace's name and then the name of each one containing it. */
    private IEnumerable<string> GetNameSpaceAndParentNames(PackNameSpace nameSpace)
    {
        string? Name = nameSpace.SelfIdentifier.SourceCodeName;
        while (Name != null)
        {
            yield return Name;

            int SeparatorIndex = Name.LastIndexOf(KGVL.NAMESPACE_SEPARATOR);
            Name = (SeparatorIndex == -1) ? null : Name[..SeparatorIndex];
        }
    }
}
