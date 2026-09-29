using KeigValCompiler.Error;
using KeigValCompiler.Semantician.Member;
using KeigValCompiler.Semantician.Types;

namespace KeigValCompiler.Semantician.Resolver;

/* Checks what members do with those their types inherit, against C#'s rules. An override overrides the
 * nearest member of a base class it can see with the same name and signature, unless a nearer member of
 * that name and another kind hides it, and two members which only become the same once a base class's
 * generic parameters are given types leave it unknown which. What it overrides is virtual, abstract or an
 * override itself, and not sealed. It keeps that member's access and type, except that a function, or a
 * property or indexer without a setter, may have a type deriving from its type, and it overrides only
 * accessors that member has or inherits and it can see, each keeping its access. A class which is not
 * abstract overrides every abstract member it inherits, and every accessor of one. A member which hides one
 * it inherits is warned about unless it says "new", one saying "new" which hides nothing is warned about
 * too, and a class's member cannot hide an abstract one. Which members hide which is InheritedMembers'
 * to say. A type deriving from itself, reported by InheritanceChecker, is left alone. */
internal class OverrideChecker : IPackResolver
{
    // Static fields.
    private static readonly string[] _accessorKeywords = { KGVL.KEYWORD_GET, KGVL.KEYWORD_SET, KGVL.KEYWORD_INIT };


    // Private methods.
    private void CheckType(PackMember type, PackResolutionContext context)
    {
        if (type is not (PackClass or PackStruct or PackInterface))
        {
            return;
        }
        DeclaredType Instance = context.TypeReader.GetInstanceType(type);
        if (context.Hierarchy.IsInBaseCycle(Instance))
        {
            return;
        }

        IReadOnlyList<DeclaredType> Bases = (type is PackInterface) ? context.Hierarchy.GetInterfaces(Instance)
            : context.Hierarchy.GetBaseClasses(Instance).ToArray();
        List<(DeclaredType Holder, DeclaredSignature Signature)> Inherited = GetInheritedMembers(type, Bases,
            context);
        TypeSubstitution NoSubstitution = new();

        foreach (PackMember Member in MemberRelations.GetSignedMembers(type).Where(InheritedMembers.IsInheritable))
        {
            DeclaredSignature Signature = context.SignatureReader.Read(Member, NoSubstitution);
            if (!Signature.IsComplete)
            {
                continue;
            }
            if (Member.HasModifier(PackMemberModifiers.Override))
            {
                CheckOverride(Signature, type, Instance, context);
                continue;
            }

            (DeclaredType Holder, DeclaredSignature Signature) Hidden = Inherited.FirstOrDefault(
                entry => InheritedMembers.IsHiding(Member, Signature, entry.Signature.Member, entry.Signature));
            if (Hidden.Signature != null)
            {
                CheckHiding(Member, Hidden.Holder, Hidden.Signature.Member, type, context);
                continue;
            }
            (DeclaredType? Holder, PackMember? HiddenType) = FindHiddenType(Member, Signature, Bases, type);
            CheckHiding(Member, Holder, HiddenType, type, context);
        }

        foreach (PackMember NestedType in ((IPackTypeHolder)type).Types)
        {
            (DeclaredType Holder, DeclaredSignature Signature) HiddenMember = Inherited.FirstOrDefault(
                entry => InheritedMembers.IsHiding(NestedType, null, entry.Signature.Member, entry.Signature));
            (DeclaredType? Holder, PackMember? Hidden) = (HiddenMember.Signature != null)
                ? (HiddenMember.Holder, HiddenMember.Signature.Member)
                : FindHiddenType(NestedType, null, Bases, type);
            CheckHiding(NestedType, Holder, Hidden, type, context);
        }

        if ((type is PackClass) && !type.HasModifier(PackMemberModifiers.Abstract))
        {
            CheckAbstractMembers(type, Instance, context);
        }
    }

    private List<(DeclaredType Holder, DeclaredSignature Signature)> GetInheritedMembers(PackMember type,
        IEnumerable<DeclaredType> bases,
        PackResolutionContext context)
    {
        List<(DeclaredType Holder, DeclaredSignature Signature)> Inherited = new();
        foreach (DeclaredType Base in bases)
        {
            TypeSubstitution Substitution = TypeSubstitution.Of(Base);
            foreach (PackMember Member in MemberRelations.GetSignedMembers(Base.Declaration).Where(
                member => InheritedMembers.IsInheritable(member) && InheritedMembers.IsVisibleFrom(member, type)))
            {
                DeclaredSignature Signature = context.SignatureReader.Read(Member, Substitution);
                if (Signature.IsComplete)
                {
                    Inherited.Add((Base, Signature));
                }
            }
        }
        return Inherited;
    }

    /* The nearest nested type of a base type which a member hides, and the base type holding it. A
     * member's signature is null when it is a nested type itself. */
    private (DeclaredType? Holder, PackMember? Hidden) FindHiddenType(PackMember member,
        DeclaredSignature? signature,
        IEnumerable<DeclaredType> bases,
        PackMember type)
    {
        foreach (DeclaredType Base in bases)
        {
            PackMember? HiddenType = ((IPackTypeHolder)Base.Declaration).Types.FirstOrDefault(
                inheritedType => InheritedMembers.IsVisibleFrom(inheritedType, type)
                    && InheritedMembers.IsHiding(member, signature, inheritedType, null));
            if (HiddenType != null)
            {
                return (Base, HiddenType);
            }
        }
        return (null, null);
    }

    /* Whether an inherited member can be replaced by an override: it is virtual, abstract, an override
     * itself, or an interface's member which is abstract without saying so. */
    private bool IsVirtual(PackMember member)
    {
        return member.HasAnyModifier(PackMemberModifiers.Virtual, PackMemberModifiers.Abstract,
            PackMemberModifiers.Override) || MemberRelations.IsAbstract(member);
    }

    /* The member hidden, and what holds it, are null when nothing is. A class's or structure's member hiding
     * a virtual one of its own kind gets a warning saying it could override it instead; an interface's
     * cannot override. A class's member hiding an abstract one is an error, with "new" or without. */
    private void CheckHiding(PackMember member,
        DeclaredType? hiddenHolder,
        PackMember? hidden,
        PackMember type,
        PackResolutionContext context)
    {
        string Kind = MemberRelations.GetKindName(member);
        string Name = MemberRelations.GetDisplayName(member);
        bool IsNew = member.HasModifier(PackMemberModifiers.New);
        if ((hidden == null) || (hiddenHolder == null))
        {
            if (IsNew)
            {
                context.AddWarning(context.ErrorCreator.NewHidesNothing.CreateOptions(Kind, Name), member);
            }
            return;
        }

        string HiddenKind = MemberRelations.GetKindName(hidden);
        string TypeName = type.SelfIdentifier.SourceCodeName;
        bool IsHiddenAbstract = !MemberRelations.IsType(hidden)
            && hidden.HasModifier(PackMemberModifiers.Abstract);
        if ((type is PackClass) && IsHiddenAbstract)
        {
            context.AddError(context.ErrorCreator.HidesAbstractMember.CreateOptions(Kind, Name, HiddenKind,
                TypeName, hiddenHolder.ToString()), member);
        }
        if (IsNew)
        {
            return;
        }

        bool IsReplaceable = (type is not PackInterface) && (HiddenKind == Kind) && IsVirtual(hidden);
        WarningDefinition Warning = IsReplaceable ? context.ErrorCreator.MemberHidesVirtual
            : context.ErrorCreator.MemberHidesInherited;
        context.AddWarning(Warning.CreateOptions(Kind, Name, HiddenKind, TypeName, hiddenHolder.ToString()),
            member);
    }

    private void CheckOverride(DeclaredSignature signature,
        PackMember type,
        DeclaredType instance,
        PackResolutionContext context)
    {
        PackMember Member = signature.Member;
        string Kind = MemberRelations.GetKindName(Member);
        string Name = MemberRelations.GetDisplayName(Member);
        OverrideLookup Lookup = InheritedMembers.FindOverridden(signature, instance, type, context);

        if ((Lookup.Hiding != null) && (Lookup.Holder != null))
        {
            context.AddError(context.ErrorCreator.OverrideHiddenByOtherKind.CreateOptions(Kind, Name,
                Lookup.Holder.ToString(), MemberRelations.GetKindName(Lookup.Hiding)), Member);
        }
        else if ((Lookup.Overridden == null) || (Lookup.Holder == null))
        {
            context.AddError(context.ErrorCreator.NothingToOverride.CreateOptions(Kind, Name,
                type.SelfIdentifier.SourceCodeName), Member);
        }
        else if (Lookup.Duplicate != null)
        {
            context.AddError(context.ErrorCreator.OverrideAmbiguous.CreateOptions(Kind, Name,
                Lookup.Holder.ToString(), Lookup.Overridden.GetDisplayName()), Member);
        }
        else if (Lookup.Overridden.IsComplete)
        {
            CheckOverridden(signature, Lookup.Holder, Lookup.Overridden, type, context);
        }
    }

    private void CheckOverridden(DeclaredSignature signature,
        DeclaredType holder,
        DeclaredSignature overridden,
        PackMember type,
        PackResolutionContext context)
    {
        PackMember Member = signature.Member;
        PackMember Overridden = overridden.Member;
        string Kind = MemberRelations.GetKindName(Member);
        string Name = MemberRelations.GetDisplayName(Member);
        string HolderName = holder.ToString();

        if (!Overridden.HasAnyModifier(PackMemberModifiers.Virtual, PackMemberModifiers.Abstract,
            PackMemberModifiers.Override))
        {
            context.AddError(context.ErrorCreator.OverrideNotVirtual.CreateOptions(Kind, Name, HolderName),
                Member);
            return;
        }
        if (Overridden.HasModifier(PackMemberModifiers.Sealed))
        {
            context.AddError(context.ErrorCreator.OverrideSealed.CreateOptions(Kind, Name, HolderName), Member);
            return;
        }

        if (!signature.HasSameType(overridden) && !IsCovariant(signature, overridden, context))
        {
            context.AddError(context.ErrorCreator.OverrideReturnType.CreateOptions(Kind, Name,
                FormatType(signature.Type), HolderName, FormatType(overridden.Type)), Member);
        }

        bool IsAccessWrong = CheckAccess(Member, Overridden, HolderName, context);
        CheckOverriddenAccessors(signature, holder, overridden, !IsAccessWrong, type, context);
    }

    /* Whether an override's access differs from what it overrides, which is reported. A protected internal
     * member overridden from the other side of the library's boundary can only be reached there as
     * protected, as across C# assemblies. A private override, written so or private by default, and one
     * whose access modifier was taken off as one it cannot have, have been reported by ModifierChecker
     * already, and are not compared. */
    private bool CheckAccess(PackMember member,
        PackMember overridden,
        string holderName,
        PackResolutionContext context)
    {
        PackMemberModifiers Access = MemberRelations.GetEffectiveAccess(member);
        PackMemberModifiers Expected = MemberRelations.GetEffectiveAccess(overridden);
        if ((Expected == (PackMemberModifiers.Protected | PackMemberModifiers.Internal))
            && (member.SourceFile.Kind != overridden.SourceFile.Kind))
        {
            Expected = PackMemberModifiers.Protected;
        }

        bool IsAccessReported = (Access == PackMemberModifiers.Private)
            || ((member.RejectedModifiers & MemberRelations.ACCESS_MODIFIERS) != PackMemberModifiers.None);
        if (IsAccessReported || (Access == Expected))
        {
            return false;
        }
        context.AddError(context.ErrorCreator.OverrideAccess.CreateOptions(MemberRelations.GetKindName(member),
            MemberRelations.GetDisplayName(member), ModifierKeywords.FormatAccess(Access), holderName,
            ModifierKeywords.FormatAccess(Expected)), member);
        return true;
    }

    /* A function, or a property or indexer without a setter, can have a type deriving from what it
     * overrides has, as C#'s covariant return types allow. */
    private bool IsCovariant(DeclaredSignature signature,
        DeclaredSignature overridden,
        PackResolutionContext context)
    {
        bool IsReadOnly = signature.Member switch
        {
            PackProperty Property => (Property.SetFunction == null) && (Property.InitFunction == null),
            PackIndexer Indexer => Indexer.SetFunction == null,
            _ => false
        };
        if (((signature.Kind != DeclaredSignatureKind.Method) && !IsReadOnly) || (signature.Type == null)
            || (overridden.Type == null))
        {
            return false;
        }

        SemanticType OverriddenType = signature.MapFromOther(overridden, overridden.Type);
        return context.Hierarchy.IsReferenceType(signature.Type)
            && context.Hierarchy.IsSameOrDerived(signature.Type, OverriddenType);
    }

    /* Each accessor an override declares overrides the one of what it overrides, which that member has or
     * inherits, and which is not private to it. Each keeps that one's access, unless the override's own
     * access is wrong already. */
    private void CheckOverriddenAccessors(DeclaredSignature signature,
        DeclaredType holder,
        DeclaredSignature overridden,
        bool isAccessCompared,
        PackMember type,
        PackResolutionContext context)
    {
        PackMember Member = signature.Member;
        foreach (string Keyword in _accessorKeywords)
        {
            PackFunction? Accessor = InheritedMembers.GetAccessor(Member, Keyword);
            if (Accessor == null)
            {
                continue;
            }

            PackFunction? BaseAccessor = InheritedMembers.FindAccessor(overridden, holder, Keyword, type,
                context);
            if (BaseAccessor == null)
            {
                context.AddError(context.ErrorCreator.OverrideMissingAccessor.CreateOptions(
                    MemberRelations.GetKindName(Member), MemberRelations.GetDisplayName(Member), Keyword,
                    holder.ToString()), Member);
            }
            else if (isAccessCompared)
            {
                CheckAccess(Accessor, BaseAccessor, holder.ToString(), context);
            }
        }
    }

    /* A class which is not abstract has a body for every abstract member it inherits, and every accessor of
     * one: going from the class furthest back to the class itself, each abstract member, or each accessor of
     * an abstract property or indexer, is noted, and each override takes what it overrides away again. Only
     * an abstract class's abstract members count, since one in any other class has been reported. */
    private void CheckAbstractMembers(PackMember type, DeclaredType instance, PackResolutionContext context)
    {
        List<(DeclaredType Holder, DeclaredSignature Signature, string? Accessor)> Pending = new();
        IEnumerable<DeclaredType> Chain = context.Hierarchy.GetBaseClasses(instance).Reverse()
            .Concat(new DeclaredType[] { instance });
        foreach (DeclaredType Holder in Chain)
        {
            /* A record's positional parameters are not properties yet, so a property one of them would give
             * is taken to be there, rather than reported missing. */
            HashSet<string> PositionalNames = MemberRelations.GetPositionalParameters(Holder.Declaration)
                .Select(parameter => parameter.SelfIdentifier.SourceCodeName).ToHashSet();
            Pending.RemoveAll(pending => (pending.Signature.Kind == DeclaredSignatureKind.Property)
                && PositionalNames.Contains(pending.Signature.Name));

            TypeSubstitution Substitution = TypeSubstitution.Of(Holder);
            foreach (PackMember Member in MemberRelations.GetSignedMembers(Holder.Declaration)
                .Where(InheritedMembers.IsInheritable))
            {
                if (!Member.HasAnyModifier(PackMemberModifiers.Override, PackMemberModifiers.Abstract))
                {
                    continue;
                }

                DeclaredSignature Signature = context.SignatureReader.Read(Member, Substitution);
                if (Member.HasModifier(PackMemberModifiers.Override))
                {
                    PackMember? Overridden = Pending.FirstOrDefault(
                        entry => InheritedMembers.IsSameMember(Signature, entry.Signature)).Signature?.Member;
                    Pending.RemoveAll(
                        entry => IsTakenAway(entry.Signature, entry.Accessor, Overridden, Signature));
                }
                if (Member.HasModifier(PackMemberModifiers.Abstract) && !ReferenceEquals(Holder, instance)
                    && Holder.Declaration.HasModifier(PackMemberModifiers.Abstract) && Signature.IsComplete)
                {
                    Pending.AddRange(GetAbstractParts(Signature).Select(
                        accessor => (Holder, Signature, accessor)));
                }
            }
        }
        ReportAbstractMembers(type, Pending, context);
    }

    /* An override takes away the first pending member it overrides, or those of its accessors it declares.
     * One some of whose types did not resolve, which has been reported, is taken to override whatever of its
     * kind and name is pending, rather than leave it seeming missing. */
    private bool IsTakenAway(DeclaredSignature pending,
        string? accessor,
        PackMember? overridden,
        DeclaredSignature signature)
    {
        if (!signature.IsComplete)
        {
            return (pending.Kind == signature.Kind) && (pending.Name == signature.Name);
        }
        return ReferenceEquals(pending.Member, overridden)
            && ((accessor == null) || (InheritedMembers.GetAccessor(signature.Member, accessor) != null));
    }

    /* What of an abstract member needs overriding: each accessor of a property or an indexer, which is
     * given, or for any other member, the member itself, given as null. */
    private IEnumerable<string?> GetAbstractParts(DeclaredSignature signature)
    {
        if (signature.Member is not (PackProperty or PackIndexer))
        {
            return new string?[] { null };
        }
        return _accessorKeywords.Where(keyword => InheritedMembers.GetAccessor(signature.Member, keyword) != null);
    }

    /* A member none of whose accessors is overridden is reported as a whole, and one only some of whose are
     * by each accessor left. */
    private void ReportAbstractMembers(PackMember type,
        List<(DeclaredType Holder, DeclaredSignature Signature, string? Accessor)> pending,
        PackResolutionContext context)
    {
        string TypeName = type.SelfIdentifier.SourceCodeName;
        IEqualityComparer<PackMember> ByReference = ReferenceEqualityComparer.Instance;
        foreach (IGrouping<PackMember, (DeclaredType Holder, DeclaredSignature Signature, string? Accessor)> Group
            in pending.GroupBy(entry => entry.Signature.Member, ByReference))
        {
            (DeclaredType Holder, DeclaredSignature Signature, string? Accessor) First = Group.First();
            string Kind = MemberRelations.GetKindName(First.Signature.Member);
            string Name = First.Signature.GetDisplayName();
            int AccessorCount = First.Signature.Member.SubMembers.Count();
            if ((First.Accessor == null) || (Group.Count() == AccessorCount))
            {
                context.AddError(context.ErrorCreator.AbstractMemberNotImplemented.CreateOptions(TypeName, Kind,
                    Name, First.Holder.ToString()), type);
                continue;
            }
            foreach ((DeclaredType Holder, DeclaredSignature Signature, string? Accessor) Entry in Group)
            {
                context.AddError(context.ErrorCreator.AbstractAccessorNotImplemented.CreateOptions(TypeName,
                    Entry.Accessor!, Kind, Name, Entry.Holder.ToString()), type);
            }
        }
    }

    private string FormatType(SemanticType? type)
    {
        return type?.ToString() ?? KGVL.KEYWORD_VOID;
    }


    // Inherited methods.
    public void ResolvePack(PackResolutionContext context)
    {
        ArgumentNullException.ThrowIfNull(context, nameof(context));

        foreach (PackMember Type in context.Pack.Types)
        {
            CheckType(Type, context);
        }
    }
}
