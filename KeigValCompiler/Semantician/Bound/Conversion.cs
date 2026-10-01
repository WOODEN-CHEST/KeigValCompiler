using KeigValCompiler.Semantician.Member;
using KeigValCompiler.Semantician.Types;

namespace KeigValCompiler.Semantician.Bound;

/* A conversion the classifier found from one type to another, or that none exists, and how it goes: the
 * library operator a numeric one, or the unwrapping and wrapping of a nullable one, goes through, the
 * conversion between the underlying types of a nullable one, and for a user-defined one, the operator with the
 * standard conversions before and after it. */
internal sealed class Conversion
{
    // Static fields.
    internal static Conversion None { get; } = new(ConversionKind.None);
    internal static Conversion Identity { get; } = new(ConversionKind.Identity);
    internal static Conversion ImplicitConstant { get; } = new(ConversionKind.ImplicitConstant);
    internal static Conversion ImplicitEnumeration { get; } = new(ConversionKind.ImplicitEnumeration);
    internal static Conversion ExplicitEnumeration { get; } = new(ConversionKind.ExplicitEnumeration);
    internal static Conversion NullLiteral { get; } = new(ConversionKind.NullLiteral);
    internal static Conversion DefaultLiteral { get; } = new(ConversionKind.DefaultLiteral);
    internal static Conversion ImplicitReference { get; } = new(ConversionKind.ImplicitReference);
    internal static Conversion ExplicitReference { get; } = new(ConversionKind.ExplicitReference);
    internal static Conversion Boxing { get; } = new(ConversionKind.Boxing);
    internal static Conversion Unboxing { get; } = new(ConversionKind.Unboxing);


    // Internal fields.
    internal ConversionKind Kind { get; private init; }

    /* The operator the conversion goes through: the library's for a numeric conversion, and for a nullable one
     * the Nullable's own which wraps or unwraps the value, or the one a type declares for a user-defined one.
     * Null for the rest. */
    internal PackFunction? Method { get; private init; }

    /* For a nullable conversion, the conversion between the underlying types, which is the identity when they
     * are the same. */
    internal Conversion? Underlying { get; private init; }

    /* For a user-defined conversion, the standard conversions from the source to what the operator takes, and
     * from what it gives to the target, and the types it converts between, which are the nullable forms of its
     * own when it is lifted. */
    internal Conversion? Before { get; private init; }
    internal Conversion? After { get; private init; }
    internal SemanticType? FromType { get; private init; }
    internal SemanticType? ToType { get; private init; }
    internal bool IsLifted { get; private init; } = false;

    /* For a user-defined conversion through an interface's static abstract or virtual operator, the generic
     * parameter constrained to the interface, whose type argument's operator runs. Null otherwise. */
    internal GenericParameterType? ConstrainedTo { get; private init; }

    /* The operators which fit a user-defined conversion equally well, when none fits best, as C# reports it.
     * The conversion then does not exist. */
    internal IReadOnlyList<PackFunction> AmbiguousMethods { get; private init; } = Array.Empty<PackFunction>();

    internal bool DoesExist => Kind != ConversionKind.None;
    internal bool IsImplicit => Kind is ConversionKind.Identity or ConversionKind.ImplicitNumeric
        or ConversionKind.ImplicitConstant or ConversionKind.ImplicitEnumeration or ConversionKind.ImplicitNullable
        or ConversionKind.NullLiteral or ConversionKind.DefaultLiteral or ConversionKind.ImplicitReference
        or ConversionKind.Boxing or ConversionKind.ImplicitUserDefined;
    internal bool IsExplicit => DoesExist && !IsImplicit;
    internal bool IsUserDefined => Kind is ConversionKind.ImplicitUserDefined
        or ConversionKind.ExplicitUserDefined;
    internal bool IsAmbiguous => AmbiguousMethods.Count > 0;


    // Constructors.
    private Conversion(ConversionKind kind)
    {
        Kind = kind;
    }


    // Internal static methods.
    internal static Conversion Numeric(PackFunction method, bool isImplicit)
    {
        return new(isImplicit ? ConversionKind.ImplicitNumeric : ConversionKind.ExplicitNumeric)
        {
            Method = method ?? throw new ArgumentNullException(nameof(method))
        };
    }

    /* The method is the Nullable's operator which wraps or unwraps the value, or null when the conversion is
     * from one nullable to another, which does both. */
    internal static Conversion Nullable(Conversion underlying, PackFunction? method, bool isImplicit)
    {
        return new(isImplicit ? ConversionKind.ImplicitNullable : ConversionKind.ExplicitNullable)
        {
            Underlying = underlying ?? throw new ArgumentNullException(nameof(underlying)),
            Method = method
        };
    }

    internal static Conversion UserDefined(PackFunction method,
        Conversion before,
        Conversion after,
        SemanticType fromType,
        SemanticType toType,
        bool isLifted,
        GenericParameterType? constrainedTo,
        bool isImplicit)
    {
        return new(isImplicit ? ConversionKind.ImplicitUserDefined : ConversionKind.ExplicitUserDefined)
        {
            Method = method ?? throw new ArgumentNullException(nameof(method)),
            Before = before ?? throw new ArgumentNullException(nameof(before)),
            After = after ?? throw new ArgumentNullException(nameof(after)),
            FromType = fromType ?? throw new ArgumentNullException(nameof(fromType)),
            ToType = toType ?? throw new ArgumentNullException(nameof(toType)),
            IsLifted = isLifted,
            ConstrainedTo = constrainedTo
        };
    }

    internal static Conversion Ambiguous(IReadOnlyList<PackFunction> methods)
    {
        return new(ConversionKind.None)
        {
            AmbiguousMethods = methods ?? throw new ArgumentNullException(nameof(methods))
        };
    }


    // Inherited methods.
    public override string ToString()
    {
        return Kind.ToString();
    }
}
