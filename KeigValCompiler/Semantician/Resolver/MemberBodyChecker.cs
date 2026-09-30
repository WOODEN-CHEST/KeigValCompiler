using KeigValCompiler.Semantician.Member;
using KeigValCompiler.Semantician.Types;

namespace KeigValCompiler.Semantician.Resolver;

/* Checks which functions have a body, against C#'s rules. A function needs one unless it is abstract,
 * builtin, or an interface's own instance member which is not private, virtual or sealed, which is
 * abstract without saying so. An abstract one cannot have one. A property's accessors without bodies make
 * it store its own value, which needs a getter, and is the only kind of property which can be given a
 * starting value; an indexer cannot store values. A builtin member's body is LibraryBindingResolver's to
 * check. Also checks what constructors can run first, that a static constructor, which nothing calls,
 * has no parameters, and that a record's positional parameters are not passed by "ref" or "out". It relies
 * on ModifierChecker having taken off the modifiers members cannot have. */
internal class MemberBodyChecker : IPackResolver
{
    // Private methods.
    private void CheckMember(PackMember member, PackResolutionContext context)
    {
        if (member is PackProperty InitializedProperty)
        {
            CheckInitializer(InitializedProperty, context);
        }

        /* A member whose "abstract" or "builtin" was taken off, as one it cannot have, was written to have no
         * body, and is not reported again for having none. */
        if (member.HasModifier(PackMemberModifiers.BuiltIn) || MemberRelations.IsAccessor(member)
            || ((member.RejectedModifiers & (PackMemberModifiers.Abstract | PackMemberModifiers.BuiltIn))
                != PackMemberModifiers.None))
        {
            return;
        }

        switch (member)
        {
            case PackProperty Property:
                CheckAccessors(Property, Property.SubMembers.Cast<PackFunction>(), true, context);
                CheckInitAccessor(Property, context);
                break;

            case PackIndexer Indexer:
                CheckAccessors(Indexer, Indexer.SubMembers.Cast<PackFunction>(), false, context);
                break;

            case PackConstructor Constructor:
                CheckConstructor(Constructor, context);
                break;

            case PackFunction Function:
                CheckFunctionBody(Function, MemberRelations.IsAbstract(Function), context);
                break;
        }
    }

    private void CheckFunctionBody(PackFunction function, bool isAbstract, PackResolutionContext context)
    {
        bool HasBody = function.Statements != null;
        if (isAbstract && HasBody && function.HasModifier(PackMemberModifiers.Abstract))
        {
            context.AddError(context.ErrorCreator.AbstractWithBody.CreateOptions(
                MemberRelations.GetKindName(function), MemberRelations.GetDisplayName(function)), function);
        }
        else if (!isAbstract && !HasBody)
        {
            context.AddError(context.ErrorCreator.MissingBody.CreateOptions(
                MemberRelations.GetKindName(function), MemberRelations.GetDisplayName(function)), function);
        }
    }

    /* An interface holds no data of its own, so only its static properties can store a value. */
    private bool CanStoreValue(PackMember property)
    {
        return (MemberRelations.GetHoldingMember(property) is not PackInterface)
            || property.HasModifier(PackMemberModifiers.Static);
    }

    /* A property or indexer is abstract when it says so, and an interface's is when none of its accessors
     * has a body. Otherwise a property whose accessors are without bodies stores its own value, as long as
     * it can be read back, and an indexer's accessors all need bodies. */
    private void CheckAccessors(PackMember member,
        IEnumerable<PackFunction> accessors,
        bool isProperty,
        PackResolutionContext context)
    {
        PackFunction[] Accessors = accessors.ToArray();
        if (Accessors.Length == 0)
        {
            context.AddError(context.ErrorCreator.MemberWithoutAccessors.CreateOptions(
                MemberRelations.GetKindName(member), MemberRelations.GetDisplayName(member)), member);
            return;
        }

        if (MemberRelations.IsAbstract(member))
        {
            foreach (PackFunction Accessor in Accessors.Where(accessor => accessor.Statements != null))
            {
                context.AddError(context.ErrorCreator.AbstractWithBody.CreateOptions(
                    MemberRelations.GetKindName(Accessor), MemberRelations.GetDisplayName(Accessor)), Accessor);
            }
            return;
        }

        PackFunction[] BodilessAccessors = Accessors.Where(accessor => accessor.Statements == null).ToArray();
        if (!isProperty || !CanStoreValue(member))
        {
            foreach (PackFunction Accessor in BodilessAccessors)
            {
                context.AddError(context.ErrorCreator.MissingAccessorBody.CreateOptions(
                    MemberRelations.GetDisplayName(Accessor)), Accessor);
            }
        }
        else if ((BodilessAccessors.Length > 0) && (((PackProperty)member).GetFunction == null))
        {
            context.AddError(context.ErrorCreator.AutoPropertyWithoutGetter.CreateOptions(
                member.SelfIdentifier.SourceCodeName), member);
        }
    }

    /* A starting value is what a property which stores its own value starts with, so any other property
     * cannot have one. */
    private void CheckInitializer(PackProperty property, PackResolutionContext context)
    {
        if (property.InitialValue == null)
        {
            return;
        }

        bool IsStoring = !property.HasAnyModifier(PackMemberModifiers.Abstract, PackMemberModifiers.BuiltIn)
            && CanStoreValue(property)
            && property.SubMembers.Any(accessor => ((PackFunction)accessor).Statements == null);
        if (!IsStoring)
        {
            context.AddError(context.ErrorCreator.InitializerWithoutStorage.CreateOptions(
                property.SelfIdentifier.SourceCodeName), property);
        }
    }

    /* "init" sets a property while an object is created, so what belongs to no object cannot have one. */
    private void CheckInitAccessor(PackProperty property, PackResolutionContext context)
    {
        bool IsStatic = property.HasModifier(PackMemberModifiers.Static)
            || (MemberRelations.GetHoldingMember(property) == null);
        if (IsStatic && (property.InitFunction != null))
        {
            context.AddError(context.ErrorCreator.InitAccessorOnStaticProperty.CreateOptions(
                property.SelfIdentifier.SourceCodeName), property.InitFunction);
        }
    }

    /* A record's primary constructor has no body of its own, and its parameters are values its properties
     * keep, so none is passed by "ref" or "out", as in C#. */
    private void CheckConstructor(PackConstructor constructor, PackResolutionContext context)
    {
        string TypeName = constructor.SelfIdentifier.SourceCodeName;
        if (!constructor.IsPrimary)
        {
            CheckFunctionBody(constructor, false, context);
        }
        else
        {
            foreach (FunctionParameter Parameter in constructor.Parameters)
            {
                string? Keyword = (Parameter.Modifiers & (FunctionParameterModifier.Ref
                    | FunctionParameterModifier.Out)) switch
                {
                    FunctionParameterModifier.Ref => KGVL.KEYWORD_REF,
                    FunctionParameterModifier.Out => KGVL.KEYWORD_OUT,
                    _ => null
                };
                if (Keyword != null)
                {
                    context.AddError(context.ErrorCreator.RecordParameterByReference.CreateOptions(
                        Parameter.SelfIdentifier.SourceCodeName, TypeName, Keyword), constructor);
                }
            }
        }

        if (MemberRelations.IsStaticConstructor(constructor))
        {
            if (constructor.ChainKind != ConstructorChainKind.None)
            {
                context.AddError(context.ErrorCreator.StaticConstructorChain.CreateOptions(TypeName),
                    constructor);
            }
            if (constructor.Parameters.Count > 0)
            {
                context.AddError(context.ErrorCreator.StaticConstructorParameters.CreateOptions(TypeName),
                    constructor);
            }
        }
        else if ((constructor.ChainKind == ConstructorChainKind.Base)
            && (MemberRelations.GetHoldingMember(constructor) is PackStruct))
        {
            context.AddError(context.ErrorCreator.StructConstructorBaseChain.CreateOptions(TypeName),
                constructor);
        }
        else if (IsUnchainedRecordConstructor(constructor, context))
        {
            context.AddError(context.ErrorCreator.RecordConstructorWithoutThis.CreateOptions(TypeName),
                constructor);
        }
    }

    /* As in C#, a record with a parameter list is created through its primary constructor, so each other
     * constructor it declares runs another of its own first, with ": this(...)", which in the end leads there.
     * Its copy constructor, taking the record itself by value, need not, nor a static constructor, checked
     * apart. One whose only parameter's type did not resolve, which has been reported, is not checked, since
     * it may have been meant as the copy constructor. */
    private bool IsUnchainedRecordConstructor(PackConstructor constructor, PackResolutionContext context)
    {
        if (constructor.IsPrimary || (constructor.ChainKind == ConstructorChainKind.This)
            || (MemberRelations.GetHoldingMember(constructor) is not PackClass Record)
            || !Record.HasModifier(PackMemberModifiers.Record)
            || !Record.Functions.OfType<PackConstructor>().Any(other => other.IsPrimary))
        {
            return false;
        }

        FunctionParameter? OnlyParameter = (constructor.Parameters.Count == 1)
            ? constructor.Parameters.Single() : null;
        if (OnlyParameter?.Type == null)
        {
            return true;
        }

        SemanticType? ParameterType = context.TypeReader.Read(OnlyParameter.Type);
        bool IsByValue = (OnlyParameter.Modifiers & (FunctionParameterModifier.In
            | FunctionParameterModifier.Out | FunctionParameterModifier.Ref)) == FunctionParameterModifier.None;
        bool IsCopyConstructor = IsByValue && context.TypeReader.GetInstanceType(Record).Equals(ParameterType);
        return (ParameterType != null) && !IsCopyConstructor;
    }


    // Inherited methods.
    public void ResolvePack(PackResolutionContext context)
    {
        ArgumentNullException.ThrowIfNull(context, nameof(context));

        foreach (PackMember Member in context.Pack.Members)
        {
            CheckMember(Member, context);
        }
    }
}
