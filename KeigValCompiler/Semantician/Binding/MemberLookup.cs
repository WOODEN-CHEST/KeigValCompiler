using KeigValCompiler.Semantician.Member;
using KeigValCompiler.Semantician.Resolver;
using KeigValCompiler.Semantician.Types;

namespace KeigValCompiler.Semantician.Binding;

/* C#'s member lookup (§12.5): what a name means in a type, or in a namespace, which in KGVL may hold fields,
 * properties, functions and events as well as types.
 *
 * In a type, the members of the name which can be used from where it is written are gathered from the type and
 * every type it derives from: for a class, structure, enum or delegate its base classes, ending with object;
 * for an interface every interface it derives from, and object; for a generic parameter what its constraints
 * name, and object. Constructors, operators, indexers, accessors and explicit implementations are found by no
 * name. A function which overrides another is not found itself, the function it overrides standing for it, as
 * calls choose among original declarations; a property or an event which overrides is found, as Roslyn finds
 * it, and hides what it overrides, so that a member is the one of the type nearest the lookup's. Written with
 * type arguments, a name finds only members with as many generic parameters; written without, it finds no
 * generic type, but does find generic functions, whose types a call may infer. A member of the name passed over
 * for that is noted, for a better error.
 *
 * Then, as in C#, a member hides what the types it derives from declare: a field, property or event hides
 * everything there, a function everything but functions, and a type everything but types with another number
 * of generic parameters. What is left is one member, a group of functions, or, when members of different kinds
 * are left, an ambiguity. */
internal sealed class MemberLookup
{
    // Private fields.
    private readonly PackResolutionContext _context;
    private readonly IBaseTypeSource _bases;


    // Constructors.
    internal MemberLookup(PackResolutionContext context, IBaseTypeSource bases)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _bases = bases ?? throw new ArgumentNullException(nameof(bases));
    }


    // Internal methods.
    /* The members of a name in a type, as the viewer, the member whose body the name is written in, can see
     * them. */
    internal MemberLookupResult LookupInType(SemanticType type, string name, int typeArgumentCount,
        PackMember viewer)
    {
        ArgumentNullException.ThrowIfNull(type, nameof(type));
        ArgumentNullException.ThrowIfNull(name, nameof(name));
        ArgumentNullException.ThrowIfNull(viewer, nameof(viewer));

        if ((type is DeclaredType { Declaration: PackEnumeration Enumeration } EnumType)
            && (typeArgumentCount == 0))
        {
            PackEnumerationConstant? Constant = Enumeration.Constants.FirstOrDefault(
                constant => constant.Name == name);
            if (Constant != null)
            {
                return MemberLookupResult.FoundEnumConstant(Constant, EnumType);
            }
        }

        List<FoundMember> Found = new();
        PackMember? Inaccessible = null;
        PackMember? OtherArity = null;
        foreach (DeclaredType Holder in GetSearchedTypes(type))
        {
            foreach (PackMember Member in GetNamedMembers(Holder.Declaration, name))
            {
                if (!HasArity(Member, typeArgumentCount))
                {
                    OtherArity ??= Member;
                }
                else if (IsAccessible(Member, viewer))
                {
                    Found.Add(new(Member, Holder));
                }
                else
                {
                    Inaccessible ??= Member;
                }
            }
        }

        List<FoundMember> Unhidden = RemoveHidden(Found);
        if (type is GenericParameterType)
        {
            Unhidden = RemoveInterfaceMembersHiddenByClasses(Unhidden);
        }
        return (Unhidden.Count > 0) ? MemberLookupResult.Found(Unhidden)
            : MemberLookupResult.NotFound(Inaccessible, OtherArity);
    }

    /* The fields, properties, functions and events of a name a namespace holds, which the viewer can see. Types
     * and namespaces are looked for as TypeSearcher looks for them. */
    internal MemberLookupResult LookupInNameSpace(PackNameSpace nameSpace, string name, int typeArgumentCount,
        PackMember viewer)
    {
        ArgumentNullException.ThrowIfNull(nameSpace, nameof(nameSpace));
        ArgumentNullException.ThrowIfNull(name, nameof(name));
        ArgumentNullException.ThrowIfNull(viewer, nameof(viewer));

        List<FoundMember> Found = new();
        PackMember? Inaccessible = null;
        PackMember? OtherArity = null;
        foreach (PackMember Member in MemberRelations.GetSignedMembers(nameSpace).Where(
            member => IsFoundByName(member, name)))
        {
            if (!HasArity(Member, typeArgumentCount))
            {
                OtherArity ??= Member;
            }
            else if (IsNameSpaceMemberVisible(Member, viewer))
            {
                Found.Add(new(Member, null));
            }
            else
            {
                Inaccessible ??= Member;
            }
        }
        return (Found.Count > 0) ? MemberLookupResult.Found(Found)
            : MemberLookupResult.NotFound(Inaccessible, OtherArity);
    }

    /* Whether a member of a type can be used where the viewer is, by its own access, the type holding it being
     * taken to be usable, as for any member a lookup in that type finds. */
    internal bool IsAccessible(PackMember member, PackMember viewer)
    {
        ArgumentNullException.ThrowIfNull(member, nameof(member));
        ArgumentNullException.ThrowIfNull(viewer, nameof(viewer));

        if (MemberRelations.GetHoldingMember(member) == null)
        {
            return IsNameSpaceMemberVisible(member, viewer);
        }
        return AccessDomains.IsAccessibleFrom(member, viewer, _bases);
    }


    // Private methods.
    /* The type a lookup starts in, then every type it derives from, each as the first sees it. */
    private List<DeclaredType> GetSearchedTypes(SemanticType type)
    {
        List<DeclaredType> Types = new();
        AddSearchedTypes(Types, type, new(ReferenceEqualityComparer.Instance));

        DeclaredType? ObjectType = _context.Hierarchy.GetObjectType();
        if ((ObjectType != null) && !Types.Contains(ObjectType))
        {
            Types.Add(ObjectType);
        }
        return Types;
    }

    private void AddSearchedTypes(List<DeclaredType> types, SemanticType type,
        HashSet<GenericTypeParameter> followed)
    {
        if (type is GenericParameterType Parameter)
        {
            if (!followed.Add(Parameter.Parameter))
            {
                return;
            }
            foreach (SemanticType Constraint in _context.Constraints.GetTypeConstraints(Parameter.Parameter))
            {
                AddSearchedTypes(types, Constraint, followed);
            }
            return;
        }
        if (type is not DeclaredType Declared)
        {
            return;
        }

        IEnumerable<DeclaredType> Bases = (Declared.Declaration is PackInterface)
            ? _context.Hierarchy.GetInterfaces(Declared) : _context.Hierarchy.GetBaseClasses(Declared);
        foreach (DeclaredType Searched in Bases.Prepend(Declared).Where(searched => !types.Contains(searched)))
        {
            types.Add(Searched);
        }
    }

    /* The members a type declares itself which a name can find. */
    private IEnumerable<PackMember> GetNamedMembers(PackMember holder, string name)
    {
        IEnumerable<PackMember> Members = MemberRelations.GetSignedMembers(holder);
        if (holder is IPackTypeHolder TypeHolder)
        {
            Members = Members.Concat(TypeHolder.Types);
        }
        return Members.Where(member => IsFoundByName(member, name));
    }

    /* A member a name can find: not a constructor, an operator, an indexer or an explicit implementation, and not
     * a function which overrides another, which that one stands for. */
    private bool IsFoundByName(PackMember member, string name)
    {
        return (member.SelfIdentifier.SourceCodeName == name) && (member is not (PackConstructor or PackIndexer))
            && (MemberRelations.GetOperatorOverload(member) == null)
            && (MemberRelations.GetExplicitInterface(member) == null)
            && !((member is PackFunction) && member.HasModifier(PackMemberModifiers.Override));
    }

    /* Written with type arguments, a name finds only members with as many generic parameters; written without,
     * it finds no generic type, though generic functions, whose types may be inferred. */
    private bool HasArity(PackMember member, int typeArgumentCount)
    {
        int Arity = InheritedMembers.GetArity(member);
        if (typeArgumentCount > 0)
        {
            return Arity == typeArgumentCount;
        }
        return (Arity == 0) || (member is PackFunction);
    }

    /* C#'s hiding: a member hides those of the types its holder derives from, as each kind of member does. */
    private List<FoundMember> RemoveHidden(List<FoundMember> found)
    {
        HashSet<FoundMember> Hidden = new(ReferenceEqualityComparer.Instance);
        foreach (FoundMember Hiding in found)
        {
            foreach (FoundMember Other in found.Where(other => IsProperBase(other.Holder!, Hiding.Holder!)
                && IsHiding(Hiding.Member, other.Member)))
            {
                Hidden.Add(Other);
            }
        }
        return found.Where(member => !Hidden.Contains(member)).ToList();
    }

    private bool IsHiding(PackMember member, PackMember other)
    {
        bool IsType = MemberRelations.IsType(member);
        bool IsOtherType = MemberRelations.IsType(other);
        if (IsType)
        {
            return !IsOtherType || (InheritedMembers.GetArity(member) == InheritedMembers.GetArity(other));
        }
        if (member is PackFunction)
        {
            return other is not PackFunction;
        }
        return true;
    }

    /* For a generic parameter, the members its constraints' interfaces declare which a member of a class it is
     * constrained to hides, as C# removes them: all of them for a field, property or event, and those which are
     * not functions for a function. */
    private List<FoundMember> RemoveInterfaceMembersHiddenByClasses(List<FoundMember> found)
    {
        DeclaredType? ObjectType = _context.Hierarchy.GetObjectType();
        List<FoundMember> ClassMembers = found.Where(member => (member.Holder!.Declaration is not PackInterface)
            && !member.Holder.Equals(ObjectType)).ToList();
        if (ClassMembers.Count == 0)
        {
            return found;
        }

        bool IsHidingAll = ClassMembers.Any(member => member.Member is not PackFunction);
        return found.Where(member => (member.Holder!.Declaration is not PackInterface)
            || (!IsHidingAll && (member.Member is PackFunction))).ToList();
    }

    /* Whether one type a lookup searched is among those another derives from. Object counts as one every
     * interface derives from, since a lookup in an interface searches it too. */
    private bool IsProperBase(DeclaredType candidate, DeclaredType derived)
    {
        if (candidate.Equals(derived))
        {
            return false;
        }
        return candidate.Equals(_context.Hierarchy.GetObjectType())
            || _context.Hierarchy.IsSameOrDerived(derived, candidate);
    }

    /* What a namespace holds is public or internal, and an internal one is seen on its own side of the standard
     * library's boundary only. The library, looked at as its own assembly, sees none of the user's code. */
    private bool IsNameSpaceMemberVisible(PackMember member, PackMember viewer)
    {
        return (member.SourceFile.Kind == viewer.SourceFile.Kind)
            || ((viewer.SourceFile.Kind == SourceFileKind.User)
                && (MemberRelations.GetEffectiveAccess(member) == PackMemberModifiers.Public));
    }
}
