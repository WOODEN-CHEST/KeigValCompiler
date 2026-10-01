using KeigValCompiler.Semantician.Bound;
using KeigValCompiler.Semantician.Library;
using KeigValCompiler.Semantician.Member;
using KeigValCompiler.Semantician.Resolver;
using KeigValCompiler.Semantician.Types;

namespace KeigValCompiler.Semantician.Binding;

/* Classifies conversions as C# does: which conversion, if any, takes a value or a type to another type, and
 * how. The conversions C# predefines between its built-in types are the operators the standard library
 * declares as builtin on its built-in types: between the numeric types, and putting a value in a Nullable and
 * taking it out. They count as C#'s standard conversions, so they come before and after a user-defined
 * conversion as C#'s do, and are never taken for user-defined ones. Everything else follows C#'s rules:
 * nullable conversions lifted from numeric ones, references, boxing, enums, constants, the literal null and
 * "default", generic parameters, and user-defined conversions with their most specific operator, which follow
 * Roslyn where it differs from the specification, a generic parameter's constraints' static interface operators
 * included. KGVL has no array covariance, since T[] is the generic class Array<T> (decided 2026-10-01), and no
 * variance on generic parameters.
 *
 * A conversion involving the error type is never asked for: whoever asks has reported what made it. */
internal sealed class ConversionClassifier
{
    // Private fields.
    private readonly PackResolutionContext _context;

    /* The library's conversion operator from each built-in numeric type to each other, read when first needed,
     * by declaration. */
    private readonly Dictionary<PackMember, Dictionary<PackMember, (PackFunction Method, bool IsImplicit)>>
        _numericOperators = new(ReferenceEqualityComparer.Instance);


    // Constructors.
    internal ConversionClassifier(PackResolutionContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }


    // Internal methods.
    /* The implicit conversion of a value to a type: a standard one of its type's, one its being a constant
     * allows, or for a value with no type of its own, the one the literal null or "default" has; failing those,
     * a user-defined one, which may be ambiguous. */
    internal Conversion ClassifyImplicit(BoundExpression source, SemanticType target)
    {
        ArgumentNullException.ThrowIfNull(source, nameof(source));
        ArgumentNullException.ThrowIfNull(target, nameof(target));

        Conversion BuiltIn = ClassifyImplicitBuiltIn(source, target);
        return BuiltIn.DoesExist ? BuiltIn : ClassifyUserDefined(source, target, false);
    }

    /* The conversion only a cast can make, for a value with no implicit one: a built-in explicit conversion,
     * failing which a user-defined one, implicit or explicit operators both taking part. */
    internal Conversion ClassifyExplicitOnly(BoundExpression source, SemanticType target)
    {
        ArgumentNullException.ThrowIfNull(source, nameof(source));
        ArgumentNullException.ThrowIfNull(target, nameof(target));

        if (source.Type != null)
        {
            Conversion BuiltIn = ClassifyExplicitBuiltIn(source.Type, target);
            if (BuiltIn.DoesExist)
            {
                return BuiltIn;
            }
        }
        return ClassifyUserDefined(source, target, true);
    }

    /* The standard implicit conversion between two types: the identity, numeric, nullable, reference, boxing
     * and generic parameter conversions, which a user-defined conversion can have before and after it. */
    internal Conversion ClassifyStandardImplicit(SemanticType source, SemanticType target)
    {
        ArgumentNullException.ThrowIfNull(source, nameof(source));
        ArgumentNullException.ThrowIfNull(target, nameof(target));

        if (source.Equals(target))
        {
            return Conversion.Identity;
        }

        Conversion Numeric = ClassifyNumeric(source, target);
        if (Numeric.IsImplicit)
        {
            return Numeric;
        }

        if (IsNullable(target, out SemanticType? TargetUnderlying))
        {
            return ClassifyImplicitNullable(source, TargetUnderlying!);
        }

        bool IsSourceReference = IsReferenceType(source);
        bool IsTargetReference = IsReferenceType(target);
        if (IsSourceReference && IsTargetReference && _context.Hierarchy.IsSameOrDerived(source, target))
        {
            return Conversion.ImplicitReference;
        }
        if (!IsSourceReference && IsTargetReference && IsBoxable(source, target))
        {
            return Conversion.Boxing;
        }

        /* A generic parameter to another it is constrained to, which is a reference conversion only when the
         * first is known to be a reference type. */
        if ((source is GenericParameterType) && (target is GenericParameterType)
            && _context.Hierarchy.IsSameOrDerived(source, target))
        {
            return IsSourceReference ? Conversion.ImplicitReference : Conversion.Boxing;
        }
        return Conversion.None;
    }

    /* Whether a constant of one type has an implicit conversion to another when its value fits: an int to a
     * smaller integer type, or to uint or ulong, or a long to ulong. As in C#, a constant which does not fit is
     * reported as not fitting, rather than as needing a cast, when its type and the target are such a pair. */
    internal bool IsConstantConvertible(SemanticType source, SemanticType target)
    {
        ArgumentNullException.ThrowIfNull(source, nameof(source));
        ArgumentNullException.ThrowIfNull(target, nameof(target));

        LibraryType? Source = NumericTypes.GetLibraryType(source);
        LibraryType? Target = NumericTypes.GetLibraryType(target);
        if (Source == LibraryTypes.Int32)
        {
            return (Target == LibraryTypes.Int8) || (Target == LibraryTypes.UInt8)
                || (Target == LibraryTypes.Int16) || (Target == LibraryTypes.UInt16)
                || (Target == LibraryTypes.UInt32) || (Target == LibraryTypes.UInt64);
        }
        return (Source == LibraryTypes.Int64) && (Target == LibraryTypes.UInt64);
    }

    /* A Nullable's underlying type, or null for any other type. */
    internal SemanticType? GetNullableUnderlying(SemanticType type)
    {
        ArgumentNullException.ThrowIfNull(type, nameof(type));

        IsNullable(type, out SemanticType? Underlying);
        return Underlying;
    }


    // Private methods.
    /* The implicit conversions which are not user-defined, of a value rather than of a type. */
    private Conversion ClassifyImplicitBuiltIn(BoundExpression source, SemanticType target)
    {
        if (source.Type == null)
        {
            if (IsNullLiteral(source))
            {
                return (IsReferenceType(target) || IsNullable(target, out _)) ? Conversion.NullLiteral
                    : Conversion.None;
            }
            return (source is BoundDefault) ? Conversion.DefaultLiteral : Conversion.None;
        }

        Conversion FromType = ClassifyStandardImplicit(source.Type, target);
        if (FromType.DoesExist || (source.ConstantValue == null))
        {
            return FromType;
        }

        if (IsNullable(target, out SemanticType? TargetUnderlying))
        {
            Conversion Underlying = ClassifyConstant(source.Type, source.ConstantValue, TargetUnderlying!);
            return Underlying.DoesExist ? Conversion.Nullable(Underlying, GetWrapOperator(), true)
                : Conversion.None;
        }
        return ClassifyConstant(source.Type, source.ConstantValue, target);
    }

    /* What a constant's value allows beyond its type's conversions: a zero to an enum, and an integer to a
     * smaller integer type when it fits. As in Roslyn, which allows more than C#'s specification, any integer or
     * decimal zero converts to an enum, though a char's does not. */
    private Conversion ClassifyConstant(SemanticType source, ConstantValue value, SemanticType target)
    {
        bool IsZero = ((value.Kind == ConstantValueKind.Integer) && (value.Integer == 0))
            || ((value.Kind == ConstantValueKind.Decimal) && TwoIntDecimal.IsZero(value.Decimal));
        if (IsEnum(target) && IsZero && (NumericTypes.IsInteger(source)
            || NumericTypes.IsLibraryType(source, LibraryTypes.Decimal)))
        {
            return Conversion.ImplicitEnumeration;
        }
        if (value.Kind != ConstantValueKind.Integer)
        {
            return Conversion.None;
        }
        if (IsConstantConvertible(source, target) && NumericTypes.IsInRange(value.Integer,
            NumericTypes.GetLibraryType(target)!))
        {
            return Conversion.ImplicitConstant;
        }
        return Conversion.None;
    }

    /* To a Nullable: from one of a value type which converts to the target's by identity or implicitly by a
     * numeric conversion, or from such a value type itself, which is then put in the Nullable. */
    private Conversion ClassifyImplicitNullable(SemanticType source, SemanticType targetUnderlying)
    {
        if (IsNullable(source, out SemanticType? SourceUnderlying))
        {
            Conversion Between = ClassifyIdentityOrImplicitNumeric(SourceUnderlying!, targetUnderlying);
            return Between.DoesExist ? Conversion.Nullable(Between, null, true) : Conversion.None;
        }
        if (!IsNonNullableValueType(source))
        {
            return Conversion.None;
        }

        Conversion Underlying = ClassifyIdentityOrImplicitNumeric(source, targetUnderlying);
        return Underlying.DoesExist ? Conversion.Nullable(Underlying, GetWrapOperator(), true) : Conversion.None;
    }

    private Conversion ClassifyIdentityOrImplicitNumeric(SemanticType source, SemanticType target)
    {
        if (source.Equals(target))
        {
            return Conversion.Identity;
        }
        Conversion Numeric = ClassifyNumeric(source, target);
        return Numeric.IsImplicit ? Numeric : Conversion.None;
    }

    /* The explicit conversions which are not user-defined, for types with no implicit conversion between them:
     * numeric, enumeration, nullable, reference and unboxing conversions, generic parameters' included. */
    private Conversion ClassifyExplicitBuiltIn(SemanticType source, SemanticType target)
    {
        Conversion Numeric = ClassifyNumeric(source, target);
        if (Numeric.DoesExist)
        {
            return Numeric;
        }
        if ((IsEnum(source) && (NumericTypes.IsNumeric(target) || IsEnum(target)))
            || (NumericTypes.IsNumeric(source) && IsEnum(target)))
        {
            return Conversion.ExplicitEnumeration;
        }

        Conversion Nullable = ClassifyExplicitNullable(source, target);
        if (Nullable.DoesExist)
        {
            return Nullable;
        }
        if (HasExplicitReference(source, target))
        {
            return Conversion.ExplicitReference;
        }
        return HasUnboxing(source, target) ? Conversion.Unboxing : Conversion.None;
    }

    /* Between a Nullable and a value type, or two Nullables, whose value types convert by any conversion C#
     * predefines between value types, implicit or explicit: the value is taken out of the Nullable, converted,
     * and put in the other. */
    private Conversion ClassifyExplicitNullable(SemanticType source, SemanticType target)
    {
        bool IsSourceNullable = IsNullable(source, out SemanticType? SourceUnderlying);
        bool IsTargetNullable = IsNullable(target, out SemanticType? TargetUnderlying);
        if ((!IsSourceNullable && !IsTargetNullable)
            || (!IsSourceNullable && !IsNonNullableValueType(source))
            || (!IsTargetNullable && !IsNonNullableValueType(target)))
        {
            return Conversion.None;
        }

        SemanticType From = SourceUnderlying ?? source;
        SemanticType To = TargetUnderlying ?? target;
        Conversion Between = From.Equals(To) ? Conversion.Identity : ClassifyNumeric(From, To);
        if (!Between.DoesExist && ((IsEnum(From) && (NumericTypes.IsNumeric(To) || IsEnum(To)))
            || (NumericTypes.IsNumeric(From) && IsEnum(To))))
        {
            Between = Conversion.ExplicitEnumeration;
        }
        if (!Between.DoesExist)
        {
            return Conversion.None;
        }

        PackFunction? Method = (IsSourceNullable && IsTargetNullable) ? null
            : (IsSourceNullable ? GetUnwrapOperator() : GetWrapOperator());
        return Conversion.Nullable(Between, Method, false);
    }

    /* C#'s explicit reference conversions, between reference types with no implicit conversion between them,
     * a generic parameter known to be a reference type included. */
    private bool HasExplicitReference(SemanticType source, SemanticType target)
    {
        if (!IsReferenceType(source) || !IsReferenceType(target))
        {
            return false;
        }
        if ((source is GenericParameterType) || (target is GenericParameterType))
        {
            return HasExplicitGenericParameterConversion(source, target);
        }

        DeclaredType Source = (DeclaredType)source;
        DeclaredType Target = (DeclaredType)target;
        if (Source.Equals(_context.Hierarchy.GetObjectType()))
        {
            return true;
        }
        bool IsSourceInterface = Source.Declaration is PackInterface;
        bool IsTargetInterface = Target.Declaration is PackInterface;
        if (!IsSourceInterface && !IsTargetInterface)
        {
            return (Target.Declaration is PackClass) && _context.Hierarchy.IsSameOrDerivedClass(Target, Source);
        }
        if (!IsSourceInterface)
        {
            return (Source.Declaration is PackClass) && !IsSealed(Source);
        }
        if (!IsTargetInterface)
        {
            return (Target.Declaration is PackClass)
                && (!IsSealed(Target) || _context.Hierarchy.IsSameOrDerived(Target, Source));
        }
        return true;
    }

    /* C#'s unboxing conversions: from object, or an interface, to a value type implementing it, a Nullable
     * of one included, and the explicit conversions to and from a generic parameter not known to be a
     * reference type. */
    private bool HasUnboxing(SemanticType source, SemanticType target)
    {
        if ((source is GenericParameterType) || (target is GenericParameterType))
        {
            bool IsBetweenReferences = IsReferenceType(source) && IsReferenceType(target);
            return !IsBetweenReferences && HasExplicitGenericParameterConversion(source, target);
        }
        if (!IsReferenceType(source) || IsReferenceType(target))
        {
            return false;
        }

        SemanticType Value = GetNullableUnderlying(target) ?? target;
        if (!IsNonNullableValueType(Value))
        {
            return false;
        }
        return source.Equals(_context.Hierarchy.GetObjectType())
            || ((source is DeclaredType { Declaration: PackInterface })
                && _context.Hierarchy.IsSameOrDerived(Value, source));
    }

    /* The explicit conversions C# gives a generic parameter T: from its effective base class, or a class that
     * one derives from, to T; from any interface to T; from T to any interface; and to T from a generic
     * parameter T is constrained to. Which of the two kinds of conversion it is, the caller decides. */
    private bool HasExplicitGenericParameterConversion(SemanticType source, SemanticType target)
    {
        if (target is GenericParameterType TargetParameter)
        {
            DeclaredType? BaseClass = GetEffectiveBaseClass(TargetParameter);
            if ((BaseClass != null) && (BaseClass.Equals(source)
                || _context.Hierarchy.GetBaseClasses(BaseClass).Any(baseClass => baseClass.Equals(source))))
            {
                return true;
            }
            if (source.Equals(_context.Hierarchy.GetObjectType()) || IsInterface(source))
            {
                return true;
            }
            if ((source is GenericParameterType) && _context.Hierarchy.IsSameOrDerived(target, source))
            {
                return true;
            }
        }
        return (source is GenericParameterType) && IsInterface(target);
    }

    /* The library's operator between two built-in numeric types, implicit or explicit, or none. */
    private Conversion ClassifyNumeric(SemanticType source, SemanticType target)
    {
        if (!NumericTypes.IsNumeric(source) || !NumericTypes.IsNumeric(target))
        {
            return Conversion.None;
        }

        PackMember From = ((DeclaredType)source).Declaration;
        PackMember To = ((DeclaredType)target).Declaration;
        return GetNumericOperators(From).TryGetValue(To, out (PackFunction Method, bool IsImplicit) Operator)
            ? Conversion.Numeric(Operator.Method, Operator.IsImplicit) : Conversion.None;
    }

    /* The builtin conversion operators a built-in numeric type declares, by the type each converts to, and
     * whether each is implicit. */
    private Dictionary<PackMember, (PackFunction Method, bool IsImplicit)> GetNumericOperators(
        PackMember numericType)
    {
        if (_numericOperators.TryGetValue(numericType,
            out Dictionary<PackMember, (PackFunction Method, bool IsImplicit)>? Known))
        {
            return Known;
        }

        Dictionary<PackMember, (PackFunction Method, bool IsImplicit)> Operators =
            new(ReferenceEqualityComparer.Instance);
        foreach (OperatorOverload Overload in ((IOperatorOverloadHolder)numericType).OperatorOverloads.Where(
            overload => OverloadableOperatorKinds.IsConversion(overload.OverloadedOperator)
                && overload.Function.HasModifier(PackMemberModifiers.BuiltIn)))
        {
            if ((Overload.Function.ReturnType?.MainTarget.Target is PackMember To)
                && (_context.Registry.GetLibraryType(To) != null))
            {
                Operators.TryAdd(To, (Overload.Function,
                    Overload.OverloadedOperator == OverloadableOperator.ImplicitCast));
            }
        }
        _numericOperators.Add(numericType, Operators);
        return Operators;
    }

    /* The Nullable's operators which put a value in one, and take it out. Null when the library declares none,
     * which the binding of the library reports. */
    private PackFunction? GetWrapOperator()
    {
        return GetNullableOperator(OverloadableOperator.ImplicitCast);
    }

    private PackFunction? GetUnwrapOperator()
    {
        return GetNullableOperator(OverloadableOperator.ExplicitCast);
    }

    private PackFunction? GetNullableOperator(OverloadableOperator conversion)
    {
        return (_context.Registry.GetDeclaredType(LibraryTypes.Nullable) as IOperatorOverloadHolder)
            ?.OperatorOverloads[conversion].Select(overload => overload.Function)
            .FirstOrDefault(function => function.HasModifier(PackMemberModifiers.BuiltIn));
    }


    /* User-defined conversions. */
    /* C#'s user-defined conversion of a value to a type, through the most specific of the conversion operators
     * declared by the classes and structures involved, as Roslyn finds it: an implicit one only through an
     * implicit operator from a type encompassing the value to one the target encompasses; an explicit one
     * through either kind of operator, from or to a type which encompasses or is encompassed by the source or
     * the target. A type encompasses another which converts to it by a standard implicit conversion, neither
     * being an interface. As in Roslyn, an operator between two value types is lifted to their Nullables only
     * when it does not apply as it is and the value is a Nullable, and for an implicit conversion, when the
     * target can be null; an explicit one may take the lifted result's value out of its Nullable. Two operators
     * of one type converting between the same types have been reported, and count as one. */
    private Conversion ClassifyUserDefined(BoundExpression source, SemanticType target, bool isExplicit)
    {
        SemanticType? SourceType = source.Type;
        SemanticType? Source = (SourceType == null) ? null : (GetNullableUnderlying(SourceType) ?? SourceType);
        SemanticType Target = GetNullableUnderlying(target) ?? target;

        List<UserDefinedCandidate> Candidates = new();
        foreach ((DeclaredType Holder, GenericParameterType? ConstrainedTo) in GetUserDefinedHolders(
            Source, Target, isExplicit))
        {
            foreach (OperatorOverload Overload in ((IOperatorOverloadHolder)Holder.Declaration).OperatorOverloads)
            {
                bool IsApplicableKind = (Overload.OverloadedOperator == OverloadableOperator.ImplicitCast)
                    || (isExplicit && (Overload.OverloadedOperator == OverloadableOperator.ExplicitCast));
                if (IsApplicableKind && !Overload.Function.HasModifier(PackMemberModifiers.BuiltIn))
                {
                    AddCandidate(Candidates, Overload.Function, Holder, ConstrainedTo, source, target, isExplicit);
                }
            }
        }
        if (Candidates.Count == 0)
        {
            return Conversion.None;
        }

        SemanticType? From = isExplicit ? GetExplicitMostSpecificSource(Candidates, source)
            : GetImplicitMostSpecificSource(Candidates, SourceType);
        SemanticType? To = isExplicit ? GetExplicitMostSpecificTarget(Candidates, target)
            : GetImplicitMostSpecificTarget(Candidates, target);
        UserDefinedCandidate? Chosen = ((From == null) || (To == null)) ? null
            : FindMostSpecificOperator(Candidates, From, To);
        if (Chosen == null)
        {
            return Conversion.Ambiguous(Candidates.Select(candidate => candidate.Method)
                .Distinct(ReferenceEqualityComparer.Instance).Cast<PackFunction>().ToArray());
        }

        Conversion Before = isExplicit ? ClassifyStandard(source, Chosen.FromType)
            : ClassifyImplicitBuiltIn(source, Chosen.FromType);
        Conversion After = isExplicit ? ClassifyStandard(Chosen.ToType, target)
            : ClassifyStandardImplicit(Chosen.ToType, target);
        return Conversion.UserDefined(Chosen.Method, Before, After, Chosen.FromType, Chosen.ToType,
            Chosen.IsLifted, Chosen.ConstrainedTo, !isExplicit);
    }

    /* An operator, as it is or lifted, when it can convert the value to the target, unless an operator of the
     * same type converting between the same types is a candidate already. */
    private void AddCandidate(List<UserDefinedCandidate> candidates,
        PackFunction method,
        DeclaredType holder,
        GenericParameterType? constrainedTo,
        BoundExpression source,
        SemanticType target,
        bool isExplicit)
    {
        TypeSubstitution Substitution = TypeSubstitution.Of(holder);
        TypeTargetIdentifier? ParameterType = method.Parameters.FirstOrDefault()?.Type;
        SemanticType? From = (ParameterType == null) ? null
            : _context.TypeReader.Read(ParameterType)?.Substitute(Substitution);
        SemanticType? To = (method.ReturnType == null) ? null
            : _context.TypeReader.Read(method.ReturnType)?.Substitute(Substitution);
        if ((From == null) || (To == null))
        {
            return;
        }

        if (IsApplicable(source, From, To, target, isExplicit))
        {
            AddDistinctCandidate(candidates, new(method, From, To, false, constrainedTo));
            return;
        }

        bool IsLiftable = (source.Type != null) && IsNullable(source.Type, out _) && IsNonNullableValueType(From)
            && (isExplicit || IsReferenceType(target) || IsNullable(target, out _));
        if (!IsLiftable)
        {
            return;
        }
        SemanticType? LiftedFrom = _context.TypeReader.ReadKnownType(LibraryTypes.Nullable, From);
        SemanticType? LiftedTo = IsNonNullableValueType(To)
            ? _context.TypeReader.ReadKnownType(LibraryTypes.Nullable, To) : To;
        if ((LiftedFrom != null) && (LiftedTo != null)
            && IsApplicable(source, LiftedFrom, LiftedTo, target, isExplicit))
        {
            AddDistinctCandidate(candidates, new(method, LiftedFrom, LiftedTo, true, constrainedTo));
        }
    }

    private void AddDistinctCandidate(List<UserDefinedCandidate> candidates, UserDefinedCandidate candidate)
    {
        PackMember? Holder = MemberRelations.GetHoldingMember(candidate.Method);
        bool IsDuplicate = candidates.Any(other => ReferenceEquals(MemberRelations.GetHoldingMember(other.Method),
            Holder) && other.FromType.Equals(candidate.FromType) && other.ToType.Equals(candidate.ToType)
            && (other.IsLifted == candidate.IsLifted));
        if (!IsDuplicate)
        {
            candidates.Add(candidate);
        }
    }

    private bool IsApplicable(BoundExpression source,
        SemanticType from,
        SemanticType to,
        SemanticType target,
        bool isExplicit)
    {
        if (!isExplicit)
        {
            return IsEncompassedBy(source, from) && IsEncompassedBy(to, target);
        }
        bool IsFromApplicable = IsEncompassedBy(source, from)
            || ((source.Type != null) && IsEncompassedBy(from, source.Type));
        return IsFromApplicable && (IsEncompassedBy(to, target) || IsEncompassedBy(target, to));
    }

    /* The classes and structures whose conversion operators are considered: the source's and the classes it
     * derives from, and the target's, together with the classes it derives from for an explicit conversion. A
     * generic parameter counts as its effective base class, and, as in Roslyn, offers the static operators of the
     * interfaces it is constrained to: as a source, those and every interface they derive from, and as a target,
     * only those, unless the conversion is explicit. No other interface is considered. */
    private List<(DeclaredType Holder, GenericParameterType? ConstrainedTo)> GetUserDefinedHolders(
        SemanticType? source,
        SemanticType target,
        bool isExplicit)
    {
        List<(DeclaredType Holder, GenericParameterType? ConstrainedTo)> Holders = new();
        AddUserDefinedHolders(Holders, source, true);
        AddUserDefinedHolders(Holders, target, isExplicit);
        return Holders;
    }

    private void AddUserDefinedHolders(List<(DeclaredType Holder, GenericParameterType? ConstrainedTo)> holders,
        SemanticType? type,
        bool isWithBases)
    {
        GenericParameterType? Parameter = type as GenericParameterType;
        DeclaredType? Declared = (Parameter != null) ? GetEffectiveBaseClass(Parameter) : type as DeclaredType;
        List<(DeclaredType Holder, GenericParameterType? ConstrainedTo)> Added = new();
        if ((Declared != null) && (Declared.Declaration is PackClass or PackStruct))
        {
            Added.Add((Declared, null));
            if (isWithBases && (Declared.Declaration is PackClass))
            {
                Added.AddRange(_context.Hierarchy.GetBaseClasses(Declared).Select(
                    baseClass => (baseClass, (GenericParameterType?)null)));
            }
        }
        if (Parameter != null)
        {
            Added.AddRange(GetConstraintInterfaces(Parameter, isWithBases).Select(
                constraint => (constraint, (GenericParameterType?)Parameter)));
        }
        holders.AddRange(Added.Where(added => !holders.Any(holder => holder.Holder.Equals(added.Holder))));
    }

    /* The interfaces a generic parameter is constrained to, directly or through the generic parameters it is
     * constrained to, and when asked for, every interface those derive from. */
    private List<DeclaredType> GetConstraintInterfaces(GenericParameterType parameter, bool isWithBases)
    {
        List<DeclaredType> Interfaces = new();
        HashSet<GenericTypeParameter> Followed = new(ReferenceEqualityComparer.Instance);
        Queue<GenericTypeParameter> ToFollow = new(new GenericTypeParameter[] { parameter.Parameter });
        while (ToFollow.Count > 0)
        {
            GenericTypeParameter Current = ToFollow.Dequeue();
            if (!Followed.Add(Current))
            {
                continue;
            }
            foreach (SemanticType Constraint in _context.Constraints.GetTypeConstraints(Current))
            {
                if (Constraint is GenericParameterType Other)
                {
                    ToFollow.Enqueue(Other.Parameter);
                }
                else if (Constraint is DeclaredType { Declaration: PackInterface } Interface)
                {
                    Interfaces.Add(Interface);
                    if (isWithBases)
                    {
                        Interfaces.AddRange(_context.Hierarchy.GetInterfaces(Interface));
                    }
                }
            }
        }
        return Interfaces.Distinct().ToList();
    }

    /* For an implicit conversion, the source itself if an operator converts from it, or else the type which
     * the source types of all the operators encompass. */
    private SemanticType? GetImplicitMostSpecificSource(List<UserDefinedCandidate> candidates,
        SemanticType? source)
    {
        if ((source != null) && candidates.Any(candidate => candidate.FromType.Equals(source)))
        {
            return source;
        }
        return FindMostEncompassed(candidates.Select(candidate => candidate.FromType));
    }

    /* For an implicit conversion, the target itself if an operator converts to it, or else the type which
     * encompasses the target types of all the operators. */
    private SemanticType? GetImplicitMostSpecificTarget(List<UserDefinedCandidate> candidates, SemanticType target)
    {
        if (candidates.Any(candidate => candidate.ToType.Equals(target)))
        {
            return target;
        }
        return FindMostEncompassing(candidates.Select(candidate => candidate.ToType));
    }

    /* For an explicit conversion, the source itself if an operator converts from it; or else, among the operators
     * converting from a type encompassing the value, the source type the others encompass; or else the source
     * type which encompasses all the others. */
    private SemanticType? GetExplicitMostSpecificSource(List<UserDefinedCandidate> candidates,
        BoundExpression source)
    {
        if ((source.Type != null) && candidates.Any(candidate => candidate.FromType.Equals(source.Type)))
        {
            return source.Type;
        }
        List<SemanticType> Encompassing = candidates
            .Where(candidate => IsEncompassedBy(source, candidate.FromType))
            .Select(candidate => candidate.FromType).ToList();
        return (Encompassing.Count > 0) ? FindMostEncompassed(Encompassing)
            : FindMostEncompassing(candidates.Select(candidate => candidate.FromType));
    }

    /* For an explicit conversion, the target itself if an operator converts to it; or else, among the operators
     * converting to a type the target encompasses, the target type encompassing the others; or else the target
     * type all the others encompass. */
    private SemanticType? GetExplicitMostSpecificTarget(List<UserDefinedCandidate> candidates, SemanticType target)
    {
        if (candidates.Any(candidate => candidate.ToType.Equals(target)))
        {
            return target;
        }
        List<SemanticType> Encompassed = candidates.Where(candidate => IsEncompassedBy(candidate.ToType, target))
            .Select(candidate => candidate.ToType).ToList();
        return (Encompassed.Count > 0) ? FindMostEncompassing(Encompassed)
            : FindMostEncompassed(candidates.Select(candidate => candidate.ToType));
    }

    /* The one operator converting from the most specific source to the most specific target, or else the one
     * lifted operator doing so, or null when there is not exactly one. */
    private UserDefinedCandidate? FindMostSpecificOperator(List<UserDefinedCandidate> candidates,
        SemanticType from,
        SemanticType to)
    {
        List<UserDefinedCandidate> Matching = candidates.Where(
            candidate => candidate.FromType.Equals(from) && candidate.ToType.Equals(to)).ToList();
        List<UserDefinedCandidate> Unlifted = Matching.Where(candidate => !candidate.IsLifted).ToList();
        if (Unlifted.Count == 1)
        {
            return Unlifted[0];
        }
        List<UserDefinedCandidate> Lifted = Matching.Where(candidate => candidate.IsLifted).ToList();
        return ((Unlifted.Count == 0) && (Lifted.Count == 1)) ? Lifted[0] : null;
    }

    /* The one type among some which every other encompasses, or null when there is not exactly one. */
    private SemanticType? FindMostEncompassed(IEnumerable<SemanticType> types)
    {
        List<SemanticType> Distinct = types.Distinct().ToList();
        List<SemanticType> Most = Distinct.Where(type => Distinct.All(other => IsEncompassedBy(type, other)))
            .ToList();
        return (Most.Count == 1) ? Most[0] : null;
    }

    /* The one type among some which encompasses every other, or null when there is not exactly one. */
    private SemanticType? FindMostEncompassing(IEnumerable<SemanticType> types)
    {
        List<SemanticType> Distinct = types.Distinct().ToList();
        List<SemanticType> Most = Distinct.Where(type => Distinct.All(other => IsEncompassedBy(other, type)))
            .ToList();
        return (Most.Count == 1) ? Most[0] : null;
    }

    /* Whether one type is encompassed by another: converts to it by a standard implicit conversion, neither
     * being an interface. */
    private bool IsEncompassedBy(SemanticType type, SemanticType other)
    {
        return !IsInterface(type) && !IsInterface(other) && ClassifyStandardImplicit(type, other).DoesExist;
    }

    /* As for a type, for a value, which may convert by what its being a constant or the literal null allows, but
     * not by a zero's conversion to an enum, which, as in C#, is no standard conversion. */
    private bool IsEncompassedBy(BoundExpression value, SemanticType other)
    {
        if (((value.Type != null) && IsInterface(value.Type)) || IsInterface(other))
        {
            return false;
        }
        Conversion Builtin = ClassifyImplicitBuiltIn(value, other);
        return Builtin.DoesExist && (Builtin.Kind != ConversionKind.ImplicitEnumeration)
            && (Builtin.Underlying?.Kind != ConversionKind.ImplicitEnumeration);
    }

    /* A standard conversion of a value, implicit if there is one, or else explicit. */
    private Conversion ClassifyStandard(BoundExpression source, SemanticType target)
    {
        Conversion Implicit = ClassifyImplicitBuiltIn(source, target);
        return (Implicit.DoesExist || (source.Type == null)) ? Implicit
            : ClassifyExplicitBuiltIn(source.Type, target);
    }

    private Conversion ClassifyStandard(SemanticType source, SemanticType target)
    {
        Conversion Implicit = ClassifyStandardImplicit(source, target);
        return Implicit.DoesExist ? Implicit : ClassifyExplicitBuiltIn(source, target);
    }


    /* What types are. */
    /* Whether a value of a type converts to a reference type by boxing: a value type, or a generic parameter not
     * known to be a reference type, to object or to an interface it implements, and a Nullable to whatever its
     * value type boxes to. */
    private bool IsBoxable(SemanticType source, SemanticType target)
    {
        if (target.Equals(_context.Hierarchy.GetObjectType()))
        {
            return true;
        }
        if (IsNullable(source, out SemanticType? Underlying))
        {
            return IsBoxable(Underlying!, target);
        }
        return _context.Hierarchy.IsSameOrDerived(source, target);
    }

    private bool IsNullable(SemanticType type, out SemanticType? underlying)
    {
        underlying = (type is DeclaredType { IsNullable: true } Nullable) ? Nullable.TypeArguments[0] : null;
        return underlying != null;
    }

    /* A structure or an enum, but not a Nullable, or a generic parameter constrained to be a structure. */
    private bool IsNonNullableValueType(SemanticType type)
    {
        return _context.Hierarchy.IsValueType(type) && !IsNullable(type, out _);
    }

    private bool IsReferenceType(SemanticType type)
    {
        return _context.Hierarchy.IsReferenceType(type);
    }

    private bool IsEnum(SemanticType type)
    {
        return type is DeclaredType { Declaration: PackEnumeration };
    }

    private bool IsInterface(SemanticType type)
    {
        return type is DeclaredType { Declaration: PackInterface };
    }

    /* A class nothing can derive from: one written sealed or static. Structures, enums and delegates are never
     * asked about, being sealed by nature. */
    private bool IsSealed(DeclaredType type)
    {
        return type.Declaration.HasAnyModifier(PackMemberModifiers.Sealed, PackMemberModifiers.Static);
    }

    private bool IsNullLiteral(BoundExpression value)
    {
        return (value is BoundLiteral) && (value.ConstantValue?.Kind == ConstantValueKind.Null);
    }

    /* The class a generic parameter's constraints make it derive from: the one of the classes it is constrained
     * to, directly or through other generic parameters, which derives from all the others, or null when it is
     * constrained to none, which leaves object. */
    private DeclaredType? GetEffectiveBaseClass(GenericParameterType parameter)
    {
        IReadOnlyList<DeclaredType> Classes = _context.Hierarchy.GetConstraintClasses(parameter.Parameter);
        return Classes.FirstOrDefault(candidate => Classes.All(
            other => _context.Hierarchy.IsSameOrDerivedClass(candidate, other)));
    }
}
