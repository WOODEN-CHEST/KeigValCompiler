using KeigValCompiler.Semantician.Member;

namespace KeigValCompiler.Semantician.Resolver;

/* Checks that no declaration uses a static class as the type of a value, against C#'s rules: a static class
 * has no instances, so it is not the type of a field, a property, an indexer or an event, nor what a function
 * returns, nor a parameter's type, nor an array's elements, nor a type argument. As in C#, where these were
 * once allowed, an interface's members returning one or taking one are only warned about, and a delegate
 * may return one. Deriving from one and being constrained to one are InheritanceChecker's and
 * ConstraintChecker's to report. Types written in function bodies are not checked yet. */
internal class StaticClassUsageChecker : IPackResolver
{
    // Private methods.
    /* A record's property made for a positional parameter has the parameter's type, which is checked with
     * the parameter. */
    private void CheckMember(PackMember member, PackResolutionContext context)
    {
        if (IsMisplacedInStaticClass(member)
            || ((member is PackProperty RecordProperty) && RecordProperty.IsSynthesized))
        {
            return;
        }

        TypeTargetIdentifier? Type = member switch
        {
            PackField Field => Field.Type,
            PackProperty Property => Property.Type,
            PackIndexer Indexer => Indexer.Type,
            PackEvent Event => Event.Type,
            PackDelegate Delegate => Delegate.ReturnType,
            PackFunction Function => Function.ReturnType,
            _ => null
        };
        FunctionParameterCollection? Parameters = member switch
        {
            PackIndexer Indexer => Indexer.Parameters,
            PackDelegate Delegate => Delegate.Parameters,
            PackFunction Function => Function.Parameters,
            _ => null
        };
        bool IsInInterface = (MemberRelations.GetHoldingMember(member) is PackInterface)
            && (member is not PackField);

        string Kind = MemberRelations.GetKindName(member);
        string Name = MemberRelations.GetDisplayName(member);
        if ((Type != null) && IsStaticClass(Type) && (Type.ArrayRank == 0) && (member is not PackDelegate))
        {
            if (IsInInterface)
            {
                context.AddWarning(context.ErrorCreator.StaticClassReturnedInInterface.CreateOptions(Kind, Name,
                    Type.ToString()), member);
            }
            else
            {
                context.AddError((member is PackFunction)
                    ? context.ErrorCreator.StaticClassAsReturnType.CreateOptions(Kind, Name, Type.ToString())
                    : context.ErrorCreator.StaticClassAsType.CreateOptions(Kind, Name, Type.ToString()), member);
            }
        }
        foreach (FunctionParameter Parameter in Parameters ?? Enumerable.Empty<FunctionParameter>())
        {
            if ((Parameter.Type == null) || !IsStaticClass(Parameter.Type) || (Parameter.Type.ArrayRank > 0))
            {
                continue;
            }
            string ParameterName = Parameter.SelfIdentifier.SourceCodeName;
            string TypeName = Parameter.Type.ToString();
            if (IsInInterface)
            {
                context.AddWarning(context.ErrorCreator.StaticClassTakenInInterface.CreateOptions(ParameterName,
                    Kind, Name, TypeName), member);
            }
            else
            {
                context.AddError(context.ErrorCreator.StaticClassAsParameterType.CreateOptions(ParameterName,
                    Kind, Name, TypeName), member);
            }
        }

        foreach (TypeTargetIdentifier Written in MemberRelations.GetWrittenTypes(member))
        {
            CheckWrittenType(Written, member, context);
        }
    }

    /* An array of a static class anywhere in a type, and a static class given as a type argument. */
    private void CheckWrittenType(TypeTargetIdentifier written, PackMember member, PackResolutionContext context)
    {
        string Kind = MemberRelations.GetKindName(member);
        string Name = MemberRelations.GetDisplayName(member);
        if (IsStaticClass(written) && (written.ArrayRank > 0))
        {
            context.AddError(context.ErrorCreator.StaticClassAsArrayElement.CreateOptions(Kind, Name,
                written.MainTarget.SourceCodeName, written.ToString()), member);
        }

        foreach (TypeTargetIdentifier Argument in written.TypeArguments)
        {
            if (IsStaticClass(Argument) && (Argument.ArrayRank == 0))
            {
                context.AddError(context.ErrorCreator.StaticClassAsTypeArgument.CreateOptions(Kind, Name,
                    Argument.ToString(), written.ToString()), member);
            }
            CheckWrittenType(Argument, member, context);
        }
    }

    /* A member a static class cannot hold, which MemberPlacementChecker reports: an instance member, an
     * operator or an indexer. One of those, as an operator does, can only work on the class's instances,
     * and naming the class there follows from being in it. */
    private bool IsMisplacedInStaticClass(PackMember member)
    {
        PackMember? Holder = MemberRelations.GetHoldingMember(member);
        return (Holder != null) && MemberRelations.IsStaticClass(Holder) && !MemberRelations.IsType(member)
            && !MemberRelations.CanStaticClassHold(member);
    }

    private bool IsStaticClass(TypeTargetIdentifier type)
    {
        return (type.MainTarget.Target is PackMember Declaration) && MemberRelations.IsStaticClass(Declaration);
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
