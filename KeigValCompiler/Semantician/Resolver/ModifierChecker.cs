using KeigValCompiler.Error;
using KeigValCompiler.Semantician.Member;

namespace KeigValCompiler.Semantician.Resolver;

/* Checks the modifiers each member is written with against C#'s rules: which ones each kind of member
 * can have where it is declared, which cannot go together, and which an operator needs. A modifier the
 * member cannot have at all is reported and then taken off the member, as C# does, so that the checks
 * after this one see only what the member may have, and one mistake is reported once. That is why it has
 * to run before the other checks. What a namespace, an interface or a static class can hold in the first
 * place is MemberPlacementChecker's to report. */
internal class ModifierChecker : IPackResolver
{
    // Static fields.
    private const PackMemberModifiers ACCESS = MemberRelations.ACCESS_MODIFIERS;

    /* What a namespace holds is seen either everywhere or only by its own side of the standard library's
     * boundary, as a C# assembly's types are. */
    private const PackMemberModifiers NAMESPACE_ACCESS = PackMemberModifiers.Public
        | PackMemberModifiers.Internal;

    /* Nothing derives from a structure, so nothing in one can be protected, and nothing but a type in a
     * static class can be, since only types there are not static. */
    private const PackMemberModifiers UNPROTECTED_ACCESS = PackMemberModifiers.Public
        | PackMemberModifiers.Internal | PackMemberModifiers.Private;

    private const PackMemberModifiers OVERRIDING = PackMemberModifiers.Abstract | PackMemberModifiers.Virtual
        | PackMemberModifiers.Override | PackMemberModifiers.Sealed;

    private const PackMemberModifiers CONSTRUCTOR = PackMemberModifiers.Static | PackMemberModifiers.BuiltIn
        | PackMemberModifiers.Inline;

    /* An operator is public wherever it is, which CheckOperatorModifiers requires to be written outside an
     * interface. In an interface it can also be abstract or virtual, to be supplied by the types
     * implementing it, or sealed, which it is anyway unless it is either. */
    private const PackMemberModifiers OPERATOR = PackMemberModifiers.Public | PackMemberModifiers.Static
        | PackMemberModifiers.BuiltIn | PackMemberModifiers.Inline;
    private const PackMemberModifiers INTERFACE_OPERATOR = PackMemberModifiers.Public
        | PackMemberModifiers.Static | PackMemberModifiers.Abstract | PackMemberModifiers.Virtual
        | PackMemberModifiers.Sealed | PackMemberModifiers.Inline;

    /* An explicit implementation is reached only through its interface, so it can have none of the
     * modifiers which decide who reaches a member or how it is overridden. An interface re-declaring an
     * inherited interface's member explicitly may still make it abstract again. */
    private const PackMemberModifiers EXPLICIT_IMPLEMENTATION = PackMemberModifiers.Static
        | PackMemberModifiers.Readonly | PackMemberModifiers.BuiltIn | PackMemberModifiers.Inline;

    /* The two pairs of access modifiers C# gives a meaning of their own. */
    private const PackMemberModifiers PROTECTED_INTERNAL = PackMemberModifiers.Protected
        | PackMemberModifiers.Internal;
    private const PackMemberModifiers PRIVATE_PROTECTED = PackMemberModifiers.Private
        | PackMemberModifiers.Protected;


    // Private methods.
    private void CheckMember(PackMember member, PackResolutionContext context)
    {
        PackMemberModifiers Allowed = GetAllowedModifiers(member);
        ReportModifiersNotAllowed(member, Allowed, context);

        member.RejectedModifiers = member.Modifiers & ~Allowed;
        member.Modifiers &= Allowed;
        CheckAccessModifiers(member, context);
        if (member is PackClass)
        {
            CheckClassConflicts(member, context);
        }
        else if (!MemberRelations.IsType(member))
        {
            CheckMemberConflicts(member, context);
            CheckInterfaceConflicts(member, context);
            CheckHolderConflicts(member, context);
            CheckOperatorModifiers(member, context);
            CheckAccessorModifiers(member, context);
        }
    }

    /* Whether a member is private, as written or by default. One whose access modifier was taken off, as one
     * it cannot have, is not taken for private by default: that was not what was written. */
    private bool IsPrivate(PackMember member)
    {
        return ((member.RejectedModifiers & ACCESS) == PackMemberModifiers.None)
            && (MemberRelations.GetEffectiveAccess(member) == PackMemberModifiers.Private);
    }

    /* A member which implements another explicitly is told what it can have as one, and any other member
     * what it can have where it is declared. */
    private void ReportModifiersNotAllowed(PackMember member,
        PackMemberModifiers allowed,
        PackResolutionContext context)
    {
        /* "builtin" is reserved for the standard library, so it is not offered to anything else. */
        PackMemberModifiers Offered = member.SourceFile.IsLibraryFile
            ? allowed : (allowed & ~PackMemberModifiers.BuiltIn);
        TypeTargetIdentifier? ExplicitInterface = MemberRelations.GetExplicitInterface(member);

        foreach (PackMemberModifiers Modifier in ModifierKeywords.Split(member.Modifiers & ~allowed))
        {
            string Keyword = ModifierKeywords.GetKeyword(Modifier);
            string Kind = MemberRelations.GetKindName(member);
            string Name = MemberRelations.GetDisplayName(member);

            if ((ExplicitInterface != null) && (Offered == PackMemberModifiers.None))
            {
                context.AddError(context.ErrorCreator.ExplicitImplementationWithoutModifiers.CreateOptions(Kind,
                    Name, ExplicitInterface.ToString(), Keyword), member);
            }
            else if (ExplicitInterface != null)
            {
                context.AddError(context.ErrorCreator.ExplicitImplementationModifier.CreateOptions(Kind, Name,
                    ExplicitInterface.ToString(), Keyword, ModifierKeywords.FormatList(Offered)), member);
            }
            else
            {
                context.AddError(context.ErrorCreator.ModifierNotAllowed.CreateOptions(Keyword, Kind, Name,
                    MemberRelations.GetHolderKindName(member), MemberRelations.GetHolderDisplayName(member),
                    ModifierKeywords.FormatList(Offered)), member);
            }
        }
    }

    /* A member's own modifiers, as C# allows them where it is declared, and, for an explicit
     * implementation, as one. */
    private PackMemberModifiers GetAllowedModifiers(PackMember member)
    {
        PackMember? Holder = MemberRelations.GetHoldingMember(member);
        if (MemberRelations.IsAccessor(member))
        {
            return GetAllowedAccessorModifiers(Holder!);
        }
        if (MemberRelations.IsType(member))
        {
            PackMemberModifiers Hiding = (Holder != null) ? PackMemberModifiers.New : PackMemberModifiers.None;
            return GetAllowedAccess(Holder, true) | Hiding | GetAllowedTypeModifiers(member);
        }

        PackMemberModifiers Allowed;
        if (MemberRelations.GetOperatorOverload(member) != null)
        {
            Allowed = (Holder is PackInterface) ? INTERFACE_OPERATOR : OPERATOR;
        }
        else
        {
            Allowed = GetAllowedAccess(Holder, false) | Holder switch
            {
                null => GetAllowedNamespaceMemberModifiers(member),
                PackInterface => GetAllowedInterfaceMemberModifiers(member),
                PackStruct => GetAllowedStructMemberModifiers(member),
                _ => GetAllowedClassMemberModifiers(member)
            };
        }

        if (MemberRelations.GetExplicitInterface(member) == null)
        {
            return Allowed;
        }
        PackMemberModifiers ExplicitAllowed = EXPLICIT_IMPLEMENTATION;
        if (Holder is PackInterface)
        {
            ExplicitAllowed |= PackMemberModifiers.Abstract;
        }
        return Allowed & ExplicitAllowed;
    }

    private PackMemberModifiers GetAllowedAccess(PackMember? holder, bool isType)
    {
        if (holder == null)
        {
            return NAMESPACE_ACCESS;
        }
        if ((holder is PackStruct) || (MemberRelations.IsStaticClass(holder) && !isType))
        {
            return UNPROTECTED_ACCESS;
        }
        return ACCESS;
    }

    /* Only the standard library's types can be builtin, which the parser checks, and then only a class or
     * a structure, whose values the compiler can decide how to store. */
    private PackMemberModifiers GetAllowedTypeModifiers(PackMember type)
    {
        return type switch
        {
            PackClass when type.HasModifier(PackMemberModifiers.Record) => PackMemberModifiers.Abstract
                | PackMemberModifiers.Sealed | PackMemberModifiers.Record | PackMemberModifiers.BuiltIn,
            PackClass => PackMemberModifiers.Abstract | PackMemberModifiers.Sealed | PackMemberModifiers.Static
                | PackMemberModifiers.BuiltIn,
            PackStruct => PackMemberModifiers.Readonly | PackMemberModifiers.BuiltIn,
            _ => PackMemberModifiers.None
        };
    }

    /* Whether "static" may be written on what a namespace holds, which belongs to no object anyway, is
     * not decided yet, so it is accepted for now. A builtin field or event is accepted here only because
     * LibraryBindingResolver reports it already. */
    private PackMemberModifiers GetAllowedNamespaceMemberModifiers(PackMember member)
    {
        return member switch
        {
            PackField => PackMemberModifiers.Static | PackMemberModifiers.Readonly | PackMemberModifiers.Const
                | PackMemberModifiers.BuiltIn,
            PackProperty or PackEvent => PackMemberModifiers.Static | PackMemberModifiers.BuiltIn,
            PackIndexer or PackConstructor => PackMemberModifiers.None,
            _ => PackMemberModifiers.Static | PackMemberModifiers.BuiltIn | PackMemberModifiers.Inline
        };
    }

    private PackMemberModifiers GetAllowedClassMemberModifiers(PackMember member)
    {
        return member switch
        {
            PackField => PackMemberModifiers.Static | PackMemberModifiers.Readonly | PackMemberModifiers.Const
                | PackMemberModifiers.Required | PackMemberModifiers.New | PackMemberModifiers.BuiltIn,
            PackProperty => PackMemberModifiers.Static | OVERRIDING | PackMemberModifiers.Required
                | PackMemberModifiers.New | PackMemberModifiers.BuiltIn,
            PackIndexer => OVERRIDING | PackMemberModifiers.New | PackMemberModifiers.BuiltIn,
            PackEvent => PackMemberModifiers.Static | OVERRIDING | PackMemberModifiers.New
                | PackMemberModifiers.BuiltIn,
            PackConstructor => CONSTRUCTOR,
            _ => PackMemberModifiers.Static | OVERRIDING | PackMemberModifiers.New | PackMemberModifiers.BuiltIn
                | PackMemberModifiers.Inline
        };
    }

    /* A structure derives from nothing but object, so its members can override object's, and can be
     * readonly, but can be neither abstract, virtual nor sealed. */
    private PackMemberModifiers GetAllowedStructMemberModifiers(PackMember member)
    {
        return member switch
        {
            PackField => PackMemberModifiers.Static | PackMemberModifiers.Readonly | PackMemberModifiers.Const
                | PackMemberModifiers.Required | PackMemberModifiers.New | PackMemberModifiers.BuiltIn,
            PackProperty => PackMemberModifiers.Static | PackMemberModifiers.Readonly
                | PackMemberModifiers.Override | PackMemberModifiers.Required | PackMemberModifiers.New
                | PackMemberModifiers.BuiltIn,
            PackIndexer => PackMemberModifiers.Readonly | PackMemberModifiers.Override | PackMemberModifiers.New
                | PackMemberModifiers.BuiltIn,
            PackEvent => PackMemberModifiers.Static | PackMemberModifiers.Override | PackMemberModifiers.New
                | PackMemberModifiers.BuiltIn,
            PackConstructor => CONSTRUCTOR,
            _ => PackMemberModifiers.Static | PackMemberModifiers.Readonly | PackMemberModifiers.Override
                | PackMemberModifiers.New | PackMemberModifiers.BuiltIn | PackMemberModifiers.Inline
        };
    }

    /* As in C#, an interface's members can be abstract, virtual or sealed, and static ones abstract or
     * virtual too, to be supplied by the types implementing it. Nothing in an interface is builtin. */
    private PackMemberModifiers GetAllowedInterfaceMemberModifiers(PackMember member)
    {
        return member switch
        {
            PackField => PackMemberModifiers.Static | PackMemberModifiers.Readonly | PackMemberModifiers.Const
                | PackMemberModifiers.New,
            PackProperty or PackEvent => PackMemberModifiers.Static | PackMemberModifiers.Abstract
                | PackMemberModifiers.Virtual | PackMemberModifiers.Sealed | PackMemberModifiers.New,
            PackIndexer => PackMemberModifiers.Abstract | PackMemberModifiers.Virtual | PackMemberModifiers.Sealed
                | PackMemberModifiers.New,
            PackConstructor => PackMemberModifiers.Static | PackMemberModifiers.Inline,
            _ => PackMemberModifiers.Static | PackMemberModifiers.Abstract | PackMemberModifiers.Virtual
                | PackMemberModifiers.Sealed | PackMemberModifiers.New | PackMemberModifiers.Inline
        };
    }

    /* An accessor can narrow its member's access, with the access modifiers what holds its member allows,
     * unless its member is an explicit implementation, whose access its interface decides. It can be
     * inlined like any function, and, in a structure, promise not to change it. */
    private PackMemberModifiers GetAllowedAccessorModifiers(PackMember accessorHolder)
    {
        PackMember? Holder = MemberRelations.GetHoldingMember(accessorHolder);
        PackMemberModifiers Allowed = PackMemberModifiers.Inline;
        if (MemberRelations.GetExplicitInterface(accessorHolder) == null)
        {
            Allowed |= GetAllowedAccess(Holder, false);
        }
        if (Holder is PackStruct)
        {
            Allowed |= PackMemberModifiers.Readonly;
        }
        return Allowed;
    }

    private void CheckAccessModifiers(PackMember member, PackResolutionContext context)
    {
        PackMemberModifiers Access = member.Modifiers & ACCESS;
        if ((ModifierKeywords.Split(Access).Count() > 1) && (Access != PROTECTED_INTERNAL)
            && (Access != PRIVATE_PROTECTED))
        {
            context.AddError(context.ErrorCreator.MultipleAccessModifiers.CreateOptions(
                MemberRelations.GetKindName(member), MemberRelations.GetDisplayName(member)), member);
        }
    }

    /* Only the first conflicting pair is reported, since any one of them already says what is wrong. */
    private void CheckClassConflicts(PackMember type, PackResolutionContext context)
    {
        PackMemberModifiers[] Present = ModifierKeywords.Split(type.Modifiers & (PackMemberModifiers.Abstract
            | PackMemberModifiers.Sealed | PackMemberModifiers.Static)).ToArray();

        if (Present.Length > 1)
        {
            context.AddError(context.ErrorCreator.ClassModifierConflict.CreateOptions(
                type.SelfIdentifier.SourceCodeName, ModifierKeywords.GetKeyword(Present[0]),
                ModifierKeywords.GetKeyword(Present[1])), type);
        }
    }

    private void CheckMemberConflicts(PackMember member, PackResolutionContext context)
    {
        PackMemberModifiers Modifiers = member.Modifiers;
        string Kind = MemberRelations.GetKindName(member);
        string Name = MemberRelations.GetDisplayName(member);
        bool IsInInterface = MemberRelations.GetHoldingMember(member) is PackInterface;

        if (HasAll(Modifiers, PackMemberModifiers.Const))
        {
            ReportEachConflict(member, Modifiers & (PackMemberModifiers.Static | PackMemberModifiers.Readonly
                | PackMemberModifiers.Required), conflict => context.ErrorCreator.ConstModifierConflict
                .CreateOptions(Name, conflict), context);
        }
        if (HasAll(Modifiers, PackMemberModifiers.Abstract | PackMemberModifiers.Virtual))
        {
            context.AddError(context.ErrorCreator.AbstractVirtualConflict.CreateOptions(Kind, Name), member);
        }
        if (HasAll(Modifiers, PackMemberModifiers.Override))
        {
            ReportEachConflict(member, Modifiers & (PackMemberModifiers.Virtual | PackMemberModifiers.New),
                conflict => context.ErrorCreator.OverrideConflict.CreateOptions(Kind, Name, conflict), context);
        }

        /* An interface's "sealed" has rules of its own, which CheckInterfaceConflicts checks. */
        if (HasAll(Modifiers, PackMemberModifiers.Abstract | PackMemberModifiers.Sealed))
        {
            context.AddError(context.ErrorCreator.AbstractSealedConflict.CreateOptions(Kind, Name), member);
        }
        else if (HasAll(Modifiers, PackMemberModifiers.Sealed) && !HasAll(Modifiers, PackMemberModifiers.Override)
            && !IsInInterface)
        {
            context.AddError(context.ErrorCreator.SealedWithoutOverride.CreateOptions(Kind, Name), member);
        }

        if (HasAll(Modifiers, PackMemberModifiers.Static))
        {
            PackMemberModifiers Dispatching = IsInInterface ? PackMemberModifiers.Override
                : (PackMemberModifiers.Abstract | PackMemberModifiers.Virtual | PackMemberModifiers.Override);
            ReportEachConflict(member, Modifiers & Dispatching, conflict => context.ErrorCreator
                .StaticVirtualConflict.CreateOptions(Kind, Name, conflict), context);
        }

        /* A member written with no access modifier is private in a class or structure, as in C#. An
         * explicit implementation counts as private too, but may still be abstract, in an interface. */
        if (IsPrivate(member) && (MemberRelations.GetExplicitInterface(member) == null))
        {
            ReportEachConflict(member, Modifiers & (PackMemberModifiers.Abstract | PackMemberModifiers.Virtual
                | PackMemberModifiers.Override), conflict => context.ErrorCreator.PrivateVirtualConflict
                .CreateOptions(Kind, Name, conflict), context);
        }

        CheckRequiredConflicts(member, context);

        if (HasAll(Modifiers, PackMemberModifiers.BuiltIn | PackMemberModifiers.Abstract))
        {
            context.AddError(context.ErrorCreator.BuiltInAbstractConflict.CreateOptions(Kind, Name), member);
        }
        if (HasAll(Modifiers, PackMemberModifiers.Readonly) && IsStaticForReadonly(member))
        {
            context.AddError(context.ErrorCreator.ReadonlyStaticConflict.CreateOptions(Kind, Name), member);
        }
        if (MemberRelations.IsStaticConstructor(member) && ((Modifiers & ACCESS) != PackMemberModifiers.None))
        {
            context.AddError(context.ErrorCreator.StaticConstructorAccessModifier.CreateOptions(
                member.SelfIdentifier.SourceCodeName), member);
        }
    }

    /* A readonly member promises not to change the structure it is used on, which a static member, or an
     * accessor of a static property, is used on none of. A readonly field is another thing, and can be
     * static. */
    private bool IsStaticForReadonly(PackMember member)
    {
        if (member is PackField)
        {
            return false;
        }
        PackMember StaticHolder = MemberRelations.IsAccessor(member) ? MemberRelations.GetHoldingMember(member)!
            : member;
        return StaticHolder.HasModifier(PackMemberModifiers.Static);
    }

    /* In an interface, "sealed" says that a member's body is the only one, so it cannot go with "virtual",
     * and means nothing on a private instance member, which nothing could replace anyway. A static member,
     * which nothing could replace either unless it is virtual, can say it all the same, as in C#. An
     * instance event can only be abstract there, since it has no accessors to give it a body. */
    private void CheckInterfaceConflicts(PackMember member, PackResolutionContext context)
    {
        if (MemberRelations.GetHoldingMember(member) is not PackInterface Holder)
        {
            return;
        }

        PackMemberModifiers Modifiers = member.Modifiers;
        bool IsStatic = HasAll(Modifiers, PackMemberModifiers.Static);
        PackMemberModifiers Private = (IsPrivate(member) && !IsStatic) ? PackMemberModifiers.Private
            : PackMemberModifiers.None;
        if ((member is PackEvent) && !IsStatic)
        {
            PackMemberModifiers Conflicts = (Modifiers & (PackMemberModifiers.Virtual
                | PackMemberModifiers.Sealed)) | Private;
            ReportEachConflict(member, Conflicts, conflict => context.ErrorCreator.InterfaceEventNotAbstract
                .CreateOptions(member.SelfIdentifier.SourceCodeName, Holder.SelfIdentifier.SourceCodeName,
                conflict), context);
            return;
        }

        if (HasAll(Modifiers, PackMemberModifiers.Sealed))
        {
            PackMemberModifiers Conflicts = (Modifiers & PackMemberModifiers.Virtual) | Private;
            ReportEachConflict(member, Conflicts, conflict => context.ErrorCreator.InterfaceSealedConflict
                .CreateOptions(MemberRelations.GetKindName(member), MemberRelations.GetDisplayName(member),
                conflict), context);
        }
    }

    /* One error for each of the conflicting modifiers, made by the given function from its keyword. */
    private void ReportEachConflict(PackMember member,
        PackMemberModifiers conflicts,
        Func<string, ErrorCreateOptions> createError,
        PackResolutionContext context)
    {
        foreach (PackMemberModifiers Conflict in ModifierKeywords.Split(conflicts))
        {
            context.AddError(createError(ModifierKeywords.GetKeyword(Conflict)), member);
        }
    }

    /* A required member is set where each object is created, so it has to belong to the object and be
     * settable. A constant is already reported by the conflicts of "const". */
    private void CheckRequiredConflicts(PackMember member, PackResolutionContext context)
    {
        PackMemberModifiers Modifiers = member.Modifiers;
        if (!HasAll(Modifiers, PackMemberModifiers.Required) || HasAll(Modifiers, PackMemberModifiers.Const))
        {
            return;
        }

        string Kind = MemberRelations.GetKindName(member);
        string Name = MemberRelations.GetDisplayName(member);
        if (HasAll(Modifiers, PackMemberModifiers.Static))
        {
            context.AddError(context.ErrorCreator.RequiredStatic.CreateOptions(Kind, Name), member);
        }

        bool IsSettable = member switch
        {
            PackField => !HasAll(Modifiers, PackMemberModifiers.Readonly),
            PackProperty Property => (Property.SetFunction != null) || (Property.InitFunction != null),
            _ => true
        };
        if (!IsSettable)
        {
            context.AddError(context.ErrorCreator.RequiredNotSettable.CreateOptions(Kind, Name), member);
        }
    }

    /* What a member's modifiers say about the class holding it: only an abstract class can hold abstract
     * members, and a sealed class's virtual members could never be overridden. A static member is left
     * alone, since its "abstract" or "virtual" is reported as a conflict already. */
    private void CheckHolderConflicts(PackMember member, PackResolutionContext context)
    {
        PackMemberModifiers Modifiers = member.Modifiers;
        if ((MemberRelations.GetHoldingMember(member) is not PackClass Holder)
            || HasAll(Modifiers, PackMemberModifiers.Static))
        {
            return;
        }

        string Kind = MemberRelations.GetKindName(member);
        string Name = MemberRelations.GetDisplayName(member);
        if (HasAll(Modifiers, PackMemberModifiers.Abstract) && !Holder.HasModifier(PackMemberModifiers.Abstract))
        {
            context.AddError(context.ErrorCreator.AbstractMemberInConcreteClass.CreateOptions(Kind, Name,
                Holder.SelfIdentifier.SourceCodeName), member);
        }
        if (HasAll(Modifiers, PackMemberModifiers.Virtual) && Holder.HasModifier(PackMemberModifiers.Sealed))
        {
            context.AddError(context.ErrorCreator.VirtualMemberInSealedClass.CreateOptions(Kind, Name,
                Holder.SelfIdentifier.SourceCodeName), member);
        }
    }

    /* Every operator is static, and outside an interface, where members are public unless they say
     * otherwise, it has to say it is public too. An access modifier other than "public" has been reported
     * already, as one an operator cannot have, so only an operator written with none is told to be
     * public. An explicit implementation cannot say who can reach it, so it only has to be static. */
    private void CheckOperatorModifiers(PackMember member, PackResolutionContext context)
    {
        if (MemberRelations.GetOperatorOverload(member) == null)
        {
            return;
        }

        PackMember Holder = MemberRelations.GetHoldingMember(member)!;
        string Name = MemberRelations.GetDisplayName(member);
        string HolderName = Holder.SelfIdentifier.SourceCodeName;
        bool IsStatic = member.HasModifier(PackMemberModifiers.Static);
        TypeTargetIdentifier? ExplicitInterface = MemberRelations.GetExplicitInterface(member);

        if (ExplicitInterface != null)
        {
            if (!IsStatic)
            {
                context.AddError(context.ErrorCreator.ExplicitOperatorNotStatic.CreateOptions(Name, HolderName,
                    ExplicitInterface.ToString()), member);
            }
            return;
        }

        bool IsAccessWritten = ((member.Modifiers | member.RejectedModifiers) & ACCESS)
            != PackMemberModifiers.None;
        if (!IsStatic || (!IsAccessWritten && (Holder is not PackInterface)))
        {
            context.AddError(context.ErrorCreator.OperatorNotPublicStatic.CreateOptions(Name, HolderName),
                member);
        }
    }

    /* An accessor's own access is checked by CheckAccessorAccess. An accessor of an abstract property or
     * indexer cannot be private, since whatever supplies the member could not see it. In a structure, where an accessor can be readonly, it can only be when its member
     * is not, and only when its member has another accessor which is not, as in C#; and an "init"
     * accessor, or a "set" accessor without a body, which stores a value, cannot be at all. A static
     * member's readonly is reported as a conflict already. */
    private void CheckAccessorModifiers(PackMember member, PackResolutionContext context)
    {
        if (!MemberRelations.IsAccessor(member))
        {
            CheckReadonlyProperty(member, context);
            return;
        }

        PackMember Holder = MemberRelations.GetHoldingMember(member)!;
        string Name = MemberRelations.GetDisplayName(member);
        string HolderKind = MemberRelations.GetKindName(Holder);
        string HolderName = MemberRelations.GetDisplayName(Holder);
        CheckAccessorAccess(member, Holder, context);

        /* Only beside another accessor, and alone in having its own access, as C# reports it; otherwise what
         * CheckAccessorAccess reports is the mistake. */
        PackMember[] Siblings = Holder.SubMembers.ToArray();
        bool IsAccessAlone = (Siblings.Length == 2)
            && (Siblings.Count(sibling => (sibling.Modifiers & ACCESS) != PackMemberModifiers.None) == 1);
        if (((member.Modifiers & ACCESS) == PackMemberModifiers.Private) && IsAccessAlone
            && MemberRelations.IsAbstract(Holder))
        {
            context.AddError(context.ErrorCreator.PrivateAccessorOfAbstract.CreateOptions(Name, HolderKind,
                HolderName), member);
        }
        if (!member.HasModifier(PackMemberModifiers.Readonly) || Holder.HasModifier(PackMemberModifiers.Static))
        {
            return;
        }

        PackFunction[] Accessors = Holder.SubMembers.Cast<PackFunction>().ToArray();
        PackFunction? Setter = ((IPackAccessorHolder)Holder).SetFunction;
        bool IsInit = ReferenceEquals(((IPackAccessorHolder)Holder).InitFunction, member);
        bool IsStoringSetter = ReferenceEquals(Setter, member) && (Setter!.Statements == null);

        /* Of two readonly accessors, the later one is reported, as the one which makes them two. */
        int Position = Array.IndexOf(Accessors, (PackFunction)member);
        bool IsMisplaced = Holder.HasModifier(PackMemberModifiers.Readonly) || (Accessors.Length < 2)
            || Accessors.Take(Position).Any(earlier => earlier.HasModifier(PackMemberModifiers.Readonly));

        if (IsInit)
        {
            context.AddError(context.ErrorCreator.ReadonlyInitAccessor.CreateOptions(Name), member);
        }
        else if (IsStoringSetter)
        {
            context.AddError(context.ErrorCreator.ReadonlyAutoSetter.CreateOptions(
                MemberRelations.GetKindName(member), Name), member);
        }
        else if (IsMisplaced)
        {
            context.AddError(context.ErrorCreator.ReadonlyAccessorMisplaced.CreateOptions(Name, HolderKind,
                HolderName), member);
        }
    }

    /* An accessor's own access narrows its member's, as in C#. It is written on one accessor of a member
     * which has another, unless the member is an override, whose other accessor is inherited, and it is
     * strictly narrower than the member's access, where "protected" and "internal" are each narrower than
     * "protected internal" but not than each other. Of two accessors with it, the later is reported. */
    private void CheckAccessorAccess(PackMember accessor, PackMember holder, PackResolutionContext context)
    {
        PackMemberModifiers Access = accessor.Modifiers & ACCESS;
        if (Access == PackMemberModifiers.None)
        {
            return;
        }

        string Name = MemberRelations.GetDisplayName(accessor);
        string HolderKind = MemberRelations.GetKindName(holder);
        string HolderName = MemberRelations.GetDisplayName(holder);
        PackMember[] Accessors = holder.SubMembers.ToArray();
        if (Accessors.Length < 2)
        {
            if (!holder.HasModifier(PackMemberModifiers.Override))
            {
                context.AddError(context.ErrorCreator.AccessorAccessWithoutOther.CreateOptions(Name, HolderKind,
                    HolderName), accessor);
                return;
            }
        }
        else if (Accessors.TakeWhile(other => !ReferenceEquals(other, accessor))
            .Any(earlier => (earlier.Modifiers & ACCESS) != PackMemberModifiers.None))
        {
            context.AddError(context.ErrorCreator.AccessorAccessOnBoth.CreateOptions(HolderKind, HolderName),
                accessor);
            return;
        }

        /* An access mix, or a member access taken off, has been reported, and is not compared. */
        PackMemberModifiers HolderAccess = MemberRelations.GetEffectiveAccess(holder);
        bool IsReported = !MemberRelations.IsValidAccess(Access)
            || !MemberRelations.IsValidAccess(holder.Modifiers & ACCESS)
            || ((holder.RejectedModifiers & ACCESS) != PackMemberModifiers.None);
        if (!IsReported && !IsNarrower(Access, HolderAccess))
        {
            context.AddError(context.ErrorCreator.AccessorAccessNotNarrower.CreateOptions(Name,
                ModifierKeywords.FormatAccess(Access), HolderKind, HolderName,
                ModifierKeywords.FormatAccess(HolderAccess)), accessor);
        }
    }

    private bool IsNarrower(PackMemberModifiers access, PackMemberModifiers than)
    {
        PackMemberModifiers PrivateProtected = PackMemberModifiers.Private | PackMemberModifiers.Protected;
        return than switch
        {
            PackMemberModifiers.Public => access != PackMemberModifiers.Public,
            PackMemberModifiers.Protected | PackMemberModifiers.Internal => (access != PackMemberModifiers.Public)
                && (access != (PackMemberModifiers.Protected | PackMemberModifiers.Internal)),
            PackMemberModifiers.Protected or PackMemberModifiers.Internal => (access == PrivateProtected)
                || (access == PackMemberModifiers.Private),
            PackMemberModifiers.Private | PackMemberModifiers.Protected => access == PackMemberModifiers.Private,
            _ => false
        };
    }

    /* A readonly property of a structure cannot store a value it sets after the structure is created. In a
     * readonly structure that is MemberPlacementChecker's to report, as it is for every property there. */
    private void CheckReadonlyProperty(PackMember member, PackResolutionContext context)
    {
        if ((member is not PackProperty Property)
            || !Property.HasModifier(PackMemberModifiers.Readonly)
            || Property.HasAnyModifier(PackMemberModifiers.Static, PackMemberModifiers.BuiltIn)
            || (MemberRelations.GetHoldingMember(Property) is not PackStruct Holder)
            || Holder.HasModifier(PackMemberModifiers.Readonly))
        {
            return;
        }

        if ((Property.SetFunction != null) && (Property.SetFunction.Statements == null))
        {
            context.AddError(context.ErrorCreator.ReadonlyAutoSetter.CreateOptions(
                MemberRelations.GetKindName(Property), MemberRelations.GetDisplayName(Property)), Property);
        }
    }

    private bool HasAll(PackMemberModifiers modifiers, PackMemberModifiers wanted)
    {
        return (modifiers & wanted) == wanted;
    }


    // Inherited methods.
    /* A member's holder is always checked before the member, since the pack lists members after what
     * holds them, so a holder's rejected modifiers are gone before its members are checked. */
    public void ResolvePack(PackResolutionContext context)
    {
        ArgumentNullException.ThrowIfNull(context, nameof(context));

        foreach (PackMember Member in context.Pack.Members)
        {
            CheckMember(Member, context);
        }
    }
}
