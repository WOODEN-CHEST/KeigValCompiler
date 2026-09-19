using KeigValCompiler.Error;
using KeigValCompiler.Semantician;
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
    // Private fields.
    private readonly ExpressionParser _expressionParser;

    private static readonly char[] _statementResumeChars = new char[] { KGVL.SEMICOLON };
    private static readonly char[] _statementTerminatorChars = new char[] { KGVL.CLOSE_CURLY_BRACKET };


    // Constructors.
    public StatementParser(PackParsingContext context) : base(context)
    {
        _expressionParser = new(context, this);
    }


    // Methods.
    internal Statement ParseStatement()
    {
        return ParseStatementInternal(KGVL.SEMICOLON);
    }

    /* One value, with no terminator consumed. For the places outside a statement body which still
     * need a value, such as a field's starting value or a "=>" member body. */
    internal Statement ParseExpressionValue()
    {
        Parser.SkipUntilNonWhitespace(null);
        return _expressionParser.ParseExpression();
    }

    /* A braced run of statements. One broken statement inside it is recovered from so that the
     * rest of the body still gets parsed and its errors still get reported. */
    internal StatementCollection ParseStatementBody()
    {
        StatementCollection Statements = new();

        Parser.SkipUntilNonWhitespace(null);
        if (Parser.GetCharAtDataIndex() != KGVL.OPEN_CURLY_BRACKET)
        {
            throw new SourceFileReadException(Parser, ErrorCreator.ExpectedStatementBodyStart.CreateOptions());
        }
        Parser.IncrementDataIndex();

        while (Parser.IsMoreDataAvailable
            && Parser.SkipUntilNonWhitespace(null)
            && (Parser.GetCharAtDataIndex() != KGVL.CLOSE_CURLY_BRACKET))
        {
            try
            {
                Statements.AddStatement(ParseStatement());
            }
            catch (SourceFileReadException e)
            {
                if (!RecoverFromError(e, _statementResumeChars, _statementTerminatorChars)
                    || _statementTerminatorChars.Contains(Parser.GetCharAtDataIndex()))
                {
                    break;
                }
            }
        }

        if (Parser.GetCharAtDataIndex() != KGVL.CLOSE_CURLY_BRACKET)
        {
            throw new SourceFileReadException(Parser, ErrorCreator.ExpectedStatementBodyEnd.CreateOptions());
        }
        Parser.IncrementDataIndex();
        return Statements;
    }

    /* The body of an "if" or a loop, which is either a braced run of statements or a single one. */
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
        string Keyword = Parser.IsIdentifierFirstChar(Parser.GetCharAtDataIndex())
            ? Parser.ReadIdentifier(null) : string.Empty;

        bool NeedsTerminator = true;
        Statement? ReturnedStatement = Keyword.Length > 0
            ? ParseKeywordStatement(Keyword, ref NeedsTerminator) : null;

        if (ReturnedStatement == null)
        {
            Parser.DataIndex = WordStartIndex;
            ReturnedStatement = ParseNonKeywordStatement();
        }
        ReturnedStatement.Origin = Origin;

        if (!NeedsTerminator)
        {
            return ReturnedStatement;
        }

        Parser.SkipUntilNonWhitespace(null);
        if (Parser.GetCharAtDataIndex() != stopChar)
        {
            throw new SourceFileReadException(Parser, ErrorCreator.ExpectedStatementEnd.CreateOptions(stopChar));
        }
        Parser.IncrementDataIndex();
        return ReturnedStatement;
    }

    /* Statements which end in a body rather than a terminator clear needsTerminator, because there
     * is no ';' after the closing bracket of an "if" or a "while". */
    private Statement? ParseKeywordStatement(string keyword, ref bool needsTerminator)
    {
        switch (keyword)
        {
            case KGVL.KEYWORD_CONTINUE:
                return new ContinueStatement();

            case KGVL.KEYWORD_BREAK:
                return new BreakStatement();

            case KGVL.KEYWORD_RETURN:
                return ParseReturnStatement();

            case KGVL.KEYWORD_THROW:
                return ParseThrowStatement();

            case KGVL.KEYWORD_YIELD:
                return ParseYieldStatement();

            case KGVL.KEYWORD_DO:
                return ParseDoWhileStatement();

            case KGVL.KEYWORD_TRY:
                needsTerminator = false;
                return ParseTryStatement();

            case KGVL.KEYWORD_IF:
                needsTerminator = false;
                return ParseIfStatement();

            case KGVL.KEYWORD_WHILE:
                needsTerminator = false;
                return ParseWhileStatement();

            case KGVL.KEYWORD_FOR:
                needsTerminator = false;
                return ParseForStatement();

            case KGVL.KEYWORD_FOREACH:
                needsTerminator = false;
                return ParseForEachStatement();

            case KGVL.KEYWORD_SWITCH:
                needsTerminator = false;
                return ParseSwitchStatement();

            default:
                return null;
        }
    }

    /* Value carrying statements. The terminating ';' is left for ParseStatementInternal. */
    private ReturnStatement ParseReturnStatement()
    {
        Parser.SkipUntilNonWhitespace(null);
        if (Parser.GetCharAtDataIndex() == KGVL.SEMICOLON)
        {
            return new ReturnStatement();
        }
        return new ReturnStatement() { ReturnValue = _expressionParser.ParseExpression() };
    }

    private ThrowStatement ParseThrowStatement()
    {
        Parser.SkipUntilNonWhitespace(null);
        return new ThrowStatement(_expressionParser.ParseExpression());
    }

    private YieldStatement ParseYieldStatement()
    {
        Parser.SkipUntilNonWhitespace(null);

        int StartIndex = Parser.DataIndex;
        string Keyword = Parser.IsIdentifierFirstChar(Parser.GetCharAtDataIndex())
            ? Parser.ReadIdentifier(null) : string.Empty;

        if (Keyword == KGVL.KEYWORD_BREAK)
        {
            return new YieldStatement();
        }

        if (Keyword != KGVL.KEYWORD_RETURN)
        {
            Parser.DataIndex = StartIndex;
            throw new SourceFileReadException(Parser, ErrorCreator.ExpectedYieldContinuation.CreateOptions());
        }

        Parser.SkipUntilNonWhitespace(null);
        return new YieldStatement(_expressionParser.ParseExpression());
    }


    /* Exception handling. */
    private TryStatement ParseTryStatement()
    {
        Parser.SkipUntilNonWhitespace(null);
        StatementCollection TryBody = ParseStatementBody();

        Parser.SkipUntilNonWhitespace(null);
        List<CatchClause> CatchClauses = ParseCatchClauses();

        Parser.SkipUntilNonWhitespace(null);
        int SavedIndex = Parser.DataIndex;
        string Keyword = Parser.IsIdentifierFirstChar(Parser.GetCharAtDataIndex())
            ? Parser.ReadIdentifier(null) : string.Empty;

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
        string Keyword = Parser.IsIdentifierFirstChar(Parser.GetCharAtDataIndex())
            ? Parser.ReadIdentifier(null) : string.Empty;

        while (Keyword == KGVL.KEYWORD_CATCH)
        {
            Clauses.Add(ParseSingleCatchClause());

            Parser.SkipUntilNonWhitespace(null);
            SavedIndex = Parser.DataIndex;
            Keyword = Parser.IsIdentifierFirstChar(Parser.GetCharAtDataIndex())
                ? Parser.ReadIdentifier(null) : string.Empty;
        }
        Parser.DataIndex = SavedIndex;

        if (Clauses.Count <= 0)
        {
            throw new SourceFileReadException(Parser, ErrorCreator.ExpectedCatchClause.CreateOptions());
        }

        return Clauses;
    }

    private CatchClause ParseSingleCatchClause()
    {
        Parser.SkipUntilNonWhitespace(null);
        if (Parser.GetCharAtDataIndex() != KGVL.OPEN_PARENTHESIS)
        {
            throw new SourceFileReadException(Parser, ErrorCreator.ExpectedCatchClauseStart.CreateOptions());
        }
        Parser.IncrementDataIndex();
        Parser.SkipUntilNonWhitespace(null);

        TypeTargetIdentifier ExceptionType = Parser.ReadTypeTargetIdentifier(
            ErrorCreator.ExpectedCaughtExceptionType.CreateOptions());
        Parser.SkipUntilNonWhitespace(null);

        /* The exception identifier is optional, as in "catch (SomeException)". */
        string ExceptionIdentifierName = Parser.IsIdentifierFirstChar(Parser.GetCharAtDataIndex())
            ? Parser.ReadIdentifier(null) : string.Empty;
        Identifier? ExceptionIdentifier = ExceptionIdentifierName.Length > 0
            ? new(ExceptionIdentifierName) : null;

        Parser.SkipUntilNonWhitespace(null);
        if (Parser.GetCharAtDataIndex() != KGVL.CLOSE_PARENTHESIS)
        {
            throw new SourceFileReadException(Parser, ErrorCreator.ExpectedCatchClauseEnd.CreateOptions());
        }
        Parser.IncrementDataIndex();

        CatchClause Clause = new(ExceptionType);
        Clause.ExceptionIdentifier = ExceptionIdentifier;
        Clause.WhenCondition = TryParseCatchWhenCondition();

        Parser.SkipUntilNonWhitespace(null);
        Clause.Body.SetFrom(ParseStatementBody());
        return Clause;
    }

    /* "catch (E e) when (condition)" only runs the clause when the condition holds. */
    private Statement? TryParseCatchWhenCondition()
    {
        Parser.SkipUntilNonWhitespace(null);
        int StartIndex = Parser.DataIndex;

        if (!Parser.IsIdentifierFirstChar(Parser.GetCharAtDataIndex())
            || (Parser.ReadIdentifier(null) != KGVL.KEYWORD_WHEN))
        {
            Parser.DataIndex = StartIndex;
            return null;
        }

        Parser.SkipUntilNonWhitespace(null);
        return ParseSimpleCondition();
    }


    /* Branching and loops. */
    private IfStatement ParseIfStatement()
    {
        Parser.SkipUntilNonWhitespace(null);
        Statement Condition = ParseSimpleCondition();

        Parser.SkipUntilNonWhitespace(null);
        StatementCollection IfBody = ParseVariableLengthStatement();
        StatementCollection? ElseBody = null;

        Parser.SkipUntilNonWhitespace(null);
        int StartIndex = Parser.DataIndex;
        string Keyword = Parser.IsIdentifierFirstChar(Parser.GetCharAtDataIndex())
            ? Parser.ReadIdentifier(null) : string.Empty;

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

    /* A bracketed value, as used by "if", "while" and "switch". */
    private Statement ParseSimpleCondition()
    {
        if (Parser.GetCharAtDataIndex() != KGVL.OPEN_PARENTHESIS)
        {
            throw new SourceFileReadException(Parser, ErrorCreator.ExpectedConditionStart.CreateOptions());
        }
        Parser.IncrementDataIndex();

        Parser.SkipUntilNonWhitespace(null);
        Statement Condition = _expressionParser.ParseExpression();

        Parser.SkipUntilNonWhitespace(null);
        if (Parser.GetCharAtDataIndex() != KGVL.CLOSE_PARENTHESIS)
        {
            throw new SourceFileReadException(Parser, ErrorCreator.ExpectedConditionEnd.CreateOptions());
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

    /* "do { ... } while (condition);" keeps its trailing ';', which ParseStatementInternal takes. */
    private WhileStatement ParseDoWhileStatement()
    {
        Parser.SkipUntilNonWhitespace(null);
        StatementCollection Body = ParseVariableLengthStatement();

        Parser.SkipUntilNonWhitespace(null);
        if (!Parser.IsIdentifierFirstChar(Parser.GetCharAtDataIndex())
            || (Parser.ReadIdentifier(null) != KGVL.KEYWORD_WHILE))
        {
            throw new SourceFileReadException(Parser, ErrorCreator.ExpectedDoWhileCondition.CreateOptions());
        }

        Parser.SkipUntilNonWhitespace(null);
        Statement Condition = ParseSimpleCondition();

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
            throw new SourceFileReadException(Parser, ErrorCreator.ExpectedForHeaderStart.CreateOptions());
        }
        Parser.IncrementDataIndex();

        Statement Assignment = ParseForSection(KGVL.SEMICOLON);
        Statement? Condition = ParseForSection(KGVL.SEMICOLON);
        Statement Increment = ParseForSection(KGVL.CLOSE_PARENTHESIS);

        if (Condition is EmptyStatement)
        {
            Condition = null;
        }

        Parser.SkipUntilNonWhitespace(null);
        StatementCollection Body = ParseVariableLengthStatement();

        ForStatement TargetStatement = new(Assignment, Condition, Increment);
        TargetStatement.Body.SetFrom(Body);
        return TargetStatement;
    }

    /* One of the three parts of a for loop's header. Any of them may be left out entirely. */
    private Statement ParseForSection(char stopChar)
    {
        Parser.SkipUntilNonWhitespace(null);
        if (Parser.GetCharAtDataIndex() == stopChar)
        {
            Parser.IncrementDataIndex();
            return new EmptyStatement();
        }

        Statement Section = ParseNonKeywordStatement();

        Parser.SkipUntilNonWhitespace(null);
        if (Parser.GetCharAtDataIndex() != stopChar)
        {
            throw new SourceFileReadException(Parser, ErrorCreator.ExpectedStatementEnd.CreateOptions(stopChar));
        }
        Parser.IncrementDataIndex();
        return Section;
    }

    private ForEachStatement ParseForEachStatement()
    {
        Parser.SkipUntilNonWhitespace(null);
        if (Parser.GetCharAtDataIndex() != KGVL.OPEN_PARENTHESIS)
        {
            throw new SourceFileReadException(Parser, ErrorCreator.ExpectedForEachHeaderStart.CreateOptions());
        }
        Parser.IncrementDataIndex();
        Parser.SkipUntilNonWhitespace(null);

        TypeTargetIdentifier ElementType = Parser.ReadTypeTargetIdentifier(
            ErrorCreator.ExpectedForEachElementType.CreateOptions());
        Parser.SkipUntilNonWhitespace(null);

        Identifier ElementName = new(Parser.ReadIdentifier(
            ErrorCreator.ExpectedForEachElementName.CreateOptions()));
        Parser.SkipUntilNonWhitespace(null);

        if (!Parser.IsIdentifierFirstChar(Parser.GetCharAtDataIndex())
            || (Parser.ReadIdentifier(null) != KGVL.KEYWORD_IN))
        {
            throw new SourceFileReadException(Parser, ErrorCreator.ExpectedForEachInKeyword.CreateOptions());
        }

        Parser.SkipUntilNonWhitespace(null);
        Statement EnumeratorProvider = _expressionParser.ParseExpression();

        Parser.SkipUntilNonWhitespace(null);
        if (Parser.GetCharAtDataIndex() != KGVL.CLOSE_PARENTHESIS)
        {
            throw new SourceFileReadException(Parser, ErrorCreator.ExpectedForEachHeaderEnd.CreateOptions());
        }
        Parser.IncrementDataIndex();

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


    /* Switch statements. The switch used as a value is an expression and lives in ExpressionParser. */
    private SwitchStatement ParseSwitchStatement()
    {
        Parser.SkipUntilNonWhitespace(null);
        Statement SwitchTarget = ParseSimpleCondition();

        Parser.SkipUntilNonWhitespace(null);
        if (Parser.GetCharAtDataIndex() != KGVL.OPEN_CURLY_BRACKET)
        {
            throw new SourceFileReadException(Parser, ErrorCreator.ExpectedSwitchBodyStart.CreateOptions());
        }
        Parser.IncrementDataIndex();

        SwitchStatement TargetStatement = new(SwitchTarget);
        ParseSwitchCases(TargetStatement);

        Parser.SkipUntilNonWhitespace(null);
        if (Parser.GetCharAtDataIndex() != KGVL.CLOSE_CURLY_BRACKET)
        {
            throw new SourceFileReadException(Parser, ErrorCreator.ExpectedSwitchBodyEnd.CreateOptions());
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
        string Keyword = Parser.IsIdentifierFirstChar(Parser.GetCharAtDataIndex())
            ? Parser.ReadIdentifier(null) : string.Empty;

        while ((Keyword == KGVL.KEYWORD_CASE) || (Keyword == KGVL.KEYWORD_DEFAULT))
        {
            if (Keyword == KGVL.KEYWORD_DEFAULT)
            {
                if (IsDefaultCase)
                {
                    throw new SourceFileReadException(Parser, ErrorCreator.DuplicateDefaultCase.CreateOptions());
                }
                IsDefaultCase = true;
            }

            Parser.SkipUntilNonWhitespace(null);
            if (Keyword == KGVL.KEYWORD_CASE)
            {
                Case.CaseConditions.AddStatement(_expressionParser.ParseExpression());
            }

            Parser.SkipUntilNonWhitespace(null);
            if (Parser.GetCharAtDataIndex() != KGVL.COLON)
            {
                throw new SourceFileReadException(Parser, ErrorCreator.ExpectedSwitchCaseColon.CreateOptions());
            }
            Parser.IncrementDataIndex();

            Parser.SkipUntilNonWhitespace(null);
            StartIndex = Parser.DataIndex;
            Keyword = Parser.IsIdentifierFirstChar(Parser.GetCharAtDataIndex())
                ? Parser.ReadIdentifier(null) : string.Empty;
        }

        Parser.DataIndex = StartIndex;

        if (IsDefaultCase && (Case.CaseConditions.Count > 0))
        {
            throw new SourceFileReadException(Parser, ErrorCreator.DefaultCaseWithConditions.CreateOptions());
        }

        Statement BodyStatement;
        do
        {
            BodyStatement = ParseStatement();
            Case.Body.AddStatement(BodyStatement);
            Parser.SkipUntilNonWhitespace(null);
        } while (Parser.IsMoreDataAvailable
            && (Parser.GetCharAtDataIndex() != KGVL.CLOSE_CURLY_BRACKET)
            && (BodyStatement is not (BreakStatement or ThrowStatement or ReturnStatement)));

        Case.IsBrokenOutOf = BodyStatement is BreakStatement;

        if (IsDefaultCase)
        {
            targetStatement.DefaultCase.SetFrom(Case.Body);
            return;
        }

        if (Case.CaseConditions.IsEmpty)
        {
            throw new SourceFileReadException(Parser, ErrorCreator.ExpectedSwitchCaseCondition.CreateOptions());
        }

        targetStatement.AddCase(Case);
    }


    /* Everything which is not introduced by a keyword: declarations, assignments, calls and any
     * other value used as a statement on its own. */
    private Statement ParseNonKeywordStatement()
    {
        Parser.SkipUntilNonWhitespace(null);
        if (TryParseVariableDeclaration(out Statement? Declaration))
        {
            return Declaration!;
        }
        return _expressionParser.ParseExpression();
    }

    /* A declaration and a value both start with a name, and only what follows the name tells them
     * apart: "Thing a" declares one, while "Thing.a" and "Thing(a)" are values. The type is read
     * speculatively and put back when no name follows it. */
    private bool TryParseVariableDeclaration(out Statement? result)
    {
        result = null;
        if (!Parser.IsIdentifierFirstChar(Parser.GetCharAtDataIndex()))
        {
            return false;
        }

        int StartIndex = Parser.DataIndex;
        TypeTargetIdentifier DeclaredType = Parser.ReadTypeTargetIdentifier(null);
        Parser.SkipUntilNonWhitespace(null);

        if (!Parser.IsIdentifierFirstChar(Parser.GetCharAtDataIndex()))
        {
            Parser.DataIndex = StartIndex;
            return false;
        }

        bool IsInferred = DeclaredType.MainTarget.SourceCodeName == KGVL.KEYWORD_VAR;
        VariableDeclarationStatement Declaration = new(IsInferred ? null : DeclaredType);

        while (true)
        {
            Identifier Name = new(Parser.ReadIdentifier(ErrorCreator.ExpectedVariableName.CreateOptions()));
            Parser.SkipUntilNonWhitespace(null);

            Statement? Value = null;
            if ((Parser.GetCharAtDataIndex() == KGVL.ASSIGNMENT_OPERATOR)
                && !Parser.HasStringAtIndex(Parser.DataIndex, KGVL.OPERATOR_EQUALS))
            {
                Parser.IncrementDataIndex();
                Parser.SkipUntilNonWhitespace(null);
                Value = _expressionParser.ParseExpression();
                Parser.SkipUntilNonWhitespace(null);
            }

            Declaration.Declarations.AddItem(new VariableAssignment(Name, Value));

            if (Parser.GetCharAtDataIndex() != KGVL.COMMA)
            {
                break;
            }
            Parser.IncrementDataIndex();
            Parser.SkipUntilNonWhitespace(null);
        }

        result = Declaration;
        return true;
    }
}
