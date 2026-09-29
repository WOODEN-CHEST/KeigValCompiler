using KeigValCompiler.Semantician.Member;

namespace KeigValCompiler.Semantician.Resolver;

/* Checks the names declarations give, where C# does not let them coincide: two constants of one enum,
 * two parameters or two generic parameters of one member, a generic parameter named like what declares
 * it, a parameter named like one of its function's generic parameters, a member named like the type
 * holding it or like one of that type's generic parameters, and a member named like a namespace beside
 * it. Of the members a namespace or type holds, only functions can share a name, as overloads, and
 * types, whose numbers of generic parameters tell them apart; two types which cannot be told apart are
 * TypeDeclarationResolver's to report. A generic parameter hiding one of a type around it is only warned
 * about, as in C#. Two functions with the same parameters need their types compared, and are not checked
 * here. */
internal class DeclarationNameChecker : IPackResolver
{
    // Private methods.
    private void CheckEnumConstants(PackEnumeration enumeration, PackResolutionContext context)
    {
        Dictionary<string, PackEnumerationConstant> ConstantsByName = new();
        foreach (PackEnumerationConstant Constant in enumeration.Constants)
        {
            if (ConstantsByName.TryGetValue(Constant.Name, out PackEnumerationConstant? EarlierConstant))
            {
                context.AddError(context.ErrorCreator.DuplicateEnumConstant.CreateOptions(
                    enumeration.SelfIdentifier.SourceCodeName, Constant.Name,
                    EarlierConstant.SourceFileOrigin.Line), enumeration.SourceFile, Constant.SourceFileOrigin);
                continue;
            }
            ConstantsByName.Add(Constant.Name, Constant);
        }
    }

    private void CheckParameterNames(PackMember member, PackResolutionContext context)
    {
        FunctionParameterCollection? Parameters = member switch
        {
            PackFunction Function => Function.Parameters,
            PackIndexer Indexer => Indexer.Parameters,
            PackDelegate Delegate => Delegate.Parameters,
            _ => null
        };
        if (Parameters == null)
        {
            return;
        }

        foreach (string Name in GetRepeatedNames(Parameters.Select(parameter => parameter.SelfIdentifier)))
        {
            context.AddError(context.ErrorCreator.DuplicateParameterName.CreateOptions(
                MemberRelations.GetKindName(member), MemberRelations.GetDisplayName(member), Name), member);
        }

        /* Only a function's generic parameters rule out a parameter's name. A delegate's do not, in C#. */
        if (member is not PackFunction GenericFunction)
        {
            return;
        }
        foreach (FunctionParameter Parameter in Parameters)
        {
            string Name = Parameter.SelfIdentifier.SourceCodeName;
            if (GenericFunction.GenericParameters.GetBySourceCodeName(Name) != null)
            {
                context.AddError(context.ErrorCreator.ParameterNamedLikeGenericParameter.CreateOptions(Name,
                    MemberRelations.GetKindName(member), MemberRelations.GetDisplayName(member)), member);
            }
        }
    }

    private void CheckGenericParameterNames(PackMember member, PackResolutionContext context)
    {
        if ((member is not IGenericParameterHolder GenericsHolder)
            || (GenericsHolder.GenericParameters.Count == 0))
        {
            return;
        }

        string Kind = MemberRelations.GetKindName(member);
        string Name = MemberRelations.GetDisplayName(member);
        foreach (string RepeatedName in GetRepeatedNames(GenericsHolder.GenericParameters
            .Select(parameter => parameter.SelfIdentifier)))
        {
            context.AddError(context.ErrorCreator.DuplicateGenericParameterName.CreateOptions(Kind, Name,
                RepeatedName), member);
        }

        foreach (GenericTypeParameter Parameter in GenericsHolder.GenericParameters)
        {
            string ParameterName = Parameter.SelfIdentifier.SourceCodeName;
            if (ParameterName == member.SelfIdentifier.SourceCodeName)
            {
                context.AddError(context.ErrorCreator.GenericParameterNamedLikeHolder.CreateOptions(ParameterName,
                    Kind), member);
            }

            PackMember? OuterHolder = FindOuterGenericParameterHolder(member, ParameterName);
            if (OuterHolder != null)
            {
                context.AddWarning(context.ErrorCreator.GenericParameterHidesOuter.CreateOptions(ParameterName,
                    Kind, Name, MemberRelations.GetKindName(OuterHolder),
                    MemberRelations.GetDisplayName(OuterHolder)), member);
            }
        }
    }

    /* The innermost type around a member which has a generic parameter of the given name. */
    private PackMember? FindOuterGenericParameterHolder(PackMember member, string parameterName)
    {
        for (PackMember? Holder = MemberRelations.GetHoldingMember(member); Holder != null;
            Holder = MemberRelations.GetHoldingMember(Holder))
        {
            if ((Holder is IGenericParameterHolder GenericsHolder)
                && (GenericsHolder.GenericParameters.GetBySourceCodeName(parameterName) != null))
            {
                return Holder;
            }
        }
        return null;
    }

    /* Every name given more than once, each only once, in the order of their second use. */
    private IEnumerable<string> GetRepeatedNames(IEnumerable<Identifier> identifiers)
    {
        HashSet<string> Seen = new();
        HashSet<string> Repeated = new();
        foreach (Identifier Name in identifiers)
        {
            if (!Seen.Add(Name.SourceCodeName) && Repeated.Add(Name.SourceCodeName))
            {
                yield return Name.SourceCodeName;
            }
        }
    }

    /* The members of a namespace or type in the order they are declared in, across files, and each
     * against those declared before it, and against the names of what holds them. */
    private void CheckMemberNames(object holder,
        string holderName,
        Dictionary<PackSourceFile, int> fileOrder,
        Dictionary<SourceFileKind, HashSet<string>> nameSpaces,
        PackResolutionContext context)
    {
        PackMember[] Members = GetNamedMembers(holder)
            .OrderBy(member => fileOrder[member.SourceFile])
            .ThenBy(member => member.SourceFileOrigin.Line)
            .ToArray();

        Dictionary<string, List<PackMember>> MembersByName = new();
        foreach (PackMember Member in Members)
        {
            string Name = Member.SelfIdentifier.SourceCodeName;
            if (holder is PackMember HolderType)
            {
                CheckNameAgainstHolder(Member, HolderType, context);
            }
            else
            {
                string NameSpaceName = holderName + KGVL.NAMESPACE_SEPARATOR + Name;
                if (nameSpaces[Member.SourceFile.Kind].Contains(NameSpaceName))
                {
                    context.AddError(context.ErrorCreator.MemberNamedLikeNamespace.CreateOptions(
                        MemberRelations.GetKindName(Member), Name, NameSpaceName, holderName), Member);
                }
            }

            if (!MembersByName.TryGetValue(Name, out List<PackMember>? EarlierMembers))
            {
                MembersByName.Add(Name, new() { Member });
                continue;
            }

            PackMember? Clash = EarlierMembers.FirstOrDefault(earlier => IsNameClash(earlier, Member));
            if (Clash != null)
            {
                context.AddError(context.ErrorCreator.DuplicateMemberName.CreateOptions(
                    MemberRelations.GetKindName(Member), Name, MemberRelations.GetKindName(Clash),
                    Clash.SourceFile.Path, Clash.SourceFileOrigin.Line, holderName), Member);
            }
            EarlierMembers.Add(Member);
        }
    }

    /* A type's members share one space of names with its generic parameters, and none can have the type's
     * own name, except in an interface, whose instance members can, as in C#. */
    private void CheckNameAgainstHolder(PackMember member, PackMember holder, PackResolutionContext context)
    {
        string Name = member.SelfIdentifier.SourceCodeName;
        bool IsStatic = member.HasAnyModifier(PackMemberModifiers.Static, PackMemberModifiers.Const);
        if ((Name == holder.SelfIdentifier.SourceCodeName) && ((holder is not PackInterface) || IsStatic))
        {
            context.AddError(context.ErrorCreator.MemberNamedLikeType.CreateOptions(
                MemberRelations.GetKindName(member), Name, MemberRelations.GetKindName(holder)), member);
        }

        if ((holder is IGenericParameterHolder GenericsHolder)
            && (GenericsHolder.GenericParameters.GetBySourceCodeName(Name) != null))
        {
            context.AddError(context.ErrorCreator.MemberNamedLikeGenericParameter.CreateOptions(
                MemberRelations.GetKindName(member), Name, MemberRelations.GetKindName(holder),
                holder.SelfIdentifier.SourceCodeName), member);
        }
    }

    /* The members which share one space of names: not constructors, which are named after their type,
     * nor operators and indexers, which have no names, nor explicit implementations, which are named
     * through their interface. */
    private IEnumerable<PackMember> GetNamedMembers(object holder)
    {
        List<PackMember> Members = new();
        if (holder is IPackTypeHolder TypeHolder)
        {
            Members.AddRange(TypeHolder.Types);
        }
        if (holder is IPackFieldHolder FieldHolder)
        {
            Members.AddRange(FieldHolder.Fields);
        }
        if (holder is IPackEventHolder EventHolder)
        {
            Members.AddRange(EventHolder.Events);
        }
        if (holder is IPackFunctionHolder FunctionHolder)
        {
            Members.AddRange(FunctionHolder.Properties.Where(property => property.ExplicitInterface == null));
            Members.AddRange(FunctionHolder.Functions.Where(
                function => (function is not PackConstructor) && (function.ExplicitInterface == null)));
        }
        return Members;
    }

    private bool IsNameClash(PackMember first, PackMember second)
    {
        bool AreFunctions = (first is PackFunction) && (second is PackFunction);
        bool AreTypes = MemberRelations.IsType(first) && MemberRelations.IsType(second);
        return !AreFunctions && !AreTypes;
    }


    // Inherited methods.
    public void ResolvePack(PackResolutionContext context)
    {
        ArgumentNullException.ThrowIfNull(context, nameof(context));

        foreach (PackEnumeration Enumeration in context.Pack.Enums)
        {
            CheckEnumConstants(Enumeration, context);
        }
        foreach (PackMember Member in context.Pack.Members)
        {
            CheckParameterNames(Member, context);
            CheckGenericParameterNames(Member, context);
        }

        Dictionary<PackSourceFile, int> FileOrder = new();
        foreach (PackSourceFile SourceFile in context.Pack.SourceFiles)
        {
            FileOrder.Add(SourceFile, FileOrder.Count);
        }

        /* A member only clashes with a namespace on its own side of the standard library's boundary, as in C#,
         * where the two would be in different assemblies. */
        Dictionary<SourceFileKind, HashSet<string>> NameSpaces = new();
        foreach (SourceFileKind Kind in Enum.GetValues<SourceFileKind>())
        {
            NameSpaces.Add(Kind, context.Pack.GetExistingNamespaceNames(Kind));
        }
        foreach (PackNameSpace NameSpace in context.Pack.NameSpaces)
        {
            CheckMemberNames(NameSpace, NameSpace.SelfIdentifier.SourceCodeName, FileOrder, NameSpaces, context);
        }
        foreach (PackMember Type in context.Pack.Types)
        {
            CheckMemberNames(Type, MemberRelations.GetDisplayName(Type), FileOrder, NameSpaces, context);
        }
    }
}
