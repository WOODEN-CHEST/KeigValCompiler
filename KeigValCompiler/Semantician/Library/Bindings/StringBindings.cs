using KeigValCompiler.Semantician.Member;

namespace KeigValCompiler.Semantician.Library.Bindings;

/* KGVL.String. What the library writes in KGVL on top of these, such as StartsWith, != and +, is not
 * here. */
internal class StringBindings : ILibraryBindingProvider
{
    // Static fields.
    private const string NAME_LENGTH = "Length";
    private const string NAME_CONCAT = "Concat";
    private const string NAME_SUBSTRING = "Substring";
    private const string NAME_INDEX_OF = "IndexOf";


    // Inherited methods.
    public void AddBindings(LibraryBindingTable table)
    {
        ArgumentNullException.ThrowIfNull(table, nameof(table));
        SignatureBuilder Member = new(LibraryTypes.String);

        table.AddIntrinsic(Member.Constructor(LibraryTypes.Char, LibraryTypes.Int32),
            IntrinsicOperation.NewRepeatedString);
        table.AddIntrinsic(Member.Constructor(SignatureType.ArrayOf(LibraryTypes.Char)),
            IntrinsicOperation.NewStringFromChars);

        table.AddIntrinsic(Member.PropertyGetter(NAME_LENGTH, LibraryTypes.Int32), IntrinsicOperation.Length);
        table.AddIntrinsic(Member.IndexerGetter(LibraryTypes.Char, LibraryTypes.Int32),
            IntrinsicOperation.GetElement);

        table.AddIntrinsic(Member.StaticMethod(NAME_CONCAT, Member.Self, Member.Self, Member.Self),
            IntrinsicOperation.Concat);
        table.AddIntrinsic(Member.Method(NAME_SUBSTRING, Member.Self, LibraryTypes.Int32, LibraryTypes.Int32),
            IntrinsicOperation.Substring);
        table.AddIntrinsic(Member.Method(NAME_INDEX_OF, LibraryTypes.Int32, LibraryTypes.Char),
            IntrinsicOperation.IndexOf);
        table.AddIntrinsic(Member.Method(NAME_INDEX_OF, LibraryTypes.Int32, Member.Self),
            IntrinsicOperation.IndexOf);

        table.AddIntrinsic(Member.Method(LibraryMemberNames.COMPARE_TO, LibraryTypes.Int32, Member.Self),
            IntrinsicOperation.Compare);
        table.AddIntrinsic(Member.Comparison(OverloadableOperator.Equals), IntrinsicOperation.Equal);
        table.AddIntrinsic(Member.Method(LibraryMemberNames.GET_HASH_CODE, LibraryTypes.Int32),
            IntrinsicOperation.HashCode);
    }
}
