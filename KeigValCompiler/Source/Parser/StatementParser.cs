using KeigValCompiler.Error;
using KeigValCompiler.Semantician;
using KeigValCompiler.Semantician.Member;
using KeigValCompiler.Semantician.Member.Code;
using System;
using System.Collections.Generic;
using System.ComponentModel;
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
    internal Statement ParseStatement()
    {
        return ParseStatementInternal(KGVL.SEMICOLON);
    }

    internal StatementCollection ParseStatementBody()
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

    internal StatementCollection ParseVariableLengthStatement()
    {
        Parser.SkipUntilNonWhitespace(null);
        if (Parser.GetCharAtDataIndex() == KGVL.OPEN_CURLY_BRACKET)
        {
            return ParseStatementBody();
        }

        StatementCollection Statements = new();
        Statements.AddStatement(ParseStatement());
        return Statements;
    }


    // Private methods.
    private Statement ParseStatementInternal(char stopChar)
    {
        Parser.SkipUntilNonWhitespace(null);

        /* Captured before anything is consumed so that the origin points at the statement's start. */
        SourceFileOrigin Origin = new(Parser.Line);

        if (Parser.GetCharAtDataIndex() == stopChar)
        {
            Parser.IncrementDataIndex();
            return new EmptyStatement() { Origin = Origin };
        }

        int WordStartIndex = Parser.DataIndex;
        TypeTargetIdentifier FirstWord = Parser.ReadTypeTargetIdentifier(null);
        string Keyword = FirstWord.MainTarget.SourceCodeName;
        if (Keyword.Length == 0)
        {
            return new EmptyStatement() { Origin = Origin };
        }

        Statement? ReturnedStatement = ParseKeywordStatement(Keyword);
        if (ReturnedStatement == null)
        {
            Parser.DataIndex = WordStartIndex;
            ReturnedStatement = ParseNonKeywordStatement();
        }
        ReturnedStatement.Origin = Origin;

        if (Parser.GetCharAtDataIndex() != stopChar)
        {
            throw new SourceFileReadException(Parser, null,
                $"Expected statement end '{stopChar}'");
        }
        Parser.IncrementDataIndex();
        return ReturnedStatement;
    }

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
            KGVL.KEYWORD_DO => ParseDoWhileStatement(),
            KGVL.KEYWORD_FOR => ParseForStatement(),
            KGVL.KEYWORD_FOREACH => ParseForEachStatement(),
            KGVL.KEYWORD_SWITCH => ParseSwitchStatement(),
            KGVL.KEYWORD_THROW => ParseThrowStatement(),
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

        Parser.SkipUntilNonWhitespace(null);
        return new ReturnStatement()
        {
            ReturnValue = ParseStatementInternal(KGVL.SEMICOLON)
        };
    }

    private TryStatement ParseTryStatement()
    {
        Parser.SkipUntilNonWhitespace(null);
        StatementCollection TryBody = ParseStatementBody();

        Parser.SkipUntilNonWhitespace(null);
        List<CatchClause> CatchClauses = ParseCatchClauses();

        Parser.SkipUntilNonWhitespace(null);
        int SavedIndex = Parser.DataIndex;
        string Keyword = Parser.ReadIdentifier(null);

        StatementCollection? FinallyBody = null;
        if (Keyword == KGVL.KEYWORD_FINALLY)
        {
            Parser.SkipUntilNonWhitespace(null);
            FinallyBody = ParseStatementBody();
        }
        else
        {
            Parser.DataIndex = SavedIndex;
        }

        TryStatement TargetStatement = new()
        {
            FinallyBody = FinallyBody
        };
        TargetStatement.TryBody.SetFrom(TryBody);
        TargetStatement.CatchClauses.AddRange(CatchClauses);
        return TargetStatement;
    }

    private List<CatchClause> ParseCatchClauses()
    {
        List<CatchClause> Clauses = new();

        int SavedIndex = Parser.DataIndex;
        string Keyword = Parser.ReadIdentifier(null);
        while (Keyword == KGVL.KEYWORD_CATCH)
        {
            Clauses.Add(ParseSingleCatchClause());

            Parser.SkipUntilNonWhitespace(null);
            SavedIndex = Parser.DataIndex;
            Keyword = Parser.ReadIdentifier(null);
        }
        Parser.DataIndex = SavedIndex;

        if (Clauses.Count <= 0)
        {
            throw new SourceFileReadException(Parser, null,
                "A try statement must have at least 1 catch clause");
        }

        return Clauses;
    }

    private CatchClause ParseSingleCatchClause()
    {
        Parser.SkipUntilNonWhitespace(null);
        if (Parser.GetCharAtDataIndex() != KGVL.OPEN_PARENTHESIS)
        {
            throw new SourceFileReadException(Parser, null,
                $"Expected catch clause start '{KGVL.OPEN_PARENTHESIS}'");
        }
        Parser.IncrementDataIndex();
        Parser.SkipUntilNonWhitespace(null);

        TypeTargetIdentifier ExceptionType = Parser.ReadTypeTargetIdentifier(null);
        Parser.SkipUntilNonWhitespace(null);

        /* The exception identifier is optional, as in "catch (SomeException)". */
        string ExceptionIdentifierName = Parser.ReadIdentifier(null);
        Identifier? ExceptionIdentifier = ExceptionIdentifierName.Length > 0
            ? new(ExceptionIdentifierName) : null;

        Parser.SkipUntilNonWhitespace(null);
        if (Parser.GetCharAtDataIndex() != KGVL.CLOSE_PARENTHESIS)
        {
            throw new SourceFileReadException(Parser, null,
                $"Expected catch clause end '{KGVL.CLOSE_PARENTHESIS}'");
        }
        Parser.IncrementDataIndex();
        Parser.SkipUntilNonWhitespace(null);

        StatementCollection Body = ParseStatementBody();

        CatchClause Clause = new(ExceptionType);
        Clause.ExceptionIdentifier = ExceptionIdentifier;
        Clause.Body.SetFrom(Body);
        return Clause;
    }

    private IfStatement ParseIfStatement()
    {
        Parser.SkipUntilNonWhitespace(null);

        Parser.SkipUntilNonWhitespace(null);
        Statement Condition = ParseSimpleCondition();

        Parser.SkipUntilNonWhitespace(null);
        StatementCollection IfBody = ParseVariableLengthStatement();
        StatementCollection? ElseBody = null;

        Parser.SkipUntilNonWhitespace(null);
        int StartIndex = Parser.DataIndex;
        string Keyword = Parser.ReadIdentifier(null);
        if (Keyword == KGVL.KEYWORD_ELSE)
        {
            Parser.SkipUntilNonWhitespace(null);
            ElseBody = ParseVariableLengthStatement();
        }
        else
        {
            Parser.DataIndex = StartIndex;
        }

        IfStatement TargetStatement = new(Condition);
        TargetStatement.IfBody.SetFrom(IfBody);
        TargetStatement.ElseBody = ElseBody;
        return TargetStatement;
    }

    private Statement ParseSimpleCondition()
    {
        if (Parser.GetCharAtDataIndex() != KGVL.OPEN_PARENTHESIS)
        {
            throw new SourceFileReadException(Parser, null,
                $"Expected condition body start '{KGVL.OPEN_PARENTHESIS}'");
        }
        Parser.IncrementDataIndex();

        Parser.SkipUntilNonWhitespace(null);
        Statement Condition = ParseStatement();

        Parser.SkipUntilNonWhitespace(null);
        if (Parser.GetCharAtDataIndex() != KGVL.CLOSE_PARENTHESIS)
        {
            throw new SourceFileReadException(Parser, null,
                $"Expected condition body end '{KGVL.CLOSE_PARENTHESIS}'");
        }
        Parser.IncrementDataIndex();

        return Condition;
    }

    private WhileStatement ParseWhileStatement()
    {
        Parser.SkipUntilNonWhitespace(null);
        Statement Condition = ParseSimpleCondition();

        Parser.SkipUntilNonWhitespace(null);
        StatementCollection Body = ParseVariableLengthStatement();

        WhileStatement TargetStatement = new(Condition)
        {
            IsPairedWithDoStatement = false
        };
        TargetStatement.Body.SetFrom(Body);
        return TargetStatement;
    }

    private WhileStatement ParseDoWhileStatement()
    {
        Parser.SkipUntilNonWhitespace(null);
        StatementCollection Body = ParseVariableLengthStatement();

        Parser.SkipUntilNonWhitespace(null);
        Statement Condition = ParseSimpleCondition();

        Parser.SkipUntilNonWhitespace(null);
        if (Parser.GetCharAtDataIndex() != KGVL.SEMICOLON)
        {
            throw new SourceFileReadException(Parser, null,
                $"Expected '{KGVL.SEMICOLON}' to end a do-while statement");
        }
        Parser.IncrementDataIndex();

        WhileStatement TargetStatement = new(Condition)
        {
            IsPairedWithDoStatement = true
        };
        TargetStatement.Body.SetFrom(Body);
        return TargetStatement;
    }

    private ForStatement ParseForStatement()
    {
        Parser.SkipUntilNonWhitespace(null);
        if (Parser.GetCharAtDataIndex() != KGVL.OPEN_PARENTHESIS)
        {
            throw new SourceFileReadException(Parser, null,
                $"Expected for statement condition start '{KGVL.OPEN_PARENTHESIS}'");
        }
        Parser.IncrementDataIndex();

        Parser.SkipUntilNonWhitespace(null);
        Statement Assignment = ParseStatementInternal(KGVL.SEMICOLON);
        if (Assignment is not (VariableDeclarationStatement or AssignmentStatement or EmptyStatement))
        {
            throw new SourceFileReadException(Parser, null,
                $"Expected variable assignment statement (for assignment section) or nothing in for loop");
        }

        Parser.SkipUntilNonWhitespace(null);
        Statement Condition = ParseStatementInternal(KGVL.SEMICOLON);

        Parser.SkipUntilNonWhitespace(null);
        Statement Increment = ParseStatementInternal(KGVL.CLOSE_PARENTHESIS);
        if (Increment is not (AssignmentStatement or UnaryOperatorStatement or EmptyStatement))
        {
            throw new SourceFileReadException(Parser, null,
                $"Expected variable assignment statement (for increment section) or nothing in for loop");
        }

        Parser.SkipUntilNonWhitespace(null);
        if (Parser.GetCharAtDataIndex() != KGVL.CLOSE_PARENTHESIS)
        {
            throw new SourceFileReadException(Parser, null,
                $"Expected for statement condition end '{KGVL.CLOSE_PARENTHESIS}'");
        }
        Parser.IncrementDataIndex();

        Parser.SkipUntilNonWhitespace(null);
        StatementCollection Body = ParseVariableLengthStatement();

        ForStatement TargetStatement = new(Assignment, Condition, Increment);
        TargetStatement.Body.SetFrom(Body);
        return TargetStatement;
    }

    private ForEachStatement ParseForEachStatement()
    {
        Parser.SkipUntilNonWhitespace(null);
        if (Parser.GetCharAtDataIndex() != KGVL.OPEN_PARENTHESIS)
        {
            throw new SourceFileReadException(Parser, null,
                $"Expected foreach statement header start '{KGVL.OPEN_PARENTHESIS}'");
        }
        Parser.IncrementDataIndex();
        Parser.SkipUntilNonWhitespace(null);

        TypeTargetIdentifier ElementType = Parser.ReadTypeTargetIdentifier(null);
        Parser.SkipUntilNonWhitespace(null);

        Identifier ElementName = new(Parser.ReadIdentifier(null));
        Parser.SkipUntilNonWhitespace(null);

        string InKeyword = Parser.ReadIdentifier(null);
        if (InKeyword != KGVL.KEYWORD_IN)
        {
            throw new SourceFileReadException(Parser, null,
                $"Expected keyword \"{KGVL.KEYWORD_IN}\" between the element and the collection " +
                $"of a foreach statement");
        }

        Parser.SkipUntilNonWhitespace(null);
        Statement EnumeratorProvider = ParseStatementInternal(KGVL.CLOSE_PARENTHESIS);

        Parser.SkipUntilNonWhitespace(null);
        StatementCollection Body = ParseVariableLengthStatement();

        ForEachStatement TargetStatement = new(ElementName, EnumeratorProvider)
        {
            /* The "var" keyword leaves the element type to be inferred. */
            ElementType = ElementType.MainTarget.SourceCodeName == KGVL.KEYWORD_VAR ? null : ElementType
        };
        TargetStatement.Body.SetFrom(Body);
        return TargetStatement;
    }

    private SwitchStatement ParseSwitchStatement()
    {
        Parser.SkipUntilNonWhitespace(null);
        Statement SwitchTarget = ParseSimpleCondition();

        Parser.SkipUntilNonWhitespace(null);
        if (Parser.GetCharAtDataIndex() != KGVL.OPEN_CURLY_BRACKET)
        {
            throw new SourceFileReadException(Parser, null,
                $"Expected switch statement body start '{KGVL.OPEN_CURLY_BRACKET}'");
        }
        Parser.IncrementDataIndex();

        SwitchStatement TargetStatement = new(SwitchTarget);
        ParseSwitchCases(TargetStatement);

        Parser.SkipUntilNonWhitespace(null);
        if (Parser.GetCharAtDataIndex() != KGVL.CLOSE_CURLY_BRACKET)
        {
            throw new SourceFileReadException(Parser, null,
                $"Expected switch statement body end '{KGVL.CLOSE_CURLY_BRACKET}'");
        }
        Parser.IncrementDataIndex();

        return TargetStatement;
    }

    private void ParseSwitchCases(SwitchStatement targetStatement)
    {
        Parser.SkipUntilNonWhitespace(null);
        while (Parser.IsMoreDataAvailable && (Parser.GetCharAtDataIndex() != KGVL.CLOSE_CURLY_BRACKET))
        {
            ParseSingleSwitchCase(targetStatement);
            Parser.SkipUntilNonWhitespace(null);
        }
    }

    private void ParseSingleSwitchCase(SwitchStatement targetStatement)
    {
        SwitchCase Case = new();
        Parser.SkipUntilNonWhitespace(null);

        bool IsDefaultCase = false;
        int StartIndex = Parser.DataIndex;
        string Keyword = Parser.ReadIdentifier(null);
        
        while ((Keyword == KGVL.KEYWORD_CASE) || (Keyword == KGVL.KEYWORD_DEFAULT))
        {
            if (Keyword == KGVL.KEYWORD_DEFAULT)
            {
                if (IsDefaultCase)
                {
                    throw new SourceFileReadException(Parser, null,
                        $"Duplicate default case.");
                }
                IsDefaultCase = true;
            }

            Parser.SkipUntilNonWhitespace(null);
            if (!IsDefaultCase)
            {
                Case.CaseConditions.AddStatement(ParseStatementInternal(KGVL.COLON));
            }

            Parser.SkipUntilNonWhitespace(null);
            if (Parser.GetCharAtDataIndex() != KGVL.COLON)
            {
                throw new SourceFileReadException(Parser, null,
                    $"Expected colon '{KGVL.COLON}' after switch condition");
            }
            Parser.IncrementDataIndex();
            Parser.SkipUntilNonWhitespace(null);
            StartIndex = Parser.DataIndex;
            Keyword = Parser.ReadIdentifier(null);
        }

        Parser.DataIndex = StartIndex;

        if (IsDefaultCase && Case.CaseConditions.Count > 0)
        {
            throw new SourceFileReadException(Parser, null,
                $"Default switch case cannot have conditions.");
        }

        Statement BodyStatement;
        do
        {
            BodyStatement = ParseStatement();
            Case.Body.AddStatement(BodyStatement);
            Parser.SkipUntilNonWhitespace(null);
        } while (Parser.IsMoreDataAvailable
            && (Parser.GetCharAtDataIndex() != KGVL.CLOSE_CURLY_BRACKET)
            && (BodyStatement is not (BreakStatement or ThrowStatement)));

        Case.IsBrokenOutOf = BodyStatement is BreakStatement;

        if (IsDefaultCase)
        {
            targetStatement.DefaultCase.SetFrom(Case.Body);
            return;
        }

        if (Case.CaseConditions.IsEmpty)
        {
            throw new SourceFileReadException(Parser, null,
                $"Expected at least 1 switch case condition.");
        }

        targetStatement.AddCase(Case);
    }

    private ThrowStatement ParseThrowStatement()
    {
        Parser.SkipUntilNonWhitespace(null);
        return new ThrowStatement(ParseStatementInternal(KGVL.SEMICOLON));
    }

    private Statement ParseNonKeywordStatement()
    {
        
        throw new NotImplementedException();
    }

    /* A parenthesised section is ambiguous until the expression parser exists: "(Abc)" may be a cast
     * to the type Abc or a parenthesised access of the variable Abc, and telling them apart requires
     * parsing what follows. The partial attempt below is kept as a record of that intent. */
    private Statement ParseParenthesisStatement()
    {
        //if (Parser.GetCharAtDataIndex() != KGVL.OPEN_PARENTHESIS)
        //{
        //    throw new SourceFileReadException(Parser, null, $"Expected '{KGVL.OPEN_PARENTHESIS}'");
        //}
        //Parser.IncrementDataIndex();

        //int StartIndex = Parser.DataIndex;
        //Parser.SkipUntilNonWhitespace(null);
        //TypeTargetIdentifier Keyword = Parser.ReadTypeTargetIdentifier(null);
        //Parser.SkipUntilNonWhitespace(null);

        //if (Parser.GetCharAtDataIndex() == KGVL.CLOSE_PARENTHESIS)
        //{
        //    if (Keyword.MainTarget.SourceCodeName.Length == 0)
        //    {
        //        throw new SourceFileReadException(Parser, null,
        //             $"Expected statement inside of parenthesis.");
        //    }
        //    Parser.IncrementDataIndex();
        //    Parser.SkipUntilNonWhitespace(null);
        //}

        throw new NotImplementedException();
    }

    private Statement ParseAssignmentStatement()
    {
        throw new NotImplementedException();
    }

    private string ParseString()
    {
        if (Parser.GetCharAtDataIndex() != KGVL.SEMICOLON)
        {
            throw new SourceFileReadException(Parser, null, $"Expected quote '{KGVL.DOUBLE_QUOTE}' to start a string");
        }
        Parser.IncrementDataIndex();

        StringBuilder Builder = new();
        bool IsInEscapeSequence = false;
        while (Parser.IsMoreDataAvailable
            && ((Parser.GetCharAtDataIndex() != KGVL.DOUBLE_QUOTE) || IsInEscapeSequence))
        {
            char Character = Parser.GetCharAtDataIndex();
            if (IsInEscapeSequence)
            {
                Builder.Append(Parser.EscapeSequenceToChar(ParseEscapeSequence()));
            }
            else
            {
                Builder.Append(Character);
            }

            IsInEscapeSequence = !IsInEscapeSequence && Character == KGVL.ESCAPE_CHAR;
            Parser.IncrementDataIndex();
        }

        if (Parser.GetCharAtDataIndex() != KGVL.DOUBLE_QUOTE)
        {
            throw new SourceFileReadException(Parser, null, $"Expected quote '{KGVL.DOUBLE_QUOTE}' to end a string");
        }
        Parser.IncrementDataIndex();

        return Builder.ToString();
    }

    private string ParseEscapeSequence()
    {
        if (Parser.GetCharAtDataIndex() != KGVL.ESCAPE_CHAR)
        {
            throw new SourceFileReadException(Parser, null, $"Expected escape sequence start '{KGVL.ESCAPE_CHAR}'");
        }
        Parser.IncrementDataIndex();

        char StartChar = Parser.GetCharAtDataIndex();
        Parser.IncrementDataIndex();

        if (StartChar == KGVL.ESCAPE_SEQUENCE_CODEPOINT_INDICATOR)
        {
            return ParseCodePointEscapeSequence();
        }
        else if (StartChar == KGVL.ESCAPE_SEQUENCE_HEX_INDICATOR)
        {
            ParseHexEscapeSequence();
        }
        return Parser.GetCharAtDataIndex().ToString();
    }

    private string ParseCodePointEscapeSequence()
    {
        StringBuilder Sequence = new();
        const int SEQUENCE_LENGTH = 4;
        for (int i = 0; i < SEQUENCE_LENGTH; i++)
        {
            if (!Parser.IsMoreDataAvailable)
            {
                throw new SourceFileReadException(Parser, null,
                    $"Unexpected end of file while parsing unicode escape sequence \"{Sequence}\"");
            }
            Sequence.Append(Parser.GetCharAtDataIndex());
            Parser.IncrementDataIndex();
        }
        return Sequence.ToString();
    }

    private string ParseHexEscapeSequence()
    {
        const int MAX_SEQUENCE_LENGTH = 4;
        StringBuilder Sequence = new();

        while (char.IsAsciiHexDigit(Parser.GetCharAtDataIndex()) && (Sequence.Length < MAX_SEQUENCE_LENGTH))
        {
            Sequence.Append(Parser.GetCharAtDataIndex());
            Parser.IncrementDataIndex();
        }

        if (Sequence.Length == 0)
        {
            throw new SourceFileReadException(Parser, null,
                $"Invalid empty hex character escape sequence");
        }

        return Sequence.ToString();
    }

    private string ParseInterpolatedString()
    {
        if (Parser.GetCharAtDataIndex() != KGVL.STRING_INTERPOLATION_OPERATOR)
        {
            throw new SourceFileReadException(Parser, null,
                $"Expected interpolation indicator '{KGVL.STRING_INTERPOLATION_OPERATOR}'" +
                "to start an interpolated string");
        }
        Parser.IncrementDataIndex();
        if (Parser.GetCharAtDataIndex() != KGVL.SEMICOLON)
        {
            throw new SourceFileReadException(Parser, null,
                $"Expected quote '{KGVL.DOUBLE_QUOTE}' to start an interpolated string");
        }
        Parser.IncrementDataIndex();

        List<object> Sections = new();
        StringBuilder StringSection = new();
        bool IsInEscapeSequence = false;
        bool IsInInterpolation = false;
        while (Parser.IsMoreDataAvailable
            && ((Parser.GetCharAtDataIndex() != KGVL.DOUBLE_QUOTE) || IsInEscapeSequence))
        {
            char Character = Parser.GetCharAtDataIndex();
            if (IsInEscapeSequence)
            {
                StringSection.Append(Parser.EscapeSequenceToChar(ParseEscapeSequence()));
            }
            else if (Character == KGVL.OPEN_CURLY_BRACKET)
            {
                if (!IsInInterpolation)
                {
                    StringSection.Append(KGVL.OPEN_CURLY_BRACKET);
                }
                IsInInterpolation = !IsInInterpolation;
            }
            else if (IsInInterpolation)
            {
                Sections.Add(StringSection.ToString());
                StringSection.Clear();
                Sections.Add(ParseStatementInternal(KGVL.CLOSE_CURLY_BRACKET));
                IsInInterpolation = false;
            }
            else
            {
                StringSection.Append(Character);
            }

            IsInEscapeSequence = !IsInEscapeSequence && Character == KGVL.ESCAPE_CHAR;
            Parser.IncrementDataIndex();
        }

        if (Parser.GetCharAtDataIndex() != KGVL.DOUBLE_QUOTE)
        {
            throw new SourceFileReadException(Parser, null,
                $"Expected quote '{KGVL.DOUBLE_QUOTE}' to end an interpolated string");
        }
        Parser.IncrementDataIndex();

        return StringSection.ToString();
    }
}