namespace KeigValCompiler.Main.Commandline;

/* What CommandlineParser read from the command line. Values are read through the argument they were
 * given for, which carries their type, so asking for one never needs a cast.
 * The getters for values are only meant to be used once parsing reported no errors. Before that, an
 * argument can be present with no value, because the value it was given was not valid. */
internal class CommandlineParseResult
{
    // Private fields.
    private readonly HashSet<CommandlineArgument> _presentArguments = new();
    private readonly Dictionary<CommandlineArgument, List<object?>> _valuesByArgument = new();


    // Methods.
    internal bool IsPresent(CommandlineArgument argument)
    {
        ArgumentNullException.ThrowIfNull(argument, nameof(argument));
        return _presentArguments.Contains(argument);
    }

    /* Positional arguments are all required, so once parsing reported no errors each one has a value. */
    internal T GetValue<T>(CommandlinePositionalArgument<T> argument)
    {
        ArgumentNullException.ThrowIfNull(argument, nameof(argument));

        if (!_valuesByArgument.TryGetValue(argument, out List<object?>? Values))
        {
            throw new InvalidOperationException($"The argument {argument.DisplayName} has no value. Values "
                + "are only meant to be read once parsing the command line reported no errors.");
        }
        return (T)Values[0]!;
    }

    internal T GetValueOrDefault<T>(CommandlineOption<T> option, T defaultValue)
    {
        ArgumentNullException.ThrowIfNull(option, nameof(option));

        if (option.IsRepeatable)
        {
            throw new InvalidOperationException($"The option \"{option.DisplayName}\" is repeatable, so it can "
                + $"have more than one value. Read it with {nameof(GetValues)} instead.");
        }
        if (!_valuesByArgument.TryGetValue(option, out List<object?>? Values))
        {
            return defaultValue;
        }
        return (T)Values[0]!;
    }

    /* Every value the option was given, in the order it was given them. Empty when it was not given. */
    internal IReadOnlyList<T> GetValues<T>(CommandlineOption<T> option)
    {
        ArgumentNullException.ThrowIfNull(option, nameof(option));

        if (!_valuesByArgument.TryGetValue(option, out List<object?>? Values))
        {
            return Array.Empty<T>();
        }
        return Values.Select(Value => (T)Value!).ToArray();
    }

    /* Filled in by CommandlineParser. An argument is present as soon as it is given, even if the value it
     * was given turns out not to be valid, so that it is not also reported as missing. */
    internal void MarkPresent(CommandlineArgument argument)
    {
        ArgumentNullException.ThrowIfNull(argument, nameof(argument));
        _presentArguments.Add(argument);
    }

    internal void AddValue(CommandlineArgument argument, object? value)
    {
        ArgumentNullException.ThrowIfNull(argument, nameof(argument));

        if (!_valuesByArgument.TryGetValue(argument, out List<object?>? Values))
        {
            Values = new();
            _valuesByArgument.Add(argument, Values);
        }
        Values.Add(value);
    }
}
