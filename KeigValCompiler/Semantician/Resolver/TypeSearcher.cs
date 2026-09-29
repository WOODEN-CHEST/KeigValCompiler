using KeigValCompiler.Semantician.Member;

namespace KeigValCompiler.Semantician.Resolver;

/* Finds the type a name written in a declaration means, looking where C# looks and taking the first
 * place with a match. First the generic parameters of the member the name is written in and of each
 * type around it, together with those types' nested types, from the innermost outwards. Then the types
 * of the member's namespace and of each namespace containing it, so "A.B" before "A". Last the types
 * of the namespaces the file imports, where two matches are ambiguous rather than one winning.
 *
 * A type matches by its name and its number of generic parameters, so "Foo<int>" never finds "Foo".
 * A type of a namespace which the use cannot see, an internal one on the other side of the standard
 * library's boundary, is passed over, as C# passes over what it cannot access, and is only given back
 * when nothing else matched, to be reported as inaccessible. The types around the use can always be
 * seen from it. */
internal class TypeSearcher
{
    // Private fields.
    private readonly DataPack _pack;


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

        foreach (PackNameSpace NameSpace in GetNameSpaceAndParents(scope.NameSpace))
        {
            PackMember? NameSpaceType = FindType(name, typeArgumentCount, NameSpace.Types, scope,
                ref OtherGenericParameterCount, ref InaccessibleType);
            if (NameSpaceType != null)
            {
                return TypeSearchResult.Found(NameSpaceType);
            }
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
            0 => TypeSearchResult.NotFound(OtherGenericParameterCount, InaccessibleType),
            1 => TypeSearchResult.Found(ImportedTypes[0]),
            _ => TypeSearchResult.Ambiguous(ImportedTypes)
        };
    }


    // Private methods.
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
     * and counts as its own side's. */
    private bool IsVisibleFrom(PackMember type, PackMember scope)
    {
        return (type.SourceFile.Kind == scope.SourceFile.Kind) || type.HasModifier(PackMemberModifiers.Public);
    }

    /* The namespace itself and then each one containing it, as far as any exists in the pack. */
    private IEnumerable<PackNameSpace> GetNameSpaceAndParents(PackNameSpace nameSpace)
    {
        string? Name = nameSpace.SelfIdentifier.SourceCodeName;
        while (Name != null)
        {
            PackNameSpace? Found = _pack.TryGetNamespace(Name);
            if (Found != null)
            {
                yield return Found;
            }

            int SeparatorIndex = Name.LastIndexOf(KGVL.NAMESPACE_SEPARATOR);
            Name = (SeparatorIndex == -1) ? null : Name[..SeparatorIndex];
        }
    }
}
