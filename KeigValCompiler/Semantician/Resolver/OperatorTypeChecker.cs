using KeigValCompiler.Error;
using KeigValCompiler.Semantician.Library;
using KeigValCompiler.Semantician.Member;
using KeigValCompiler.Semantician.Types;

namespace KeigValCompiler.Semantician.Resolver;

/* Checks the types operators are declared with, against C#'s rules. An operator works on values of the type
 * declaring it, so a unary one takes that type, a binary one takes it on at least one side, and a shift
 * takes it as the value shifted. In an interface's abstract or virtual operator, a generic parameter
 * constrained to the interface counts as the type, which is how such operators name the type implementing
 * them, and a value type's nullable form counts as the type too. "++" and "--" return what they take, or a
 * class deriving from it, since their result replaces their operand. A conversion converts from or to the
 * type, and not between it and itself, an interface, or a type it derives from or which derives from it,
 * which the language converts already; as in C#, a generic parameter is taken to derive from nothing there.
 * "==" and "!=", "<" and ">", and "<=" and ">=" are declared in pairs, with the same parameter and return
 * types. An explicit implementation takes its types from its interface, and an operator declared with the
 * wrong number of parameters, reported by OperatorDeclarationChecker, is left alone, as is one whose types
 * did not resolve, and so is the partner either would need. */
internal class OperatorTypeChecker : IPackResolver
{
    // Static fields.
    private const char SPACE = ' ';


    // Private methods.
    private void CheckHolder(PackMember holder, PackResolutionContext context)
    {
        if (holder is not IOperatorOverloadHolder OverloadHolder)
        {
            return;
        }

        DeclaredType Containing = context.TypeReader.GetInstanceType(holder);
        List<DeclaredSignature> Comparisons = new();
        HashSet<OverloadableOperator> Skipped = new();
        TypeSubstitution NoSubstitution = new();
        foreach (OperatorOverload Overload in OverloadHolder.OperatorOverloads)
        {
            if (Overload.Function.ExplicitInterface != null)
            {
                continue;
            }

            DeclaredSignature Signature = context.SignatureReader.Read(Overload.Function, NoSubstitution);
            if (!Signature.IsComplete || !HasOperandCount(Signature))
            {
                Skipped.Add(Overload.OverloadedOperator);
                continue;
            }

            CheckOperandTypes(Signature, holder, Containing, context);
            if (GetPartner(Overload.OverloadedOperator) != null)
            {
                Comparisons.Add(Signature);
            }
        }

        foreach (DeclaredSignature Comparison in Comparisons.Where(
            comparison => !Skipped.Contains(GetPartner(comparison.Operator!.Value)!.Value)))
        {
            CheckPartner(Comparison, Comparisons, holder, context);
        }
    }

    private bool HasOperandCount(DeclaredSignature signature)
    {
        bool IsSingle = IsUnary(signature.Operator!.Value) || IsConversion(signature.Operator!.Value);
        return signature.ParameterTypes.Count == (IsSingle ? 1 : 2);
    }

    private void CheckOperandTypes(DeclaredSignature signature,
        PackMember holder,
        DeclaredType containing,
        PackResolutionContext context)
    {
        ErrorCreateOptions? Error = GetOperandError(signature, holder, containing, context);
        if (Error != null)
        {
            context.AddError(Error.Value, signature.Member);
        }
    }

    private ErrorCreateOptions? GetOperandError(DeclaredSignature signature,
        PackMember holder,
        DeclaredType containing,
        PackResolutionContext context)
    {
        OverloadableOperator Operator = signature.Operator!.Value;
        if (IsConversion(Operator))
        {
            return GetConversionError(signature, holder, containing, context);
        }

        string Name = MemberRelations.GetDisplayName(signature.Member);
        string HolderName = holder.SelfIdentifier.SourceCodeName;
        string ContainingName = containing.ToString();
        IReadOnlyList<SemanticType> Parameters = signature.ParameterTypes;
        bool IsSelfAllowed = IsSelfParameterAllowed(signature, holder);
        bool IsFirstContaining = IsContaining(Parameters[0], holder, containing, IsSelfAllowed, context);

        if (IsUnary(Operator))
        {
            if (!IsFirstContaining)
            {
                return context.ErrorCreator.UnaryOperatorParameterType.CreateOptions(Name, HolderName,
                    ContainingName);
            }

            bool IsIncrement = (Operator == OverloadableOperator.Increment)
                || (Operator == OverloadableOperator.Decrement);
            if (IsIncrement && (signature.Type != null)
                && !IsIncrementResult(signature.Type, Parameters[0], holder, containing, IsSelfAllowed, context))
            {
                return context.ErrorCreator.IncrementReturnType.CreateOptions(Name, HolderName,
                    Parameters[0].ToString());
            }
            return null;
        }

        if (IsShift(Operator))
        {
            return IsFirstContaining ? null
                : context.ErrorCreator.ShiftOperatorParameterType.CreateOptions(Name, HolderName, ContainingName);
        }
        return (IsFirstContaining || IsContaining(Parameters[1], holder, containing, IsSelfAllowed, context))
            ? null : context.ErrorCreator.BinaryOperatorParameterType.CreateOptions(Name, HolderName,
                ContainingName);
    }

    /* "++" and "--" replace their operand with their result, so it has to fit where the operand was: the
     * result is what the operator takes, as written, or a class deriving from it, and when it takes a generic
     * parameter, that parameter. An interface's abstract or virtual operator taking the interface may also
     * return a generic parameter constrained to it, as C# allows. */
    private bool IsIncrementResult(SemanticType result,
        SemanticType operand,
        PackMember holder,
        DeclaredType containing,
        bool isSelfAllowed,
        PackResolutionContext context)
    {
        if (operand is GenericParameterType)
        {
            return result.Equals(operand);
        }
        if (isSelfAllowed && operand.Equals(containing) && IsSelfParameter(result, holder, containing, context))
        {
            return true;
        }
        return context.Hierarchy.IsSameOrDerivedClass(result, operand);
    }

    /* Whether an operator may name the type implementing its interface with a generic parameter constrained
     * to the interface: an interface's operator which is abstract or virtual, as C# decides. */
    private bool IsSelfParameterAllowed(DeclaredSignature signature, PackMember holder)
    {
        return (holder is PackInterface)
            && signature.Member.HasAnyModifier(PackMemberModifiers.Abstract, PackMemberModifiers.Virtual);
    }

    private bool IsUnary(OverloadableOperator overloadedOperator)
    {
        return overloadedOperator is OverloadableOperator.UnaryPlus or OverloadableOperator.Negation
            or OverloadableOperator.LogicalNot or OverloadableOperator.BitwiseComplement
            or OverloadableOperator.Increment or OverloadableOperator.Decrement;
    }

    private bool IsShift(OverloadableOperator overloadedOperator)
    {
        return overloadedOperator is OverloadableOperator.LeftShift or OverloadableOperator.RightShift
            or OverloadableOperator.UnsignedRightShift;
    }

    private bool IsConversion(OverloadableOperator overloadedOperator)
    {
        return overloadedOperator is OverloadableOperator.ImplicitCast or OverloadableOperator.ExplicitCast;
    }

    /* The source and target are compared without their nullable forms, as C# compares them, though the type
     * declaring the conversion is never taken for a nullable form: Nullable<T> itself converts T to it. */
    private ErrorCreateOptions? GetConversionError(DeclaredSignature signature,
        PackMember holder,
        DeclaredType containing,
        PackResolutionContext context)
    {
        SemanticType Source = signature.ParameterTypes[0];
        SemanticType? Target = signature.Type;
        if (Target == null)
        {
            return null;
        }

        string Name = MemberRelations.GetDisplayName(signature.Member);
        string HolderName = holder.SelfIdentifier.SourceCodeName;
        bool IsSelfAllowed = IsSelfParameterAllowed(signature, holder);
        if (!IsContaining(Source, holder, containing, IsSelfAllowed, context)
            && !IsContaining(Target, holder, containing, IsSelfAllowed, context))
        {
            return context.ErrorCreator.ConversionNotContaining.CreateOptions(Name, HolderName,
                containing.ToString());
        }

        SemanticType PlainSource = Source.Equals(containing) ? Source : Unwrap(Source);
        SemanticType PlainTarget = Target.Equals(containing) ? Target : Unwrap(Target);
        if (PlainSource.Equals(PlainTarget))
        {
            return context.ErrorCreator.ConversionIdentity.CreateOptions(Name, HolderName,
                PlainSource.ToString());
        }

        SemanticType? Interface = new SemanticType[] { PlainSource, PlainTarget }.FirstOrDefault(
            type => (type is DeclaredType Declared) && (Declared.Declaration is PackInterface));
        if (Interface != null)
        {
            return context.ErrorCreator.ConversionInterface.CreateOptions(Name, HolderName, Interface.ToString());
        }
        if ((PlainSource is DeclaredType) && (PlainTarget is DeclaredType)
            && (context.Hierarchy.IsSameOrDerived(PlainSource, PlainTarget)
                || context.Hierarchy.IsSameOrDerived(PlainTarget, PlainSource)))
        {
            return context.ErrorCreator.ConversionBaseOrDerived.CreateOptions(Name, HolderName,
                PlainSource.ToString(), PlainTarget.ToString());
        }
        return null;
    }

    /* Whether a type is the one declaring an operator: that type itself, or where allowed, one of its own
     * generic parameters constrained to it, either of them also in its nullable form. */
    private bool IsContaining(SemanticType type,
        PackMember holder,
        DeclaredType containing,
        bool isSelfAllowed,
        PackResolutionContext context)
    {
        if (type.Equals(containing))
        {
            return true;
        }
        SemanticType Plain = Unwrap(type);
        return Plain.Equals(containing) || (isSelfAllowed && IsSelfParameter(Plain, holder, containing, context));
    }

    /* Whether a type is one of an interface's own generic parameters constrained to the interface. */
    private bool IsSelfParameter(SemanticType type,
        PackMember holder,
        DeclaredType containing,
        PackResolutionContext context)
    {
        return (type is GenericParameterType ParameterType)
            && ReferenceEquals(ParameterType.Parameter.Owner, holder)
            && context.Constraints.GetTypeConstraints(ParameterType.Parameter).Contains(containing);
    }

    private SemanticType Unwrap(SemanticType type)
    {
        return ((type is DeclaredType Declared) && Declared.IsNullable) ? Declared.TypeArguments[0] : type;
    }

    /* A comparison needs its partner declared with the same parameter and return types, however each
     * parameter is passed, as C# pairs them. */
    private void CheckPartner(DeclaredSignature comparison,
        List<DeclaredSignature> comparisons,
        PackMember holder,
        PackResolutionContext context)
    {
        OverloadableOperator Partner = GetPartner(comparison.Operator!.Value)!.Value;
        bool IsPaired = comparisons.Any(other => (other.Operator == Partner)
            && other.HasSameParameterTypes(comparison) && other.HasSameType(comparison));
        if (!IsPaired)
        {
            context.AddError(context.ErrorCreator.UnpairedOperator.CreateOptions(
                MemberRelations.GetDisplayName(comparison.Member), holder.SelfIdentifier.SourceCodeName,
                KGVL.KEYWORD_OPERATOR + SPACE + SignatureFormatter.GetOperatorSpelling(Partner)),
                comparison.Member);
        }
    }

    private OverloadableOperator? GetPartner(OverloadableOperator overloadedOperator)
    {
        return overloadedOperator switch
        {
            OverloadableOperator.Equals => OverloadableOperator.NotEquals,
            OverloadableOperator.NotEquals => OverloadableOperator.Equals,
            OverloadableOperator.LessThan => OverloadableOperator.LargerThan,
            OverloadableOperator.LargerThan => OverloadableOperator.LessThan,
            OverloadableOperator.LessThanOrEqual => OverloadableOperator.LargerOrEqual,
            OverloadableOperator.LargerOrEqual => OverloadableOperator.LessThanOrEqual,
            _ => null
        };
    }


    // Inherited methods.
    public void ResolvePack(PackResolutionContext context)
    {
        ArgumentNullException.ThrowIfNull(context, nameof(context));

        foreach (PackMember Type in context.Pack.Types)
        {
            CheckHolder(Type, context);
        }
    }
}
