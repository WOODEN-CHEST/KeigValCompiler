using KeigValCompiler.Error;
using KeigValCompiler.Semantician.Member;

namespace KeigValCompiler.Semantician.Resolver;

/* Checks the shape of every operator declaration against C#'s rules: it takes as many parameters as its
 * operator has operands, those are plain values, and it produces one. An interface's equality, inequality
 * and conversion operators are abstract or virtual. That an operator is public and static is a matter of
 * its modifiers, which ModifierChecker checks. Which types the parameters have, and which operators have
 * to be declared in pairs, are OperatorTypeChecker's to check. */
internal class OperatorDeclarationChecker : IPackResolver
{
    // Private methods.
    private void CheckOverload(OperatorOverload overload, PackMember holder, PackResolutionContext context)
    {
        PackFunction Function = overload.Function;
        string Name = MemberRelations.GetDisplayName(Function);
        string HolderName = holder.SelfIdentifier.SourceCodeName;

        if (IsAbstractOnlyInInterface(overload.OverloadedOperator) && (holder is PackInterface)
            && (Function.ExplicitInterface == null)
            && !Function.HasAnyModifier(PackMemberModifiers.Abstract, PackMemberModifiers.Virtual))
        {
            context.AddError(context.ErrorCreator.InterfaceOperatorNotAbstract.CreateOptions(Name, HolderName),
                Function);
        }

        ErrorDefinition? CountError = GetParameterCountError(overload.OverloadedOperator,
            Function.Parameters.Count, context);
        if (CountError != null)
        {
            context.AddError(CountError.CreateOptions(Name, HolderName, Function.Parameters.Count), Function);
        }

        foreach (FunctionParameter Parameter in Function.Parameters)
        {
            FunctionParameterModifier Modifier = Parameter.Modifiers & (FunctionParameterModifier.Ref
                | FunctionParameterModifier.Out | FunctionParameterModifier.Params);
            if (Modifier != FunctionParameterModifier.None)
            {
                context.AddError(context.ErrorCreator.OperatorParameterModifier.CreateOptions(
                    Parameter.SelfIdentifier.SourceCodeName, Name, HolderName, GetModifierKeyword(Modifier)),
                    Function);
            }
        }

        if (Function.ReturnType == null)
        {
            context.AddError(context.ErrorCreator.OperatorReturnsVoid.CreateOptions(Name, HolderName), Function);
        }
    }

    /* The operators an interface can only declare for the types implementing it to supply, since an
     * interface's own would compete with comparing and converting references. */
    private bool IsAbstractOnlyInInterface(OverloadableOperator overloadedOperator)
    {
        return overloadedOperator is OverloadableOperator.Equals or OverloadableOperator.NotEquals
            or OverloadableOperator.ImplicitCast or OverloadableOperator.ExplicitCast;
    }

    /* Null when the count is right. "+" and "-" are unary with one parameter and binary otherwise, as the
     * parser decides from their parameters, so a wrong count for them is neither. */
    private ErrorDefinition? GetParameterCountError(OverloadableOperator overloadedOperator,
        int parameterCount,
        PackResolutionContext context)
    {
        switch (overloadedOperator)
        {
            case OverloadableOperator.Addition or OverloadableOperator.Subtraction:
                return (parameterCount == 2) ? null : context.ErrorCreator.PlusMinusOperatorParameterCount;

            case OverloadableOperator.UnaryPlus or OverloadableOperator.Negation
                or OverloadableOperator.LogicalNot or OverloadableOperator.BitwiseComplement
                or OverloadableOperator.Increment or OverloadableOperator.Decrement:
                return (parameterCount == 1) ? null : context.ErrorCreator.UnaryOperatorParameterCount;

            case OverloadableOperator.ImplicitCast or OverloadableOperator.ExplicitCast:
                return (parameterCount == 1) ? null : context.ErrorCreator.ConversionParameterCount;

            default:
                return (parameterCount == 2) ? null : context.ErrorCreator.BinaryOperatorParameterCount;
        }
    }

    private string GetModifierKeyword(FunctionParameterModifier modifier)
    {
        return modifier switch
        {
            FunctionParameterModifier.Ref => KGVL.KEYWORD_REF,
            FunctionParameterModifier.Out => KGVL.KEYWORD_OUT,
            _ => KGVL.KEYWORD_PARAMS
        };
    }


    // Inherited methods.
    public void ResolvePack(PackResolutionContext context)
    {
        ArgumentNullException.ThrowIfNull(context, nameof(context));

        foreach (PackMember Type in context.Pack.Types)
        {
            if (Type is not IOperatorOverloadHolder Holder)
            {
                continue;
            }
            foreach (OperatorOverload Overload in Holder.OperatorOverloads)
            {
                CheckOverload(Overload, Type, context);
            }
        }
    }
}
