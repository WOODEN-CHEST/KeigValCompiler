namespace KeigValCompiler.Main.Commandline;

internal class CommandlinePositionalArgument<T> : CommandlinePositionalArgument
{
    // Private fields.
    private readonly ICommandlineValueParser<T> _valueParser;


    // Constructors.
    internal CommandlinePositionalArgument(string name, ICommandlineValueParser<T> valueParser, string description)
        : base(name, description)
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
