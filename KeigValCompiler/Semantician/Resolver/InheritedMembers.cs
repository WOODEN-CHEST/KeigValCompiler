using KeigValCompiler.Semantician.Member;
using KeigValCompiler.Semantician.Types;

namespace KeigValCompiler.Semantician.Resolver;

/* How a type's members relate to those of the types it derives from, by C#'s rules, as the checks of
 * overriding, hiding, interface implementation and constraints all need it. */
internal static class InheritedMembers
{
    // Internal static methods.
    /* Whether a member is inherited by name, so that a derived type's members can override, hide or
     * implement it: not a constructor, an operator or an explicit implementation. */
    internal static bool IsInheritable(PackMember member)
    {
        ArgumentNullException.ThrowIfNull(member, nameof(member));

        return (member is not PackConstructor) && (MemberRelations.GetExplicitInterface(member) == null)
            && (MemberRelations.GetOperatorOverload(member) == null);
    }

    /* A private member is not inherited, and an internal one only on its own side of the standard library's
     * boundary, as for a C# assembly. */
    internal static bool IsVisibleFrom(PackMember member, PackMember derived)
    {
        ArgumentNullException.ThrowIfNull(member, nameof(member));
        ArgumentNullException.ThrowIfNull(derived, nameof(derived));

        PackMemberModifiers Access = MemberRelations.GetEffectiveAccess(member);
        bool IsSameAssembly = member.SourceFile.Kind == derived.SourceFile.Kind;
        return Access switch
        {
            PackMemberModifiers.Private => false,
            PackMemberModifiers.Internal => IsSameAssembly,
            PackMemberModifiers.Private | PackMemberModifiers.Protected => IsSameAssembly,
            _ => true
        };
    }

    /* How many generic parameters a function or a type declares. Other members declare none. */
    internal static int GetArity(PackMember member)
    {
        ArgumentNullException.ThrowIfNull(member, nameof(member));
        return (member as IGenericParameterHolder)?.GenericParameters.Count ?? 0;
    }

    /* Whether two members are the same member of two types, as an override and what it overrides are, or
     * an implementation and what it implements: of one kind, one name or operator, the same parameters
     * each passed the same way, and for a conversion, the same result. */
    internal static bool IsSameMember(DeclaredSignature first, DeclaredSignature second)
    {
        ArgumentNullException.ThrowIfNull(first, nameof(first));
        ArgumentNullException.ThrowIfNull(second, nameof(second));

        if (first.Kind != second.Kind)
        {
            return false;
        }
        return first.Kind switch
        {
            DeclaredSignatureKind.Indexer => first.HasSameParameters(second, true),
            DeclaredSignatureKind.Method => (first.Name == second.Name) && first.HasSameParameters(second, true),
            DeclaredSignatureKind.Operator => (first.Operator == second.Operator)
                && first.HasSameParameters(second, true),
            DeclaredSignatureKind.Conversion => (first.Operator == second.Operator)
                && first.HasSameParameters(second, true) && first.HasSameType(second),
            _ => first.Name == second.Name
        };
    }

    /* Whether a derived type's member hides an inherited one, as C# decides: a function hides the functions
     * of its name and parameters, and the other members of its name which declare no generic parameters or
     * as many as it does; any other member hides the members of its name which declare as many generic
     * parameters as it does; an indexer hides the indexers of its parameters. The signatures are needed for
     * functions and indexers, and are null for nested types, which have none. */
    internal static bool IsHiding(PackMember member,
        DeclaredSignature? signature,
        PackMember inherited,
        DeclaredSignature? inheritedSignature)
    {
        ArgumentNullException.ThrowIfNull(member, nameof(member));
        ArgumentNullException.ThrowIfNull(inherited, nameof(inherited));

        if ((member is PackIndexer) || (inherited is PackIndexer))
        {
            return (member is PackIndexer) && (inherited is PackIndexer)
                && signature!.HasSameParameters(inheritedSignature!, true);
        }
        if (member.SelfIdentifier.SourceCodeName != inherited.SelfIdentifier.SourceCodeName)
        {
            return false;
        }

        bool IsMethod = IsMethodMember(member);
        bool IsInheritedMethod = IsMethodMember(inherited);
        if (IsMethod && IsInheritedMethod)
        {
            return signature!.HasSameParameters(inheritedSignature!, true);
        }
        if (IsMethod)
        {
            return (GetArity(inherited) == 0) || (GetArity(inherited) == GetArity(member));
        }
        return GetArity(inherited) == GetArity(member);
    }

    /* Looks for what an override overrides, through the base classes of the type declaring it, nearest
     * first, among the members the type it is declared in can see. The holder is that type as the
     * signature sees it. */
    internal static OverrideLookup FindOverridden(DeclaredSignature signature,
        DeclaredType holder,
        PackMember viewer,
        PackResolutionContext context)
    {
        ArgumentNullException.ThrowIfNull(signature, nameof(signature));
        ArgumentNullException.ThrowIfNull(holder, nameof(holder));
        ArgumentNullException.ThrowIfNull(viewer, nameof(viewer));
        ArgumentNullException.ThrowIfNull(context, nameof(context));

        foreach (DeclaredType Base in context.Hierarchy.GetBaseClasses(holder))
        {
            TypeSubstitution Substitution = TypeSubstitution.Of(Base);
            DeclaredSignature? Overridden = null;
            DeclaredSignature? Duplicate = null;
            PackMember? Hiding = null;
            foreach (PackMember Candidate in MemberRelations.GetSignedMembers(Base.Declaration)
                .Where(member => IsInheritable(member) && IsVisibleFrom(member, viewer)
                    && (GetName(member) == signature.Name)))
            {
                DeclaredSignature CandidateSignature = context.SignatureReader.Read(Candidate, Substitution);
                if (!IsSameMember(signature, CandidateSignature))
                {
                    Hiding ??= IsHidingOverride(signature, Candidate) ? Candidate : null;
                }
                else if (Overridden == null)
                {
                    Overridden = CandidateSignature;
                }
                else
                {
                    Duplicate ??= CandidateSignature;
                }
            }
            Hiding ??= ((IPackTypeHolder)Base.Declaration).Types.FirstOrDefault(
                nestedType => (nestedType.SelfIdentifier.SourceCodeName == signature.Name)
                    && IsVisibleFrom(nestedType, viewer) && IsHidingOverride(signature, nestedType));

            if ((Overridden != null) || (Hiding != null))
            {
                return new(Base, Overridden, Duplicate, (Overridden == null) ? Hiding : null);
            }
        }
        return new(null, null, null, null);
    }

    /* An accessor a property or an indexer has: its own, or when it is an override which does not declare
     * that accessor, the one of what it overrides, as in C#, where an override can override only some of
     * the accessors it inherits. Null when there is none the viewer can see. The holder is the type
     * declaring the member, as the signature sees it. */
    internal static PackFunction? FindAccessor(DeclaredSignature member,
        DeclaredType holder,
        string keyword,
        PackMember viewer,
        PackResolutionContext context)
    {
        ArgumentNullException.ThrowIfNull(member, nameof(member));
        ArgumentNullException.ThrowIfNull(holder, nameof(holder));
        ArgumentNullException.ThrowIfNull(keyword, nameof(keyword));
        ArgumentNullException.ThrowIfNull(viewer, nameof(viewer));
        ArgumentNullException.ThrowIfNull(context, nameof(context));

        HashSet<PackMember> Followed = new(ReferenceEqualityComparer.Instance);
        DeclaredSignature? Current = member;
        DeclaredType? CurrentHolder = holder;
        while ((Current != null) && (CurrentHolder != null) && Followed.Add(Current.Member))
        {
            PackFunction? Accessor = GetAccessor(Current.Member, keyword);
            if (Accessor != null)
            {
                return IsVisibleFrom(Accessor, viewer) ? Accessor : null;
            }
            if (!Current.Member.HasModifier(PackMemberModifiers.Override))
            {
                return null;
            }

            OverrideLookup Lookup = FindOverridden(Current, CurrentHolder, viewer, context);
            Current = Lookup.Overridden;
            CurrentHolder = Lookup.Holder;
        }
        return null;
    }

    /* The accessor of a keyword a property or an indexer declares itself, if it does. */
    internal static PackFunction? GetAccessor(PackMember member, string keyword)
    {
        ArgumentNullException.ThrowIfNull(member, nameof(member));
        ArgumentNullException.ThrowIfNull(keyword, nameof(keyword));

        if (member is not (PackProperty or PackIndexer))
        {
            return null;
        }
        return member.SubMembers.Cast<PackFunction>().FirstOrDefault(
            accessor => accessor.SelfIdentifier.SourceCodeName == keyword);
    }

    /* The interface an explicit implementation names, and the member of it which it implements: one of the
     * same kind, name, parameters and staticness, which an implementation can replace. Null when it names
     * no interface which could be read, which has been reported, or when the interface has no such member.
     * The interface is given in terms of the type declaring the implementation. */
    internal static (DeclaredType Interface, DeclaredSignature Implemented)? FindExplicitlyImplemented(
        PackMember member,
        PackResolutionContext context)
    {
        ArgumentNullException.ThrowIfNull(member, nameof(member));
        ArgumentNullException.ThrowIfNull(context, nameof(context));

        TypeTargetIdentifier? Named = MemberRelations.GetExplicitInterface(member);
        if ((Named == null) || Named.IsArrayOrNullable
            || (context.TypeReader.Read(Named) is not DeclaredType InterfaceType)
            || (InterfaceType.Declaration is not PackInterface))
        {
            return null;
        }
        DeclaredSignature Signature = context.SignatureReader.Read(member, new());
        if (!Signature.IsComplete)
        {
            return null;
        }

        TypeSubstitution Substitution = TypeSubstitution.Of(InterfaceType);
        bool IsOperator = MemberRelations.GetOperatorOverload(member) != null;
        foreach (PackMember Candidate in MemberRelations.GetSignedMembers(InterfaceType.Declaration)
            .Where(IsExplicitlyImplementable))
        {
            DeclaredSignature CandidateSignature = context.SignatureReader.Read(Candidate, Substitution);
            bool IsSameStaticness = IsOperator || (member.HasModifier(PackMemberModifiers.Static)
                == Candidate.HasModifier(PackMemberModifiers.Static));
            if (IsSameStaticness && IsSameMember(Signature, CandidateSignature))
            {
                return (InterfaceType, CandidateSignature);
            }
        }
        return null;
    }

    /* Whether an interface's member can be implemented explicitly: one an implementation can replace, so
     * not a private or sealed one, nor a static one which is neither abstract nor virtual, and not a field,
     * a constructor or an explicit implementation itself. */
    internal static bool IsExplicitlyImplementable(PackMember member)
    {
        ArgumentNullException.ThrowIfNull(member, nameof(member));

        bool IsReplaceable = !member.HasModifier(PackMemberModifiers.Static)
            || member.HasAnyModifier(PackMemberModifiers.Abstract, PackMemberModifiers.Virtual);
        return (member is not (PackConstructor or PackField))
            && (MemberRelations.GetExplicitInterface(member) == null)
            && (MemberRelations.GetEffectiveAccess(member) != PackMemberModifiers.Private)
            && !member.HasModifier(PackMemberModifiers.Sealed) && IsReplaceable;
    }


    // Private static methods.
    /* A function other than a constructor, an operator or an accessor. */
    private static bool IsMethodMember(PackMember member)
    {
        return (member is PackFunction) && (member is not PackConstructor)
            && (MemberRelations.GetOperatorOverload(member) == null) && !MemberRelations.IsAccessor(member);
    }

    /* A member's name as a signature has it, which for an indexer is "this". */
    private static string GetName(PackMember member)
    {
        return (member is PackIndexer) ? KGVL.KEYWORD_THIS : member.SelfIdentifier.SourceCodeName;
    }

    /* Whether a member of another kind with an override's name, in a base class, hides everything further
     * back the override could have overridden: for a function, a member which is not one and declares as
     * many generic parameters as it does; for a property or an event, a function, or a member of another
     * kind which declares none. Indexers are only ever hidden by other indexers. */
    private static bool IsHidingOverride(DeclaredSignature signature, PackMember candidate)
    {
        if (signature.Kind == DeclaredSignatureKind.Indexer)
        {
            return false;
        }
        if (signature.Kind == DeclaredSignatureKind.Method)
        {
            return !IsMethodMember(candidate) && (GetArity(candidate) == signature.GenericParameters.Count);
        }
        return IsMethodMember(candidate) || (GetArity(candidate) == 0);
    }
}
