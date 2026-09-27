namespace KeigValCompiler.Main.Commandline;

/* An argument found by its name rather than its position, written as "--long-name" or, when it has
 * one, as the single character short name "-s". */
internal abstract class CommandlineNamedArgument : CommandlineArgument
{
    // Fields.
    internal string LongName { get; private init; }
    internal char? ShortName { get; private init; }
    internal override string DisplayName => $"{CommandlineSyntax.LONG_NAME_PREFIX}{LongName}";


    // Constructors.
    internal CommandlineNamedArgument(string longName, char? shortName, string description) : base(description)
    {
        LongName = longName ?? throw new ArgumentNullException(nameof(longName));
        ShortName = shortName;

        if (!IsValidLongName(longName))
        {
            throw new ArgumentException($"\"{longName}\" is not a valid long argument name. It must be made "
                + $"of ASCII letters and digits, with words separated by '{CommandlineSyntax.NAME_WORD_SEPARATOR}'.",
                nameof(longName));
        }
        if (shortName.HasValue && !char.IsAsciiLetterOrDigit(shortName.Value))
        {
            throw new ArgumentException($"'{shortName}' is not a valid short argument name. It must be a single "
                + "ASCII letter or digit.", nameof(shortName));
        }
    }


    // Private static methods.
    /* A name which could not be told apart from the syntax around it, like one starting with '-' or
     * containing '=', would make the argument impossible to give. */
    private static bool IsValidLongName(string longName)
    {
        if ((longName.Length == 0) || (longName[0] == CommandlineSyntax.NAME_WORD_SEPARATOR)
            || (longName[^1] == CommandlineSyntax.NAME_WORD_SEPARATOR))
        {
            return false;
        }

        foreach (char Character in longName)
        {
            if (!char.IsAsciiLetterOrDigit(Character) && (Character != CommandlineSyntax.NAME_WORD_SEPARATOR))
            {
                return false;
            }
        }
        return true;
    }
}
