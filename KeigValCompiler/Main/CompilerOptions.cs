using KeigValCompiler.Main.Commandline;

namespace KeigValCompiler.Main;

internal class CompilerOptions
{
    // Static fields.
    /* The name the standard library's directory has beside the compiler. KeigValCompiler.csproj copies
     * the library there under this name, so the two must be changed together. */
    internal const string DEFAULT_LIBRARY_DIRECTORY_NAME = "library-stubs";


    // Internal fields.
    internal string SourceDirectory { get; private init; }
    internal string DestinationDirectory { get; private init; }
    internal string LibraryDirectory { get; private init; }
    internal bool IsParseOnly { get; private init; }


    // Constructors.
    /* Only to be built from a command line which parsed with no errors, since that is what guarantees
     * every required argument is present and every given value is valid. */
    internal CompilerOptions(CommandlineParseResult parseResult, CompilerArguments arguments)
    {
        ArgumentNullException.ThrowIfNull(parseResult, nameof(parseResult));
        ArgumentNullException.ThrowIfNull(arguments, nameof(arguments));

        SourceDirectory = parseResult.GetValue(arguments.SourceDirectory);
        DestinationDirectory = parseResult.GetValueOrDefault(arguments.Output, SourceDirectory);
        LibraryDirectory = parseResult.GetValueOrDefault(arguments.Library,
            Path.Combine(AppContext.BaseDirectory, DEFAULT_LIBRARY_DIRECTORY_NAME));
        IsParseOnly = parseResult.IsPresent(arguments.ParseOnly);
    }
}
