using KeigValCompiler.Error;
using KeigValCompiler.Semantician;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KeigValCompiler.Source.Parser;

internal abstract class AbstractParserBase
{
    // Protected fields.
    protected SourceDataParser Parser => Context.Parser;
    protected ParserUtilities Utils => Context.Utilities;
    protected PackSourceFile SourceFile => Context.SourceFile;
    protected ErrorRepository ErrorCreator => Context.ErrorCreator;
    protected virtual PackParsingContext Context { get; private init; }


    // Constructors.
    internal AbstractParserBase(PackParsingContext context)
    {
        Context = context ?? throw new ArgumentNullException(nameof(context));
    }
}