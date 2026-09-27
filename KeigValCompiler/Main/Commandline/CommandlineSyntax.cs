namespace KeigValCompiler.Main.Commandline;

/* The syntax of the compiler's own command line. Kept apart from KGVL, which holds the syntax of the
 * language being compiled rather than of the arguments the compiler is started with. */
internal static class CommandlineSyntax
{
    public const string LONG_NAME_PREFIX = "--";
    public const char SHORT_NAME_PREFIX = '-';
    public const char NAME_WORD_SEPARATOR = '-';
    public const char VALUE_SEPARATOR = '=';
    public const char VALUE_NAME_START = '<';
    public const char VALUE_NAME_END = '>';

    /* Every token after this one is positional, even one starting with '-', which is the only way to pass
     * a positional value that would otherwise be read as an argument name. */
    public const string NAMED_ARGUMENTS_END = "--";

    /* A short name is the prefix followed by exactly one character, like "-h". */
    public const int SHORT_NAME_TOKEN_LENGTH = 2;
}
