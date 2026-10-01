using KeigValCompiler.Semantician.Library;
using KeigValCompiler.Semantician.Types;

namespace KeigValCompiler.Semantician.Binding;

/* What the built-in numeric types are, as conversions and constants need to know them: the eight integer types,
 * char and decimal, and the range of values each integer type and char holds. */
internal static class NumericTypes
{
    // Internal static methods.
    /* One of the integer types, char or decimal, between which the library declares conversions. */
    internal static bool IsNumeric(SemanticType type)
    {
        ArgumentNullException.ThrowIfNull(type, nameof(type));

        LibraryType? Known = GetLibraryType(type);
        return (Known != null) && (LibraryTypes.Integers.Contains(Known) || (Known == LibraryTypes.Char)
            || (Known == LibraryTypes.Decimal));
    }

    /* One of the eight integer types, char not included. */
    internal static bool IsInteger(SemanticType type)
    {
        ArgumentNullException.ThrowIfNull(type, nameof(type));

        LibraryType? Known = GetLibraryType(type);
        return (Known != null) && LibraryTypes.Integers.Contains(Known);
    }

    internal static bool IsLibraryType(SemanticType type, LibraryType libraryType)
    {
        ArgumentNullException.ThrowIfNull(type, nameof(type));
        ArgumentNullException.ThrowIfNull(libraryType, nameof(libraryType));

        return GetLibraryType(type) == libraryType;
    }

    /* The smallest and the largest value an integer type or char holds. */
    internal static (Int128 Minimum, Int128 Maximum) GetRange(LibraryType integerType)
    {
        ArgumentNullException.ThrowIfNull(integerType, nameof(integerType));

        if (integerType == LibraryTypes.Int8)
        {
            return (sbyte.MinValue, sbyte.MaxValue);
        }
        if (integerType == LibraryTypes.UInt8)
        {
            return (byte.MinValue, byte.MaxValue);
        }
        if (integerType == LibraryTypes.Int16)
        {
            return (short.MinValue, short.MaxValue);
        }
        if ((integerType == LibraryTypes.UInt16) || (integerType == LibraryTypes.Char))
        {
            return (ushort.MinValue, ushort.MaxValue);
        }
        if (integerType == LibraryTypes.Int32)
        {
            return (int.MinValue, int.MaxValue);
        }
        if (integerType == LibraryTypes.UInt32)
        {
            return (uint.MinValue, uint.MaxValue);
        }
        if (integerType == LibraryTypes.Int64)
        {
            return (long.MinValue, long.MaxValue);
        }
        if (integerType == LibraryTypes.UInt64)
        {
            return (ulong.MinValue, ulong.MaxValue);
        }
        throw new ArgumentException($"\"{integerType}\" is neither an integer type nor char.",
            nameof(integerType));
    }

    /* Whether a value fits in an integer type or char. */
    internal static bool IsInRange(Int128 value, LibraryType integerType)
    {
        (Int128 Minimum, Int128 Maximum) = GetRange(integerType);
        return (value >= Minimum) && (value <= Maximum);
    }

    /* The type a built-in type is known as, or null for any other type. */
    internal static LibraryType? GetLibraryType(SemanticType type)
    {
        return (type as DeclaredType)?.LibraryType;
    }
}
