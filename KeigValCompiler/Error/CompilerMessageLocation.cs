using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KeigValCompiler.Error;

/* Where in the source code a compiler message came from. Not every message has a full location:
 * a file which could not even be opened has a path but no line, and a message about the compilation
 * as a whole has neither. */
internal readonly struct CompilerMessageLocation
{
    // Static fields.
    internal static CompilerMessageLocation None { get; } = new(null, LINE_UNKNOWN, COLUMN_UNKNOWN);


    // Fields.
    internal const int LINE_UNKNOWN = 0;
    internal const int COLUMN_UNKNOWN = 0;

    internal string? FilePath { get; private init; }
    internal int Line { get; private init; }
    internal int Column { get; private init; }

    internal bool HasFilePath => FilePath != null;
    internal bool HasLine => Line != LINE_UNKNOWN;
    internal bool HasColumn => Column != COLUMN_UNKNOWN;


    // Constructors.
    internal CompilerMessageLocation(string? filePath, int line, int column)
    {
        FilePath = filePath;
        Line = line;
        Column = column;
    }

    internal CompilerMessageLocation(string? filePath) : this(filePath, LINE_UNKNOWN, COLUMN_UNKNOWN) { }


    // Inherited methods.
    public override string ToString()
    {
        if (!HasFilePath)
        {
            return string.Empty;
        }

        StringBuilder LocationBuilder = new();
        LocationBuilder.Append($"File \"{FilePath}\"");

        if (HasLine)
        {
            LocationBuilder.Append($", line {Line}");
        }
        if (HasColumn)
        {
            LocationBuilder.Append($", column {Column}");
        }

        return LocationBuilder.ToString();
    }
}