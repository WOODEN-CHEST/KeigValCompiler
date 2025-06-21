namespace KeigValCompiler.Source.Parser;

internal class SourceFileReadException : Exception
{
    // Constructors.
    internal SourceFileReadException(string? filePath, int line, string? message)
        : base($"Failed to read file {filePath ?? string.Empty} " +
            $"on line: {line}. Reason: {message}") { }

    internal SourceFileReadException(string message) : base(message) { }
}