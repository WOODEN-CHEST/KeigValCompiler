using KeigValCompiler.Error;
using KeigValCompiler.Main;
using KeigValCompiler.Main.Commandline;
using KeigValCompiler.Semantician;
using KeigValCompiler.Semantician.Resolver;
using KeigValCompiler.Source;
using KeigValCompiler.Source.Parser;

namespace KeigValCompilerTest;

/* Runs the compiler inside the tests: over a directory as its command line would, keeping every message
 * rather than printing it, or through resolution alone, keeping what resolution built. */
internal static class TestCompilation
{
    // Internal static methods.
    /* Every message of each stage which ran, as "dotnet run" would have printed them. */
    internal static List<CompilerMessage> Compile(string sourceDirectory,
        string libraryDirectory,
        bool isParseOnly)
    {
        ArgumentNullException.ThrowIfNull(sourceDirectory, nameof(sourceDirectory));
        ArgumentNullException.ThrowIfNull(libraryDirectory, nameof(libraryDirectory));

        ErrorRepository ErrorCreator = new();
        CompilerArguments Arguments = new();
        CompilerMessageCollection CommandlineMessages = new();
        List<string> CommandLine = new() { sourceDirectory, Arguments.Library.DisplayName, libraryDirectory };
        if (isParseOnly)
        {
            CommandLine.Add(Arguments.ParseOnly.DisplayName);
        }

        CommandlineParsingContext ParsingContext = new()
        {
            ErrorCreator = ErrorCreator,
            Messages = CommandlineMessages
        };
        CommandlineParseResult ParseResult = new CommandlineParser(Arguments.Repository, ParsingContext)
            .Parse(CommandLine.ToArray());
        if (CommandlineMessages.HasErrors)
        {
            throw new InvalidOperationException("The command line the tests built did not parse: "
                + string.Join(" ", CommandlineMessages.Select(message => message.ToString())));
        }

        List<CompilerMessage> Messages = new();
        Compiler.CompilePack(new CompilerOptions(ParseResult, Arguments), Arguments, ErrorCreator,
            Messages.AddRange);
        return Messages;
    }

    /* The standard library and one directory of the user's code, parsed and resolved, whatever either
     * reported; the context holds what resolution built, and the messages of both stages. */
    internal static PackResolutionContext Resolve(string libraryDirectory, string sourceDirectory)
    {
        ArgumentNullException.ThrowIfNull(libraryDirectory, nameof(libraryDirectory));
        ArgumentNullException.ThrowIfNull(sourceDirectory, nameof(sourceDirectory));

        ErrorRepository ErrorCreator = new();
        CompilerMessageCollection Messages = new();
        DataPack Pack = new();
        PackParser Parser = new(ErrorCreator, new ParserUtilities(), Messages);
        Parser.ParseDirectory(Pack, libraryDirectory, SourceFileKind.Library);
        Parser.ParseDirectory(Pack, sourceDirectory, SourceFileKind.User);

        PackResolutionContext Context = Compiler.CreateResolutionContext(Pack, ErrorCreator, Messages);
        new FullPackResolver().ResolvePack(Context);
        return Context;
    }
}
