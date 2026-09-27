using KeigValCompiler.Main.Commandline;

namespace KeigValCompiler.Main;

/* Every argument the compiler accepts on the command line. Adding one takes a property here, a line
 * registering it in the constructor, and reading its value in CompilerOptions. The help text is built
 * from what is registered, so it needs no changes of its own. */
internal class CompilerArguments
{
    // Fields.
    internal CommandlineArgumentRepository Repository { get; } = new();

    /* Positional. */
    internal CommandlinePositionalArgument<string> SourceDirectory { get; } = new("source directory",
        new DirectoryPathValueParser(isExistenceRequired: true),
        "Directory holding the pack's source files. Its subdirectories are searched as well.");

    /* Named. */
    internal CommandlineFlag Help { get; } = new("help", 'h', "Prints this help text and exits.");

    internal CommandlineOption<string> Output { get; } = new("output", 'o', "directory",
        isRepeatable: false,
        new DirectoryPathValueParser(isExistenceRequired: false),
        "Directory the compiled datapack is to be written to, the source directory if not given. "
            + "Unused for now, as the compiler does not produce any output yet.");


    // Constructors.
    internal CompilerArguments()
    {
        Repository.Register(SourceDirectory);
        Repository.Register(Help);
        Repository.Register(Output);
    }
}
