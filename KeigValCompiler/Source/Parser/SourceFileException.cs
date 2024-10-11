namespace KeigValCompiler.Source.Parser;

internal class SourceFileException : FileReadException
{
    // Constructors.
    public SourceFileException(SourceFileParser parser, string? message)
        : base(parser, message)
    { }
}