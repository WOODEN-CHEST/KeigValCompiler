using KeigValCompiler.Error;
using KeigValCompiler.Semantician.Member;
using KeigValCompiler.Semantician.Types;

namespace KeigValCompiler.Semantician.Resolver;

/* Checks accessibility against C#'s rules. What a member's declaration names, its type, what it returns, its
 * parameters' types and its generic parameters' constraints, is at least as accessible as the member, since
 * whatever can use the member needs those too; so is a class's base class and an interface's base
 * interfaces, though not a class's interfaces. AccessDomains compares the two. A required member, which
 * everything creating its type has to set, is as accessible as its type, and so is its setter. A record's
 * property made for a positional parameter has the parameter's type, checked with the parameter. Whether a
 * declaration can use the types it names at all is decided as they are resolved, by SignatureResolver. */
internal class AccessibilityChecker : IPackResolver
{
    // Private methods.
    private void CheckMember(PackMember member, PackResolutionContext context)
    {
        if (MemberRelations.IsAccessor(member) || ((member is PackProperty Made) && Made.IsSynthesized))
        {
            return;
        }

        switch (member)
        {
            case PackField Field:
                CheckType(Field.Type, context.ErrorCreator.InconsistentMemberType, null, member, context);
                break;

            case PackProperty Property:
                CheckType(Property.Type, context.ErrorCreator.InconsistentMemberType, null, member, context);
                break;

            case PackIndexer Indexer:
                CheckType(Indexer.Type, context.ErrorCreator.InconsistentMemberType, null, member, context);
                break;

            case PackEvent Event:
                CheckType(Event.Type, context.ErrorCreator.InconsistentMemberType, null, member, context);
                break;

            case PackDelegate Delegate:
                CheckType(Delegate.ReturnType, context.ErrorCreator.InconsistentReturnType, null, member, context);
                break;

            case PackFunction Function:
                CheckType(Function.ReturnType, context.ErrorCreator.InconsistentReturnType, null, member, context);
                break;
        }
        CheckParameters(MemberRelations.GetParameters(member), member, context);

        if (member is IGenericParameterHolder GenericsHolder)
        {
            foreach (GenericTypeParameter Parameter in GenericsHolder.GenericParameters)
            {
                foreach (GenericConstraint Constraint in Parameter.Constraints.Where(
                    constraint => constraint.ConstrainedItemName != null))
                {
                    CheckType(Constraint.ConstrainedItemName, context.ErrorCreator.InconsistentConstraintType,
                        Parameter.SelfIdentifier.SourceCodeName, member, context);
                }
            }
        }
        CheckBases(member, context);
        CheckRequired(member, context);
    }

    private void CheckParameters(FunctionParameterCollection? parameters,
        PackMember member,
        PackResolutionContext context)
    {
        foreach (FunctionParameter Parameter in parameters ?? Enumerable.Empty<FunctionParameter>())
        {
            CheckType(Parameter.Type, context.ErrorCreator.InconsistentParameterType,
                Parameter.SelfIdentifier.SourceCodeName, member, context);
        }
    }

    /* A class's base class, and an interface's base interfaces, but not the interfaces a class or structure
     * implements, which C# lets be less accessible. */
    private void CheckBases(PackMember member, PackResolutionContext context)
    {
        if (member is not (PackClass or PackInterface))
        {
            return;
        }
        /* A base which cannot be derived from at all has been reported by InheritanceChecker. */
        foreach (TypeTargetIdentifier Base in ((IPackMemberExtender)member).ExtendedMembers.Where(
            written => !written.IsArrayOrNullable && ((member is PackInterface)
                ? (written.MainTarget.Target is PackInterface)
                : ((written.MainTarget.Target is PackClass BaseClass)
                    && !BaseClass.HasModifier(PackMemberModifiers.Sealed)
                    && !MemberRelations.IsStaticClass(BaseClass)))))
        {
            CheckType(Base, context.ErrorCreator.InconsistentBaseType, null, member, context);
        }
    }

    /* The named thing is a parameter's or generic parameter's name, where the message has one. A type which
     * did not resolve has been reported. */
    private void CheckType(TypeTargetIdentifier? written,
        ErrorDefinition error,
        string? named,
        PackMember member,
        PackResolutionContext context)
    {
        SemanticType? Type = (written == null) ? null : context.TypeReader.Read(written);
        if (Type == null)
        {
            return;
        }
        PackMember? LessAccessible = AccessDomains.FindLessAccessible(Type, member, context);
        if (LessAccessible == null)
        {
            return;
        }

        string Access = ModifierKeywords.FormatAccess(MemberRelations.GetEffectiveAccess(LessAccessible));
        context.AddError(error.CreateOptions(MemberRelations.GetKindName(member),
            MemberRelations.GetDisplayName(member), written!.ToString(),
            MemberRelations.GetDisplayName(LessAccessible), Access, named ?? string.Empty), member);
    }

    /* Everything which can create a type has to be able to set its required members. */
    private void CheckRequired(PackMember member, PackResolutionContext context)
    {
        if (!member.HasModifier(PackMemberModifiers.Required)
            || (MemberRelations.GetHoldingMember(member) is not PackMember Holder))
        {
            return;
        }

        PackFunction? Setter = (member as IPackAccessorHolder)?.SetFunction
            ?? (member as IPackAccessorHolder)?.InitFunction;
        bool IsVisible = AccessDomains.IsAtLeastAsAccessible(member, Holder, context)
            && ((Setter == null) || AccessDomains.IsAtLeastAsAccessible(Setter, Holder, context));
        if (!IsVisible)
        {
            context.AddError(context.ErrorCreator.RequiredLessAccessible.CreateOptions(
                MemberRelations.GetKindName(member), MemberRelations.GetDisplayName(member),
                MemberRelations.GetKindName(Holder), Holder.SelfIdentifier.SourceCodeName), member);
        }
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
