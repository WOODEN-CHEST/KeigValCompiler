using KeigValCompiler.Main.Commandline;

namespace KeigValCompiler.Main;

internal class CompilerOptions
{
    // Internal fields.
    internal string SourceDirectory { get; private init; }
    internal string DestinationDirectory { get; private init; }


    // Constructors.
    /* Only to be built from a command line which parsed with no errors, since that is what guarantees
     * every required argument is present and every given value is valid. */
    internal CompilerOptions(CommandlineParseResult parseResult, CompilerArguments arguments)
    {
        ArgumentNullException.ThrowIfNull(parseResult, nameof(parseResult));
        ArgumentNullException.ThrowIfNull(arguments, nameof(arguments));

        SourceDirectory = parseResult.GetValue(arguments.SourceDirectory);
        DestinationDirectory = parseResult.GetValueOrDefault(arguments.Output, SourceDirectory);
    }
}
