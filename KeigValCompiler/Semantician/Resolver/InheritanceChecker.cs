using KeigValCompiler.Error;
using KeigValCompiler.Semantician.Library;
using KeigValCompiler.Semantician.Member;
using KeigValCompiler.Semantician.Types;

namespace KeigValCompiler.Semantician.Resolver;

/* Checks what each type derives from, against C#'s rules. A class derives from at most one class, which
 * is written first, is neither sealed nor static, and is a record exactly when the class is; the rest of
 * what it derives from are interfaces. A static class derives from nothing but object. A structure or an
 * interface derives only from interfaces. No type depends on itself, as C# defines depending: a class on
 * its base class, an interface on its base interfaces, and either on the type it is declared in. A base
 * type which did not resolve has been reported already, and is skipped. No interface is listed twice. It
 * relies on ModifierChecker having taken off the modifiers types cannot have. */
internal class InheritanceChecker : IPackResolver
{
    // Private methods.
    /* A base written as an array or with '?', as in "Foo[]", is no class or interface itself, and is left
     * out of every other check once reported, which would otherwise take it for the type it is made of. */
    private void CheckBaseMarkers(PackMember type, PackResolutionContext context)
    {
        foreach (TypeTargetIdentifier Base in ((IPackMemberExtender)type).ExtendedMembers)
        {
            if ((Base.MainTarget.Target != null) && Base.IsArrayOrNullable)
            {
                context.AddError(context.ErrorCreator.BaseTypeWithMarkers.CreateOptions(
                    MemberRelations.GetKindName(type), type.SelfIdentifier.SourceCodeName, Base.ToString()),
                    type);
            }
        }
    }

    /* Two listings of one interface are found by comparing types, so "IFoo<int>" and "IFoo<int?>" are
     * different but "IFoo<string>" and "IFoo<string?>" are not. Two which differ only in their '?'
     * annotations, and so in how they are written, are only warned about, as in C#, since they differ in what
     * checking nullability would make of them. */
    private void CheckDuplicateInterfaces(PackMember type, PackResolutionContext context)
    {
        Dictionary<SemanticType, SemanticType> Listed = new();
        foreach (TypeTargetIdentifier Base in GetPlainBases(type))
        {
            SemanticType? BaseType = context.TypeReader.Read(Base);
            if ((BaseType is not DeclaredType Declared) || (Declared.Declaration is not PackInterface))
            {
                continue;
            }
            if (!Listed.TryGetValue(BaseType, out SemanticType? Earlier))
            {
                Listed.Add(BaseType, BaseType);
                continue;
            }

            string Kind = MemberRelations.GetKindName(type);
            string Name = type.SelfIdentifier.SourceCodeName;
            if (Earlier.ToString() == BaseType.ToString())
            {
                context.AddError(context.ErrorCreator.DuplicateBaseInterface.CreateOptions(Kind, Name,
                    Base.ToString()), type);
            }
            else
            {
                context.AddWarning(context.ErrorCreator.DuplicateInterfaceAnnotations.CreateOptions(Kind, Name,
                    BaseType.ToString(), Earlier.ToString()), type);
            }
        }
    }

    private IEnumerable<TypeTargetIdentifier> GetPlainBases(PackMember type)
    {
        return ((IPackMemberExtender)type).ExtendedMembers.Where(written => !written.IsArrayOrNullable);
    }

    private void CheckInterfaceOnlyBases(PackMember type, PackResolutionContext context)
    {
        foreach (TypeTargetIdentifier Base in GetPlainBases(type))
        {
            IIdentifiable? Target = Base.MainTarget.Target;
            if ((Target != null) && (Target is not PackInterface))
            {
                context.AddError(context.ErrorCreator.BaseTypeNotInterface.CreateOptions(
                    MemberRelations.GetKindName(type), type.SelfIdentifier.SourceCodeName, Base.ToString()),
                    type);
            }
        }
    }

    private void CheckClassBases(PackClass type, PackMember? objectType, PackResolutionContext context)
    {
        string Name = type.SelfIdentifier.SourceCodeName;
        if (type.HasModifier(PackMemberModifiers.Static))
        {
            foreach (TypeTargetIdentifier Base in GetPlainBases(type).Where(written =>
                (written.MainTarget.Target != null) && !ReferenceEquals(written.MainTarget.Target, objectType)))
            {
                context.AddError(context.ErrorCreator.StaticClassWithBase.CreateOptions(Name, Base.ToString()),
                    type);
            }
            return;
        }

        /* Only a base written after one known to be an interface is out of place. One which did not resolve
         * could have been meant as the base class itself, and so could one which is no class: as in Roslyn,
         * either takes the base class's place, which TypeHierarchy leaves empty, and a class after it is not
         * taken for the base class, nor reported as a second one, since that follows from the first. */
        TypeTargetIdentifier? FirstClassBase = null;
        bool IsAfterInterface = false;
        bool IsPlaceTakenByError = false;
        foreach (TypeTargetIdentifier Base in GetPlainBases(type))
        {
            switch (Base.MainTarget.Target)
            {
                case null:
                    IsPlaceTakenByError |= FirstClassBase == null;
                    break;

                case PackInterface:
                    IsAfterInterface = true;
                    break;

                case PackClass when FirstClassBase != null:
                    context.AddError(context.ErrorCreator.MultipleBaseClasses.CreateOptions(Name,
                        FirstClassBase.ToString(), Base.ToString()), type);
                    break;

                case PackClass when IsPlaceTakenByError:
                    break;

                case PackClass BaseClass:
                    if (IsAfterInterface)
                    {
                        context.AddError(context.ErrorCreator.BaseClassNotFirst.CreateOptions(Name,
                            Base.ToString()), type);
                    }
                    FirstClassBase = Base;
                    CheckBaseClass(type, BaseClass, Base, objectType, context);
                    break;

                default:
                    context.AddError(context.ErrorCreator.BaseTypeNotClassOrInterface.CreateOptions(Name,
                        Base.ToString()), type);
                    IsPlaceTakenByError |= FirstClassBase == null;
                    break;
            }
        }
    }

    /* A record passes its primary constructor's base arguments to the first entry of its base list, which
     * the parser has made sure of, so they are misplaced when that entry is an interface, as in C#. */
    private void CheckRecordBaseArguments(PackClass type, PackResolutionContext context)
    {
        PackConstructor? Primary = type.Functions.OfType<PackConstructor>()
            .FirstOrDefault(constructor => constructor.IsPrimary);
        if ((Primary == null) || (Primary.ChainKind != ConstructorChainKind.Base))
        {
            return;
        }

        TypeTargetIdentifier FirstBase = type.ExtendedMembers.First();
        if (FirstBase.MainTarget.Target is PackInterface)
        {
            context.AddError(context.ErrorCreator.BaseArgumentsToInterface.CreateOptions(
                type.SelfIdentifier.SourceCodeName, FirstBase.ToString()), type);
        }
    }

    private void CheckBaseClass(PackClass type,
        PackClass baseClass,
        TypeTargetIdentifier writtenBase,
        PackMember? objectType,
        PackResolutionContext context)
    {
        string Name = type.SelfIdentifier.SourceCodeName;
        if (baseClass.HasModifier(PackMemberModifiers.Sealed))
        {
            context.AddError(context.ErrorCreator.SealedBaseClass.CreateOptions(Name, writtenBase.ToString()),
                type);
        }
        else if (baseClass.HasModifier(PackMemberModifiers.Static))
        {
            context.AddError(context.ErrorCreator.StaticBaseClass.CreateOptions(Name, writtenBase.ToString()),
                type);
        }

        bool IsRecord = type.HasModifier(PackMemberModifiers.Record);
        bool IsBaseRecord = baseClass.HasModifier(PackMemberModifiers.Record);
        if (IsRecord && !IsBaseRecord && !ReferenceEquals(baseClass, objectType))
        {
            context.AddError(context.ErrorCreator.RecordBaseNotRecord.CreateOptions(Name, writtenBase.ToString()),
                type);
        }
        else if (!IsRecord && IsBaseRecord)
        {
            context.AddError(context.ErrorCreator.NonRecordBaseRecord.CreateOptions(Name, writtenBase.ToString()),
                type);
        }
    }

    /* Searches, breadth first, what a class's or an interface's own bases depend on for a way back to it, so
     * that the chain reported is the shortest one. So each type whose base list is part of a loop reports it,
     * and a type which is only in it by being declared inside another is left out, as Roslyn leaves it. */
    private void CheckCycle(PackMember type, PackResolutionContext context)
    {
        Dictionary<PackMember, PackMember> ReachedFrom = new(ReferenceEqualityComparer.Instance);
        Queue<PackMember> ToSearch = new();
        foreach (PackMember Base in GetBaseDependencies(type))
        {
            if (ReferenceEquals(Base, type))
            {
                ReportCycle(type, new() { type, type }, context);
                return;
            }
            if (ReachedFrom.TryAdd(Base, type))
            {
                ToSearch.Enqueue(Base);
            }
        }

        while (ToSearch.Count > 0)
        {
            PackMember Current = ToSearch.Dequeue();
            foreach (PackMember Next in GetDependencies(Current))
            {
                if (ReferenceEquals(Next, type))
                {
                    ReportCycle(type, BuildChain(type, Current, ReachedFrom), context);
                    return;
                }
                if (ReachedFrom.TryAdd(Next, Current))
                {
                    ToSearch.Enqueue(Next);
                }
            }
        }
    }

    /* What a type depends on through its base list: a class its base class, which is the first of its bases
     * which is not an interface, and an interface its base interfaces. The interfaces a class or a structure
     * implements are no part of it, and nor are a sealed or static base class, nor a static class's base,
     * which are reported and which Roslyn then drops. */
    private IEnumerable<PackMember> GetBaseDependencies(PackMember type)
    {
        if (type is PackInterface)
        {
            return GetPlainBases(type).Select(written => written.MainTarget.Target).OfType<PackInterface>();
        }
        if ((type is not PackClass) || MemberRelations.IsStaticClass(type))
        {
            return Enumerable.Empty<PackMember>();
        }

        IIdentifiable? Place = GetPlainBases(type).Select(written => written.MainTarget.Target)
            .FirstOrDefault(target => target is not PackInterface);
        bool IsKept = (Place is PackClass BaseClass) && !BaseClass.HasModifier(PackMemberModifiers.Sealed)
            && !MemberRelations.IsStaticClass(BaseClass);
        return IsKept ? new PackMember[] { (PackClass)Place! } : Enumerable.Empty<PackMember>();
    }

    /* Everything a type depends on: its bases, and the type it is declared in. */
    private IEnumerable<PackMember> GetDependencies(PackMember type)
    {
        PackMember? Holder = MemberRelations.GetHoldingMember(type);
        return ((Holder != null) && MemberRelations.IsType(Holder))
            ? GetBaseDependencies(type).Append(Holder) : GetBaseDependencies(type);
    }

    /* The chain from a type to the last interface found before coming back to it, and back to the type. */
    private List<PackMember> BuildChain(PackMember start,
        PackMember last,
        Dictionary<PackMember, PackMember> reachedFrom)
    {
        List<PackMember> Chain = new() { start };
        for (PackMember Current = last; !ReferenceEquals(Current, start); Current = reachedFrom[Current])
        {
            Chain.Insert(1, Current);
        }
        Chain.Add(start);
        return Chain;
    }

    /* A loop through a type declared inside another is told apart, since the type holding it is not one of
     * its bases, and nested types are named with the types around them to show it. */
    private void ReportCycle(PackMember type, List<PackMember> chain, PackResolutionContext context)
    {
        bool IsThroughNesting = false;
        for (int Index = 1; Index < chain.Count; Index++)
        {
            IsThroughNesting |= !GetBaseDependencies(chain[Index - 1]).Contains(chain[Index],
                ReferenceEqualityComparer.Instance);
        }

        string TypeName = GetNestedName(type);
        string ChainText = MemberRelations.FormatChain(chain.Select(GetNestedName));
        ErrorDefinition Error = IsThroughNesting ? context.ErrorCreator.CircularBaseThroughNesting
            : context.ErrorCreator.CircularBase;
        context.AddError(Error.CreateOptions(TypeName, ChainText), type);
    }

    /* A type's name after the names of the types it is declared in, as "Outer.Inner". */
    private string GetNestedName(PackMember type)
    {
        PackMember? Holder = MemberRelations.GetHoldingMember(type);
        return ((Holder != null) && MemberRelations.IsType(Holder))
            ? GetNestedName(Holder) + KGVL.NAMESPACE_SEPARATOR + type.SelfIdentifier.SourceCodeName
            : type.SelfIdentifier.SourceCodeName;
    }


    // Inherited methods.
    public void ResolvePack(PackResolutionContext context)
    {
        ArgumentNullException.ThrowIfNull(context, nameof(context));

        PackMember? ObjectType = context.Registry.GetDeclaredType(LibraryTypes.Object);
        foreach (PackMember Type in context.Pack.Types)
        {
            if (Type is IPackMemberExtender)
            {
                CheckBaseMarkers(Type, context);
                CheckDuplicateInterfaces(Type, context);
            }
            switch (Type)
            {
                case PackClass Class:
                    CheckClassBases(Class, ObjectType, context);
                    CheckRecordBaseArguments(Class, context);
                    CheckCycle(Class, context);
                    break;

                case PackInterface Interface:
                    CheckInterfaceOnlyBases(Interface, context);
                    CheckCycle(Interface, context);
                    break;

                case PackStruct:
                    CheckInterfaceOnlyBases(Type, context);
                    break;
            }
        }
    }
}
