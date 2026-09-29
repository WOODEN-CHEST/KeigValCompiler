using KeigValCompiler.Semantician.Library;
using KeigValCompiler.Semantician.Member;
using System.Text;

namespace KeigValCompiler.Semantician.Resolver;

/* What the declaration checks need to know about a member beyond the member itself: what holds it,
 * what kind of member it is, which for a function depends on what holds it, and how messages name it.
 * It goes through ParentItem, so it only works once every member is named. */
internal static class MemberRelations
{
    // Static fields.
    private const char SPACE = ' ';
    private const string SEPARATOR = ", ";


    // Internal static methods.
    /* The member holding this one, or null for a member its namespace holds. */
    internal static PackMember? GetHoldingMember(PackMember member)
    {
        ArgumentNullException.ThrowIfNull(member, nameof(member));
        return member.ParentItem?.Target as PackMember;
    }

    internal static bool IsType(PackMember member)
    {
        return member is PackClass or PackStruct or PackInterface or PackEnumeration or PackDelegate;
    }

    /* A property's or an indexer's "get", "set" or "init". */
    internal static bool IsAccessor(PackMember member)
    {
        return (member is PackFunction) && (GetHoldingMember(member) is PackProperty or PackIndexer);
    }

    /* The overload a function is the body of, or null for a function which is not an operator. */
    internal static OperatorOverload? GetOperatorOverload(PackMember member)
    {
        if ((member is not PackFunction) || (GetHoldingMember(member) is not IOperatorOverloadHolder Holder))
        {
            return null;
        }
        return Holder.OperatorOverloads.FirstOrDefault(overload => ReferenceEquals(overload.Function, member));
    }

    /* The interface an explicit implementation names, or null for an ordinary member. */
    internal static TypeTargetIdentifier? GetExplicitInterface(PackMember member)
    {
        return (member as IExplicitInterfaceMember)?.ExplicitInterface;
    }

    internal static bool IsStaticClass(PackMember member)
    {
        return (member is PackClass) && member.HasModifier(PackMemberModifiers.Static);
    }

    internal static bool IsStaticConstructor(PackMember member)
    {
        return (member is PackConstructor) && member.HasModifier(PackMemberModifiers.Static);
    }

    /* Whether a member is abstract: written so, or an interface's own instance member without a body which
     * is not private, virtual or sealed, which is abstract without saying so, as in C#. A property or
     * indexer is without a body when none of its accessors has one, and an event always is. An explicit
     * implementation in an interface is abstract only if it says so. */
    internal static bool IsAbstract(PackMember member)
    {
        ArgumentNullException.ThrowIfNull(member, nameof(member));

        if (member.HasModifier(PackMemberModifiers.Abstract))
        {
            return true;
        }
        bool IsImplicitlyAbstract = (GetHoldingMember(member) is PackInterface)
            && (GetExplicitInterface(member) == null)
            && !member.HasAnyModifier(PackMemberModifiers.Static, PackMemberModifiers.Virtual,
                PackMemberModifiers.Sealed)
            && (GetEffectiveAccess(member) != PackMemberModifiers.Private);
        if (!IsImplicitlyAbstract)
        {
            return false;
        }

        return member switch
        {
            PackFunction Function => Function.Statements == null,
            PackProperty or PackIndexer => member.SubMembers.All(
                accessor => ((PackFunction)accessor).Statements == null),
            PackEvent => true,
            _ => false
        };
    }

    /* The access a member has, which is the one written, or C#'s default where none is: internal for what
     * a namespace holds, public for an interface's members, and private for a class's or structure's.
     * An accessor has its property's or indexer's, and an explicit implementation counts as private, as
     * in C#, since it is reached only through its interface. */
    internal static PackMemberModifiers GetEffectiveAccess(PackMember member)
    {
        ArgumentNullException.ThrowIfNull(member, nameof(member));

        PackMemberModifiers WrittenAccess = member.Modifiers & (PackMemberModifiers.Public
            | PackMemberModifiers.Protected | PackMemberModifiers.Internal | PackMemberModifiers.Private);
        if (WrittenAccess != PackMemberModifiers.None)
        {
            return WrittenAccess;
        }
        if (GetExplicitInterface(member) != null)
        {
            return PackMemberModifiers.Private;
        }

        return GetHoldingMember(member) switch
        {
            null => PackMemberModifiers.Internal,
            PackProperty or PackIndexer => GetEffectiveAccess(GetHoldingMember(member)!),
            PackInterface => PackMemberModifiers.Public,
            _ => PackMemberModifiers.Private
        };
    }

    /* What a member is, as messages call it: "field", "record class", "accessor" and so on. */
    internal static string GetKindName(PackMember member)
    {
        ArgumentNullException.ThrowIfNull(member, nameof(member));

        return member switch
        {
            PackClass when member.HasModifier(PackMemberModifiers.Record) => KGVL.NAME_RECORD_CLASS,
            PackClass when member.HasModifier(PackMemberModifiers.Static) => KGVL.NAME_STATIC_CLASS,
            PackClass => KGVL.NAME_CLASS,
            PackStruct => KGVL.NAME_STRUCT,
            PackInterface => KGVL.NAME_INTERFACE,
            PackEnumeration => KGVL.NAME_ENUM,
            PackDelegate => KGVL.NAME_DELEGATE,
            PackField => KGVL.NAME_FIELD,
            PackProperty => KGVL.NAME_PROPERTY,
            PackIndexer => KGVL.NAME_INDEXER,
            PackEvent => KGVL.NAME_EVENT,
            PackConstructor => KGVL.NAME_CONSTRUCTOR,
            _ when IsAccessor(member) => KGVL.NAME_ACCESSOR,
            _ when GetOperatorOverload(member) != null => KGVL.NAME_OPERATOR_OVERLOAD,
            _ => KGVL.NAME_FUNCTION
        };
    }

    /* A member as messages name it: its own name, after the interface it implements explicitly if it
     * does. An accessor is named after what it belongs to, as in "Count.get", an indexer by its parameter
     * types, as in "this[int]", and an operator as it is declared, as in "operator +". */
    internal static string GetDisplayName(PackMember member)
    {
        ArgumentNullException.ThrowIfNull(member, nameof(member));

        if (IsAccessor(member))
        {
            return GetDisplayName(GetHoldingMember(member)!) + KGVL.MEMBER_ACCESS
                + member.SelfIdentifier.SourceCodeName;
        }

        OperatorOverload? Overload = GetOperatorOverload(member);
        StringBuilder Builder = new();
        TypeTargetIdentifier? ExplicitInterface = GetExplicitInterface(member);
        if (ExplicitInterface != null)
        {
            Builder.Append(ExplicitInterface.ToString()).Append(KGVL.MEMBER_ACCESS);
        }

        if (Overload != null)
        {
            AppendOperatorName(Builder, Overload);
        }
        else if (member is PackIndexer Indexer)
        {
            Builder.Append(KGVL.KEYWORD_THIS).Append(KGVL.OPEN_SQUARE_BRACKET)
                .Append(string.Join(SEPARATOR, Indexer.Parameters.Select(
                    parameter => parameter.Type?.ToString() ?? string.Empty)))
                .Append(KGVL.CLOSE_SQUARE_BRACKET);
        }
        else
        {
            Builder.Append(member.SelfIdentifier.SourceCodeName);
        }
        return Builder.ToString();
    }

    /* What holds a member, as messages call it: a member's kind, or "namespace". */
    internal static string GetHolderKindName(PackMember member)
    {
        PackMember? Holder = GetHoldingMember(member);
        return (Holder != null) ? GetKindName(Holder) : KGVL.NAME_NAMESPACE;
    }

    /* What holds a member, as messages name it: a member's name, or the namespace's full name. */
    internal static string GetHolderDisplayName(PackMember member)
    {
        PackMember? Holder = GetHoldingMember(member);
        return (Holder != null) ? GetDisplayName(Holder) : member.NameSpace.SelfIdentifier.SourceCodeName;
    }


    // Private static methods.
    private static void AppendOperatorName(StringBuilder builder, OperatorOverload overload)
    {
        PackFunction Function = overload.Function;
        if ((overload.OverloadedOperator == OverloadableOperator.ImplicitCast)
            || (overload.OverloadedOperator == OverloadableOperator.ExplicitCast))
        {
            string Keyword = (overload.OverloadedOperator == OverloadableOperator.ImplicitCast)
                ? KGVL.KEYWORD_IMPLICIT : KGVL.KEYWORD_EXPLICIT;
            builder.Append(Keyword).Append(SPACE).Append(KGVL.KEYWORD_OPERATOR).Append(SPACE)
                .Append(Function.ReturnType?.ToString() ?? KGVL.KEYWORD_VOID);
            return;
        }

        builder.Append(KGVL.KEYWORD_OPERATOR).Append(SPACE)
            .Append(SignatureFormatter.GetOperatorSpelling(overload.OverloadedOperator));
    }
}
