using KeigValCompiler.Semantician.Library;
using KeigValCompiler.Semantician.Member;

namespace KeigValCompiler.Semantician.Resolver;

/* Checks what each type derives from, against C#'s rules. A class derives from at most one class, which
 * is written first, is neither sealed nor static, and is a record exactly when the class is; the rest of
 * what it derives from are interfaces. A static class derives from nothing but object. A structure or an
 * interface derives only from interfaces. No type is among its own base types. A base type which did not
 * resolve has been reported already, and is skipped. It relies on ModifierChecker having taken off the
 * modifiers types cannot have. Whether an interface is listed twice needs types
 * compared, and is not checked here. */
internal class InheritanceChecker : IPackResolver
{
    // Static fields.
    /* A chain of types, each deriving from the next, is written as base lists would be: "A : B : A". */
    private static readonly string _chainSeparator = $" {KGVL.COLON} ";


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
         * could have been meant as the base class itself. */
        TypeTargetIdentifier? FirstClassBase = null;
        bool IsAfterInterface = false;
        foreach (TypeTargetIdentifier Base in GetPlainBases(type))
        {
            switch (Base.MainTarget.Target)
            {
                case null:
                    break;

                case PackInterface:
                    IsAfterInterface = true;
                    break;

                case PackClass when FirstClassBase != null:
                    context.AddError(context.ErrorCreator.MultipleBaseClasses.CreateOptions(Name,
                        FirstClassBase.ToString(), Base.ToString()), type);
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
                    break;
            }
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

    /* Follows each class's first base class until the chain either ends or comes back to where it
     * started. A chain running into a loop which does not pass through its start is left to the classes
     * in that loop to report. */
    private void CheckClassCycle(PackClass type, PackResolutionContext context)
    {
        List<PackMember> Chain = new() { type };
        HashSet<PackMember> Visited = new(ReferenceEqualityComparer.Instance) { type };
        for (PackClass? Base = GetFirstBaseClass(type); Base != null; Base = GetFirstBaseClass(Base))
        {
            Chain.Add(Base);
            if (ReferenceEquals(Base, type))
            {
                ReportCycle(type, Chain, context);
                return;
            }
            if (!Visited.Add(Base))
            {
                return;
            }
        }
    }

    private PackClass? GetFirstBaseClass(PackClass type)
    {
        return GetPlainBases(type).Select(written => written.MainTarget.Target).OfType<PackClass>()
            .FirstOrDefault();
    }

    /* Searches the interfaces an interface derives from, breadth first, for a way back to it, so that the
     * chain reported is the shortest one. */
    private void CheckInterfaceCycle(PackInterface type, PackResolutionContext context)
    {
        Dictionary<PackMember, PackMember> ReachedFrom = new(ReferenceEqualityComparer.Instance);
        Queue<PackInterface> ToSearch = new();
        ToSearch.Enqueue(type);

        while (ToSearch.Count > 0)
        {
            PackInterface Current = ToSearch.Dequeue();
            foreach (PackInterface Base in GetBaseInterfaces(Current))
            {
                if (ReferenceEquals(Base, type))
                {
                    ReportCycle(type, BuildChain(type, Current, ReachedFrom), context);
                    return;
                }
                if (ReachedFrom.TryAdd(Base, Current))
                {
                    ToSearch.Enqueue(Base);
                }
            }
        }
    }

    private IEnumerable<PackInterface> GetBaseInterfaces(PackInterface type)
    {
        return GetPlainBases(type).Select(written => written.MainTarget.Target).OfType<PackInterface>();
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

    private void ReportCycle(PackMember type, List<PackMember> chain, PackResolutionContext context)
    {
        string ChainText = string.Join(_chainSeparator,
            chain.Select(member => member.SelfIdentifier.SourceCodeName));
        context.AddError(context.ErrorCreator.CircularBase.CreateOptions(type.SelfIdentifier.SourceCodeName,
            ChainText), type);
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
            }
            switch (Type)
            {
                case PackClass Class:
                    CheckClassBases(Class, ObjectType, context);
                    CheckClassCycle(Class, context);
                    break;

                case PackInterface Interface:
                    CheckInterfaceOnlyBases(Interface, context);
                    CheckInterfaceCycle(Interface, context);
                    break;

                case PackStruct:
                    CheckInterfaceOnlyBases(Type, context);
                    break;
            }
        }
    }
}
