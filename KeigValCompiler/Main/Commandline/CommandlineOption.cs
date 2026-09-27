namespace KeigValCompiler.Main.Commandline;

/* A named argument which carries a value, written as "--name value" or "--name=value". Only
 * CommandlineOption<T> knows what type that value is; this is the part of it the parser works with. */
internal abstract class CommandlineOption : CommandlineNamedArgument
{
    // Fields.
    /* What the value is called in the help text and in error messages, like the "directory" in
     * "--output <directory>". */
    internal string ValueName { get; private init; }

    /* A repeatable option may be given any number of times, each time adding one more value. Any other
     * option given twice is an error, since there would be no telling which of the two values was meant. */
    internal bool IsRepeatable { get; private init; }


    // Constructors.
    internal CommandlineOption(string longName,
        char? shortName,
        string valueName,
        bool isRepeatable,
        string description)
        : base(longName, shortName, description)
    {
        ValueName = valueName ?? throw new ArgumentNullException(nameof(valueName));
        IsRepeatable = isRepeatable;
    }


    // Methods.
    /* When the text is not a valid value, the error saying why is queued into the context and false is
     * returned. */
    internal abstract bool TryParseValue(string text, CommandlineParsingContext context, out object? value);
}
