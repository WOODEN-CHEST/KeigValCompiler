using KeigValCompiler.Semantician.Bound;
using KeigValCompiler.Semantician.Library;
using KeigValCompiler.Semantician.Types;

namespace KeigValCompiler.Semantician.Binding;

/* Works out constants as C# does, and as the runtime would compute them, decimals with TwoIntDecimal. */
internal static class ConstantFolder
{
    // Internal static methods.
    /* A constant converted implicitly, which is still a constant when C# says so: through the identity, a numeric
     * conversion, a constant's own conversion to a smaller integer type or a zero to an enum, the literal null to
     * a reference type, and a reference conversion of a constant which is null. Null when the result is no
     * constant. An implicit numeric conversion never loses
     * the value, so it is only given its new type, or for a decimal, the decimal it is. */
    internal static ConstantValue? FoldImplicitConversion(ConstantValue? value, Conversion conversion,
        SemanticType target, bool isTargetReference)
    {
        ArgumentNullException.ThrowIfNull(conversion, nameof(conversion));
        ArgumentNullException.ThrowIfNull(target, nameof(target));

        if (value == null)
        {
            return null;
        }
        switch (conversion.Kind)
        {
            case ConversionKind.Identity:
                return value;

            case ConversionKind.ImplicitConstant:
            case ConversionKind.ImplicitEnumeration:
                return ConstantValue.FromInteger(value.Integer);

            case ConversionKind.ImplicitNumeric:
                return FoldWideningNumeric(value, target);

            case ConversionKind.NullLiteral:
                return isTargetReference ? ConstantValue.Null : null;

            case ConversionKind.ImplicitReference:
                return (value.Kind == ConstantValueKind.Null) ? value : null;

            default:
                return null;
        }
    }


    // Private static methods.
    private static ConstantValue? FoldWideningNumeric(ConstantValue value, SemanticType target)
    {
        if ((value.Kind != ConstantValueKind.Integer) && (value.Kind != ConstantValueKind.Char))
        {
            return null;
        }
        if (NumericTypes.IsLibraryType(target, LibraryTypes.Decimal))
        {
            /* Only the 8 and 16 bit integers and char convert to a decimal implicitly, so the value
             * fits an int. */
            return ConstantValue.FromDecimal(new TwoIntDecimal((int)value.Integer));
        }
        return NumericTypes.IsInteger(target) ? ConstantValue.FromInteger(value.Integer) : null;
    }
}
