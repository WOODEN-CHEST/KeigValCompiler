using KeigValCompiler.Error;
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

    public StatementCollection ParseStatementBody()
    {
        StatementCollection Statements = new();
        if (Parser.GetCharAtDataIndex() != KGVL.OPEN_CURLY_BRACKET)
        {
            throw new SourceFileReadException(Parser, null, 
                $"Expected statement body start '{KGVL.OPEN_CURLY_BRACKET}'");
        }

        while (Parser.IsMoreDataAvailable
            && Parser.SkipUntilNonWhitespace(null)
            && (Parser.GetCharAtDataIndex() != KGVL.CLOSE_CURLY_BRACKET))
        {
            Statements.AddStatement(ParseStatement());
        }

        if (Parser.GetCharAtDataIndex() != KGVL.CLOSE_CURLY_BRACKET)
        {
            throw new SourceFileReadException(Parser, null,
                $"Expected statement body end '{KGVL.CLOSE_CURLY_BRACKET}'");
        }
        return Statements;
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
            KGVL.KEYWORD_WHILE or KGVL.KEYWORD_DO => ParseWhileStatement(),
            KGVL.KEYWORD_FOR => ParseForStatement(),
            KGVL.KEYWORD_SWITCH => ParseSwitchStatement(),
            _ => null
        };
    }

    private ReturnStatement ParseReturnStatement()
    {
        Parser.SkipUntilNonWhitespace(null);
        if (Parser.GetCharAtDataIndex() == KGVL.SEMICOLON)
        {
            Parser.IncrementDataIndex();
            return new ReturnStatement();
        }
        
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

    private ForStatement ParseSwitchStatement()
    {
        throw new NotImplementedException();
    }

    private Statement ParseNonKeywordStatement()
    {
        throw new NotImplementedException();
    }

    private string ParseString()
    {
        if (Parser.GetCharAtDataIndex() != KGVL.SEMICOLON)
        {

        }
    }

    private InterpolatedString ParseInterpolatedString()
    {

    }
}