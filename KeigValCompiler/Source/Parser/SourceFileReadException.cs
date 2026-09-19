using KeigValCompiler.Error;

namespace KeigValCompiler.Source.Parser;

/* Thrown when the parser no longer knows where it is in a source file. The exception exists to unwind
 * to the nearest error recovery point; the message it carries is queued there rather than here, so that
 * a caller which catches this on purpose can throw the message away with it. */
internal class SourceFileReadException : Exception
{
    // Fields.
    internal CompilerMessage CompilerMessage { get; private init; }


    // Constructors.
    internal SourceFileReadException(SourceDataParser parser, ErrorCreateOptions? error)
        : this(parser, error, null) { }

    internal SourceFileReadException(SourceDataParser parser, ErrorCreateOptions? error, string? notes)
        : this(CreateCompilerMessage(parser, error, notes)) { }

    internal SourceFileReadException(CompilerMessage compilerMessage)
        : base(compilerMessage?.ToString() ?? throw new ArgumentNullException(nameof(compilerMessage)))
    {
        CompilerMessage = compilerMessage;
    }


    // Private static methods.
    private static CompilerMessage CreateCompilerMessage(SourceDataParser parser,
        ErrorCreateOptions? error,
        string? notes)
    {
        ArgumentNullException.ThrowIfNull(parser, nameof(parser));

        CompilerMessageLocation Location = new(parser.FilePath, parser.Line, parser.GetColumn(parser.DataIndex));

        if (error.HasValue)
        {
            return new(error.Value, Location, notes);
        }

        /* No definition means one of the placeholder throws which still keep their text in the notes.
         * They are not the convention, see the parser errors section of agents/code-style.md. */
        return new(string.Empty, Location, notes);
    }
}