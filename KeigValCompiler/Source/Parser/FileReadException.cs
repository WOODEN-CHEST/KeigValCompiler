namespace KeigValCompiler.Source.Parser;

internal class FileReadException : Exception
{
    // Constructors.
    internal FileReadException(SourceFileParser sourceFileParser, string? message)
        : base($"Failed to read file {sourceFileParser.FilePath} on line: {sourceFileParser.Line}. Reason: {message}") { }
}