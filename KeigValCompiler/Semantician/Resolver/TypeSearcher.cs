using KeigValCompiler.Semantician.Member;

namespace KeigValCompiler.Semantician.Resolver;

/* Finds the type a name written in a declaration means, looking where C# looks and taking the first
 * place with a match. First the generic parameters of the member the name is written in and of each
 * type around it, together with those types' nested types, from the innermost outwards. Then, at the
 * member's namespace and at each namespace containing it, so "A.B" before "A", a namespace of the name
 * inside it, and then a type it holds; then a namespace of the name at the root. Last the types of the
 * namespaces the file imports, where two matches are ambiguous rather than one winning. A namespace is
 * found as in C#, which lets a name start a qualified one, as the "KGVL" of "KGVL.Int32", and only for a
 * name written without type arguments. A namespace exists if a file declares it or one inside it; the
 * standard library, compiled as if on its own, sees only its own, and the user's code both sides'.
 *
 * Where one namespace holds both a namespace and a type of the name, Roslyn prefers what the compilation
 * declares to what it imports, so from the user's code a namespace of its own wins over a library type,
 * and a type of its own over a library namespace. Two of one side are a clash, which DeclarationNameChecker
 * reports, and the type is taken, so that nothing else is reported about them.
 *
 * A name after a qualifier is looked for only in what the qualifier names: in a namespace, a namespace
 * inside it and then a type it holds, and in a type, a type it declares.
 *
 * A type matches by its name and its number of generic parameters, so "Foo<int>" never finds "Foo".
 * A type of a namespace which the use cannot see, an internal one on the other side of the standard
 * library's boundary, is passed over, as C# passes over what it cannot access, and is only given back
 * when nothing else matched, to be reported as inaccessible. The types around the use can always be
 * seen from it. Whether a type nested in another which a qualifier names can be seen depends on what
 * derives from what, which is only known once every base type is resolved, so AccessibilityChecker
 * decides it. */
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
    internal TypeSearchResult Search(string name, int typeArgumentCount, PackMember scope)
    {
        ArgumentNullException.ThrowIfNull(name, nameof(name));
        ArgumentNullException.ThrowIfNull(scope, nameof(scope));

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
            if (Member is IPackTypeHolder TypeHolder)
            {
                PackMember? NestedType = FindType(name, typeArgumentCount, TypeHolder.Types, null,
                    ref OtherGenericParameterCount, ref InaccessibleType);
                if (NestedType != null)
                {
                    return TypeSearchResult.Found(NestedType);
                }
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
            PackMember? ImportedType = FindType(name, typeArgumentCount, Import.Types, scope,
                ref OtherGenericParameterCount, ref InaccessibleType);
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

    /* The name after a type's, as the "Inner" of "Outer<int>.Inner": a type it declares. */
    internal TypeSearchResult SearchNested(PackMember type, string name, int typeArgumentCount)
    {
        ArgumentNullException.ThrowIfNull(type, nameof(type));
        ArgumentNullException.ThrowIfNull(name, nameof(name));

        int? OtherGenericParameterCount = null;
        PackMember? InaccessibleType = null;
        PackMember? Nested = (type is IPackTypeHolder Holder) ? FindType(name, typeArgumentCount, Holder.Types,
            null, ref OtherGenericParameterCount, ref InaccessibleType) : null;
        return (Nested != null) ? TypeSearchResult.Found(Nested)
            : TypeSearchResult.NotFound(OtherGenericParameterCount, null, null);
    }


    // Private methods.
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
        PackMember? Type = (NameSpace == null) ? null : FindType(name, typeArgumentCount, NameSpace.Types, scope,
            ref otherGenericParameterCount, ref inaccessibleType);

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

    /* Also notes the generic parameter count of a type which has the name but not the count, and, when a
     * scope is given to see from, the first type which matches but cannot be seen from it. */
    private PackMember? FindType(string name,
        int genericParameterCount,
        IEnumerable<PackMember> types,
        PackMember? scope,
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
            else if ((scope == null) || IsVisibleFrom(Type, scope))
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
