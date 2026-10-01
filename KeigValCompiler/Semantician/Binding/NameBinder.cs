using KeigValCompiler.Error;
using KeigValCompiler.Semantician.Bound;
using KeigValCompiler.Semantician.Member;
using KeigValCompiler.Semantician.Member.Code;
using KeigValCompiler.Semantician.Resolver;
using KeigValCompiler.Semantician.Types;

namespace KeigValCompiler.Semantician.Binding;

/* Binds the names in a body, as KGVL looks them up (decided 2026-10-01). A name on its own is looked for among
 * the locals and parameters, from the innermost block out; then among the function's generic parameters; then
 * in each type around the code, from the innermost out, among its generic parameters and the members it has,
 * inherited ones included, as C#'s member lookup finds them; then, at the code's namespace and each one holding
 * it, among the fields, properties, functions and events it holds, with its types and namespaces; then as a
 * namespace at the root; last among the members and types of the namespaces the file imports, where two imports
 * offering the name are ambiguous. Looking a name up reports nothing, and binding what it found reports what is
 * wrong with it.
 *
 * A name after a '.' is looked for in what stands before it, as C#'s member access does: among a namespace's
 * members, types and namespaces, a type's static members and types, or a value's instance members. As in C#, a
 * name standing for a local, a parameter, a field or a property whose type is the very type the name stands for
 * as a type stands for that type before a '.', when the member after it is a static member or a type of it
 * ("Color Color"); then nothing is reported about the other meaning.
 *
 * A function's name and an event's, an enum's constant, a member reached through a generic parameter, and a
 * chain holding a call are bound by later steps, and are BoundNotYetSupported until then. */
internal sealed class NameBinder
{
    // Private fields.
    private readonly BodyBinder _body;
    private PackResolutionContext Resolution => _body.Resolution;
    private ErrorRepository ErrorCreator => _body.Resolution.ErrorCreator;


    // Constructors.
    internal NameBinder(BodyBinder body)
    {
        _body = body ?? throw new ArgumentNullException(nameof(body));
    }


    // Internal methods.
    /* A name on its own, which may stand for a value, a type or a namespace. */
    internal BoundExpression BindSimpleName(IdentifiableAccessStatement access)
    {
        ArgumentNullException.ThrowIfNull(access, nameof(access));
        return BindMeaning(access, LookUpSimpleName(access));
    }

    /* A chain of member accesses, as "a.b.c", each name looked for in what the chain so far is. A chain with
     * a call in it is bound by a later step. */
    internal BoundExpression BindChain(CompositeAccessStatement chain)
    {
        ArgumentNullException.ThrowIfNull(chain, nameof(chain));

        List<Statement> Components = chain.Components.ToList();
        if (Components.Skip(1).Any(component => component is not IdentifiableAccessStatement)
            || (Components[0] is FunctionCallStatement))
        {
            return new BoundNotYetSupported(chain);
        }

        BoundExpression Current = Components[0] switch
        {
            IdentifiableAccessStatement First => BindChainStart(First, (IdentifiableAccessStatement)Components[1]),
            BaseStatement Base => _body.Expressions.BindBaseReceiver(Base),
            _ => _body.Expressions.BindExpression(Components[0])
        };
        foreach (IdentifiableAccessStatement Access in Components.Skip(1).Cast<IdentifiableAccessStatement>())
        {
            Current = BindMemberAccess(Current, Access);
        }
        return Current;
    }


    // Private methods.
    /* Looking a name on its own up, in the decided order, reporting nothing. */
    private SimpleNameMeaning LookUpSimpleName(IdentifiableAccessStatement access)
    {
        string Name = access.MemberIdentifier.SourceCodeName;
        int TypeArgumentCount = access.GenericArguments.Length;
        if (KGVL.TYPE_KEYWORDS.Contains(Name))
        {
            return SimpleNameMeaning.ForKeywordType(Resolution.Registry.GetTypeByKeyword(Name));
        }
        if (TypeArgumentCount == 0)
        {
            SimpleNameMeaning? Local = LookUpLocalOrParameter(Name);
            if (Local != null)
            {
                return Local;
            }
            GenericTypeParameter? FunctionParameter = _body.FindFunctionGenericParameter(Name);
            if (FunctionParameter != null)
            {
                return SimpleNameMeaning.ForGenericParameter(FunctionParameter);
            }
        }

        PackMember? Inaccessible = null;
        PackMember? OtherArity = null;
        int? OtherTypeArity = null;
        bool IsInnermost = true;
        foreach (PackMember Type in _body.GetTypesAround())
        {
            GenericTypeParameter? TypeParameter = (TypeArgumentCount == 0) ? FindGenericParameter(Type, Name)
                : null;
            if (TypeParameter != null)
            {
                return SimpleNameMeaning.ForGenericParameter(TypeParameter);
            }

            MemberLookupResult Result = _body.Context.Members.LookupInType(
                Resolution.TypeReader.GetInstanceType(Type), Name, TypeArgumentCount, _body.Member);
            if (Result.IsFound)
            {
                return SimpleNameMeaning.ForTypeMember(Result, IsInnermost);
            }
            Inaccessible ??= Result.InaccessibleMember;
            OtherArity ??= Result.OtherArityMember;
            IsInnermost = false;
        }

        foreach (string NameSpaceName in Resolution.TypeSearcher.GetNameSpaceNames(_body.Member))
        {
            SimpleNameMeaning? AtLevel = LookUpInNameSpace(Name, TypeArgumentCount, NameSpaceName,
                ref Inaccessible, ref OtherArity, ref OtherTypeArity);
            if (AtLevel != null)
            {
                return AtLevel;
            }
        }
        if ((TypeArgumentCount == 0) && Resolution.TypeSearcher.IsRootNameSpace(Name, _body.Member))
        {
            return SimpleNameMeaning.ForNameSpace(Name);
        }
        bool IsInheritanceUnknown = _body.GetTypesAround().Any(type => Resolution.Hierarchy.HasUnknownBases(
            Resolution.TypeReader.GetInstanceType(type)));
        return LookUpImported(Name, TypeArgumentCount, ref Inaccessible)
            ?? SimpleNameMeaning.ForNotFound(Inaccessible, OtherArity, OtherTypeArity,
                (TypeArgumentCount > 0) ? FindNonGenericKind(Name) : null, IsInheritanceUnknown);
    }

    /* What kind of thing taking no type arguments, other than a member or a type, a name stands for, so that, as
     * in C#, type arguments written for it are reported as given to that rather than the name being unknown: a
     * local or a parameter around, a generic parameter of the function or of a type around, or a namespace. Null
     * when it stands for none of them. */
    private string? FindNonGenericKind(string name)
    {
        SimpleNameMeaning? Local = LookUpLocalOrParameter(name);
        if (Local != null)
        {
            return (Local.Kind == SimpleNameMeaningKind.Local) ? KGVL.NAME_LOCAL : KGVL.NAME_PARAMETER;
        }
        if ((_body.FindFunctionGenericParameter(name) != null)
            || _body.GetTypesAround().Any(type => FindGenericParameter(type, name) != null))
        {
            return KGVL.NAME_GENERIC_PARAMETER;
        }
        bool IsNameSpace = Resolution.TypeSearcher.IsRootNameSpace(name, _body.Member)
            || Resolution.TypeSearcher.GetNameSpaceNames(_body.Member).Any(nameSpaceName =>
                Resolution.TypeSearcher.SearchNameSpace(nameSpaceName, name, 0, _body.Member).IsNameSpace);
        return IsNameSpace ? KGVL.NAME_NAMESPACE : null;
    }

    /* A local or a parameter of the name in the scopes around, innermost first. */
    private SimpleNameMeaning? LookUpLocalOrParameter(string name)
    {
        for (LocalScope? Scope = _body.Scope; Scope != null; Scope = Scope.Parent)
        {
            if ((Scope is BlockScope Block) && (Block.Find(name) is LocalSymbol Local))
            {
                return SimpleNameMeaning.ForLocal(Local, Block);
            }
            if ((Scope is ParameterScope Parameters)
                && Parameters.TryFind(name, out FunctionParameter? Parameter, out SemanticType? Type,
                    out bool IsUsable))
            {
                return SimpleNameMeaning.ForParameter(Parameter!, Type!, IsUsable);
            }
        }
        return null;
    }

    /* A name looked for at one namespace: a member it holds, then a namespace inside it or a type it holds, as
     * TypeSearcher decides between those. Null when none is found, noting a member or a type which cannot be used
     * here, and what has the name with another number of generic parameters, for a better error. */
    private SimpleNameMeaning? LookUpInNameSpace(string name,
        int typeArgumentCount,
        string nameSpaceName,
        ref PackMember? inaccessible,
        ref PackMember? otherArity,
        ref int? otherTypeArity)
    {
        PackNameSpace? NameSpace = Resolution.Pack.TryGetNamespace(nameSpaceName);
        if (NameSpace != null)
        {
            MemberLookupResult Members = _body.Context.Members.LookupInNameSpace(NameSpace, name,
                typeArgumentCount, _body.Member);
            if (Members.IsFound)
            {
                return SimpleNameMeaning.ForNameSpaceMember(Members);
            }
            inaccessible ??= Members.InaccessibleMember;
            otherArity ??= Members.OtherArityMember;
        }

        TypeSearchResult Result = Resolution.TypeSearcher.SearchNameSpace(nameSpaceName, name, typeArgumentCount,
            _body.Member);
        if (Result.IsNameSpace)
        {
            return SimpleNameMeaning.ForNameSpace(Result.NameSpaceName!);
        }
        if (Result.IsFound)
        {
            return SimpleNameMeaning.ForNameSpaceType((PackMember)Result.Target!);
        }
        inaccessible ??= Result.InaccessibleType;
        otherTypeArity ??= Result.OtherGenericParameterCount;
        return null;
    }

    /* A name looked for among what the namespaces the file imports hold. Two of them offering it is ambiguous. */
    private SimpleNameMeaning? LookUpImported(string name, int typeArgumentCount, ref PackMember? inaccessible)
    {
        List<(PackNameSpace NameSpace, MemberLookupResult Members, PackMember? Type)> Offers = new();
        foreach (PackNameSpace Import in _body.Member.SourceFile.NamespaceImports.Distinct(
            ReferenceEqualityComparer.Instance).Cast<PackNameSpace>())
        {
            MemberLookupResult Members = _body.Context.Members.LookupInNameSpace(Import, name, typeArgumentCount,
                _body.Member);
            PackMember? Type = Resolution.TypeSearcher.FindNameSpaceType(Import, name, typeArgumentCount,
                _body.Member);
            if (Members.IsFound || (Type != null))
            {
                Offers.Add((Import, Members, Type));
            }
            inaccessible ??= Members.InaccessibleMember;
        }

        if (Offers.Count == 0)
        {
            return null;
        }
        if (Offers.Count > 1)
        {
            return SimpleNameMeaning.ForImportAmbiguity(Offers.Select(offer =>
                offer.NameSpace.SelfIdentifier.SourceCodeName + KGVL.NAMESPACE_SEPARATOR + name).ToArray());
        }
        return Offers[0].Members.IsFound ? SimpleNameMeaning.ForNameSpaceMember(Offers[0].Members)
            : SimpleNameMeaning.ForNameSpaceType(Offers[0].Type!);
    }

    /* What a name on its own was found to stand for, bound, with what is wrong with it reported. */
    private BoundExpression BindMeaning(IdentifiableAccessStatement access, SimpleNameMeaning meaning)
    {
        switch (meaning.Kind)
        {
            case SimpleNameMeaningKind.KeywordType:
                return BindKeywordType(access, meaning.TypeDeclaration);

            case SimpleNameMeaningKind.Local:
                return BindLocal(access, meaning.Local!, meaning.LocalBlock!);

            case SimpleNameMeaningKind.Parameter when !meaning.IsParameterUsable:
                _body.AddError(ErrorCreator.PositionalParameterInStaticCode.CreateOptions(
                    access.MemberIdentifier.SourceCodeName), access);
                return new BoundBadExpression(access);

            case SimpleNameMeaningKind.Parameter:
                return new BoundParameter(access, meaning.Parameter!, meaning.ParameterType!);

            case SimpleNameMeaningKind.GenericParameter:
                return new BoundTypeExpression(access, new GenericParameterType(meaning.GenericParameter!, false));

            case SimpleNameMeaningKind.TypeMember:
                return BindFoundAround(access, meaning.Members!, meaning.IsInnermostType);

            case SimpleNameMeaningKind.NameSpaceMember:
                return BindFoundInNameSpace(access, meaning.Members!);

            case SimpleNameMeaningKind.NameSpaceType:
                return BindNameSpaceType(access, meaning.TypeDeclaration!);

            case SimpleNameMeaningKind.NameSpace:
                return new BoundNameSpaceExpression(access, meaning.NameSpaceName!);

            case SimpleNameMeaningKind.ImportAmbiguity:
                string Candidates = string.Join(", ", meaning.AmbiguousCandidates.Select(name => $"\"{name}\""));
                _body.AddError(ErrorCreator.AmbiguousType.CreateOptions(access.MemberIdentifier.SourceCodeName,
                    Candidates), access);
                return new BoundBadExpression(access);

            /* A name which may have been inherited from a base which could not be resolved has been reported with
             * that base. */
            case SimpleNameMeaningKind.NotFound when meaning.IsInheritanceUnknown:
                return new BoundBadExpression(access);

            /* As in C#, a local, a parameter or a generic parameter given type arguments is reported as that
             * before what types around have of the name, being nearer, and a namespace only when nothing else
             * is. */
            case SimpleNameMeaningKind.NotFound when (meaning.NonGenericKind != null)
                && (meaning.NonGenericKind != KGVL.NAME_NAMESPACE):
                _body.AddError(ErrorCreator.NotGeneric.CreateOptions(meaning.NonGenericKind,
                    access.MemberIdentifier.SourceCodeName), access);
                return new BoundBadExpression(access);

            default:
                string Name = access.MemberIdentifier.SourceCodeName;
                return ReportNotFound(access, meaning.InaccessibleMember, meaning.OtherArityMember,
                    meaning.OtherTypeArity, (meaning.NonGenericKind == KGVL.NAME_NAMESPACE)
                        ? ErrorCreator.NotGeneric.CreateOptions(KGVL.NAME_NAMESPACE, Name)
                        : ErrorCreator.NameNotFound.CreateOptions(Name));
        }
    }

    /* A local, which can be named only after binding reaches its declaration, as C# reports it, also saying when
     * it hides a field there, and a constant not in its own value. A constant whose value could not be had has
     * been reported, and naming it reports nothing more. */
    private BoundExpression BindLocal(IdentifiableAccessStatement access, LocalSymbol local, BlockScope block)
    {
        string Name = local.Name;
        if (local.IsConstant && _body.Statements.IsConstantBeingBound(local))
        {
            _body.AddError(ErrorCreator.CircularConstant.CreateOptions(Name), access);
            return new BoundBadExpression(access);
        }
        if (!block.IsDeclared(local))
        {
            PackField? Hidden = FindFieldInInnermostType(Name);
            _body.AddError((Hidden == null) ? ErrorCreator.LocalUsedBeforeDeclaration.CreateOptions(Name)
                : ErrorCreator.LocalUsedBeforeDeclarationHidingField.CreateOptions(Name,
                    MemberRelations.GetQualifiedDisplayName(Hidden)), access);
            return new BoundBadExpression(access);
        }
        if (local.IsConstant && (local.ConstantValue == null))
        {
            return new BoundBadExpression(access);
        }
        return new BoundLocal(access, local);
    }

    /* The first name of a chain, which, standing for a value of the very type it stands for as a type, stands
     * for that type instead when the member after it is a static member or a type of it, as C# decides it. */
    private BoundExpression BindChainStart(IdentifiableAccessStatement first, IdentifiableAccessStatement next)
    {
        SimpleNameMeaning Meaning = LookUpSimpleName(first);
        if ((first.GenericArguments.Length > 0) || (GetValueType(Meaning) is not DeclaredType ValueType)
            || (ValueType.Declaration.SelfIdentifier.SourceCodeName != first.MemberIdentifier.SourceCodeName)
            || !ValueType.Equals(ReadAsTypeName(first)))
        {
            return BindMeaning(first, Meaning);
        }

        /* A member neither meaning has is reported as one the value lacks, as Roslyn reports it, and nothing about
         * the value meaning is. */
        MemberLookupResult Result = _body.Context.Members.LookupInType(ValueType,
            next.MemberIdentifier.SourceCodeName, next.GenericArguments.Length, _body.Member);
        if (!Result.IsFound)
        {
            if (!Resolution.Hierarchy.HasUnknownBases(ValueType))
            {
                ReportNotFound(next, Result.InaccessibleMember, Result.OtherArityMember, null,
                    ErrorCreator.MemberNotFoundThroughValue.CreateOptions(ValueType.ToString(),
                        next.MemberIdentifier.SourceCodeName));
            }
            return new BoundBadExpression(first);
        }
        bool IsTypeMeant = (Result.EnumConstant != null) || ((Result.SingleMember is FoundMember Found)
            && (MemberRelations.IsType(Found.Member) || MemberRelations.IsStaticMember(Found.Member)));
        return IsTypeMeant ? new BoundTypeExpression(first, ValueType) : BindMeaning(first, Meaning);
    }

    /* The type of what a name stands for, when it stands for a local, a parameter, a field or a property, as
     * declared, though using it may be wrong where it is. As in Roslyn, a local not declared yet has none, so
     * that using it is reported. */
    private SemanticType? GetValueType(SimpleNameMeaning meaning)
    {
        switch (meaning.Kind)
        {
            case SimpleNameMeaningKind.Local when meaning.LocalBlock!.IsDeclared(meaning.Local!):
                return meaning.Local!.Type;

            case SimpleNameMeaningKind.Parameter:
                return meaning.ParameterType;

            case SimpleNameMeaningKind.TypeMember or SimpleNameMeaningKind.NameSpaceMember:
                FoundMember? Found = meaning.Members!.SingleMember;
                return Found?.Member switch
                {
                    PackField Field => ReadMemberType(Field.Type, Found.Holder),
                    PackProperty Property => ReadMemberType(Property.Type, Found.Holder),
                    _ => null
                };

            default:
                return null;
        }
    }

    /* What a name stands for as a type, where it is written, or null when it stands for none, reporting
     * nothing. */
    private SemanticType? ReadAsTypeName(IdentifiableAccessStatement name)
    {
        TypeTargetIdentifier AsType = new(name.MemberIdentifier.SourceCodeName);
        SemanticType Type = _body.HoldMessages(() => _body.ReadType(AsType, name), out _);
        return (Type is ErrorType) ? null : Type;
    }

    /* A name after a '.', looked for in what stands before it. Nothing is reported about a name after what has
     * errors, which have been. */
    private BoundExpression BindMemberAccess(BoundExpression receiver, IdentifiableAccessStatement access)
    {
        if (receiver.HasErrors)
        {
            return new BoundBadExpression(access, receiver);
        }

        return receiver switch
        {
            BoundNameSpaceExpression NameSpace => BindInQualifyingNameSpace(NameSpace, access),
            BoundTypeExpression Type => BindStaticMemberAccess(Type, access),
            _ => BindInstanceMemberAccess(receiver, access)
        };
    }

    /* What a namespace before a '.' holds of the name: a member, a type or a namespace. */
    private BoundExpression BindInQualifyingNameSpace(BoundNameSpaceExpression nameSpace,
        IdentifiableAccessStatement access)
    {
        PackMember? Inaccessible = null;
        PackMember? OtherArity = null;
        int? OtherTypeArity = null;
        string Name = access.MemberIdentifier.SourceCodeName;
        SimpleNameMeaning? Meaning = LookUpInNameSpace(Name, access.GenericArguments.Length, nameSpace.Name,
            ref Inaccessible, ref OtherArity, ref OtherTypeArity);
        if (Meaning != null)
        {
            return BindMeaning(access, Meaning);
        }
        return ReportNotFound(access, Inaccessible, OtherArity, OtherTypeArity,
            ErrorCreator.MemberNotFoundInNameSpace.CreateOptions(nameSpace.Name, Name));
    }

    /* A static member or a type of a type before a '.'. */
    private BoundExpression BindStaticMemberAccess(BoundTypeExpression type, IdentifiableAccessStatement access)
    {
        if (type.Type is GenericParameterType)
        {
            return new BoundNotYetSupported(access);
        }

        string Name = access.MemberIdentifier.SourceCodeName;
        MemberLookupResult Result = _body.Context.Members.LookupInType(type.Type!, Name,
            access.GenericArguments.Length, _body.Member);
        if (!Result.IsFound)
        {
            return IsInheritanceUnknown(type.Type!, null) ? new BoundBadExpression(access, type)
                : ReportNotFound(access, Result.InaccessibleMember, Result.OtherArityMember, null,
                    ErrorCreator.MemberNotFoundThroughType.CreateOptions(type.Type!.ToString(), Name));
        }
        if ((Result.EnumConstant != null) || Result.IsFunctionGroup || Result.IsAmbiguous)
        {
            return BindUnusualResult(access, Result);
        }

        FoundMember Found = Result.SingleMember!;
        if (MemberRelations.IsType(Found.Member))
        {
            return BindNestedType(access, Found);
        }
        if (Found.Member is PackEvent)
        {
            return new BoundNotYetSupported(access);
        }
        if (!MemberRelations.IsStaticMember(Found.Member))
        {
            _body.AddError(ErrorCreator.InstanceMemberThroughType.CreateOptions(MemberRelations.GetKindName(
                Found.Member), MemberRelations.GetQualifiedDisplayName(Found.Member)), access);
            return new BoundBadExpression(access, type);
        }
        return MakeMemberAccess(access, null, Found);
    }

    /* An instance member of a value before a '.', which is read. A static member, or a type, is reached through
     * its type instead, as C# reports. Through "base", the members are the base class's. */
    private BoundExpression BindInstanceMemberAccess(BoundExpression receiver, IdentifiableAccessStatement access)
    {
        BoundExpression Receiver = _body.Expressions.CheckValue(receiver);
        if (Receiver.HasErrors)
        {
            return new BoundBadExpression(access, Receiver);
        }
        if (Receiver.Type == null)
        {
            _body.AddError((Receiver is BoundDefault) ? ErrorCreator.DefaultWithoutType.CreateOptions()
                : ErrorCreator.MemberAccessOnNull.CreateOptions(), access);
            return new BoundBadExpression(access, Receiver);
        }

        string Name = access.MemberIdentifier.SourceCodeName;
        MemberLookupResult Result = _body.Context.Members.LookupInType(Receiver.Type, Name,
            access.GenericArguments.Length, _body.Member);
        if (!Result.IsFound && IsInheritanceUnknown(Receiver.Type, Receiver))
        {
            return new BoundBadExpression(access, Receiver);
        }
        if (!Result.IsFound)
        {
            ErrorCreateOptions NotFound = (Receiver is BoundBaseReference)
                ? ErrorCreator.MemberNotFoundThroughType.CreateOptions(Receiver.Type.ToString(), Name)
                : ErrorCreator.MemberNotFoundThroughValue.CreateOptions(Receiver.Type.ToString(), Name);
            return ReportNotFound(access, Result.InaccessibleMember, Result.OtherArityMember, null, NotFound);
        }
        if (Result.EnumConstant != null)
        {
            _body.AddError(ErrorCreator.StaticMemberThroughValue.CreateOptions(KGVL.NAME_ENUM_CONSTANT, Name,
                Result.EnumType!.ToString()), access);
            return new BoundBadExpression(access, Receiver);
        }
        if (Result.IsFunctionGroup || Result.IsAmbiguous)
        {
            return BindUnusualResult(access, Result);
        }

        FoundMember Found = Result.SingleMember!;
        if (MemberRelations.IsType(Found.Member))
        {
            _body.AddError(ErrorCreator.TypeThroughValue.CreateOptions(Name, Found.Holder!.ToString()), access);
            return new BoundBadExpression(access, Receiver);
        }
        if (Found.Member is PackEvent)
        {
            return new BoundNotYetSupported(access);
        }
        if (MemberRelations.IsStaticMember(Found.Member))
        {
            _body.AddError(ErrorCreator.StaticMemberThroughValue.CreateOptions(MemberRelations.GetKindName(
                Found.Member), Name, Found.Holder!.ToString()), access);
            return new BoundBadExpression(access, Receiver);
        }
        return MakeMemberAccess(access, Receiver, Found);
    }

    /* A member found in a type around the code, by a name on its own. An instance member is reached through the
     * object the code runs on, which has to be the innermost type's, and there has to be one, as C# requires. */
    private BoundExpression BindFoundAround(IdentifiableAccessStatement access, MemberLookupResult result,
        bool isInnermost)
    {
        if ((result.EnumConstant != null) || result.IsFunctionGroup || result.IsAmbiguous)
        {
            return BindUnusualResult(access, result);
        }

        FoundMember Found = result.SingleMember!;
        if (MemberRelations.IsType(Found.Member))
        {
            return BindNestedType(access, Found);
        }
        if (Found.Member is PackEvent)
        {
            return new BoundNotYetSupported(access);
        }
        if (MemberRelations.IsStaticMember(Found.Member))
        {
            return MakeMemberAccess(access, null, Found);
        }

        /* As in C#, a starting value is reported as one, static or not. */
        if (isInnermost && _body.IsInitializer)
        {
            _body.AddError(ErrorCreator.InstanceMemberInInitializer.CreateOptions(MemberRelations.GetKindName(
                _body.Member), MemberRelations.GetDisplayName(_body.Member), MemberRelations.GetKindName(
                Found.Member), MemberRelations.GetQualifiedDisplayName(Found.Member)), access);
            return new BoundBadExpression(access);
        }
        if (!isInnermost || _body.IsStatic)
        {
            _body.AddError(ErrorCreator.InstanceMemberWithoutObject.CreateOptions(MemberRelations.GetKindName(
                Found.Member), MemberRelations.GetQualifiedDisplayName(Found.Member)), access);
            return new BoundBadExpression(access);
        }
        return MakeMemberAccess(access, new BoundThisReference(access, _body.ContainingType!, true), Found);
    }

    /* What a lookup found which is not one field, property, event or type: an enum's constant or a group of
     * functions, bound by later steps, or an ambiguity, which is reported. */
    private BoundExpression BindUnusualResult(IdentifiableAccessStatement access, MemberLookupResult result)
    {
        if (!result.IsAmbiguous)
        {
            return new BoundNotYetSupported(access);
        }
        _body.AddError(ErrorCreator.AmbiguousMember.CreateOptions(access.MemberIdentifier.SourceCodeName,
            MemberRelations.GetQualifiedDisplayName(result.Members[0].Member),
            MemberRelations.GetQualifiedDisplayName(result.Members[1].Member)), access);
        return new BoundBadExpression(access);
    }

    /* A field or a property reached through a receiver, or through none for a static one. A constant field has its
     * value, worked out when first needed; one whose value could not be had has been reported where it is. */
    private BoundExpression MakeMemberAccess(Statement syntax, BoundExpression? receiver, FoundMember found)
    {
        switch (found.Member)
        {
            case PackField Field:
                ConstantValue? Constant = null;
                if (Field.HasModifier(PackMemberModifiers.Const))
                {
                    Constant = _body.Context.GetConstantValue(Field);
                    if (Constant == null)
                    {
                        return new BoundBadExpression(syntax);
                    }
                }
                return new BoundFieldAccess(syntax, receiver, Field, found.Holder,
                    ReadMemberType(Field.Type, found.Holder), Constant);

            case PackProperty Property:
                return new BoundPropertyAccess(syntax, receiver, Property, found.Holder, null,
                    ReadMemberType(Property.Type, found.Holder));

            default:
                throw new InvalidOperationException($"\"{found.Member}\" is neither a field nor a property.");
        }
    }

    /* A member's type, as the type holding it is seen where it is named. */
    private SemanticType ReadMemberType(TypeTargetIdentifier type, DeclaredType? holder)
    {
        SemanticType? Type = Resolution.TypeReader.Read(type);
        if (Type == null)
        {
            return ErrorType.Instance;
        }
        return (holder == null) ? Type : Type.Substitute(TypeSubstitution.Of(holder));
    }

    /* What a namespace holds belongs to no object, so it is reached as a static member is. */
    private BoundExpression BindFoundInNameSpace(IdentifiableAccessStatement access, MemberLookupResult result)
    {
        if (result.IsFunctionGroup || result.IsAmbiguous)
        {
            return BindUnusualResult(access, result);
        }
        FoundMember Found = result.SingleMember!;
        return (Found.Member is PackEvent) ? new BoundNotYetSupported(access)
            : MakeMemberAccess(access, null, Found);
    }

    /* A type a namespace holds, with the type arguments written for it. */
    private BoundExpression BindNameSpaceType(IdentifiableAccessStatement access, PackMember declaration)
    {
        return MakeTypeExpression(access, declaration, null);
    }

    /* A type a type holds, as the type holding it is seen where it is named, with the type arguments written for
     * it. */
    private BoundExpression BindNestedType(IdentifiableAccessStatement access, FoundMember found)
    {
        return MakeTypeExpression(access, found.Member, found.Holder);
    }

    /* A type with the type arguments written for it, each resolved where it is written and checked as C# checks
     * them: none a static class or using one, and each satisfying the constraints of its parameter. A bad
     * expression when one could not be resolved or is wrong, which has been reported. */
    private BoundExpression MakeTypeExpression(IdentifiableAccessStatement access,
        PackMember declaration,
        DeclaredType? holder)
    {
        SemanticType[] TypeArguments = access.GenericArguments.Select(argument => _body.ReadType(argument, access))
            .ToArray();
        if (TypeArguments.Any(argument => argument is ErrorType))
        {
            return new BoundBadExpression(access);
        }

        DeclaredType Type = new(declaration, TypeArguments, holder,
            Resolution.Registry.GetLibraryType(declaration), false);
        string Kind = MemberRelations.GetKindName(_body.Member);
        string Name = MemberRelations.GetDisplayName(_body.Member);
        bool IsWrong = false;
        foreach (TypeTargetIdentifier Argument in access.GenericArguments)
        {
            if (StaticClassUsageChecker.IsStaticClass(Argument) && (Argument.ArrayRank == 0))
            {
                _body.AddError(ErrorCreator.StaticClassAsTypeArgument.CreateOptions(Kind, Name,
                    Argument.ToString(), Type.ToString()), access);
                IsWrong = true;
            }
            IsWrong = _body.CheckWrittenType(Argument, Kind, Name, access) || IsWrong;
        }
        if (!IsWrong && (declaration is IGenericParameterHolder GenericsHolder) && (TypeArguments.Length > 0))
        {
            ConstraintChecker.CheckGivenArguments(GenericsHolder, holder, TypeArguments, Type.ToString(),
                Resolution, error =>
                {
                    _body.AddError(error, access);
                    IsWrong = true;
                });
        }
        return IsWrong ? new BoundBadExpression(access) : new BoundTypeExpression(access, Type);
    }

    /* A type keyword, as in "int.MaxValue", which takes no type arguments. One the library does not declare
     * has been reported. */
    private BoundExpression BindKeywordType(IdentifiableAccessStatement access, PackMember? declaration)
    {
        if (access.GenericArguments.Length > 0)
        {
            _body.AddError(ErrorCreator.WrongTypeArgumentCount.CreateOptions(
                access.MemberIdentifier.SourceCodeName, access.GenericArguments.Length, 0), access);
            return new BoundBadExpression(access);
        }
        return (declaration == null) ? new BoundBadExpression(access)
            : new BoundTypeExpression(access, Resolution.TypeReader.GetInstanceType(declaration));
    }

    /* The field a local hides, which as in C# is one the innermost type around the code has of the local's name,
     * whether it can be used here or not. */
    private PackField? FindFieldInInnermostType(string name)
    {
        PackMember? Innermost = _body.GetTypesAround().FirstOrDefault();
        if (Innermost == null)
        {
            return null;
        }

        MemberLookupResult Result = _body.Context.Members.LookupInType(
            Resolution.TypeReader.GetInstanceType(Innermost), name, 0, _body.Member);
        return (Result.SingleMember?.Member ?? Result.InaccessibleMember) as PackField;
    }

    private GenericTypeParameter? FindGenericParameter(PackMember type, string name)
    {
        return (type as IGenericParameterHolder)?.GenericParameters.FirstOrDefault(
            parameter => parameter.SelfIdentifier.SourceCodeName == name);
    }

    /* Whether a member not found in a type may still be one it inherits, from a base which could not be resolved
     * or through a loop of bases, either of which has been reported, so that nothing more is. What "base" reaches
     * is what the type around inherits. */
    private bool IsInheritanceUnknown(SemanticType type, BoundExpression? receiver)
    {
        DeclaredType? Searched = (receiver is BoundBaseReference) ? _body.ContainingType : type as DeclaredType;
        return (Searched != null) && Resolution.Hierarchy.HasUnknownBases(Searched);
    }

    /* A name which found nothing which can be used here: a member or a type it found which cannot be is reported
     * as that; then a type of the name with another number of generic parameters, and any other member written
     * with type arguments it does not take; then the error given. */
    private BoundExpression ReportNotFound(IdentifiableAccessStatement access,
        PackMember? inaccessible,
        PackMember? otherArity,
        int? otherTypeArity,
        ErrorCreateOptions notFound)
    {
        string Name = access.MemberIdentifier.SourceCodeName;
        int TypeArgumentCount = access.GenericArguments.Length;
        if (inaccessible != null)
        {
            _body.AddError(ErrorCreator.MemberInaccessible.CreateOptions(MemberRelations.GetKindName(inaccessible),
                MemberRelations.GetQualifiedDisplayName(inaccessible), ModifierKeywords.FormatAccess(
                    MemberRelations.GetEffectiveAccess(inaccessible))), access);
        }
        else if ((otherArity != null) && MemberRelations.IsType(otherArity))
        {
            _body.AddError(ErrorCreator.WrongTypeArgumentCount.CreateOptions(Name, TypeArgumentCount,
                InheritedMembers.GetArity(otherArity)), access);
        }
        else if (otherArity != null)
        {
            _body.AddError(ErrorCreator.NotGeneric.CreateOptions(MemberRelations.GetKindName(otherArity),
                MemberRelations.GetQualifiedDisplayName(otherArity)), access);
        }
        else if (otherTypeArity != null)
        {
            _body.AddError(ErrorCreator.WrongTypeArgumentCount.CreateOptions(Name, TypeArgumentCount,
                otherTypeArity.Value), access);
        }
        else
        {
            _body.AddError(notFound, access);
        }
        return new BoundBadExpression(access);
    }
}
