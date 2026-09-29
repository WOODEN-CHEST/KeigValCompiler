using KeigValCompiler.Error;
using KeigValCompiler.Semantician.Member;
using KeigValCompiler.Semantician.Types;

namespace KeigValCompiler.Semantician.Resolver;

/* Checks, against C#'s rules, that each class and structure implements every abstract member of every
 * interface it implements, however indirectly, static abstract ones included: with a member implementing it
 * explicitly, or with a public member of the same kind, name, signature and type, static exactly when the
 * interface's is, the nearest found in the type or a class it derives from. One of these which is wrong in
 * another way is reported only when no other is right. A property or indexer implementing one also has, or
 * inherits, each accessor the interface's has, public, and a function implementing a generic one has its
 * constraints. An interface member with a body needs no implementation, unless an interface deriving from
 * its own makes it abstract again, and one such interface may give it a body instead: the interface
 * deriving from all the others which do decides, and when none does, the type has to implement it itself.
 * Also checks that every explicit implementation names an interface its type lists, or one those derive
 * from, and a member of it which can be implemented, with that member's type and accessors; and that no
 * two interfaces a type lists could become one for some type arguments. A static class, whose interfaces
 * are reported already, and a type deriving from itself, are left alone. */
internal class InterfaceImplementationChecker : IPackResolver
{
    // Static fields.
    private static readonly string[] _accessorKeywords = { KGVL.KEYWORD_GET, KGVL.KEYWORD_SET, KGVL.KEYWORD_INIT };


    // Private methods.
    private void CheckType(PackMember type, PackResolutionContext context)
    {
        if ((type is not (PackClass or PackStruct or PackInterface)) || MemberRelations.IsStaticClass(type))
        {
            return;
        }
        DeclaredType Instance = context.TypeReader.GetInstanceType(type);
        if (context.Hierarchy.IsInBaseCycle(Instance))
        {
            return;
        }

        CheckUnifyingInterfaces(type, Instance, context);
        CheckExplicitImplementations(type, Instance, context);
        if (type is PackInterface)
        {
            return;
        }

        IReadOnlyList<DeclaredType> Interfaces = context.Hierarchy.GetInterfaces(Instance);
        List<DeclaredType> Holders = new() { Instance };
        Holders.AddRange(context.Hierarchy.GetBaseClasses(Instance));
        foreach (DeclaredType Interface in Interfaces)
        {
            TypeSubstitution Substitution = TypeSubstitution.Of(Interface);
            foreach (PackMember Member in MemberRelations.GetSignedMembers(Interface.Declaration)
                .Where(IsImplementable))
            {
                DeclaredSignature Signature = context.SignatureReader.Read(Member, Substitution);
                if (Signature.IsComplete)
                {
                    CheckMember(type, Holders, Interface, Interfaces, Signature, context);
                }
            }
        }
    }

    /* What an interface can ask its implementers for: not constructors, fields or explicit implementations,
     * which implement another interface's members themselves. */
    private bool IsImplementable(PackMember member)
    {
        return (member is not (PackConstructor or PackField))
            && (MemberRelations.GetExplicitInterface(member) == null);
    }

    /* A member which has a body needs no implementation, and whatever of the same name the type has, public
     * or not, is then no concern of the interface's, as in C#. */
    private void CheckMember(PackMember type,
        List<DeclaredType> holders,
        DeclaredType interfaceType,
        IReadOnlyList<DeclaredType> interfaces,
        DeclaredSignature signature,
        PackResolutionContext context)
    {
        (bool IsRequired, DeclaredType? First, DeclaredType? Second) = FindMostSpecific(interfaceType,
            signature, interfaces, context);
        if (!IsRequired || IsImplemented(type, holders, interfaceType, signature, context))
        {
            return;
        }

        string TypeKind = MemberRelations.GetKindName(type);
        string TypeName = type.SelfIdentifier.SourceCodeName;
        string MemberKind = MemberRelations.GetKindName(signature.Member);
        if ((First != null) && (Second != null))
        {
            context.AddError(context.ErrorCreator.NoMostSpecificImplementation.CreateOptions(TypeKind, TypeName,
                MemberKind, signature.GetDisplayName(), interfaceType.ToString(), First.ToString(),
                Second.ToString()), type);
            return;
        }
        context.AddError(context.ErrorCreator.InterfaceMemberNotImplemented.CreateOptions(TypeKind, TypeName,
            MemberKind, signature.GetDisplayName(), interfaceType.ToString()), type);
    }

    /* Whether an interface's member still needs implementing once the interfaces the type implements have
     * had their say, as C# decides it: of the member's own interface, when its member has a body, and each
     * interface implementing the member explicitly, with a body or abstract again, the one deriving from all
     * the others decides. The member needs implementing when that one is abstract, or when there is none,
     * in which case the two interfaces given are two with nothing deriving from both. */
    private (bool IsRequired, DeclaredType? First, DeclaredType? Second) FindMostSpecific(DeclaredType holder,
        DeclaredSignature signature,
        IReadOnlyList<DeclaredType> interfaces,
        PackResolutionContext context)
    {
        List<(DeclaredType Interface, bool IsAbstract)> Candidates = new();
        if (!MemberRelations.IsAbstract(signature.Member))
        {
            Candidates.Add((holder, false));
        }
        foreach (DeclaredType Other in interfaces.Where(other => !other.Equals(holder)))
        {
            PackMember? Implementation = FindExplicitImplementation(Other, holder, signature, context);
            if (Implementation != null)
            {
                Candidates.Add((Other, Implementation.HasModifier(PackMemberModifiers.Abstract)));
            }
        }

        (DeclaredType Interface, bool IsAbstract)[] MostSpecific = Candidates.Where(candidate => !Candidates.Any(
            other => !other.Interface.Equals(candidate.Interface)
                && context.Hierarchy.GetInterfaces(other.Interface).Contains(candidate.Interface))).ToArray();
        return MostSpecific.Length switch
        {
            0 => (MemberRelations.IsAbstract(signature.Member), null, null),
            1 => (MostSpecific[0].IsAbstract, null, null),
            _ => (true, MostSpecific[0].Interface, MostSpecific[1].Interface)
        };
    }

    /* Whether a type or a class it derives from implements an interface's member, explicitly or with a
     * public member, the nearest first. A member of the right kind, name and signature which is wrong in
     * another way is passed over, as C# does; if no other is right, the nearest of those is reported, and
     * counts as the implementation, so that it is not reported as missing as well. */
    private bool IsImplemented(PackMember type,
        List<DeclaredType> holders,
        DeclaredType interfaceType,
        DeclaredSignature signature,
        PackResolutionContext context)
    {
        if (holders.Any(holder => FindExplicitImplementation(holder, interfaceType, signature, context) != null))
        {
            return true;
        }

        ErrorCreateOptions? Mismatch = null;
        foreach (DeclaredType Holder in holders)
        {
            TypeSubstitution Substitution = TypeSubstitution.Of(Holder);
            bool IsOwn = ReferenceEquals(Holder.Declaration, type);
            foreach (PackMember Candidate in MemberRelations.GetSignedMembers(Holder.Declaration).Where(
                candidate => (MemberRelations.GetExplicitInterface(candidate) == null)
                    && (IsOwn || InheritedMembers.IsVisibleFrom(candidate, type))))
            {
                DeclaredSignature CandidateSignature = context.SignatureReader.Read(Candidate, Substitution);
                if (!InheritedMembers.IsSameMember(signature, CandidateSignature))
                {
                    continue;
                }
                if (!CandidateSignature.IsComplete)
                {
                    return true;
                }

                ErrorCreateOptions? Error = GetImplementationError(type, interfaceType, signature,
                    CandidateSignature, context);
                if (Error == null)
                {
                    CheckImplementation(type, Holder, interfaceType, signature, CandidateSignature, context);
                    return true;
                }
                Mismatch ??= Error;
            }
        }

        if (Mismatch != null)
        {
            context.AddError(Mismatch.Value, type);
            return true;
        }

        /* A record's positional parameters are not properties yet, so a property one of them would give is
         * taken to be there, rather than reported missing. */
        return (signature.Kind == DeclaredSignatureKind.Property) && holders.Any(
            holder => MemberRelations.GetPositionalParameters(holder.Declaration).Any(
                parameter => parameter.SelfIdentifier.SourceCodeName == signature.Name));
    }

    /* The member of a type which implements an interface's member explicitly, if it has one. */
    private PackMember? FindExplicitImplementation(DeclaredType holder,
        DeclaredType interfaceType,
        DeclaredSignature signature,
        PackResolutionContext context)
    {
        TypeSubstitution Substitution = TypeSubstitution.Of(holder);
        foreach (PackMember Candidate in MemberRelations.GetSignedMembers(holder.Declaration))
        {
            TypeTargetIdentifier? Named = MemberRelations.GetExplicitInterface(Candidate);
            if ((Named == null) || Named.IsArrayOrNullable
                || !interfaceType.Equals(context.TypeReader.Read(Named)?.Substitute(Substitution)))
            {
                continue;
            }
            if (InheritedMembers.IsSameMember(signature, context.SignatureReader.Read(Candidate, Substitution)))
            {
                return Candidate;
            }
        }
        return null;
    }

    /* What keeps a member of the right kind, name and signature from implementing an interface's member,
     * or null when nothing does. An operator is static whatever it says, which ModifierChecker reports, so
     * only other members are compared for being static. */
    private ErrorCreateOptions? GetImplementationError(PackMember type,
        DeclaredType interfaceType,
        DeclaredSignature signature,
        DeclaredSignature implementation,
        PackResolutionContext context)
    {
        PackMember Member = signature.Member;
        PackMember Implementing = implementation.Member;
        string TypeKind = MemberRelations.GetKindName(type);
        string TypeName = type.SelfIdentifier.SourceCodeName;
        string MemberKind = MemberRelations.GetKindName(Member);
        string MemberName = signature.GetDisplayName();
        string InterfaceName = interfaceType.ToString();
        bool IsOperator = signature.Kind is DeclaredSignatureKind.Operator or DeclaredSignatureKind.Conversion;

        if (!IsOperator && (Member.HasModifier(PackMemberModifiers.Static)
            != Implementing.HasModifier(PackMemberModifiers.Static)))
        {
            return context.ErrorCreator.InterfaceImplementationStatic.CreateOptions(TypeKind, TypeName,
                MemberKind, MemberName, InterfaceName);
        }
        if (MemberRelations.GetEffectiveAccess(Implementing) != PackMemberModifiers.Public)
        {
            return context.ErrorCreator.InterfaceImplementationNotPublic.CreateOptions(TypeKind, TypeName,
                MemberKind, MemberName, InterfaceName);
        }
        if (!signature.HasSameType(implementation))
        {
            return context.ErrorCreator.InterfaceImplementationType.CreateOptions(TypeKind, TypeName, MemberKind,
                MemberName, InterfaceName, implementation.Type?.ToString() ?? KGVL.KEYWORD_VOID,
                signature.Type?.ToString() ?? KGVL.KEYWORD_VOID);
        }
        return null;
    }

    /* What an implementation has to have besides its signature: each accessor of the interface's property
     * or indexer, public, its own or inherited from what it overrides; and for a generic function, the
     * constraints of the interface's, as in C#, where the two can only differ with an explicit
     * implementation, which takes them from the interface without restating them. The holder is the type
     * declaring the implementation, as the signatures see it. */
    private void CheckImplementation(PackMember type,
        DeclaredType holder,
        DeclaredType interfaceType,
        DeclaredSignature signature,
        DeclaredSignature implementation,
        PackResolutionContext context)
    {
        string TypeKind = MemberRelations.GetKindName(type);
        string TypeName = type.SelfIdentifier.SourceCodeName;
        string MemberKind = MemberRelations.GetKindName(signature.Member);
        string MemberName = signature.GetDisplayName();

        foreach (string Keyword in _accessorKeywords.Where(
            keyword => InheritedMembers.GetAccessor(signature.Member, keyword) != null))
        {
            PackFunction? Accessor = InheritedMembers.FindAccessor(implementation, holder, Keyword, type,
                context);
            if ((Accessor == null) || (MemberRelations.GetEffectiveAccess(Accessor) != PackMemberModifiers.Public))
            {
                context.AddError(context.ErrorCreator.InterfaceImplementationAccessor.CreateOptions(TypeKind,
                    TypeName, MemberKind, MemberName, interfaceType.ToString(), Keyword), type);
                return;
            }
        }

        TypeSubstitution InterfaceSide = TypeSubstitution.Of(interfaceType);
        TypeSubstitution Positions = TypeSubstitution.ByPosition(signature.GenericParameters,
            implementation.GenericParameters);
        TypeSubstitution ImplementationSide = TypeSubstitution.Of(holder);
        foreach ((GenericTypeParameter Expected, GenericTypeParameter Actual) in signature.GenericParameters
            .Zip(implementation.GenericParameters))
        {
            HashSet<SemanticType> ExpectedTypes = context.Constraints.GetTypeConstraints(Expected)
                .Select(constraint => constraint.Substitute(InterfaceSide).Substitute(Positions)).ToHashSet();
            HashSet<SemanticType> ActualTypes = context.Constraints.GetTypeConstraints(Actual)
                .Select(constraint => constraint.Substitute(ImplementationSide)).ToHashSet();
            bool IsSame = ExpectedTypes.SetEquals(ActualTypes)
                && IsSameSpecialConstraint(Expected, Actual, SpecialGenericConstraint.Class, context)
                && IsSameSpecialConstraint(Expected, Actual, SpecialGenericConstraint.Struct, context);
            if (!IsSame)
            {
                context.AddError(context.ErrorCreator.ImplementationConstraints.CreateOptions(TypeKind, TypeName,
                    MemberName, interfaceType.ToString(), Actual.SelfIdentifier.SourceCodeName,
                    Expected.SelfIdentifier.SourceCodeName), type);
            }
        }
    }

    /* Whether two parameters agree on a special constraint. "notnull" is left out: as in C#, where the two
     * differing in it is only a warning about nullability, it is left to checking nullability. */
    private bool IsSameSpecialConstraint(GenericTypeParameter first,
        GenericTypeParameter second,
        SpecialGenericConstraint constraint,
        PackResolutionContext context)
    {
        return context.Constraints.HasSpecialConstraint(first, constraint)
            == context.Constraints.HasSpecialConstraint(second, constraint);
    }

    /* An explicit implementation names an interface its type lists, or for an interface, one it derives
     * from, or one those derive from, and a member of it which can be implemented, as in C#. It has that
     * member's type and accessors. One naming what did not resolve, or what is no interface, has been
     * reported, and so has one which a namespace holds. */
    private void CheckExplicitImplementations(PackMember type,
        DeclaredType instance,
        PackResolutionContext context)
    {
        IReadOnlyList<DeclaredType> OwnInterfaces = context.Hierarchy.GetOwnInterfaces(instance);
        TypeSubstitution NoSubstitution = new();
        foreach (PackMember Member in MemberRelations.GetSignedMembers(type))
        {
            TypeTargetIdentifier? Named = MemberRelations.GetExplicitInterface(Member);
            if ((Named == null) || Named.IsArrayOrNullable
                || (context.TypeReader.Read(Named) is not DeclaredType InterfaceType)
                || (InterfaceType.Declaration is not PackInterface))
            {
                continue;
            }

            string Name = MemberRelations.GetDisplayName(Member);
            string InterfaceName = InterfaceType.ToString();
            if (!OwnInterfaces.Contains(InterfaceType))
            {
                context.AddError(context.ErrorCreator.ExplicitInterfaceNotImplemented.CreateOptions(Name,
                    InterfaceName, type.SelfIdentifier.SourceCodeName), Member);
                continue;
            }

            DeclaredSignature Signature = context.SignatureReader.Read(Member, NoSubstitution);
            if (!Signature.IsComplete)
            {
                continue;
            }
            (DeclaredType Interface, DeclaredSignature Implemented)? Found =
                InheritedMembers.FindExplicitlyImplemented(Member, context);
            if (Found == null)
            {
                context.AddError(context.ErrorCreator.ExplicitMemberNotInInterface.CreateOptions(Name,
                    InterfaceName, MemberRelations.GetKindName(Member)), Member);
                continue;
            }
            CheckExplicitImplementation(Signature, Found.Value.Implemented, InterfaceName, context);
        }
    }

    private void CheckExplicitImplementation(DeclaredSignature signature,
        DeclaredSignature implemented,
        string interfaceName,
        PackResolutionContext context)
    {
        PackMember Member = signature.Member;
        string Name = MemberRelations.GetDisplayName(Member);
        string Kind = MemberRelations.GetKindName(Member);
        if (!signature.HasSameType(implemented))
        {
            context.AddError(context.ErrorCreator.ExplicitImplementationType.CreateOptions(Name, Kind,
                implemented.GetDisplayName(), interfaceName, signature.Type?.ToString() ?? KGVL.KEYWORD_VOID,
                implemented.Type?.ToString() ?? KGVL.KEYWORD_VOID), Member);
        }

        foreach (string Keyword in _accessorKeywords)
        {
            bool IsOwn = InheritedMembers.GetAccessor(Member, Keyword) != null;
            bool IsInterfaces = InheritedMembers.GetAccessor(implemented.Member, Keyword) != null;
            if (IsInterfaces && !IsOwn)
            {
                context.AddError(context.ErrorCreator.ExplicitImplementationMissingAccessor.CreateOptions(Name,
                    Kind, interfaceName, Keyword), Member);
            }
            else if (IsOwn && !IsInterfaces)
            {
                context.AddError(context.ErrorCreator.ExplicitImplementationExtraAccessor.CreateOptions(Name,
                    Kind, interfaceName, Keyword), Member);
            }
        }
    }

    /* Two interfaces a type lists, or which those derive from, may not be one generic interface with type
     * arguments which some choice of the type's own generic parameters would make the same, as "I<T>" and
     * "I<int>" are when T is int, since which of the two a member implements would then be unknown. */
    private void CheckUnifyingInterfaces(PackMember type, DeclaredType instance, PackResolutionContext context)
    {
        IReadOnlyList<DeclaredType> Interfaces = context.Hierarchy.GetOwnInterfaces(instance);
        for (int First = 0; First < Interfaces.Count; First++)
        {
            for (int Second = First + 1; Second < Interfaces.Count; Second++)
            {
                if (ReferenceEquals(Interfaces[First].Declaration, Interfaces[Second].Declaration)
                    && CanUnify(Interfaces[First], Interfaces[Second], new(ReferenceEqualityComparer.Instance)))
                {
                    context.AddError(context.ErrorCreator.UnifyingInterfaces.CreateOptions(
                        MemberRelations.GetKindName(type), type.SelfIdentifier.SourceCodeName,
                        Interfaces[First].ToString(), Interfaces[Second].ToString()), type);
                }
            }
        }
    }

    /* Whether some types for the generic parameters in two types would make them the same type. The
     * bindings hold the types chosen so far. */
    private bool CanUnify(SemanticType first,
        SemanticType second,
        Dictionary<GenericTypeParameter, SemanticType> bindings)
    {
        SemanticType BoundFirst = GetBound(first, bindings);
        SemanticType BoundSecond = GetBound(second, bindings);
        if (BoundFirst.Equals(BoundSecond))
        {
            return true;
        }
        if (BoundFirst is GenericParameterType FirstParameter)
        {
            return Bind(FirstParameter.Parameter, BoundSecond, bindings);
        }
        if (BoundSecond is GenericParameterType SecondParameter)
        {
            return Bind(SecondParameter.Parameter, BoundFirst, bindings);
        }
        if ((BoundFirst is not DeclaredType FirstType) || (BoundSecond is not DeclaredType SecondType)
            || !ReferenceEquals(FirstType.Declaration, SecondType.Declaration)
            || (FirstType.TypeArguments.Count != SecondType.TypeArguments.Count))
        {
            return false;
        }

        if ((FirstType.ContainingType != null) && (SecondType.ContainingType != null)
            && !CanUnify(FirstType.ContainingType, SecondType.ContainingType, bindings))
        {
            return false;
        }
        for (int Index = 0; Index < FirstType.TypeArguments.Count; Index++)
        {
            if (!CanUnify(FirstType.TypeArguments[Index], SecondType.TypeArguments[Index], bindings))
            {
                return false;
            }
        }
        return true;
    }

    /* A parameter cannot stand for a type holding it, as T cannot be T[]. */
    private bool Bind(GenericTypeParameter parameter,
        SemanticType type,
        Dictionary<GenericTypeParameter, SemanticType> bindings)
    {
        if (IsOccurring(parameter, type, bindings))
        {
            return false;
        }
        bindings[parameter] = type;
        return true;
    }

    private bool IsOccurring(GenericTypeParameter parameter,
        SemanticType type,
        Dictionary<GenericTypeParameter, SemanticType> bindings)
    {
        SemanticType Bound = GetBound(type, bindings);
        if (Bound is GenericParameterType ParameterType)
        {
            return ReferenceEquals(ParameterType.Parameter, parameter);
        }
        if (Bound is not DeclaredType Declared)
        {
            return false;
        }
        return ((Declared.ContainingType != null) && IsOccurring(parameter, Declared.ContainingType, bindings))
            || Declared.TypeArguments.Any(argument => IsOccurring(parameter, argument, bindings));
    }

    private SemanticType GetBound(SemanticType type, Dictionary<GenericTypeParameter, SemanticType> bindings)
    {
        SemanticType Current = type;
        while ((Current is GenericParameterType ParameterType)
            && bindings.TryGetValue(ParameterType.Parameter, out SemanticType? Bound))
        {
            Current = Bound;
        }
        return Current;
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
