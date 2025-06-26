using KeigValCompiler.Error;
using KeigValCompiler.Semantician;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KeigValCompiler.Source.Parser;

internal class PackParsingContext
{
    internal required ErrorRepository ErrorCreator { get; init; }
    internal required ParserUtilities Utilities { get; init; }
    internal required SourceDataParser Parser { get; init; }
    internal required PackSourceFile SourceFile { get; init; }
}