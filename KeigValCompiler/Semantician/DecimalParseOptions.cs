namespace KeigValCompiler.Semantician;

/* What TwoIntDecimal's parser accepts beyond plain decimal text. The default, None, is the strict form
 * that parsing a string at runtime should use; the compiler opts into what KGVL source literals may
 * also contain. */
[Flags]
internal enum DecimalParseOptions
{
    None = 0,

    /* Underscores between digits, as in 1_000.5, only ever between two digits of the same part. */
    AllowDigitSeparators = 1 << 0
}
