using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace KeigValCompiler.Source.Parser;

internal class SourceFileException : FileReadException
{
    // Constructors.
    public SourceFileException(SourceFileParser parser, string? message)
        : base(parser, message)
    { }
}