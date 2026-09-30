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

    /* A class's base class, as C# decides it: the first base it lists which is not an interface, when that is
     * a class. One which did not resolve, or is no class, has been reported, and leaves the class with none of
     * its own, rather than letting a class written after it take its place. Null for any other type. */
    internal static PackClass? GetWrittenBaseClass(PackMember type)
    {
        ArgumentNullException.ThrowIfNull(type, nameof(type));

        return GetWrittenBaseClassName(type)?.MainTarget.Target as PackClass;
    }

    /* The base class, as GetWrittenBaseClass decides it, as the class writes it. */
    internal static TypeTargetIdentifier? GetWrittenBaseClassName(PackMember type)
    {
        ArgumentNullException.ThrowIfNull(type, nameof(type));

        if (type is not PackClass Class)
        {
            return null;
        }
        TypeTargetIdentifier? Written = Class.ExtendedMembers.FirstOrDefault(written => !written.IsArrayOrNullable
            && (written.MainTarget.Target is not PackInterface));
        return (Written?.MainTarget.Target is PackClass) ? Written : null;
    }

    /* The types among a type and those it derives from, however far back, which derive from themselves,
     * following the bases a source gives. As in C#, such a type is taken to derive from nothing, once
     * InheritanceChecker has reported it. */
    internal static HashSet<PackMember> FindSelfDerivingTypes(PackMember type, IBaseTypeSource bases)
    {
        ArgumentNullException.ThrowIfNull(type, nameof(type));
        ArgumentNullException.ThrowIfNull(bases, nameof(bases));

        Dictionary<PackMember, List<PackMember>> BasesOf = new(ReferenceEqualityComparer.Instance);
        Dictionary<PackMember, List<PackMember>> DerivedOf = new(ReferenceEqualityComparer.Instance);
        Queue<PackMember> ToSearch = new();
        ToSearch.Enqueue(type);
        while (ToSearch.Count > 0)
        {
            PackMember Current = ToSearch.Dequeue();
            if (BasesOf.ContainsKey(Current))
            {
                continue;
            }
            List<PackMember> Bases = bases.GetBaseTypes(Current).ToList();
            BasesOf.Add(Current, Bases);
            foreach (PackMember Base in Bases)
            {
                if (!DerivedOf.TryGetValue(Base, out List<PackMember>? Derived))
                {
                    Derived = new();
                    DerivedOf.Add(Base, Derived);
                }
                Derived.Add(Current);
                ToSearch.Enqueue(Base);
            }
        }

        /* Taking away the types whose bases have all been taken away, until none is left to take, leaves only
         * those which derive from themselves and those deriving from one of them. */
        Dictionary<PackMember, int> BasesLeft = new(ReferenceEqualityComparer.Instance);
        foreach (KeyValuePair<PackMember, List<PackMember>> Pair in BasesOf)
        {
            BasesLeft.Add(Pair.Key, Pair.Value.Count);
        }
        Queue<PackMember> Removable = new(BasesLeft.Where(pair => pair.Value == 0).Select(pair => pair.Key));
        while (Removable.Count > 0)
        {
            PackMember Removed = Removable.Dequeue();
            BasesLeft.Remove(Removed);
            foreach (PackMember Derived in DerivedOf.GetValueOrDefault(Removed) ?? new List<PackMember>())
            {
                BasesLeft[Derived]--;
                if (BasesLeft[Derived] == 0)
                {
                    Removable.Enqueue(Derived);
                }
            }
        }

        HashSet<PackMember> SelfDeriving = new(ReferenceEqualityComparer.Instance);
        foreach (PackMember Left in BasesLeft.Keys)
        {
            if (IsReachedAgain(Left, BasesOf, BasesLeft))
            {
                SelfDeriving.Add(Left);
            }
        }
        return SelfDeriving;
    }

    /* Whether a type is another or derives from it, through its base class or the interfaces it lists, however
     * far back, compared by declaration, as C# compares original definitions. */
    internal static bool IsDerivedOrSame(PackMember type, PackMember baseType, IBaseTypeSource bases)
    {
        ArgumentNullException.ThrowIfNull(type, nameof(type));
        ArgumentNullException.ThrowIfNull(baseType, nameof(baseType));
        ArgumentNullException.ThrowIfNull(bases, nameof(bases));

        HashSet<PackMember> Seen = new(ReferenceEqualityComparer.Instance) { type };
        Queue<PackMember> ToSearch = new();
        ToSearch.Enqueue(type);
        while (ToSearch.Count > 0)
        {
            PackMember Current = ToSearch.Dequeue();
            if (ReferenceEquals(Current, baseType))
            {
                return true;
            }
            foreach (PackMember Base in bases.GetBaseTypes(Current).Where(Seen.Add))
            {
                ToSearch.Enqueue(Base);
            }
        }
        return false;
    }

    /* The parameters a member declares: a function's, an indexer's or a delegate's, or null for any other. */
    internal static FunctionParameterCollection? GetParameters(PackMember member)
    {
        ArgumentNullException.ThrowIfNull(member, nameof(member));

        return member switch
        {
            PackFunction Function => Function.Parameters,
            PackIndexer Indexer => Indexer.Parameters,
            PackDelegate Delegate => Delegate.Parameters,
            _ => null
        };
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
                break;

            case PackEvent Event:
                Written.Add(Event.Type);
                break;

            case PackDelegate Delegate:
                Written.Add(Delegate.ReturnType);
                break;

            case PackFunction Function:
                Written.Add(Function.ReturnType);
                break;
        }
        Written.AddRange(GetParameters(member)?.Select(parameter => parameter.Type)
            ?? Enumerable.Empty<TypeTargetIdentifier?>());

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
        return (member is PackFunction) && (GetHoldingMember(member) is IPackAccessorHolder);
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
    /* Whether a type is reached again from its own bases, among the types left, which are the only ones that
     * could lead back to it. */
    private static bool IsReachedAgain(PackMember type,
        Dictionary<PackMember, List<PackMember>> basesOf,
        Dictionary<PackMember, int> left)
    {
        HashSet<PackMember> Seen = new(ReferenceEqualityComparer.Instance);
        Queue<PackMember> ToSearch = new(basesOf[type].Where(left.ContainsKey));
        while (ToSearch.Count > 0)
        {
            PackMember Current = ToSearch.Dequeue();
            if (ReferenceEquals(Current, type))
            {
                return true;
            }
            if (Seen.Add(Current))
            {
                foreach (PackMember Base in basesOf[Current].Where(left.ContainsKey))
                {
                    ToSearch.Enqueue(Base);
                }
            }
        }
        return false;
    }

    private static void AppendOperatorName(StringBuilder builder, OperatorOverload overload)
    {
        builder.Append(GetOperatorName(overload.OverloadedOperator,
            overload.Function.ReturnType?.ToString() ?? KGVL.KEYWORD_VOID));
    }
}
