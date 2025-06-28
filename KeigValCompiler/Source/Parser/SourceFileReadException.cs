using KeigValCompiler.Error;

namespace KeigValCompiler.Source.Parser;

internal class SourceFileReadException : Exception
{
    // Constructors.
    internal SourceFileReadException(SourceDataParser parser, ErrorCreateOptions? error)
        : this(parser, error, null) { }

    internal SourceFileReadException(SourceDataParser parser, ErrorCreateOptions? error, string? notes)
        : this(CreateErrorMessage(parser, error, notes)) { }

    internal SourceFileReadException(string message) : base(message) { }


    // Private static methods.
    private static string CreateErrorMessage(SourceDataParser parser, ErrorCreateOptions? error, string? notes)
    {
        string Notes = notes != null ? $"Notes: {notes}" : string.Empty;
        return $"Failed to read file \"{parser.FilePath}\" on line {parser.Line}. " +
            $"{error?.CreateMessage() ?? string.Empty}. {Notes}.";
    }
}