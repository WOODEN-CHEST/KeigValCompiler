namespace KeigValCompiler.Semantician;

/* Why and where a text failed to parse as a TwoIntDecimal. The default value is the absence of an
 * error. */
internal readonly struct DecimalParseError
{
    // Fields.
    internal DecimalParseErrorKind Kind { get; }

    /* The index into the parsed text of the character at fault. For a missing digit it is where the
     * digit should have been, which can be the length of the text. */
    internal int Index { get; }


    // Constructors.
    internal DecimalParseError(DecimalParseErrorKind kind, int index)
    {
        Kind = kind;
        Index = index;
    }


    // Inherited methods.
    public override string ToString()
    {
        return $"{Kind} at index {Index}";
    }
}
