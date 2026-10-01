using KeigValCompiler.Error;
using KeigValCompiler.Semantician.Bound;
using KeigValCompiler.Semantician.Library;
using KeigValCompiler.Semantician.Member;
using KeigValCompiler.Semantician.Member.Code;
using KeigValCompiler.Semantician.Resolver;
using KeigValCompiler.Semantician.Types;

namespace KeigValCompiler.Semantician.Binding;

/* Binds the values in a body: literals, names and member accesses, through the NameBinder, "this", "default"
 * and assignments, and converts a value to the type it is needed as, as C# does, reporting a conversion which
 * does not exist as C# reports it. A kind of value not bound yet becomes a BoundNotYetSupported, which counts as
 * having errors, so that nothing around it is reported.
 *
 * Binding a name may give what is not a value: a type or a namespace, which may stand before a '.', or a
 * property, whose accessor depends on whether it is read or given a value. A value read is checked for that:
 * a type or namespace is reported, and a property is read through its getter. A property's accessor is its own,
 * or, for an override declaring only some, the one it inherits, as in C#. */
internal sealed class ExpressionBinder
{
    // Private fields.
    private readonly BodyBinder _body;
    private PackResolutionContext Resolution => _body.Resolution;
    private ErrorRepository ErrorCreator => _body.Resolution.ErrorCreator;


    // Constructors.
    internal ExpressionBinder(BodyBinder body)
    {
        _body = body ?? throw new ArgumentNullException(nameof(body));
    }


    // Internal methods.
    /* What a value written in the body is, which may be a type or a namespace, before a '.', or a property not
     * yet read. */
    internal BoundExpression BindExpression(Statement syntax)
    {
        ArgumentNullException.ThrowIfNull(syntax, nameof(syntax));

        return syntax switch
        {
            PrimitiveValueStatement Literal => BindLiteral(Literal),
            IdentifiableAccessStatement Name => _body.Names.BindSimpleName(Name),
            CompositeAccessStatement Chain => _body.Names.BindChain(Chain),
            ThisStatement This => BindThis(This),
            BaseStatement Base => BindBaseAlone(Base),
            DefaultStatement Default => BindDefault(Default),
            AssignmentStatement { Operator: AssignmentOperator.Assign } Assignment => BindAssignment(Assignment),
            _ => new BoundNotYetSupported(syntax)
        };
    }

    /* A value written in the body, read. */
    internal BoundExpression BindValue(Statement syntax)
    {
        return CheckValue(BindExpression(syntax));
    }

    /* A value written in the body, read and converted implicitly to the type it is needed as. */
    internal BoundExpression BindConvertedValue(Statement syntax, SemanticType target)
    {
        ArgumentNullException.ThrowIfNull(target, nameof(target));
        return Convert(BindValue(syntax), target, syntax);
    }

    /* What is bound, as a value read: a type or a namespace is reported, as C# reports one where a value is
     * needed, and a property is read through its getter, which it has to have, and which has to be usable here. */
    internal BoundExpression CheckValue(BoundExpression bound)
    {
        ArgumentNullException.ThrowIfNull(bound, nameof(bound));

        switch (bound)
        {
            case BoundTypeExpression TypeExpression:
                _body.AddError(ErrorCreator.TypeUsedAsValue.CreateOptions(TypeExpression.Type!.ToString(),
                    DescribeTypeKind(TypeExpression.Type)), bound.Syntax!);
                return new BoundBadExpression(bound.Syntax!, bound);

            case BoundNameSpaceExpression NameSpace:
                _body.AddError(ErrorCreator.NameSpaceUsedAsValue.CreateOptions(NameSpace.Name), bound.Syntax!);
                return new BoundBadExpression(bound.Syntax!, bound);

            case BoundPropertyAccess { Accessor: null } Property:
                return ReadProperty(Property);

            default:
                return bound;
        }
    }

    /* A value converted implicitly to the type it is needed as, as written where the syntax is, which is where a
     * conversion which does not exist is reported, as C# reports it: a constant which does not fit, a
     * conversion which needs a cast, null where it cannot be, and a user-defined conversion with no best
     * operator. The value is still given the type then, so that nothing using it reports the mistake again. A
     * value or a type which has errors is left as it is. */
    internal BoundExpression Convert(BoundExpression value, SemanticType target, Statement syntax)
    {
        ArgumentNullException.ThrowIfNull(value, nameof(value));
        ArgumentNullException.ThrowIfNull(target, nameof(target));
        ArgumentNullException.ThrowIfNull(syntax, nameof(syntax));

        if ((value.Type is ErrorType) || (target is ErrorType) || value.HasErrors)
        {
            return value;
        }

        Conversion Implicit = _body.Context.Conversions.ClassifyImplicit(value, target);
        if (!Implicit.DoesExist)
        {
            ReportConversion(value, target, Implicit, syntax);
            return new BoundConversion(syntax, value, Implicit, target, false, null);
        }
        if (Implicit.Kind == ConversionKind.Identity)
        {
            return value;
        }
        if (Implicit.Kind == ConversionKind.DefaultLiteral)
        {
            return new BoundDefault(syntax, target, GetDefaultConstant(target));
        }

        ConstantValue? Constant = ConstantFolder.FoldImplicitConversion(value.ConstantValue, Implicit, target,
            Resolution.Hierarchy.IsReferenceType(target));
        return new BoundConversion(syntax, value, Implicit, target, false, Constant);
    }

    /* "base" before a '.': the object the member runs on, as its base class, whose members are reached without
     * overriding taking part. An interface has no base class, and neither has object. */
    internal BoundExpression BindBaseReceiver(BaseStatement syntax)
    {
        ArgumentNullException.ThrowIfNull(syntax, nameof(syntax));

        if (_body.IsStatic || (_body.ContainingType == null))
        {
            _body.AddError(ErrorCreator.BaseInStaticCode.CreateOptions(MemberRelations.GetKindName(_body.Member),
                MemberRelations.GetDisplayName(_body.Member)), syntax);
            return new BoundBadExpression(syntax);
        }
        if (_body.IsInitializer)
        {
            _body.AddError(ErrorCreator.BaseInInitializer.CreateOptions(MemberRelations.GetKindName(_body.Member),
                MemberRelations.GetDisplayName(_body.Member)), syntax);
            return new BoundBadExpression(syntax);
        }

        DeclaredType? BaseClass = Resolution.Hierarchy.GetBaseClass(_body.ContainingType);
        if (BaseClass == null)
        {
            _body.AddError(ErrorCreator.BaseWithoutBaseClass.CreateOptions(MemberRelations.GetKindName(
                _body.ContainingType.Declaration), _body.ContainingType.ToString()), syntax);
            return new BoundBadExpression(syntax);
        }
        return new BoundBaseReference(syntax, BaseClass);
    }


    // Private methods.
    /* A literal's type is C#'s: a whole number's the first of int, uint, long and ulong which holds it, narrowed
     * by its suffix, and a number with a fraction or exponent a decimal, KGVL having no binary floating point. The
     * literal null has no type. */
    private BoundExpression BindLiteral(PrimitiveValueStatement literal)
    {
        switch (literal.Value)
        {
            case string Text:
                return new BoundLiteral(literal, _body.GetLibraryType(LibraryTypes.String),
                    ConstantValue.FromString(Text));

            case bool Truth:
                return new BoundLiteral(literal, _body.GetLibraryType(LibraryTypes.Boolean),
                    ConstantValue.FromBoolean(Truth));

            case IntegerNumber Number:
                return new BoundLiteral(literal, _body.GetLibraryType(GetIntegerLiteralType(Number)),
                    ConstantValue.FromInteger(Number.Value));

            case int Number:
                /* What the parser keeps for a number it could not read, which has been reported. */
                return new BoundLiteral(literal, _body.GetLibraryType(LibraryTypes.Int32),
                    ConstantValue.FromInteger(Number));

            case DecimalNumber Number:
                return new BoundLiteral(literal, _body.GetLibraryType(LibraryTypes.Decimal),
                    ConstantValue.FromDecimal(Number.Value));

            case CharConstant Character:
                return new BoundLiteral(literal, _body.GetLibraryType(LibraryTypes.Char),
                    ConstantValue.FromChar(Character.Value));

            case NullConstant:
                return new BoundLiteral(literal, null, ConstantValue.Null);

            default:
                throw new InvalidOperationException($"The parser made a literal of the unknown kind " +
                    $"\"{literal.Value.GetType()}\".");
        }
    }

    private LibraryType GetIntegerLiteralType(IntegerNumber number)
    {
        if (!number.IsUnsigned && !number.IsLong && (number.Value <= int.MaxValue))
        {
            return LibraryTypes.Int32;
        }
        if (!number.IsLong && (number.Value <= uint.MaxValue))
        {
            return LibraryTypes.UInt32;
        }
        if (!number.IsUnsigned && (number.Value <= long.MaxValue))
        {
            return LibraryTypes.Int64;
        }
        return LibraryTypes.UInt64;
    }

    /* "this", the object the member runs on, which a static member, what a namespace holds, and a starting value
     * have none of. */
    private BoundExpression BindThis(ThisStatement syntax)
    {
        if (_body.IsStatic || (_body.ContainingType == null))
        {
            _body.AddError(ErrorCreator.ThisInStaticCode.CreateOptions(MemberRelations.GetKindName(_body.Member),
                MemberRelations.GetDisplayName(_body.Member)), syntax);
            return new BoundBadExpression(syntax);
        }
        if (_body.IsInitializer)
        {
            _body.AddError(ErrorCreator.ThisInInitializer.CreateOptions(MemberRelations.GetKindName(_body.Member),
                MemberRelations.GetDisplayName(_body.Member)), syntax);
            return new BoundBadExpression(syntax);
        }
        return new BoundThisReference(syntax, _body.ContainingType, false);
    }

    /* "base" anywhere but before a '.', which is no value. */
    private BoundExpression BindBaseAlone(BaseStatement syntax)
    {
        _body.AddError(ErrorCreator.BaseWithoutMember.CreateOptions(), syntax);
        return new BoundBadExpression(syntax);
    }

    /* "default(T)" has the type named, which, as in C#, may be a static class, though not use one inside it;
     * "default" alone has none until it is converted to one. */
    private BoundExpression BindDefault(DefaultStatement syntax)
    {
        if (syntax.TargetType == null)
        {
            return new BoundDefault(syntax, null, null);
        }

        SemanticType Type = _body.ReadType(syntax.TargetType, syntax);
        if ((Type is ErrorType) || _body.CheckWrittenType(syntax.TargetType,
            MemberRelations.GetKindName(_body.Member), MemberRelations.GetDisplayName(_body.Member), syntax))
        {
            return new BoundBadExpression(syntax);
        }
        return new BoundDefault(syntax, Type, GetDefaultConstant(Type));
    }

    /* A type's default value is a constant for the types C# says: zero of a numeric type or an enum, false, the
     * char zero, and null of a reference type, a generic parameter known to be one included. */
    private ConstantValue? GetDefaultConstant(SemanticType type)
    {
        LibraryType? Known = NumericTypes.GetLibraryType(type);
        if (Known == LibraryTypes.Decimal)
        {
            return ConstantValue.FromDecimal(TwoIntDecimal.Zero);
        }
        if (Known == LibraryTypes.Char)
        {
            return ConstantValue.FromChar(KGVL.CHAR_NONE);
        }
        if (NumericTypes.IsInteger(type) || (type is DeclaredType { Declaration: PackEnumeration }))
        {
            return ConstantValue.FromInteger(0);
        }
        if (Known == LibraryTypes.Boolean)
        {
            return ConstantValue.False;
        }
        return Resolution.Hierarchy.IsReferenceType(type) ? ConstantValue.Null : null;
    }

    /* "target = value", whose value is converted to the target's type, which is the assignment's type. A target
     * which cannot be given a value is reported; the value is still bound, for its own mistakes. */
    private BoundExpression BindAssignment(AssignmentStatement syntax)
    {
        BoundExpression Target = BindAssignmentTarget(syntax.Target);
        if (Target.HasErrors)
        {
            BoundExpression Unconverted = BindValue(syntax.Value);
            return new BoundBadExpression(syntax, Target, Unconverted);
        }

        BoundExpression Value = BindConvertedValue(syntax.Value, Target.Type!);
        return new BoundAssignment(syntax, Target, Value, Target.Type!);
    }

    /* What a value is given to: a local, a parameter, a field, a property through its setter, or a structure's
     * "this". What cannot be given one is reported, as C# reports it. */
    private BoundExpression BindAssignmentTarget(Statement syntax)
    {
        BoundExpression Target = BindExpression(syntax);
        if (Target.HasErrors)
        {
            return Target;
        }

        switch (Target)
        {
            case BoundTypeExpression TypeExpression:
                return Reject(Target, ErrorCreator.TypeOrNameSpaceAssigned.CreateOptions(
                    TypeExpression.Type!.ToString(), DescribeTypeKind(TypeExpression.Type)));

            case BoundNameSpaceExpression NameSpace:
                return Reject(Target, ErrorCreator.TypeOrNameSpaceAssigned.CreateOptions(NameSpace.Name,
                    KGVL.NAME_NAMESPACE));

            case BoundLocal { Local.IsConstant: true }:
                return Reject(Target, ErrorCreator.NotAssignable.CreateOptions());

            case BoundLocal:
                return Target;

            case BoundParameter Parameter when Parameter.Parameter.Modifiers == FunctionParameterModifier.In:
                return Reject(Target, ErrorCreator.ReadOnlyParameterAssigned.CreateOptions(
                    Parameter.Parameter.SelfIdentifier.SourceCodeName));

            case BoundParameter:
                return Target;

            case BoundFieldAccess Field:
                return CheckAssignedField(Field);

            case BoundPropertyAccess Property:
                return WriteProperty(Property);

            case BoundThisReference This when !This.Type!.IsValueType:
                return Reject(Target, ErrorCreator.ThisAssigned.CreateOptions());

            case BoundThisReference when _body.IsThisReadOnly:
                return Reject(Target, ErrorCreator.ReadOnlyThisAssigned.CreateOptions(KGVL.KEYWORD_THIS));

            case BoundThisReference:
                return Target;

            default:
                return Reject(Target, ErrorCreator.NotAssignable.CreateOptions());
        }
    }

    /* A field given a value: not a constant, a readonly one only where it is being made, and one of a value type's
     * value only when that value is stored somewhere it can change. */
    private BoundExpression CheckAssignedField(BoundFieldAccess field)
    {
        PackField Field = field.Field;
        string Name = MemberRelations.GetQualifiedDisplayName(Field);
        if (Field.HasModifier(PackMemberModifiers.Const))
        {
            return Reject(field, ErrorCreator.NotAssignable.CreateOptions());
        }
        if (Field.HasModifier(PackMemberModifiers.Readonly) && !IsBeingMade(Field, field.Receiver, true))
        {
            ErrorCreateOptions Error = (MemberRelations.GetHoldingMember(Field) == null)
                ? ErrorCreator.NameSpaceReadOnlyFieldAssigned.CreateOptions(Name)
                : (MemberRelations.IsWrittenStatic(Field)
                    ? ErrorCreator.StaticReadOnlyFieldAssigned.CreateOptions(Name)
                    : ErrorCreator.ReadOnlyFieldAssigned.CreateOptions(Name));
            return Reject(field, Error);
        }
        return CheckStorage(field, field.Receiver);
    }

    /* A property given a value, through its setter, written "set" or "init", which has to be usable here; an
     * "init" one only while the object is being made, unless the property is static, where "init" has been
     * reported as one it cannot have. A property with no setter which stores its value can be given one in a
     * constructor of the type declaring it, which writes what it stores, as C# allows it: reached in that very
     * type, so that a generic type's static constructor cannot set another instantiation's. */
    private BoundExpression WriteProperty(BoundPropertyAccess property)
    {
        PackProperty Property = property.Property;
        string Name = MemberRelations.GetQualifiedDisplayName(Property);
        PackFunction? Setter = FindAccessor(property, KGVL.KEYWORD_SET, true);
        if (Setter == null)
        {
            if (!MemberRelations.IsStoringProperty(Property) || !IsBeingMade(Property, property.Receiver, false)
                || !Equals(property.ContainingType, _body.ContainingType))
            {
                return Reject(property, ErrorCreator.PropertyWithoutSetter.CreateOptions(Name));
            }
        }
        else if (!_body.Context.Members.IsAccessible(Setter, _body.Member))
        {
            return Reject(property, ErrorCreator.AccessorInaccessible.CreateOptions(
                Setter.SelfIdentifier.SourceCodeName, Name,
                ModifierKeywords.FormatAccess(MemberRelations.GetEffectiveAccess(Setter))));
        }
        else if ((property.Receiver is BoundBaseReference) && IsAbstractAccessor(Setter))
        {
            return Reject(property, ErrorCreator.AbstractBaseMember.CreateOptions(Name));
        }
        else if ((Setter.SelfIdentifier.SourceCodeName == KGVL.KEYWORD_INIT)
            && !MemberRelations.IsStaticMember(Property) && !IsInitSetterUsable(Property, property.Receiver))
        {
            return Reject(property, ErrorCreator.InitOnlyAssigned.CreateOptions(Name));
        }

        BoundPropertyAccess Written = new(property.Syntax!, property.Receiver, Property, property.ContainingType,
            Setter, property.Type!);
        return CheckStorage(Written, property.Receiver);
    }

    /* A property read, through its getter, which it has to have, which has to be usable here, and which "base"
     * cannot reach when it is abstract. */
    private BoundExpression ReadProperty(BoundPropertyAccess property)
    {
        PackProperty Property = property.Property;
        string Name = MemberRelations.GetQualifiedDisplayName(Property);
        PackFunction? Getter = FindAccessor(property, KGVL.KEYWORD_GET, false);
        if (Getter == null)
        {
            return Reject(property, ErrorCreator.PropertyWithoutGetter.CreateOptions(Name));
        }
        if (!_body.Context.Members.IsAccessible(Getter, _body.Member))
        {
            return Reject(property, ErrorCreator.AccessorInaccessible.CreateOptions(
                Getter.SelfIdentifier.SourceCodeName, Name,
                ModifierKeywords.FormatAccess(MemberRelations.GetEffectiveAccess(Getter))));
        }
        if ((property.Receiver is BoundBaseReference) && IsAbstractAccessor(Getter))
        {
            return Reject(property, ErrorCreator.AbstractBaseMember.CreateOptions(Name));
        }
        return new BoundPropertyAccess(property.Syntax!, property.Receiver, Property, property.ContainingType,
            Getter, property.Type!);
    }

    /* A property's accessor of a keyword: its own, or for an override declaring none there, the one of what it
     * overrides, as in C#. In place, "set" and "init" are one place, the setter's. */
    private PackFunction? FindAccessor(BoundPropertyAccess property, string keyword, bool isInPlace)
    {
        PackProperty Property = property.Property;
        PackFunction? Own = isInPlace ? InheritedMembers.GetAccessorInPlace(Property, keyword)
            : InheritedMembers.GetAccessor(Property, keyword);
        if ((Own != null) || !Property.HasModifier(PackMemberModifiers.Override)
            || (property.ContainingType == null))
        {
            return Own;
        }

        DeclaredSignature Signature = Resolution.SignatureReader.Read(Property,
            TypeSubstitution.Of(property.ContainingType));
        return InheritedMembers.FindAccessor(Signature, property.ContainingType, keyword, isInPlace, _body.Member,
            Resolution);
    }

    /* An accessor of an abstract property, which has no body for "base" to run. */
    private bool IsAbstractAccessor(PackFunction accessor)
    {
        return (MemberRelations.GetHoldingMember(accessor) is PackMember Holder)
            && MemberRelations.IsAbstract(Holder);
    }

    /* Whether a member is being given a value while what it belongs to is being made: in a constructor of the
     * type declaring it, static for a static member, and for an instance member on the object being made; and
     * for an instance member, when allowed, in an "init" accessor of that type. */
    private bool IsBeingMade(PackMember member, BoundExpression? receiver, bool isInitAccessorAllowed)
    {
        PackFunction? Function = _body.Function;
        if ((Function == null) || !ReferenceEquals(_body.ContainingType?.Declaration,
            MemberRelations.GetHoldingMember(member)))
        {
            return false;
        }

        bool IsStaticMember = MemberRelations.IsWrittenStatic(member);
        bool IsConstructor = (Function is PackConstructor)
            && (MemberRelations.IsWrittenStatic(Function) == IsStaticMember);
        bool IsInitAccessor = isInitAccessorAllowed && !IsStaticMember && MemberRelations.IsAccessor(Function)
            && (Function.SelfIdentifier.SourceCodeName == KGVL.KEYWORD_INIT);
        return (IsConstructor || IsInitAccessor) && (IsStaticMember || (receiver is BoundThisReference));
    }

    /* As in C#, an "init" setter works while an object is being made: in a constructor of the type declaring the
     * property or of one deriving from it, or in any "init" accessor of such a type, on the object being made,
     * named as "this" or "base". An object initializer is the other place, which binds it on its own. */
    private bool IsInitSetterUsable(PackProperty property, BoundExpression? receiver)
    {
        PackFunction? Function = _body.Function;
        PackMember? Holder = MemberRelations.GetHoldingMember(property);
        if ((Function == null) || (Holder == null) || (_body.ContainingType == null)
            || MemberRelations.IsWrittenStatic(Function)
            || !MemberRelations.IsDerivedOrSame(_body.ContainingType.Declaration, Holder, _body.Context.Bases))
        {
            return false;
        }

        bool IsInitAccessor = MemberRelations.IsAccessor(Function)
            && (Function.SelfIdentifier.SourceCodeName == KGVL.KEYWORD_INIT);
        return ((Function is PackConstructor) || IsInitAccessor)
            && (receiver is BoundThisReference or BoundBaseReference);
    }

    /* A member of a value type's value can only be given a value where that value is stored somewhere it can
     * change: a local, a parameter not passed "in", a field which is not readonly, or "this" of a structure in
     * code which may change it. The first link of the receivers which is none of those is reported, as C#
     * reports it. */
    private BoundExpression CheckStorage(BoundExpression target, BoundExpression? receiver)
    {
        string Name = DescribeValue(target);
        BoundExpression? Current = receiver;
        while ((Current?.Type != null) && Current.Type.IsValueType)
        {
            switch (Current)
            {
                case BoundLocal:
                    return target;

                case BoundParameter Parameter when Parameter.Parameter.Modifiers == FunctionParameterModifier.In:
                    return Reject(target, ErrorCreator.ReadOnlyParameterMemberAssigned.CreateOptions(
                        Parameter.Parameter.SelfIdentifier.SourceCodeName));

                case BoundParameter:
                    return target;

                case BoundThisReference when _body.IsThisReadOnly:
                    return Reject(target, ErrorCreator.ReadOnlyThisAssigned.CreateOptions(Name));

                case BoundThisReference:
                    return target;

                case BoundFieldAccess Field when Field.Field.HasModifier(PackMemberModifiers.Readonly)
                    && !IsBeingMade(Field.Field, Field.Receiver, true):
                    string FieldName = MemberRelations.GetQualifiedDisplayName(Field.Field);
                    return Reject(target, MemberRelations.IsStaticMember(Field.Field)
                        ? ErrorCreator.StaticReadOnlyFieldMemberModified.CreateOptions(FieldName)
                        : ErrorCreator.ReadOnlyFieldMemberModified.CreateOptions(FieldName));

                case BoundFieldAccess Field:
                    Current = Field.Receiver;
                    break;

                case BoundPropertyAccess Property:
                    return Reject(target, ErrorCreator.ModifiedCopy.CreateOptions(
                        MemberRelations.GetQualifiedDisplayName(Property.Property)));

                default:
                    return Reject(target, ErrorCreator.NotAssignable.CreateOptions());
            }
        }
        return target;
    }

    /* A field or property given a value, as messages name it. */
    private string DescribeValue(BoundExpression value)
    {
        return value switch
        {
            BoundFieldAccess Field => MemberRelations.GetQualifiedDisplayName(Field.Field),
            BoundPropertyAccess Property => MemberRelations.GetQualifiedDisplayName(Property.Property),
            _ => value.Type?.ToString() ?? string.Empty
        };
    }

    /* What a type standing where a value is needed is, as messages call it. */
    private string DescribeTypeKind(SemanticType type)
    {
        return (type is GenericParameterType) ? KGVL.NAME_GENERIC_PARAMETER : KGVL.NAME_TYPE;
    }

    /* Reports a mistake in what is bound, which is then a value with errors. */
    private BoundExpression Reject(BoundExpression bound, ErrorCreateOptions error)
    {
        _body.AddError(error, bound.Syntax!);
        return new BoundBadExpression(bound.Syntax!, bound);
    }

    /* Why a value does not convert implicitly, as C# says it: through an ambiguous user-defined conversion; null
     * where a value type or a generic parameter is needed; a constant which does not fit, when a constant of its
     * type would convert if it did; a conversion which needs a cast; or none at all. */
    private void ReportConversion(BoundExpression value, SemanticType target, Conversion implicitConversion,
        Statement syntax)
    {
        string Source = value.Type?.ToString() ?? string.Empty;
        if (implicitConversion.IsAmbiguous)
        {
            ReportAmbiguousConversion(value, target, implicitConversion, syntax);
            return;
        }
        if ((value is BoundLiteral) && (value.ConstantValue?.Kind == ConstantValueKind.Null))
        {
            _body.AddError((target is GenericParameterType)
                ? ErrorCreator.NullToGenericParameter.CreateOptions(target.ToString())
                : ErrorCreator.NullToValueType.CreateOptions(target.ToString()), syntax);
            return;
        }

        /* An explicit conversion more than one operator could make still exists, as Roslyn sees it, which reports
         * the ambiguity only where the conversion is written. */
        Conversion Explicit = _body.Context.Conversions.ClassifyExplicitOnly(value, target);
        if ((Explicit.Kind == ConversionKind.ExplicitNumeric) && (value.ConstantValue != null)
            && _body.Context.Conversions.IsConstantConvertible(value.Type!, target))
        {
            _body.AddError(ErrorCreator.ConstantDoesNotFit.CreateOptions(value.ConstantValue.ToString(),
                target.ToString()), syntax);
        }
        else if (Explicit.DoesExist || Explicit.IsAmbiguous)
        {
            _body.AddError(ErrorCreator.ImplicitConversionNeedsCast.CreateOptions(Source,
                target.ToString()), syntax);
        }
        else
        {
            _body.AddError(ErrorCreator.NoImplicitConversion.CreateOptions(Source, target.ToString()), syntax);
        }
    }

    private void ReportAmbiguousConversion(BoundExpression value, SemanticType target, Conversion conversion,
        Statement syntax)
    {
        _body.AddError(ErrorCreator.AmbiguousUserConversion.CreateOptions(value.Type?.ToString() ?? string.Empty,
            target.ToString(), DescribeOperator(conversion.AmbiguousMethods[0]),
            DescribeOperator(conversion.AmbiguousMethods[1])), syntax);
    }

    /* A conversion operator as messages name it, after its type and with what it converts from, since two can
     * differ only in that: "Meters.implicit operator Meters(int)". */
    private string DescribeOperator(PackFunction conversion)
    {
        return MemberRelations.GetQualifiedDisplayName(conversion) + KGVL.OPEN_PARENTHESIS
            + (conversion.Parameters.FirstOrDefault()?.Type?.ToString() ?? string.Empty) + KGVL.CLOSE_PARENTHESIS;
    }
}
