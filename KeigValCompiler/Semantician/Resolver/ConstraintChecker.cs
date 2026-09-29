using KeigValCompiler.Semantician.Member;
using KeigValCompiler.Semantician.Types;

namespace KeigValCompiler.Semantician.Resolver;

/* Checks generic constraints against C#'s rules. Each generic parameter's constraints are valid: at most one
 * of "class", "struct" and "notnull", written first; at most one class, which is neither sealed, static nor
 * object, written before the interfaces and not with "class" or "struct"; otherwise interfaces and other
 * generic parameters, none constrained to "struct", none twice, and no parameter constrained to itself. The
 * classes a parameter gets through the parameters it is constrained to are ones of which one derives from
 * all the others. A function which overrides another or implements one explicitly takes its constraints
 * from that one, and can only say "class" or "struct", which have to agree with them; what it restates is
 * reported once, and not checked further. And every type argument written in a declaration satisfies the
 * constraints of the parameter it stands for: "class" wants a reference type, "struct" a value type which is
 * not nullable, and a type the type itself, or one deriving from it. Whether "notnull" is satisfied is a
 * matter of nullability, which is not checked yet. Types written in function bodies are not checked yet
 * either. */
internal class ConstraintChecker : IPackResolver
{
    // Static fields.
    private static readonly string _chainSeparator = $" {KGVL.COLON} ";


    // Private methods.
    private void CheckMember(PackMember member, PackResolutionContext context)
    {
        bool IsRestating = (member is PackFunction Function)
            && (Function.HasModifier(PackMemberModifiers.Override) || (Function.ExplicitInterface != null));
        IEnumerable<TypeTargetIdentifier> Written = MemberRelations.GetWrittenTypes(member);
        if (member is IGenericParameterHolder GenericsHolder)
        {
            foreach (GenericTypeParameter Parameter in GenericsHolder.GenericParameters)
            {
                bool IsListValid = CheckConstraintList(Parameter, member, IsRestating, context);
                if (IsRestating)
                {
                    CheckRestatedConstraints(Parameter, member, context);
                }
                else if (IsListValid)
                {
                    CheckConstraintClasses(Parameter, member, context);
                }
            }

            /* What a function restating constraints writes is reported, and not checked any further. */
            if (IsRestating)
            {
                HashSet<TypeTargetIdentifier> Restated = new(GenericsHolder.GenericParameters
                    .SelectMany(parameter => parameter.Constraints)
                    .Where(constraint => constraint.ConstrainedItemName != null)
                    .Select(constraint => constraint.ConstrainedItemName!), ReferenceEqualityComparer.Instance);
                Written = Written.Where(type => !Restated.Contains(type));
            }
            else
            {
                CheckCircularConstraints(member, GenericsHolder, context);
            }
        }

        foreach (TypeTargetIdentifier Type in Written)
        {
            CheckTypeArguments(Type, member, context);
        }
    }

    /* Whether the list had nothing to report. */
    private bool CheckConstraintList(GenericTypeParameter parameter,
        PackMember holder,
        bool isRestating,
        PackResolutionContext context)
    {
        string Name = parameter.SelfIdentifier.SourceCodeName;
        HashSet<SemanticType> Seen = new();
        SpecialGenericConstraint Special = SpecialGenericConstraint.None;
        bool IsTypeSeen = false;
        bool IsValid = true;
        int Position = 0;

        foreach (GenericConstraint Constraint in parameter.Constraints)
        {
            Position++;
            if (Constraint.ConstrainedItemName == null)
            {
                string Keyword = GetKeyword(Constraint.SpecialConstraint);
                if (isRestating && (Constraint.SpecialConstraint == SpecialGenericConstraint.NotNull))
                {
                    context.AddError(context.ErrorCreator.OverrideRestatesConstraint.CreateOptions(Name,
                        MemberRelations.GetDisplayName(holder), Keyword), holder);
                    IsValid = false;
                }
                else if (Position > 1)
                {
                    context.AddError(context.ErrorCreator.SpecialConstraintNotFirst.CreateOptions(Keyword, Name),
                        holder);
                    IsValid = false;
                }
                else
                {
                    Special = Constraint.SpecialConstraint;
                }
                continue;
            }

            if (isRestating)
            {
                context.AddError(context.ErrorCreator.OverrideRestatesConstraint.CreateOptions(Name,
                    MemberRelations.GetDisplayName(holder), Constraint.ConstrainedItemName.ToString()), holder);
                IsValid = false;
                continue;
            }

            SemanticType? Type = context.TypeReader.Read(Constraint.ConstrainedItemName);
            if (Type == null)
            {
                IsValid = false;
                continue;
            }
            if (!Seen.Add(Type))
            {
                context.AddError(context.ErrorCreator.DuplicateConstraint.CreateOptions(Name, Type.ToString()),
                    holder);
                IsValid = false;
                continue;
            }
            IsValid &= CheckConstraintType(Type, Name, Special, IsTypeSeen, holder, context);
            IsTypeSeen = true;
        }
        return IsValid;
    }

    /* Whether the type can constrain the parameter, which is reported when it cannot. */
    private bool CheckConstraintType(SemanticType type,
        string parameterName,
        SpecialGenericConstraint special,
        bool isTypeSeen,
        PackMember holder,
        PackResolutionContext context)
    {
        if ((type is DeclaredType Interface) && (Interface.Declaration is PackInterface))
        {
            return true;
        }
        if (type is GenericParameterType)
        {
            if (!context.Hierarchy.IsValueType(type))
            {
                return true;
            }
            context.AddError(context.ErrorCreator.StructParameterAsConstraint.CreateOptions(parameterName,
                type.ToString()), holder);
            return false;
        }

        bool IsClass = (type is DeclaredType Class) && (Class.Declaration is PackClass)
            && !Class.Declaration.HasModifier(PackMemberModifiers.Sealed)
            && !MemberRelations.IsStaticClass(Class.Declaration)
            && !type.Equals(context.Hierarchy.GetObjectType());
        if (!IsClass)
        {
            context.AddError(context.ErrorCreator.InvalidConstraintType.CreateOptions(type.ToString(),
                parameterName), holder);
        }
        else if (isTypeSeen)
        {
            context.AddError(context.ErrorCreator.ClassConstraintNotFirst.CreateOptions(type.ToString(),
                parameterName), holder);
        }
        else if ((special == SpecialGenericConstraint.Class) || (special == SpecialGenericConstraint.Struct))
        {
            context.AddError(context.ErrorCreator.ClassConstraintWithSpecial.CreateOptions(parameterName,
                type.ToString(), GetKeyword(special)), holder);
        }
        else
        {
            return true;
        }
        return false;
    }

    /* The classes a parameter gets through the parameters it is constrained to, and the one it names
     * itself, leave some type able to satisfy them all: of each two, one derives from the other. Two it
     * names itself have been reported by the checks of its list. */
    private void CheckConstraintClasses(GenericTypeParameter parameter,
        PackMember holder,
        PackResolutionContext context)
    {
        IReadOnlyList<DeclaredType> Classes = context.Hierarchy.GetConstraintClasses(parameter);
        for (int First = 0; First < Classes.Count; First++)
        {
            for (int Second = First + 1; Second < Classes.Count; Second++)
            {
                if (!context.Hierarchy.IsSameOrDerivedClass(Classes[First], Classes[Second])
                    && !context.Hierarchy.IsSameOrDerivedClass(Classes[Second], Classes[First]))
                {
                    context.AddError(context.ErrorCreator.ConflictingClassConstraints.CreateOptions(
                        parameter.SelfIdentifier.SourceCodeName, Classes[First].ToString(),
                        Classes[Second].ToString()), holder);
                    return;
                }
            }
        }
    }

    /* A function restating constraints can say "class" or "struct" of a parameter, to tell what a '?' on it
     * means, only when the constraints it takes from the other function's parameter say so too. One which
     * overrides or implements nothing, reported by OverrideChecker or InterfaceImplementationChecker, takes
     * no constraints, and is left alone. */
    private void CheckRestatedConstraints(GenericTypeParameter parameter,
        PackMember holder,
        PackResolutionContext context)
    {
        GenericTypeParameter? Source = context.Constraints.GetSource(parameter);
        if ((Source == null) || (Source.Owner == null))
        {
            return;
        }

        GenericParameterType Type = new(parameter, false);
        string Name = parameter.SelfIdentifier.SourceCodeName;
        string HolderName = MemberRelations.GetDisplayName(holder);
        string SourceName = MemberRelations.GetDisplayName(Source.Owner);
        string SourceHolderName = MemberRelations.GetHolderDisplayName(Source.Owner);
        foreach (GenericConstraint Constraint in parameter.Constraints)
        {
            if ((Constraint.SpecialConstraint == SpecialGenericConstraint.Class)
                && !context.Hierarchy.IsReferenceType(Type))
            {
                context.AddError(context.ErrorCreator.RestatedClassConstraint.CreateOptions(Name, HolderName,
                    SourceName, SourceHolderName), holder);
            }
            else if ((Constraint.SpecialConstraint == SpecialGenericConstraint.Struct)
                && !context.Hierarchy.IsValueType(Type))
            {
                context.AddError(context.ErrorCreator.RestatedStructConstraint.CreateOptions(Name, HolderName,
                    SourceName, SourceHolderName), holder);
            }
        }
    }

    /* Follows each generic parameter's constraints to other generic parameters of the same declaration, and
     * reports the parameters which come round to themselves. */
    private void CheckCircularConstraints(PackMember holder,
        IGenericParameterHolder genericsHolder,
        PackResolutionContext context)
    {
        foreach (GenericTypeParameter Parameter in genericsHolder.GenericParameters)
        {
            List<GenericTypeParameter> Chain = new() { Parameter };
            if (IsConstrainedTo(Parameter, Parameter, holder, Chain, context))
            {
                context.AddError(context.ErrorCreator.CircularConstraint.CreateOptions(
                    Parameter.SelfIdentifier.SourceCodeName, string.Join(_chainSeparator,
                        Chain.Select(link => link.SelfIdentifier.SourceCodeName))), holder);
            }
        }
    }

    /* Whether a parameter's constraints lead to the target. The chain holds the parameters followed, and
     * gets the target at its end when the answer is yes. */
    private bool IsConstrainedTo(GenericTypeParameter parameter,
        GenericTypeParameter target,
        PackMember holder,
        List<GenericTypeParameter> chain,
        PackResolutionContext context)
    {
        foreach (GenericConstraint Constraint in parameter.Constraints.Where(
            constraint => constraint.ConstrainedItemName != null))
        {
            if ((context.TypeReader.Read(Constraint.ConstrainedItemName!) is not GenericParameterType Next)
                || !ReferenceEquals(Next.Parameter.Owner, holder))
            {
                continue;
            }
            if (ReferenceEquals(Next.Parameter, target))
            {
                chain.Add(target);
                return true;
            }
            if (chain.Contains(Next.Parameter, ReferenceEqualityComparer.Instance))
            {
                continue;
            }

            chain.Add(Next.Parameter);
            if (IsConstrainedTo(Next.Parameter, target, holder, chain, context))
            {
                return true;
            }
            chain.RemoveAt(chain.Count - 1);
        }
        return false;
    }

    /* The type arguments written inside other type arguments are checked first, then the ones given to the
     * type itself. A type which did not resolve, or which is given the wrong number of arguments, has been
     * reported. */
    private void CheckTypeArguments(TypeTargetIdentifier written,
        PackMember member,
        PackResolutionContext context)
    {
        foreach (TypeTargetIdentifier Argument in written.TypeArguments)
        {
            CheckTypeArguments(Argument, member, context);
        }

        if ((written.MainTarget.Target is not PackMember Declaration)
            || (Declaration is not IGenericParameterHolder GenericsHolder)
            || (written.TypeArguments.Length == 0)
            || (written.TypeArguments.Length != GenericsHolder.GenericParameters.Count))
        {
            return;
        }

        List<SemanticType> Arguments = new();
        foreach (TypeTargetIdentifier Argument in written.TypeArguments)
        {
            SemanticType? ArgumentType = context.TypeReader.Read(Argument);
            if (ArgumentType == null)
            {
                return;
            }
            Arguments.Add(ArgumentType);
        }

        (GenericTypeParameter Parameter, SemanticType Argument)[] Given = GenericsHolder.GenericParameters
            .Zip(Arguments).ToArray();
        TypeSubstitution Substitution = new();
        foreach ((GenericTypeParameter Parameter, SemanticType Argument) in Given)
        {
            Substitution.Add(Parameter, Argument);
        }
        foreach ((GenericTypeParameter Parameter, SemanticType Argument) in Given)
        {
            string? Unsatisfied = FindUnsatisfiedConstraint(Argument, Parameter, Substitution, context);
            if (Unsatisfied != null)
            {
                context.AddError(context.ErrorCreator.ConstraintNotSatisfied.CreateOptions(Argument.ToString(),
                    Parameter.SelfIdentifier.SourceCodeName, written.ToString(), Unsatisfied), member);
            }
        }
    }

    /* The first constraint of a parameter the argument does not satisfy, as written in messages, or null. The
     * types the constraints name are given in terms of the arguments, as "IComparable<T>" is
     * "IComparable<int>" when int stands for T. */
    private string? FindUnsatisfiedConstraint(SemanticType argument,
        GenericTypeParameter parameter,
        TypeSubstitution substitution,
        PackResolutionContext context)
    {
        foreach (GenericConstraint Constraint in parameter.Constraints)
        {
            if (Constraint.ConstrainedItemName == null)
            {
                bool IsSatisfied = Constraint.SpecialConstraint switch
                {
                    SpecialGenericConstraint.Class => context.Hierarchy.IsReferenceType(argument),
                    SpecialGenericConstraint.Struct => context.Hierarchy.IsValueType(argument)
                        && !((argument is DeclaredType Declared) && Declared.IsNullable),
                    _ => true
                };
                if (!IsSatisfied)
                {
                    return GetKeyword(Constraint.SpecialConstraint);
                }
                continue;
            }

            SemanticType? Type = context.TypeReader.Read(Constraint.ConstrainedItemName)
                ?.Substitute(substitution);
            if ((Type != null) && !Type.Equals(context.Hierarchy.GetObjectType())
                && !context.Hierarchy.IsSameOrDerived(argument, Type))
            {
                return Type.ToString();
            }
        }
        return null;
    }

    private string GetKeyword(SpecialGenericConstraint constraint)
    {
        return constraint switch
        {
            SpecialGenericConstraint.Class => KGVL.KEYWORD_CLASS,
            SpecialGenericConstraint.Struct => KGVL.KEYWORD_STRUCT,
            _ => KGVL.KEYWORD_NOTNULL
        };
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
