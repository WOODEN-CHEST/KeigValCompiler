namespace KeigValCompiler.Semantician.Library;

/* Member names which more than one binding file needs, because the language fixes them rather than
 * the type declaring them: members every type inherits from Object, and members an interface asks
 * for. A name which only one family of types uses is a constant in that family's binding file. */
internal static class LibraryMemberNames
{
    // Static fields.
    /* From Object. */
    internal const string EQUALS = "Equals";
    internal const string GET_HASH_CODE = "GetHashCode";
    internal const string TO_STRING = "ToString";

    /* From IParsable and IComparable. */
    internal const string PARSE = "Parse";
    internal const string TRY_PARSE = "TryParse";
    internal const string COMPARE_TO = "CompareTo";
}
