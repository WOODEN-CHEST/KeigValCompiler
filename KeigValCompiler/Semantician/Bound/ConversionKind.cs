namespace KeigValCompiler.Semantician.Bound;

/* How a value of one type becomes a value of another, as C# classifies conversions. Those between the
 * built-in numeric types, and wrapping and unwrapping a Nullable, are the operators the standard library
 * declares on its built-in types, which stand in for C#'s predefined conversions; the rest are rules of the
 * language. A conversion involving a generic parameter is a reference conversion when the parameter is known
 * to be a reference type, and boxing or unboxing otherwise, as Roslyn classifies it. */
internal enum ConversionKind
{
    /* No conversion exists. */
    None,

    Identity,

    /* Through a library operator between two built-in numeric types: the integers, char and decimal. */
    ImplicitNumeric,
    ExplicitNumeric,

    /* A constant int to a smaller integer type, or a constant long to ulong, whose value fits. */
    ImplicitConstant,

    /* A constant zero to an enum, implicitly; between an enum and a numeric type or another enum, explicitly. */
    ImplicitEnumeration,
    ExplicitEnumeration,

    /* To or from a Nullable, of the same value type or of one the value type converts to. */
    ImplicitNullable,
    ExplicitNullable,

    /* The literal null, to a reference type or a Nullable. */
    NullLiteral,

    /* "default" with no type of its own, to any type. */
    DefaultLiteral,

    ImplicitReference,
    ExplicitReference,
    Boxing,
    Unboxing,

    /* Through a conversion operator a type declares, with a standard conversion before and after it. */
    ImplicitUserDefined,
    ExplicitUserDefined
}
