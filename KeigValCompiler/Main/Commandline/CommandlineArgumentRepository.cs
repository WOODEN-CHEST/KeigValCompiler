namespace KeigValCompiler.Main.Commandline;

/* Every argument a program accepts, which is all CommandlineParser and CommandlineHelpPrinter go by.
 * Registering an argument which clashes with one already registered is a mistake in the compiler rather
 * than on the command line, so it throws instead of queueing an error. */
internal class CommandlineArgumentRepository
{
    // Fields.
    /* In the order they were registered. */
    internal IReadOnlyList<CommandlineNamedArgument> NamedArguments => _namedArguments;

    /* In the order they are expected in on the command line. */
    internal IReadOnlyList<CommandlinePositionalArgument> PositionalArguments => _positionalArguments;


    // Private fields.
    private readonly List<CommandlineNamedArgument> _namedArguments = new();
    private readonly List<CommandlinePositionalArgument> _positionalArguments = new();
    private readonly Dictionary<string, CommandlineNamedArgument> _namedArgumentsByLongName
        = new(StringComparer.Ordinal);
    private readonly Dictionary<char, CommandlineNamedArgument> _namedArgumentsByShortName = new();


    // Methods.
    internal void Register(CommandlineNamedArgument argument)
    {
        ArgumentNullException.ThrowIfNull(argument, nameof(argument));

        if (_namedArgumentsByLongName.ContainsKey(argument.LongName))
        {
            throw new ArgumentException($"An argument named \"{argument.DisplayName}\" is already registered.",
                nameof(argument));
        }
        if (argument.ShortName.HasValue && _namedArgumentsByShortName.ContainsKey(argument.ShortName.Value))
        {
            throw new ArgumentException($"An argument with the short name '{argument.ShortName}' is already "
                + "registered.", nameof(argument));
        }

        _namedArguments.Add(argument);
        _namedArgumentsByLongName.Add(argument.LongName, argument);
        if (argument.ShortName.HasValue)
        {
            _namedArgumentsByShortName.Add(argument.ShortName.Value, argument);
        }
    }

    /* Positional arguments are expected in the order they are registered in. */
    internal void Register(CommandlinePositionalArgument argument)
    {
        ArgumentNullException.ThrowIfNull(argument, nameof(argument));

        if (_positionalArguments.Contains(argument))
        {
            throw new ArgumentException($"The argument {argument.DisplayName} is already registered.",
                nameof(argument));
        }

        _positionalArguments.Add(argument);
    }

    internal CommandlineNamedArgument? FindNamedArgument(string longName)
    {
        ArgumentNullException.ThrowIfNull(longName, nameof(longName));

        _namedArgumentsByLongName.TryGetValue(longName, out CommandlineNamedArgument? Argument);
        return Argument;
    }

    internal CommandlineNamedArgument? FindNamedArgument(char shortName)
    {
        _namedArgumentsByShortName.TryGetValue(shortName, out CommandlineNamedArgument? Argument);
        return Argument;
    }

    /* Null when more positional arguments are given than there are registered. */
    internal CommandlinePositionalArgument? GetPositionalArgument(int position)
    {
        if ((position < 0) || (position >= _positionalArguments.Count))
        {
            return null;
        }
        return _positionalArguments[position];
    }
}
