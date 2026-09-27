using System.Diagnostics.CodeAnalysis;

namespace KeigValCompiler.Main.Commandline;

/* Turns the text given for an argument into the value it stands for. Checking the value is part of
 * reading it, so a parser of directory paths is also where a missing directory gets reported. */
internal interface ICommandlineValueParser<T>
{
    /* When the text is not a valid value, the error saying why is queued into the context and false is
     * returned. The argument is only passed so that the error can name it. */
    bool TryParseValue(string text,
        CommandlineArgument argument,
        CommandlineParsingContext context,
        [MaybeNullWhen(false)] out T value);
}
