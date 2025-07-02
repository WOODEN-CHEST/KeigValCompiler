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

        Statement? KeywordStatement = ParseKeywordStatement(ExtractedKeyword);
        if (KeywordStatement != null)
        {
            return KeywordStatement;
        }

        return ParseNonKeywordStatement();
    }


    // Private methods.
    private Statement? ParseKeywordStatement(string keyword)
    {
        return keyword switch
        {
            KGVL.KEYWORD_CONTINUE => new ContinueStatement(),
            KGVL.KEYWORD_BREAK => new BreakStatement(),
            KGVL.KEYWORD_RETURN => ParseReturnStatement(),
            KGVL.KEYWORD_TRY => ParseTryStatement(),
            KGVL.KEYWORD_IF => ParseIfStatement(),
            KGVL.KEYWORD_WHILE => ParseWhileStatement(),
            KGVL.KEYWORD_FOR => ParseForStatement(),
            _ => null
        };
    }

    private Statement ParseNonKeywordStatement()
    {
        throw new NotImplementedException();
    }

    private ReturnStatement ParseReturnStatement()
    {
        throw new NotImplementedException();
    }

    private TryStatement ParseTryStatement()
    {
        throw new NotImplementedException();
    }

    private IfStatement ParseIfStatement()
    {
        throw new NotImplementedException();
    }

    private WhileStatement ParseWhileStatement()
    {
        throw new NotImplementedException();
    }

    private ForStatement ParseForStatement()
    {
        throw new NotImplementedException();
    }
}