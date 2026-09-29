namespace KeigValCompiler.Semantician.Member;

/* The keyword source code writes for each modifier, the one table of them, which the parser reads
 * modifiers by and messages name them by, as they are written rather than by their enum names. */
internal static class ModifierKeywords
{
    // Static fields.
    private const string SEPARATOR = ", ";

    /* In the order messages list them: access first, then roughly the order C# writes the rest in. */
    private static readonly (PackMemberModifiers Modifier, string Keyword)[] _keywords = new
        (PackMemberModifiers, string)[]
    {
        (PackMemberModifiers.Public, KGVL.KEYWORD_PUBLIC),
        (PackMemberModifiers.Protected, KGVL.KEYWORD_PROTECTED),
        (PackMemberModifiers.Internal, KGVL.KEYWORD_INTERNAL),
        (PackMemberModifiers.Private, KGVL.KEYWORD_PRIVATE),
        (PackMemberModifiers.New, KGVL.KEYWORD_NEW),
        (PackMemberModifiers.Static, KGVL.KEYWORD_STATIC),
        (PackMemberModifiers.Abstract, KGVL.KEYWORD_ABSTRACT),
        (PackMemberModifiers.Virtual, KGVL.KEYWORD_VIRTUAL),
        (PackMemberModifiers.Override, KGVL.KEYWORD_OVERRIDE),
        (PackMemberModifiers.Sealed, KGVL.KEYWORD_SEALED),
        (PackMemberModifiers.Readonly, KGVL.KEYWORD_READONLY),
        (PackMemberModifiers.Const, KGVL.KEYWORD_CONST),
        (PackMemberModifiers.Required, KGVL.KEYWORD_REQUIRED),
        (PackMemberModifiers.Record, KGVL.KEYWORD_RECORD),
        (PackMemberModifiers.BuiltIn, KGVL.KEYWORD_BUILTIN),
        (PackMemberModifiers.Inline, KGVL.KEYWORD_INLINE)
    };


    // Internal static methods.
    /* The keyword of a single modifier. */
    internal static string GetKeyword(PackMemberModifiers modifier)
    {
        foreach ((PackMemberModifiers Modifier, string Keyword) in _keywords)
        {
            if (Modifier == modifier)
            {
                return Keyword;
            }
        }
        throw new ArgumentException($"{modifier} is not a single modifier.", nameof(modifier));
    }

    /* The modifier a keyword writes, or None for a word which is not one. */
    internal static PackMemberModifiers GetModifier(string keyword)
    {
        ArgumentNullException.ThrowIfNull(keyword, nameof(keyword));

        foreach ((PackMemberModifiers Modifier, string Keyword) in _keywords)
        {
            if (Keyword == keyword)
            {
                return Modifier;
            }
        }
        return PackMemberModifiers.None;
    }

    /* Each modifier in a combination of them, one at a time, in the order messages list them. */
    internal static IEnumerable<PackMemberModifiers> Split(PackMemberModifiers modifiers)
    {
        foreach ((PackMemberModifiers Modifier, string Keyword) in _keywords)
        {
            if ((modifiers & Modifier) != PackMemberModifiers.None)
            {
                yield return Modifier;
            }
        }
    }

    /* The keywords of a combination of modifiers, as in "public, static, readonly". */
    internal static string FormatList(PackMemberModifiers modifiers)
    {
        return string.Join(SEPARATOR, Split(modifiers).Select(GetKeyword));
    }
}
