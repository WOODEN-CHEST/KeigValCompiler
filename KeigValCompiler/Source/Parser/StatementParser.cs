using KeigValCompiler.Semantician.Member;
using KeigValCompiler.Semantician.Member.Code;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KeigValCompiler.Source.Parser;

internal class StatementParser : AbstractParserBase
{
    // Constructors.
    public StatementParser(PackParsingContext context) : base(context)
    {
    }


    // Methods.
    public Statement ParseStatement()
    {
        Parser.SkipUntilNonWhitespace(null);
        if (Parser.GetCharAtDataIndex() == KGVL.SEMICOLON)
        {
            return new EmptyStatement();
        }

        int StartIndex = Parser.DataIndex;
        TypeTargetIdentifier Target = Parser.ReadTypeTargetIdentifier(null);
        string? ExtractedKeyword = Target.MainTarget?.SourceCodeName;
        if (ExtractedKeyword == null)
        {
            return new EmptyStatement();
        }

        if (ExtractedKeyword == KGVL.KEYWORD_CONTINUE)
        {
            return new ContinueStatement();
        }
        else if (ExtractedKeyword == KGVL.KEYWORD_BREAK)
        {
            return new BreakStatement();
        }

        throw new NotImplementedException();
    }


    // Private methods.
    private void ParseNonEmptyStatement()
    {
        throw new NotImplementedException();
    }

    private ReturnStatement ParseReturnStatement()
    {
        throw new NotImplementedException();
    }
}