namespace KeigValCompiler.Semantician.Library;

/* Every type the compiler knows by name, one line each. This is the one place in the compiler where
 * a library type's name and namespace are written, so renaming one in the library means changing
 * its line here and nothing else. */
internal static class LibraryTypes
{
    // Static fields.
    /* Namespaces, and the name of the one generic parameter each generic type here has. */
    private const string NAMESPACE_KGVL = "KGVL";
    private const string NAMESPACE_COLLECTIONS_GENERIC = "KGVL.Collections.Generic";
    private const string GENERIC_PARAMETER = "T";

    /* Built in types, which the compiler stores and works on itself. */
    internal static LibraryType Object { get; } = new(NAMESPACE_KGVL, "Object", KGVL.KEYWORD_OBJECT);
    internal static LibraryType Boolean { get; } = new(NAMESPACE_KGVL, "Boolean", KGVL.KEYWORD_BOOL);
    internal static LibraryType Char { get; } = new(NAMESPACE_KGVL, "Char", KGVL.KEYWORD_CHAR);
    internal static LibraryType Int8 { get; } = new(NAMESPACE_KGVL, "Int8", KGVL.KEYWORD_BYTE);
    internal static LibraryType UInt8 { get; } = new(NAMESPACE_KGVL, "UInt8", KGVL.KEYWORD_UBYTE);
    internal static LibraryType Int16 { get; } = new(NAMESPACE_KGVL, "Int16", KGVL.KEYWORD_SHORT);
    internal static LibraryType UInt16 { get; } = new(NAMESPACE_KGVL, "UInt16", KGVL.KEYWORD_USHORT);
    internal static LibraryType Int32 { get; } = new(NAMESPACE_KGVL, "Int32", KGVL.KEYWORD_INT);
    internal static LibraryType UInt32 { get; } = new(NAMESPACE_KGVL, "UInt32", KGVL.KEYWORD_UINT);
    internal static LibraryType Int64 { get; } = new(NAMESPACE_KGVL, "Int64", KGVL.KEYWORD_LONG);
    internal static LibraryType UInt64 { get; } = new(NAMESPACE_KGVL, "UInt64", KGVL.KEYWORD_ULONG);
    internal static LibraryType Decimal { get; } = new(NAMESPACE_KGVL, "Decimal", KGVL.KEYWORD_DECIMAL);
    internal static LibraryType String { get; } = new(NAMESPACE_KGVL, "String", KGVL.KEYWORD_STRING);
    internal static LibraryType Array { get; } = new(NAMESPACE_KGVL, "Array", null, GENERIC_PARAMETER);
    internal static LibraryType Nullable { get; } = new(NAMESPACE_KGVL, "Nullable", null, GENERIC_PARAMETER);

    /* Types the language relies on: foreach walks an IEnumerable through an IEnumerator, and the
     * compiler throws these exceptions from the checks it inserts and from builtin members. */
    internal static LibraryType IEnumerable { get; } =
        new(NAMESPACE_COLLECTIONS_GENERIC, "IEnumerable", null, GENERIC_PARAMETER);
    internal static LibraryType IEnumerator { get; } =
        new(NAMESPACE_COLLECTIONS_GENERIC, "IEnumerator", null, GENERIC_PARAMETER);
    internal static LibraryType Exception { get; } = new(NAMESPACE_KGVL, "Exception", null);
    internal static LibraryType ArgumentNullException { get; } =
        new(NAMESPACE_KGVL, "ArgumentNullException", null);
    internal static LibraryType ArgumentOutOfRangeException { get; } =
        new(NAMESPACE_KGVL, "ArgumentOutOfRangeException", null);
    internal static LibraryType DivideByZeroException { get; } =
        new(NAMESPACE_KGVL, "DivideByZeroException", null);
    internal static LibraryType FormatException { get; } = new(NAMESPACE_KGVL, "FormatException", null);
    internal static LibraryType IndexOutOfRangeException { get; } =
        new(NAMESPACE_KGVL, "IndexOutOfRangeException", null);
    internal static LibraryType InvalidCastException { get; } =
        new(NAMESPACE_KGVL, "InvalidCastException", null);
    internal static LibraryType InvalidOperationException { get; } =
        new(NAMESPACE_KGVL, "InvalidOperationException", null);
    internal static LibraryType NullReferenceException { get; } =
        new(NAMESPACE_KGVL, "NullReferenceException", null);
    internal static LibraryType OverflowException { get; } = new(NAMESPACE_KGVL, "OverflowException", null);

    /* Groups. These come last, since they are built from the types above. */
    internal static IReadOnlyList<LibraryType> Integers { get; } = new LibraryType[]
    {
        Int8, UInt8, Int16, UInt16, Int32, UInt32, Int64, UInt64
    };

    internal static IReadOnlyList<LibraryType> All { get; } = new LibraryType[]
    {
        Object, Boolean, Char, Int8, UInt8, Int16, UInt16, Int32, UInt32, Int64, UInt64, Decimal, String,
        Array, Nullable, IEnumerable, IEnumerator, Exception, ArgumentNullException,
        ArgumentOutOfRangeException, DivideByZeroException, FormatException, IndexOutOfRangeException,
        InvalidCastException, InvalidOperationException, NullReferenceException, OverflowException
    };
}
