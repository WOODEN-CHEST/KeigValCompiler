using KeigValCompiler.Semantician.Member;
using KeigValCompiler.Semantician.Types;

namespace KeigValCompiler.Semantician.Resolver;

/* Where members can be reached from, as C# decides it: a member's accessibility domain is where its own
 * access and that of each type around it all allow it to be used. The assembly C# speaks of is a side of the
 * standard library's boundary, so "internal" reaches the files of one kind. This is Roslyn's own comparison of
 * two domains, its IsAsRestrictive, which answers without computing the domains themselves. */
internal static class AccessDomains
{
    // Internal static methods.
    /* Whether a type can be used everywhere a member can: the type, each type around it, and each type given
     * as a type argument to any of them, as C# checks a member's declaration. Generic parameters can be used
     * wherever their declaration can. The first declaration less accessible than the member, or null. */
    internal static PackMember? FindLessAccessible(SemanticType type,
        PackMember member,
        PackResolutionContext context)
    {
        ArgumentNullException.ThrowIfNull(type, nameof(type));
        ArgumentNullException.ThrowIfNull(member, nameof(member));
        ArgumentNullException.ThrowIfNull(context, nameof(context));

        if (type is not DeclaredType Declared)
        {
            return null;
        }
        if (Declared.ContainingType != null)
        {
            PackMember? Around = FindLessAccessible(Declared.ContainingType, member, context);
            if (Around != null)
            {
                return Around;
            }
        }
        foreach (SemanticType Argument in Declared.TypeArguments)
        {
            PackMember? InArgument = FindLessAccessible(Argument, member, context);
            if (InArgument != null)
            {
                return InArgument;
            }
        }
        return IsAtLeastAsAccessible(Declared.Declaration, member, context) ? null : Declared.Declaration;
    }

    /* Whether one member, by its own access, can be used everywhere another can, the other's domain being
     * within its own; the types around the first are left to the caller, as Roslyn leaves them. */
    internal static bool IsAtLeastAsAccessible(PackMember wider,
        PackMember narrower,
        PackResolutionContext context)
    {
        ArgumentNullException.ThrowIfNull(wider, nameof(wider));
        ArgumentNullException.ThrowIfNull(narrower, nameof(narrower));
        ArgumentNullException.ThrowIfNull(context, nameof(context));

        PackMemberModifiers WiderAccess = MemberRelations.GetEffectiveAccess(wider);
        if (WiderAccess == PackMemberModifiers.Public)
        {
            return true;
        }

        bool IsSameAssembly = wider.SourceFile.Kind == narrower.SourceFile.Kind;
        PackMember? WiderType = GetContainingType(wider);
        for (PackMember? Current = narrower; Current != null; Current = MemberRelations.GetHoldingMember(Current))
        {
            if (IsWithinAt(WiderAccess, WiderType, Current, IsSameAssembly, context))
            {
                return true;
            }
        }
        return false;
    }


    // Private static methods.
    /* One step of Roslyn's walk: whether a level of the narrower member's declaration, with its own access,
     * makes the whole of its domain lie within the wider member's. */
    private static bool IsWithinAt(PackMemberModifiers widerAccess,
        PackMember? widerType,
        PackMember current,
        bool isSameAssembly,
        PackResolutionContext context)
    {
        PackMemberModifiers Access = MemberRelations.GetEffectiveAccess(current);
        PackMemberModifiers PrivateProtected = PackMemberModifiers.Private | PackMemberModifiers.Protected;
        PackMemberModifiers ProtectedInternal = PackMemberModifiers.Protected | PackMemberModifiers.Internal;
        PackMember? CurrentType = GetContainingType(current);
        bool IsInternalLevel = (Access == PackMemberModifiers.Private) || (Access == PackMemberModifiers.Internal)
            || (Access == PrivateProtected);

        if (widerAccess == PackMemberModifiers.Internal)
        {
            return IsInternalLevel && isSameAssembly;
        }
        if (widerAccess == PrivateProtected)
        {
            return IsInternalLevel && isSameAssembly && IsWithinProtected(widerType, CurrentType, Access, context);
        }
        if (widerAccess == PackMemberModifiers.Protected)
        {
            return IsWithinProtected(widerType, CurrentType, Access, context);
        }
        if (widerAccess == ProtectedInternal)
        {
            if (widerType == null)
            {
                return false;
            }
            return Access switch
            {
                PackMemberModifiers.Private => isSameAssembly || IsWithinDerived(widerType, CurrentType, context),
                PackMemberModifiers.Internal => isSameAssembly,
                PackMemberModifiers.Protected => IsDerivedOrSame(CurrentType, widerType, context),
                _ when Access == PrivateProtected => isSameAssembly
                    || IsDerivedOrSame(CurrentType, widerType, context),
                _ when Access == ProtectedInternal => isSameAssembly
                    && IsDerivedOrSame(CurrentType, widerType, context),
                _ => false
            };
        }

        /* Private: within the same type, or one inside it. */
        if ((Access != PackMemberModifiers.Private) || (widerType == null))
        {
            return false;
        }
        for (PackMember? Type = CurrentType; Type != null; Type = GetContainingType(Type))
        {
            if (ReferenceEquals(Type, widerType))
            {
                return true;
            }
        }
        return false;
    }

    /* Roslyn's protected case: a private level inside the wider member's type or one deriving from it, or a
     * protected level of such a type itself. */
    private static bool IsWithinProtected(PackMember? widerType,
        PackMember? currentType,
        PackMemberModifiers access,
        PackResolutionContext context)
    {
        if (widerType == null)
        {
            return false;
        }
        if (access == PackMemberModifiers.Private)
        {
            return IsWithinDerived(widerType, currentType, context);
        }
        bool IsProtectedLevel = (access == PackMemberModifiers.Protected)
            || (access == (PackMemberModifiers.Private | PackMemberModifiers.Protected));
        return IsProtectedLevel && IsDerivedOrSame(currentType, widerType, context);
    }

    /* Whether a type, or one around it, is the given type or derives from it. */
    private static bool IsWithinDerived(PackMember widerType,
        PackMember? currentType,
        PackResolutionContext context)
    {
        for (PackMember? Type = currentType; Type != null; Type = GetContainingType(Type))
        {
            if (IsDerivedOrSame(Type, widerType, context))
            {
                return true;
            }
        }
        return false;
    }

    /* Whether a type is another or derives from it, compared by declaration, as Roslyn compares original
     * definitions; for an interface, whether the type implements or extends it. */
    private static bool IsDerivedOrSame(PackMember? type, PackMember baseType, PackResolutionContext context)
    {
        if (type == null)
        {
            return false;
        }
        if (ReferenceEquals(type, baseType))
        {
            return true;
        }
        DeclaredType Instance = context.TypeReader.GetInstanceType(type);
        return (baseType is PackInterface) ? context.Hierarchy.GetInterfaces(Instance).Any(
                implemented => ReferenceEquals(implemented.Declaration, baseType))
            : context.Hierarchy.GetBaseClasses(Instance).Any(
                baseClass => ReferenceEquals(baseClass.Declaration, baseType));
    }

    /* The nearest type holding a member, past the property or indexer holding an accessor. */
    private static PackMember? GetContainingType(PackMember member)
    {
        PackMember? Holder = MemberRelations.GetHoldingMember(member);
        while ((Holder != null) && !MemberRelations.IsType(Holder))
        {
            Holder = MemberRelations.GetHoldingMember(Holder);
        }
        return Holder;
    }
}
