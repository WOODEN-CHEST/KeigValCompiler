using KeigValCompiler.Semantician;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KeigValCompiler.Source.Parser;

internal abstract class AbstractParserBase
{
    // Fields.
    protected SourceDataParser Parser { get; private init; }
    protected ParserUtilities Utils { get; private init; }
    protected PackSourceFile SourceFile { get; private init; }


    // Constructors.
    internal AbstractParserBase(SourceDataParser parser, ParserUtilities utils, PackSourceFile sourceFile)
    {
        Parser = parser ?? throw new ArgumentNullException(nameof(parser));
        Utils = utils ?? throw new ArgumentNullException(nameof(utils));
        SourceFile = sourceFile ?? throw new ArgumentNullException(nameof(sourceFile));
    }
}