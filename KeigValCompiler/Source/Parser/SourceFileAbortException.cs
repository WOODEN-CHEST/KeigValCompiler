using KeigValCompiler.Error;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KeigValCompiler.Source.Parser;

/* Thrown when a source file has produced so many errors that carrying on with it is pointless.
 * Error recovery points must not catch this - it is meant to unwind all the way out of the file. */
internal class SourceFileAbortException : Exception
{
    // Fields.
    internal string? FilePath { get; private init; }


    // Constructors.
    internal SourceFileAbortException(string? filePath)
        : base($"Stopped reading the file \"{filePath}\" after {CompilerMessageCollection.MAX_ERRORS_PER_FILE} "
            + "errors. Any remaining errors in it were not looked for.")
    {
        FilePath = filePath;
    }
}