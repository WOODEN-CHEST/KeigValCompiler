using KeigValCompiler.Semantician.Member;

namespace KeigValCompiler.Semantician.Resolver;

/* Checks that each member is one what holds it can have, where the parser accepted it because the
 * syntax is the same everywhere: a namespace has no objects for indexers or constructors to work on, and
 * implements no interface, an interface holds no data and is never created, a static class holds only
 * static members, and a readonly structure only what cannot change. Also checks that an event's type is a
 * delegate. It relies on ModifierChecker having taken off the modifiers members cannot have. */
internal class MemberPlacementChecker : IPackResolver
{
    // Private methods.
    private void CheckMember(PackMember member, PackResolutionContext context)
    {
        if (MemberRelations.IsAccessor(member) || MemberRelations.IsType(member))
        {
            return;
        }

        switch (MemberRelations.GetHoldingMember(member))
        {
            case null:
                CheckNamespaceMember(member, context);
                break;

            case PackInterface Interface:
                CheckInterfaceMember(member, Interface, context);
                break;

            case PackClass Class when MemberRelations.IsStaticClass(Class):
                CheckStaticClassMember(member, Class, context);
                break;

            case PackStruct Struct when Struct.HasModifier(PackMemberModifiers.Readonly):
                CheckReadonlyStructMember(member, Struct, context);
                break;
        }

        if (member is PackEvent Event)
        {
            CheckEventType(Event, context);
        }
    }

    private void CheckNamespaceMember(PackMember member, PackResolutionContext context)
    {
        string NameSpaceName = member.NameSpace.SelfIdentifier.SourceCodeName;
        TypeTargetIdentifier? ExplicitInterface = MemberRelations.GetExplicitInterface(member);
        if ((member is PackIndexer) || (member is PackConstructor))
        {
            context.AddError(context.ErrorCreator.CannotHoldMemberInNamespace.CreateOptions(NameSpaceName,
                MemberRelations.GetKindName(member)), member);
        }
        else if (ExplicitInterface != null)
        {
            context.AddError(context.ErrorCreator.NamespaceExplicitImplementation.CreateOptions(NameSpaceName,
                MemberRelations.GetKindName(member), MemberRelations.GetDisplayName(member),
                ExplicitInterface.ToString()), member);
        }
    }

    private void CheckInterfaceMember(PackMember member, PackInterface holder, PackResolutionContext context)
    {
        bool IsStatic = IsWrittenStatic(member);
        if ((member is PackField) && !IsStatic)
        {
            context.AddError(context.ErrorCreator.InterfaceInstanceField.CreateOptions(
                holder.SelfIdentifier.SourceCodeName, member.SelfIdentifier.SourceCodeName), member);
        }
        else if ((member is PackConstructor) && !member.HasModifier(PackMemberModifiers.Static))
        {
            context.AddError(context.ErrorCreator.InterfaceInstanceConstructor.CreateOptions(
                holder.SelfIdentifier.SourceCodeName), member);
        }
    }

    /* A constant is static already, and an operator, although static, works on values of its type, of
     * which a static class has none. */
    private void CheckStaticClassMember(PackMember member, PackClass holder, PackResolutionContext context)
    {
        bool IsStatic = IsWrittenStatic(member);
        bool IsOperator = MemberRelations.GetOperatorOverload(member) != null;
        if (IsStatic && !IsOperator && (member is not PackIndexer))
        {
            return;
        }

        context.AddError(context.ErrorCreator.StaticClassInstanceMember.CreateOptions(
            holder.SelfIdentifier.SourceCodeName, MemberRelations.GetKindName(member),
            MemberRelations.GetDisplayName(member)), member);
    }

    /* What a readonly structure stores cannot change after it is created: every instance field is
     * readonly, a property's "set" accessor, which would set the value it stores if it has no body, has
     * one, and it holds no instance event, which subscribing to changes. */
    private void CheckReadonlyStructMember(PackMember member, PackStruct holder, PackResolutionContext context)
    {
        if (IsWrittenStatic(member))
        {
            return;
        }

        bool IsMutable = member switch
        {
            PackField => !member.HasModifier(PackMemberModifiers.Readonly),
            PackProperty Property => (Property.SetFunction != null) && (Property.SetFunction.Statements == null)
                && !Property.HasModifier(PackMemberModifiers.BuiltIn),
            PackEvent => true,
            _ => false
        };
        if (IsMutable)
        {
            context.AddError(context.ErrorCreator.ReadonlyStructMutableMember.CreateOptions(
                holder.SelfIdentifier.SourceCodeName, MemberRelations.GetKindName(member),
                member.SelfIdentifier.SourceCodeName), member);
        }
    }

    /* Whether a member is written static, as a constant is without saying so. One whose "static" or "const"
     * was taken off, as one it cannot have, still counts, since it was not meant to be an instance member,
     * and the modifier has been reported already. */
    private bool IsWrittenStatic(PackMember member)
    {
        return ((member.Modifiers | member.RejectedModifiers)
            & (PackMemberModifiers.Static | PackMemberModifiers.Const)) != PackMemberModifiers.None;
    }

    /* An event whose type did not resolve has been reported already. */
    private void CheckEventType(PackEvent packEvent, PackResolutionContext context)
    {
        IIdentifiable? Target = packEvent.Type.MainTarget.Target;
        if ((Target == null) || ((Target is PackDelegate) && !packEvent.Type.IsArray))
        {
            return;
        }

        context.AddError(context.ErrorCreator.EventTypeNotDelegate.CreateOptions(
            packEvent.SelfIdentifier.SourceCodeName, packEvent.Type.ToString()), packEvent);
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
