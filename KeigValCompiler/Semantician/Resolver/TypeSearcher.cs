using KeigValCompiler.Semantician.Member;

namespace KeigValCompiler.Semantician.Resolver;

/* Finds the type a name written in a declaration means, looking where C# looks and taking the first
 * place with a match. First the generic parameters of the member the name is written in and of each
 * type around it, together with those types' nested types, from the innermost outwards. Then the types
 * of the member's namespace and of each namespace containing it, so "A.B" before "A". Last the types
 * of the namespaces the file imports, where two matches are ambiguous rather than one winning.
 *
 * A type matches by its name and its number of generic parameters, so "Foo<int>" never finds "Foo".
 * Accessibility is not considered yet. */
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

        for (PackMember? Member = scope; Member != null; Member = Member.ParentItem?.Target as PackMember)
        {
            GenericTypeParameter? Parameter = (typeArgumentCount == 0) ? FindGenericParameter(name, Member) : null;
            if (Parameter != null)
            {
                return TypeSearchResult.Found(Parameter);
            }
            if (Member is IPackTypeHolder TypeHolder)
            {
                PackMember? NestedType = FindType(name, typeArgumentCount, TypeHolder.Types,
                    ref OtherGenericParameterCount);
                if (NestedType != null)
                {
                    return TypeSearchResult.Found(NestedType);
                }
            }
        }

        foreach (PackNameSpace NameSpace in GetNameSpaceAndParents(scope.NameSpace))
        {
            PackMember? NameSpaceType = FindType(name, typeArgumentCount, NameSpace.Types,
                ref OtherGenericParameterCount);
            if (NameSpaceType != null)
            {
                return TypeSearchResult.Found(NameSpaceType);
            }
        }

        List<PackMember> ImportedTypes = new();
        foreach (PackNameSpace Import in scope.SourceFile.NamespaceImports)
        {
            PackMember? ImportedType = FindType(name, typeArgumentCount, Import.Types,
                ref OtherGenericParameterCount);
            if (ImportedType != null)
            {
                ImportedTypes.Add(ImportedType);
            }
        }

        return ImportedTypes.Count switch
        {
            0 => TypeSearchResult.NotFound(OtherGenericParameterCount),
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

    /* Also notes the generic parameter count of a type which has the name but not the count. */
    private PackMember? FindType(string name,
        int genericParameterCount,
        IEnumerable<PackMember> types,
        ref int? otherGenericParameterCount)
    {
        foreach (PackMember Type in types)
        {
            if (Type.SelfIdentifier.SourceCodeName != name)
            {
                continue;
            }

            int TypeGenericParameterCount = TypeDeclarationResolver.GetGenericParameterCount(Type);
            if (TypeGenericParameterCount == genericParameterCount)
            {
                return Type;
            }
            otherGenericParameterCount ??= TypeGenericParameterCount;
        }
        return null;
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
