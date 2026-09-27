namespace KeigValCompiler.Semantician;

/* What made a text fail to parse as a TwoIntDecimal. The parser reports this rather than a message so
 * that each caller can say it in its own terms: the compiler maps it onto its own error definitions,
 * and TwoIntDecimal.Parse puts it into an exception. */
internal enum DecimalParseErrorKind
{
    /* Nothing went wrong. */
    None,

    /* The text was null, empty, or nothing but white space. */
    Empty,

    /* There is no digit in the number itself, as in "." or "-e5". */
    MissingDigits,

    /* A second decimal point, as in "1.2.3". */
    MultipleSeparators,

    /* An exponent symbol with no digits after it, as in "1e" or "1.5e+". */
    MissingExponentDigits,

    /* A character that has no place in a number where it stands, as in "1.5x" or "1-2". */
    UnexpectedCharacter,

    /* A digit separator that is not between two digits, as in "1_.5" or "1_". Only reported when
     * separators are allowed at all; otherwise an underscore is an unexpected character. */
    MisplacedDigitSeparator,

    /* The number is larger in magnitude than the largest finite value, as in "1e1000000000". */
    Overflow,

    /* The number is not zero but is closer to zero than the smallest one the format can hold, as in
     * "1e-1000000000", so it could only become zero. */
    Underflow
}
