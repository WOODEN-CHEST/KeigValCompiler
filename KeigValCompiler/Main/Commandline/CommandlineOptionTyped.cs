namespace KeigValCompiler.Main.Commandline;

internal class CommandlineOption<T> : CommandlineOption
{
    // Private fields.
    private readonly ICommandlineValueParser<T> _valueParser;


    // Constructors.
    internal CommandlineOption(string longName,
        char? shortName,
        string valueName,
        bool isRepeatable,
        ICommandlineValueParser<T> valueParser,
        string description)
        : base(longName, shortName, valueName, isRepeatable, description)
    {
        _valueParser = valueParser ?? throw new ArgumentNullException(nameof(valueParser));
    }


    // Inherited methods.
    internal override bool TryParseValue(string text, CommandlineParsingContext context, out object? value)
    {
        bool IsValueValid = _valueParser.TryParseValue(text, this, context, out T? ParsedValue);
        value = ParsedValue;
        return IsValueValid;
    }
}
