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
    internal const PackMemberModifiers ACCESS_MODIFIERS = PackMemberModifiers.Public
        | PackMemberModifiers.Protected | PackMemberModifiers.Internal | PackMemberModifiers.Private;

    private const char SPACE = ' ';
    private const string SEPARATOR = ", ";

    /* How FormatChain joins the links of a chain. */
    private static readonly string _chainSeparator = $" {KGVL.COLON} ";


    // Internal static methods.
    /* The member holding this one, or null for a member its namespace holds. */
    internal static PackMember? GetHoldingMember(PackMember member)
    {
        ArgumentNullException.ThrowIfNull(member, nameof(member));
        return member.ParentItem?.Target as PackMember;
    }

    /* The members a type or namespace declares itself which have a signature: functions, constructors and
     * operators, properties, indexers, events and fields, but neither nested types nor accessors. */
    internal static IEnumerable<PackMember> GetSignedMembers(object holder)
    {
        ArgumentNullException.ThrowIfNull(holder, nameof(holder));

        IEnumerable<PackMember> Members = Enumerable.Empty<PackMember>();
        if (holder is IPackFunctionHolder FunctionHolder)
        {
            Members = Members.Concat(FunctionHolder.Functions).Concat(FunctionHolder.Properties)
                .Concat(FunctionHolder.Indexers);
        }
        if (holder is IPackFieldHolder FieldHolder)
        {
            Members = Members.Concat(FieldHolder.Fields);
        }
        if (holder is IPackEventHolder EventHolder)
        {
            Members = Members.Concat(EventHolder.Events);
        }
        if (holder is IOperatorOverloadHolder OverloadHolder)
        {
            Members = Members.Concat(OverloadHolder.OperatorOverloads.Select(overload => overload.Function));
        }
        return Members;
    }

    /* Every type written in a member's own declaration: its type, or what it returns, its parameters' types,
     * what it derives from, its generic parameters' constraints, and the interface it implements explicitly.
     * Not those written in its body, nor in the members it holds. */
    internal static IEnumerable<TypeTargetIdentifier> GetWrittenTypes(PackMember member)
    {
        ArgumentNullException.ThrowIfNull(member, nameof(member));

        List<TypeTargetIdentifier?> Written = new();
        switch (member)
        {
            case PackField Field:
                Written.Add(Field.Type);
                break;

            case PackProperty Property:
                Written.Add(Property.Type);
                break;

            case PackIndexer Indexer:
                Written.Add(Indexer.Type);
                Written.AddRange(Indexer.Parameters.Select(parameter => parameter.Type));
                break;

            case PackEvent Event:
                Written.Add(Event.Type);
                break;

            case PackDelegate Delegate:
                Written.Add(Delegate.ReturnType);
                Written.AddRange(Delegate.Parameters.Select(parameter => parameter.Type));
                break;

            case PackFunction Function:
                Written.Add(Function.ReturnType);
                Written.AddRange(Function.Parameters.Select(parameter => parameter.Type));
                break;
        }

        if (member is IPackMemberExtender Extender)
        {
            Written.AddRange(Extender.ExtendedMembers);
        }
        if (member is IGenericParameterHolder GenericsHolder)
        {
            Written.AddRange(GenericsHolder.GenericParameters.SelectMany(parameter => parameter.Constraints)
                .Select(constraint => constraint.ConstrainedItemName));
        }
        Written.Add(GetExplicitInterface(member));
        return Written.Where(type => type != null).Select(type => type!);
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

    /* Whether a member is written static, as a constant is without saying so. One whose "static" or "const"
     * was taken off, as one it cannot have, still counts, since it was not meant to be an instance member,
     * and the modifier has been reported already. */
    internal static bool IsWrittenStatic(PackMember member)
    {
        ArgumentNullException.ThrowIfNull(member, nameof(member));
        return ((member.Modifiers | member.RejectedModifiers)
            & (PackMemberModifiers.Static | PackMemberModifiers.Const)) != PackMemberModifiers.None;
    }

    /* Whether a static class can hold a member other than a type: one written static, but not an operator
     * or an indexer, which work on values of their type, of which a static class has none. */
    internal static bool CanStaticClassHold(PackMember member)
    {
        ArgumentNullException.ThrowIfNull(member, nameof(member));
        return IsWrittenStatic(member) && (GetOperatorOverload(member) == null) && (member is not PackIndexer);
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

        PackMemberModifiers WrittenAccess = member.Modifiers & ACCESS_MODIFIERS;
        if (WrittenAccess != PackMemberModifiers.None)
        {
            return IsValidAccess(WrittenAccess) ? WrittenAccess : PackMemberModifiers.Public;
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

    /* Whether written access modifiers are one of C#'s accessibilities: a single one, "protected internal"
     * or "private protected". Any other mix, which ModifierChecker reports, counts as public, as Roslyn
     * reads it, so that nothing else reports what follows from it. */
    internal static bool IsValidAccess(PackMemberModifiers access)
    {
        return access is PackMemberModifiers.Public or PackMemberModifiers.Protected or PackMemberModifiers.Internal
            or PackMemberModifiers.Private or (PackMemberModifiers.Protected | PackMemberModifiers.Internal)
            or (PackMemberModifiers.Private | PackMemberModifiers.Protected);
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

    /* An operator as it is declared, as in "operator +" or "implicit operator int", the type a conversion
     * converts to being given, and ignored for any other operator. */
    internal static string GetOperatorName(OverloadableOperator overloadedOperator, string conversionType)
    {
        ArgumentNullException.ThrowIfNull(conversionType, nameof(conversionType));

        if (!OverloadableOperatorKinds.IsConversion(overloadedOperator))
        {
            return KGVL.KEYWORD_OPERATOR + SPACE + SignatureFormatter.GetOperatorSpelling(overloadedOperator);
        }
        string Keyword = (overloadedOperator == OverloadableOperator.ImplicitCast)
            ? KGVL.KEYWORD_IMPLICIT : KGVL.KEYWORD_EXPLICIT;
        return Keyword + SPACE + KGVL.KEYWORD_OPERATOR + SPACE + conversionType;
    }

    /* A chain of types or generic parameters, each deriving from or constrained to the next, as base lists
     * would write it: "A : B : A". */
    internal static string FormatChain(IEnumerable<string> names)
    {
        ArgumentNullException.ThrowIfNull(names, nameof(names));
        return string.Join(_chainSeparator, names);
    }

    /* Each source file's place in the order the pack was read in, for ordering members as they are
     * declared across files. */
    internal static Dictionary<PackSourceFile, int> GetFileOrder(DataPack pack)
    {
        ArgumentNullException.ThrowIfNull(pack, nameof(pack));

        Dictionary<PackSourceFile, int> FileOrder = new();
        foreach (PackSourceFile SourceFile in pack.SourceFiles)
        {
            FileOrder.Add(SourceFile, FileOrder.Count);
        }
        return FileOrder;
    }

    /* Members in the order they are declared in, across files, which GetFileOrder gives. */
    internal static IEnumerable<PackMember> OrderByDeclaration(IEnumerable<PackMember> members,
        Dictionary<PackSourceFile, int> fileOrder)
    {
        ArgumentNullException.ThrowIfNull(members, nameof(members));
        ArgumentNullException.ThrowIfNull(fileOrder, nameof(fileOrder));

        return members.OrderBy(member => fileOrder[member.SourceFile])
            .ThenBy(member => member.SourceFileOrigin.Line);
    }


    // Private static methods.
    private static void AppendOperatorName(StringBuilder builder, OperatorOverload overload)
    {
        builder.Append(GetOperatorName(overload.OverloadedOperator,
            overload.Function.ReturnType?.ToString() ?? KGVL.KEYWORD_VOID));
    }
}
