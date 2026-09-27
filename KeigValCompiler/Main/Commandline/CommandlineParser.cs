using KeigValCompiler.Error;

namespace KeigValCompiler.Main.Commandline;

/* Reads a command line against the arguments registered in a repository. Like the source parser, it
 * reports every mistake it finds rather than stopping at the first one.
 * Understood forms are "--name", "-n", "--name value", "--name=value" and "-n value", with "--" making
 * every token after it positional. Several short names combined into one token, like "-abc", are not. */
internal class CommandlineParser
{
    // Private fields.
    private ErrorRepository ErrorCreator => _context.ErrorCreator;

    private readonly CommandlineArgumentRepository _repository;
    private readonly CommandlineParsingContext _context;


    // Constructors.
    internal CommandlineParser(CommandlineArgumentRepository repository, CommandlineParsingContext context)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }


    // Methods.
    internal CommandlineParseResult Parse(string[] args)
    {
        ArgumentNullException.ThrowIfNull(args, nameof(args));

        CommandlineParseResult Result = new();
        Queue<string> RemainingTokens = new(args);
        int PositionalArgumentCount = 0;
        bool IsNamedArgumentsEndReached = false;

        while (RemainingTokens.Count > 0)
        {
            string Token = RemainingTokens.Dequeue();

            if (!IsNamedArgumentsEndReached && (Token == CommandlineSyntax.NAMED_ARGUMENTS_END))
            {
                IsNamedArgumentsEndReached = true;
            }
            else if (!IsNamedArgumentsEndReached && IsNamedArgumentToken(Token))
            {
                ParseNamedArgument(Token, RemainingTokens, Result);
            }
            else
            {
                ParsePositionalArgument(Token, PositionalArgumentCount, Result);
                PositionalArgumentCount++;
            }
        }

        ReportMissingPositionalArguments(PositionalArgumentCount);
        return Result;
    }


    // Private methods.
    /* A lone '-' is left positional, since by convention it is a value standing for the standard input or
     * output rather than a name. */
    private bool IsNamedArgumentToken(string token)
    {
        return (token.Length > 1) && (token[0] == CommandlineSyntax.SHORT_NAME_PREFIX);
    }

    private void ParseNamedArgument(string token, Queue<string> remainingTokens, CommandlineParseResult result)
    {
        string NameToken = token;
        string? InlineValue = null;
        CommandlineNamedArgument? Argument = null;

        if (token.StartsWith(CommandlineSyntax.LONG_NAME_PREFIX, StringComparison.Ordinal))
        {
            int SeparatorIndex = token.IndexOf(CommandlineSyntax.VALUE_SEPARATOR);
            if (SeparatorIndex != -1)
            {
                NameToken = token[..SeparatorIndex];
                InlineValue = token[(SeparatorIndex + 1)..];
            }
            Argument = _repository.FindNamedArgument(NameToken[CommandlineSyntax.LONG_NAME_PREFIX.Length..]);
        }
        else if (token.Length == CommandlineSyntax.SHORT_NAME_TOKEN_LENGTH)
        {
            Argument = _repository.FindNamedArgument(token[^1]);
        }

        switch (Argument)
        {
            case null:
                _context.AddError(ErrorCreator.CommandlineUnknownArgument.CreateOptions(NameToken));
                break;

            case CommandlineFlag Flag:
                ParseFlag(Flag, InlineValue, result);
                break;

            case CommandlineOption Option:
                ParseOption(Option, InlineValue, remainingTokens, result);
                break;

            default:
                throw new InvalidOperationException($"The command line parser has no way of reading the argument "
                    + $"\"{Argument.DisplayName}\", as it is of the unknown kind {Argument.GetType().Name}.");
        }
    }

    private void ParseFlag(CommandlineFlag flag, string? inlineValue, CommandlineParseResult result)
    {
        if (inlineValue != null)
        {
            _context.AddError(ErrorCreator.CommandlineFlagGivenValue.CreateOptions(flag.DisplayName, inlineValue));
            return;
        }
        if (result.IsPresent(flag))
        {
            _context.AddError(ErrorCreator.CommandlineRepeatedArgument.CreateOptions(flag.DisplayName));
            return;
        }

        result.MarkPresent(flag);
    }

    /* The value is either written into the same token after '=', or is the whole of the next token. A next
     * token which looks like a name is not taken as the value, so that a forgotten value is reported as
     * missing instead of quietly swallowing the argument after it. */
    private void ParseOption(CommandlineOption option,
        string? inlineValue,
        Queue<string> remainingTokens,
        CommandlineParseResult result)
    {
        string? ValueText = inlineValue;
        if ((ValueText == null) && (remainingTokens.Count > 0) && !IsNamedArgumentToken(remainingTokens.Peek()))
        {
            ValueText = remainingTokens.Dequeue();
        }

        if (string.IsNullOrEmpty(ValueText))
        {
            _context.AddError(ErrorCreator.CommandlineMissingOptionValue
                .CreateOptions(option.DisplayName, option.ValueName));
            return;
        }
        if (result.IsPresent(option) && !option.IsRepeatable)
        {
            _context.AddError(ErrorCreator.CommandlineRepeatedArgument.CreateOptions(option.DisplayName));
            return;
        }

        result.MarkPresent(option);
        if (option.TryParseValue(ValueText, _context, out object? Value))
        {
            result.AddValue(option, Value);
        }
    }

    private void ParsePositionalArgument(string token, int position, CommandlineParseResult result)
    {
        CommandlinePositionalArgument? Argument = _repository.GetPositionalArgument(position);
        if (Argument == null)
        {
            _context.AddError(ErrorCreator.CommandlineUnexpectedPositionalArgument.CreateOptions(token));
            return;
        }

        result.MarkPresent(Argument);
        if (Argument.TryParseValue(token, _context, out object? Value))
        {
            result.AddValue(Argument, Value);
        }
    }

    private void ReportMissingPositionalArguments(int givenCount)
    {
        for (int Position = givenCount; Position < _repository.PositionalArguments.Count; Position++)
        {
            _context.AddError(ErrorCreator.CommandlineMissingPositionalArgument
                .CreateOptions(_repository.PositionalArguments[Position].DisplayName));
        }
    }
}
