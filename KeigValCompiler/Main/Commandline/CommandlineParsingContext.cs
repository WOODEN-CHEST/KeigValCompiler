using KeigValCompiler.Error;

namespace KeigValCompiler.Main.Commandline;

internal class CommandlineParsingContext
{
    // Fields.
    internal required ErrorRepository ErrorCreator { get; init; }
    internal required CompilerMessageCollection Messages { get; init; }


    // Methods.
    /* A mistake on the command line has no place in any source file to point at. */
    internal void AddError(ErrorCreateOptions error)
    {
        Messages.AddError(error, CompilerMessageLocation.None, null);
    }
}
