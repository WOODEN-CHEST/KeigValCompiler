using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace KeigValCompiler.Source.Parser;

internal class FileReadException : Exception
{
    // Constructors.
    internal FileReadException(SourceFileParser sourceFile, string? message)
        : base($"Failed to read file {sourceFile.FilePath} on Line: {sourceFile.Line}. Reason: {message}") { }
}