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

/* Precedence climbing parser for everything which produces a value. Split out of StatementParser
 * only for size; in KGVL every expression is a Statement, so the two hand work back and forth -
 * a lambda body is statements, and a statement's condition is an expression. */
internal class ExpressionParser : AbstractParserBase
{
    // Private fields.
    private readonly StatementParser _statementParser;

    /* Tested in this order so that a longer spelling is never read as a shorter one which prefixes
     * it, which is what stops ">>=" being read as ">>" followed by "=". */
    private static readonly string[] _operatorSpellings = new string[]
    {
        KGVL.ASSIGN_UNSIGNED_RIGHT_SHIFT,
        KGVL.OPERATOR_UNSIGNED_RIGHT_SHIFT, KGVL.ASSIGN_LEFT_SHIFT, KGVL.ASSIGN_RIGHT_SHIFT,
        KGVL.ASSIGN_NULL_COALESCE,
        KGVL.OPERATOR_INCREMENT, KGVL.OPERATOR_DECREMENT, KGVL.OPERATOR_EQUALS,
        KGVL.OPERATOR_NOT_EQUALS, KGVL.OPERATOR_LARGER_OR_EQUAL, KGVL.OPERATOR_LESS_OR_EQUAL,
        KGVL.OPERATOR_CONDITIONAL_AND, KGVL.OPERATOR_CONDITIONAL_OR, KGVL.OPERATOR_LEFT_SHIFT,
        KGVL.OPERATOR_RIGHT_SHIFT, KGVL.OPERATOR_NULL_COALESCE, KGVL.OPERATOR_CONDITIONAL_ACCESS,
        KGVL.QUICK_METHOD_BODY,
        KGVL.ASSIGN_ADD, KGVL.ASSIGN_SUBTRACT, KGVL.ASSIGN_MULTIPLY, KGVL.ASSIGN_DIVIDE,
        KGVL.ASSIGN_MODULO, KGVL.ASSIGN_BITWISE_AND, KGVL.ASSIGN_BITWISE_OR, KGVL.ASSIGN_BITWISE_XOR,
        KGVL.OPERATOR_ADD, KGVL.OPERATOR_SUBTRACT, KGVL.OPERATOR_MULTIPLY, KGVL.OPERATOR_DIVIDE,
        KGVL.OPERATOR_MODULO, KGVL.OPERATOR_NOT, KGVL.OPERATOR_BITWISE_COMPLEMENT,
        KGVL.OPERATOR_LARGER_THAN, KGVL.OPERATOR_LESS_THAN, KGVL.OPERATOR_BITWISE_AND,
        KGVL.OPERATOR_BITWISE_OR, KGVL.OPERATOR_BITWISE_XOR, KGVL.ASSIGN
    };

    /* Higher binds tighter. Only '??' is right associative. */
    private static readonly Dictionary<string, (StatementOperator Operator, int Precedence)>
        _binaryOperators = new()
    {
        { KGVL.OPERATOR_NULL_COALESCE, (StatementOperator.NotNullOrElse, 1) },
        { KGVL.OPERATOR_CONDITIONAL_OR, (StatementOperator.ConditionalOr, 2) },
        { KGVL.OPERATOR_CONDITIONAL_AND, (StatementOperator.ConditionalAnd, 3) },
        { KGVL.OPERATOR_BITWISE_OR, (StatementOperator.BitwiseOr, 4) },
        { KGVL.OPERATOR_BITWISE_XOR, (StatementOperator.BitwiseXor, 5) },
        { KGVL.OPERATOR_BITWISE_AND, (StatementOperator.BitwiseAnd, 6) },
        { KGVL.OPERATOR_EQUALS, (StatementOperator.Equals, 7) },
        { KGVL.OPERATOR_NOT_EQUALS, (StatementOperator.NotEquals, 7) },
        { KGVL.OPERATOR_LESS_THAN, (StatementOperator.LessThan, 8) },
        { KGVL.OPERATOR_LARGER_THAN, (StatementOperator.LargerThan, 8) },
        { KGVL.OPERATOR_LESS_OR_EQUAL, (StatementOperator.LessThanOrEqual, 8) },
        { KGVL.OPERATOR_LARGER_OR_EQUAL, (StatementOperator.LargerOrEqual, 8) },
        { KGVL.OPERATOR_LEFT_SHIFT, (StatementOperator.LeftShift, 9) },
        { KGVL.OPERATOR_RIGHT_SHIFT, (StatementOperator.RightShift, 9) },
        { KGVL.OPERATOR_UNSIGNED_RIGHT_SHIFT, (StatementOperator.UnsignedRightShift, 9) },
        { KGVL.OPERATOR_ADD, (StatementOperator.Addition, 10) },
        { KGVL.OPERATOR_SUBTRACT, (StatementOperator.Subtraction, 10) },
        { KGVL.OPERATOR_MULTIPLY, (StatementOperator.Multiplication, 11) },
        { KGVL.OPERATOR_DIVIDE, (StatementOperator.Division, 11) },
        { KGVL.OPERATOR_MODULO, (StatementOperator.Modulo, 11) }
    };

    private const int PRECEDENCE_NULL_COALESCE = 1;
    private const int PRECEDENCE_IS_CHECK = 8;

    private static readonly Dictionary<string, AssignmentOperator> _assignmentOperators = new()
    {
        { KGVL.ASSIGN, AssignmentOperator.Assign },
        { KGVL.ASSIGN_ADD, AssignmentOperator.AddAssign },
        { KGVL.ASSIGN_SUBTRACT, AssignmentOperator.SubtractAssign },
        { KGVL.ASSIGN_MULTIPLY, AssignmentOperator.MultiplyAssign },
        { KGVL.ASSIGN_DIVIDE, AssignmentOperator.DivideAssign },
        { KGVL.ASSIGN_MODULO, AssignmentOperator.ModuloAssign },
        { KGVL.ASSIGN_BITWISE_AND, AssignmentOperator.BitwiseAndAssign },
        { KGVL.ASSIGN_BITWISE_OR, AssignmentOperator.BitwiseOrAssign },
        { KGVL.ASSIGN_BITWISE_XOR, AssignmentOperator.BitwiseXorAssign },
        { KGVL.ASSIGN_LEFT_SHIFT, AssignmentOperator.LeftShiftAssign },
        { KGVL.ASSIGN_RIGHT_SHIFT, AssignmentOperator.RightShiftAssign },
        { KGVL.ASSIGN_UNSIGNED_RIGHT_SHIFT, AssignmentOperator.UnsignedRightShiftAssign },
        { KGVL.ASSIGN_NULL_COALESCE, AssignmentOperator.NullCoalesceAssign }
    };

    private static readonly Dictionary<string, StatementOperator> _prefixOperators = new()
    {
        { KGVL.OPERATOR_NOT, StatementOperator.Not },
        { KGVL.OPERATOR_BITWISE_COMPLEMENT, StatementOperator.BitwiseComplement },
        { KGVL.OPERATOR_SUBTRACT, StatementOperator.Negation },
        { KGVL.OPERATOR_ADD, StatementOperator.UnaryPlus },
        { KGVL.OPERATOR_INCREMENT, StatementOperator.Increment },
        { KGVL.OPERATOR_DECREMENT, StatementOperator.Decrement }
    };


    // Constructors.
    internal ExpressionParser(PackParsingContext context, StatementParser statementParser) : base(context)
    {
        _statementParser = statementParser ?? throw new ArgumentNullException(nameof(statementParser));
    }


    // Methods.
    /* Reads one complete expression, stopping before whatever terminator the caller expects rather
     * than consuming it, so the caller decides what a valid end looks like. */
    internal Statement ParseExpression()
    {
        return ParseAssignment();
    }


    // Private methods.
    /* Assignment is right associative, so "a = b = c" is "a = (b = c)". */
    private Statement ParseAssignment()
    {
        Statement Left = ParseTernary();

        Parser.SkipUntilNonWhitespace(null);
        string Spelling = PeekOperator();
        if ((Spelling.Length == 0) || !_assignmentOperators.TryGetValue(Spelling, out AssignmentOperator Operator))
        {
            return Left;
        }

        Parser.IncrementDataIndexNTimes(Spelling.Length);
        Parser.SkipUntilNonWhitespace(null);
        return new AssignmentStatement(Left, Operator, ParseAssignment());
    }

    private Statement ParseTernary()
    {
        Statement Condition = ParseBinary(0);

        Parser.SkipUntilNonWhitespace(null);
        /* A lone '?' here is the ternary; "??" and "?." were already taken by ParseBinary and
         * ParsePostfix, so anything still starting with '?' can only be this. */
        if ((Parser.GetCharAtDataIndex() != KGVL.TYPE_NULLABLE_INDICATOR)
            || Parser.HasStringAtIndex(Parser.DataIndex, KGVL.OPERATOR_NULL_COALESCE)
            || Parser.HasStringAtIndex(Parser.DataIndex, KGVL.OPERATOR_CONDITIONAL_ACCESS))
        {
            return Condition;
        }
        Parser.IncrementDataIndex();

        Parser.SkipUntilNonWhitespace(null);
        Statement IfBranch = ParseAssignment();

        Parser.SkipUntilNonWhitespace(null);
        if (Parser.GetCharAtDataIndex() != KGVL.TERNARY_BRANCH_SEPARATOR)
        {
            throw new SourceFileReadException(Parser, ErrorCreator.ExpectedTernaryElseBranch.CreateOptions());
        }
        Parser.IncrementDataIndex();

        Parser.SkipUntilNonWhitespace(null);
        Statement ElseBranch = ParseAssignment();

        TernaryStatement Ternary = new(Condition);
        Ternary.IfBody.AddStatement(IfBranch);
        Ternary.ElseBody = new();
        Ternary.ElseBody.AddStatement(ElseBranch);
        return Ternary;
    }

    private Statement ParseBinary(int minimumPrecedence)
    {
        Statement Left = ParseUnary();

        while (true)
        {
            Parser.SkipUntilNonWhitespace(null);

            if (TryReadIsCheck(Left, minimumPrecedence, out Statement? IsCheck))
            {
                Left = IsCheck!;
                continue;
            }

            string Spelling = PeekOperator();
            if ((Spelling.Length == 0)
                || _assignmentOperators.ContainsKey(Spelling)
                || !_binaryOperators.TryGetValue(Spelling, out (StatementOperator Operator, int Precedence) Found)
                || (Found.Precedence < minimumPrecedence))
            {
                break;
            }

            Parser.IncrementDataIndexNTimes(Spelling.Length);
            Parser.SkipUntilNonWhitespace(null);

            /* Right associative operators re-enter at their own precedence so that they nest to the
             * right, left associative ones at one above so that they nest to the left. */
            int NextPrecedence = Found.Precedence == PRECEDENCE_NULL_COALESCE
                ? Found.Precedence : Found.Precedence + 1;
            Left = new BinaryOperatorStatement(Found.Operator, Left, ParseBinary(NextPrecedence));
        }

        return Left;
    }

    /* "left is SomeType" reads as a binary operator even though its right side is a type rather
     * than a value, so it is matched here rather than in the symbol table. */
    private bool TryReadIsCheck(Statement left, int minimumPrecedence, out Statement? result)
    {
        result = null;
        if (PRECEDENCE_IS_CHECK < minimumPrecedence)
        {
            return false;
        }

        int StartIndex = Parser.DataIndex;
        if (!Parser.IsIdentifierFirstChar(Parser.GetCharAtDataIndex())
            || (Parser.ReadIdentifier(null) != KGVL.KEYWORD_IS))
        {
            Parser.DataIndex = StartIndex;
            return false;
        }

        Parser.SkipUntilNonWhitespace(null);
        TypeTargetIdentifier TargetType = Parser.ReadTypeTargetIdentifier(
            ErrorCreator.ExpectedIsCheckType.CreateOptions());
        result = new BinaryOperatorStatement(StatementOperator.IsCheck, left,
            new TypeOfStatement(TargetType));
        return true;
    }

    private Statement ParseUnary()
    {
        Parser.SkipUntilNonWhitespace(null);

        string Spelling = PeekOperator();
        if ((Spelling.Length > 0) && _prefixOperators.TryGetValue(Spelling, out StatementOperator Operator))
        {
            Parser.IncrementDataIndexNTimes(Spelling.Length);
            Parser.SkipUntilNonWhitespace(null);
            return new UnaryOperatorStatement(Operator, ParseUnary(), true);
        }

        if (TryParseCast(out Statement? Cast))
        {
            return Cast!;
        }

        return ParsePostfix();
    }

    /* "(SomeType)value" and "(someValue)" start identically, so this speculatively reads a type and
     * a closing bracket and then checks whether what follows could begin a value. If any step
     * fails the cursor is put back and the brackets are left to ParsePrimary. */
    private bool TryParseCast(out Statement? result)
    {
        result = null;
        if (Parser.GetCharAtDataIndex() != KGVL.OPEN_PARENTHESIS)
        {
            return false;
        }

        int StartIndex = Parser.DataIndex;
        Parser.IncrementDataIndex();
        Parser.SkipUntilNonWhitespace(null);

        if (!Parser.IsIdentifierFirstChar(Parser.GetCharAtDataIndex()))
        {
            Parser.DataIndex = StartIndex;
            return false;
        }

        TypeTargetIdentifier TargetType = Parser.ReadTypeTargetIdentifier(null);
        Parser.SkipUntilNonWhitespace(null);

        if ((Parser.GetCharAtDataIndex() != KGVL.CLOSE_PARENTHESIS) || !IsCastTarget(TargetType))
        {
            Parser.DataIndex = StartIndex;
            return false;
        }
        Parser.IncrementDataIndex();
        Parser.SkipUntilNonWhitespace(null);

        if (!CanStartValue())
        {
            Parser.DataIndex = StartIndex;
            return false;
        }

        result = new CastStatement(TargetType, ParseUnary());
        return true;
    }

    /* A bracketed name is only worth treating as a cast when it looks like a type. A built in type
     * keyword, generic arguments, array brackets or a nullable marker can only be a type; a bare
     * name is still ambiguous and is decided by what follows the brackets. */
    private bool IsCastTarget(TypeTargetIdentifier targetType)
    {
        return targetType.MainTarget.SourceCodeName.Length > 0;
    }

    /* Whether the next characters could begin a value. After a cast they must; after a bracketed
     * value a binary operator or a terminator would follow instead. */
    private bool CanStartValue()
    {
        char Character = Parser.GetCharAtDataIndex();
        if (Parser.IsIdentifierFirstChar(Character) || char.IsAsciiDigit(Character))
        {
            return true;
        }

        return (Character == KGVL.OPEN_PARENTHESIS)
            || (Character == KGVL.DOUBLE_QUOTE)
            || (Character == KGVL.SINGLE_QUOTE)
            || (Character == KGVL.STRING_INTERPOLATION_OPERATOR)
            || (Character == KGVL.OPERATOR_NOT[0])
            || (Character == KGVL.OPERATOR_BITWISE_COMPLEMENT[0]);
    }

    private Statement ParsePostfix()
    {
        Statement Current = ParsePrimary();

        while (true)
        {
            Parser.SkipUntilNonWhitespace(null);
            char Character = Parser.GetCharAtDataIndex();

            if (Parser.HasStringAtIndex(Parser.DataIndex, KGVL.OPERATOR_CONDITIONAL_ACCESS))
            {
                Parser.IncrementDataIndexNTimes(KGVL.OPERATOR_CONDITIONAL_ACCESS.Length);
                Parser.SkipUntilNonWhitespace(null);
                /* Everything after "?." is skipped as a unit when the left side is null, so the
                 * rest of the chain becomes the right operand rather than another link. */
                Current = new BinaryOperatorStatement(StatementOperator.ContinueIfNotNull,
                    Current, ParsePostfix());
                continue;
            }

            if (Character == KGVL.MEMBER_ACCESS)
            {
                Parser.IncrementDataIndex();
                Parser.SkipUntilNonWhitespace(null);
                Current = AppendAccess(Current, ParseAccessComponent());
                continue;
            }

            if (Character == KGVL.OPEN_SQUARE_BRACKET)
            {
                Current = ParseIndexAccess(Current);
                continue;
            }

            if (Character == KGVL.OPEN_PARENTHESIS)
            {
                Current = ConvertToCall(Current);
                continue;
            }

            if (Parser.HasStringAtIndex(Parser.DataIndex, KGVL.OPERATOR_INCREMENT))
            {
                Parser.IncrementDataIndexNTimes(KGVL.OPERATOR_INCREMENT.Length);
                Current = new UnaryOperatorStatement(StatementOperator.Increment, Current, false);
                continue;
            }

            if (Parser.HasStringAtIndex(Parser.DataIndex, KGVL.OPERATOR_DECREMENT))
            {
                Parser.IncrementDataIndexNTimes(KGVL.OPERATOR_DECREMENT.Length);
                Current = new UnaryOperatorStatement(StatementOperator.Decrement, Current, false);
                continue;
            }

            if (TryParseSwitchExpression(Current, out Statement? SwitchExpression))
            {
                Current = SwitchExpression!;
                continue;
            }

            break;
        }

        return Current;
    }

    private Statement ParseAccessComponent()
    {
        Identifier Name = new(Parser.ReadIdentifier(ErrorCreator.ExpectedMemberAccessName.CreateOptions()));
        return new IdentifiableAccessStatement(Name);
    }

    /* Member chains are kept flat, so "a.b.c" is one composite of three components rather than
     * three nested pairs. Resolution wants the whole chain at once. */
    private Statement AppendAccess(Statement current, Statement component)
    {
        if (current is CompositeAccessStatement Composite)
        {
            Composite.AddStatement(component);
            return Composite;
        }

        CompositeAccessStatement NewComposite = new();
        NewComposite.AddStatement(current);
        NewComposite.AddStatement(component);
        return NewComposite;
    }

    private Statement ParseIndexAccess(Statement target)
    {
        Parser.IncrementDataIndex();
        IndexAccessStatement Access = new(target);

        Parser.SkipUntilNonWhitespace(null);
        while (Parser.GetCharAtDataIndex() != KGVL.CLOSE_SQUARE_BRACKET)
        {
            Access.AddIndex(ParseAssignment());
            Parser.SkipUntilNonWhitespace(null);

            if (Parser.GetCharAtDataIndex() != KGVL.COMMA)
            {
                break;
            }
            Parser.IncrementDataIndex();
            Parser.SkipUntilNonWhitespace(null);
        }

        if (Parser.GetCharAtDataIndex() != KGVL.CLOSE_SQUARE_BRACKET)
        {
            throw new SourceFileReadException(Parser, ErrorCreator.ExpectedIndexAccessEnd.CreateOptions());
        }
        Parser.IncrementDataIndex();

        if (Access.IndexCount == 0)
        {
            AddError(ErrorCreator.ExpectedIndexAccessArgument.CreateOptions());
        }
        return Access;
    }

    /* A call is written as a name followed by brackets, so by the time the brackets are seen the
     * name has already been read as an access. The access becomes the call's name. */
    private Statement ConvertToCall(Statement current)
    {
        if (current is IdentifiableAccessStatement DirectAccess)
        {
            NamedFunctionCallStatement Call = new(DirectAccess.MemberIdentifier);
            ParseCallArguments(Call);
            return Call;
        }

        if ((current is CompositeAccessStatement Composite)
            && (Composite.Components.LastOrDefault() is IdentifiableAccessStatement LastAccess))
        {
            NamedFunctionCallStatement Call = new(LastAccess.MemberIdentifier);
            ParseCallArguments(Call);

            Composite.RemoveStatement(LastAccess);
            Composite.AddStatement(Call);
            return Composite;
        }

        throw new SourceFileReadException(Parser, ErrorCreator.UncallableStatement.CreateOptions());
    }

    private void ParseCallArguments(FunctionCallStatement call)
    {
        Parser.IncrementDataIndex();
        Parser.SkipUntilNonWhitespace(null);

        while (Parser.GetCharAtDataIndex() != KGVL.CLOSE_PARENTHESIS)
        {
            call.AddArgument(ParseCallArgument());
            Parser.SkipUntilNonWhitespace(null);

            if (Parser.GetCharAtDataIndex() != KGVL.COMMA)
            {
                break;
            }
            Parser.IncrementDataIndex();
            Parser.SkipUntilNonWhitespace(null);
        }

        if (Parser.GetCharAtDataIndex() != KGVL.CLOSE_PARENTHESIS)
        {
            throw new SourceFileReadException(Parser, ErrorCreator.ExpectedCallArgumentsEnd.CreateOptions());
        }
        Parser.IncrementDataIndex();
    }

    private FunctionArgument ParseCallArgument()
    {
        FunctionParameterModifier Modifier = ReadArgumentModifier();

        Identifier? Name = TryReadArgumentName();
        Parser.SkipUntilNonWhitespace(null);

        return new FunctionArgument(ParseAssignment(), Name, Modifier);
    }

    private FunctionParameterModifier ReadArgumentModifier()
    {
        int StartIndex = Parser.DataIndex;
        if (!Parser.IsIdentifierFirstChar(Parser.GetCharAtDataIndex()))
        {
            return FunctionParameterModifier.None;
        }

        FunctionParameterModifier Modifier = Parser.ReadIdentifier(null) switch
        {
            KGVL.KEYWORD_REF => FunctionParameterModifier.Ref,
            KGVL.KEYWORD_OUT => FunctionParameterModifier.Out,
            KGVL.KEYWORD_IN => FunctionParameterModifier.In,
            _ => FunctionParameterModifier.None
        };

        if (Modifier == FunctionParameterModifier.None)
        {
            Parser.DataIndex = StartIndex;
            return FunctionParameterModifier.None;
        }

        Parser.SkipUntilNonWhitespace(null);
        return Modifier;
    }

    /* "name: value" names an argument, but "name" on its own is a value and "a ? b : c" also has a
     * colon in it, so the name is only taken when an identifier is directly followed by one. */
    private Identifier? TryReadArgumentName()
    {
        int StartIndex = Parser.DataIndex;
        if (!Parser.IsIdentifierFirstChar(Parser.GetCharAtDataIndex()))
        {
            return null;
        }

        string Name = Parser.ReadIdentifier(null);
        Parser.SkipUntilNonWhitespace(null);

        if ((Name.Length == 0) || (Parser.GetCharAtDataIndex() != KGVL.COLON))
        {
            Parser.DataIndex = StartIndex;
            return null;
        }

        Parser.IncrementDataIndex();
        return new Identifier(Name);
    }

    private bool TryParseSwitchExpression(Statement target, out Statement? result)
    {
        result = null;
        int StartIndex = Parser.DataIndex;

        if (!Parser.IsIdentifierFirstChar(Parser.GetCharAtDataIndex())
            || (Parser.ReadIdentifier(null) != KGVL.KEYWORD_SWITCH))
        {
            Parser.DataIndex = StartIndex;
            return false;
        }

        Parser.SkipUntilNonWhitespace(null);
        if (Parser.GetCharAtDataIndex() != KGVL.OPEN_CURLY_BRACKET)
        {
            /* A switch statement, not a switch expression - it is the statement parser's. */
            Parser.DataIndex = StartIndex;
            return false;
        }
        Parser.IncrementDataIndex();

        SwitchExpressionStatement Switch = new(target);
        Parser.SkipUntilNonWhitespace(null);

        while (Parser.IsMoreDataAvailable && (Parser.GetCharAtDataIndex() != KGVL.CLOSE_CURLY_BRACKET))
        {
            Switch.AddArm(ParseSwitchExpressionArm());
            Parser.SkipUntilNonWhitespace(null);

            if (Parser.GetCharAtDataIndex() != KGVL.COMMA)
            {
                break;
            }
            Parser.IncrementDataIndex();
            Parser.SkipUntilNonWhitespace(null);
        }

        if (Parser.GetCharAtDataIndex() != KGVL.CLOSE_CURLY_BRACKET)
        {
            throw new SourceFileReadException(Parser, ErrorCreator.ExpectedSwitchExpressionEnd.CreateOptions());
        }
        Parser.IncrementDataIndex();

        result = Switch;
        return true;
    }

    private SwitchExpressionArm ParseSwitchExpressionArm()
    {
        Statement? Pattern = null;
        if (Parser.GetCharAtDataIndex() == KGVL.UNDERSCORE)
        {
            int StartIndex = Parser.DataIndex;
            if (Parser.ReadIdentifier(null) != KGVL.UNDERSCORE.ToString())
            {
                Parser.DataIndex = StartIndex;
                Pattern = ParseAssignment();
            }
        }
        else
        {
            Pattern = ParseAssignment();
        }

        Parser.SkipUntilNonWhitespace(null);
        SwitchExpressionArm Arm = new(Pattern, ParseArmValue());
        return Arm;
    }

    private Statement ParseArmValue()
    {
        if (!Parser.HasStringAtIndex(Parser.DataIndex, KGVL.QUICK_METHOD_BODY))
        {
            throw new SourceFileReadException(Parser, ErrorCreator.ExpectedSwitchArmArrow.CreateOptions());
        }
        Parser.IncrementDataIndexNTimes(KGVL.QUICK_METHOD_BODY.Length);

        Parser.SkipUntilNonWhitespace(null);
        return ParseAssignment();
    }

    private Statement ParsePrimary()
    {
        Parser.SkipUntilNonWhitespace(null);
        char Character = Parser.GetCharAtDataIndex();

        if (Character == KGVL.DOUBLE_QUOTE)
        {
            return new PrimitiveValueStatement(ParseString());
        }
        if (Character == KGVL.STRING_INTERPOLATION_OPERATOR)
        {
            return ParseInterpolatedString();
        }
        if (Character == KGVL.SINGLE_QUOTE)
        {
            return ParseCharacter();
        }
        if (char.IsAsciiDigit(Character))
        {
            return ParseNumber();
        }
        if (Character == KGVL.OPEN_PARENTHESIS)
        {
            return ParseLambdaOrBracketedValue();
        }

        if (Parser.IsIdentifierFirstChar(Character))
        {
            return ParseIdentifierPrimary();
        }

        throw new SourceFileReadException(Parser, ErrorCreator.ExpectedValue.CreateOptions(Character));
    }

    private Statement ParseIdentifierPrimary()
    {
        int StartIndex = Parser.DataIndex;
        string Word = Parser.ReadIdentifier(null);

        switch (Word)
        {
            case KGVL.KEYWORD_TRUE:
            case KGVL.KEYWORD_FALSE:
                return new PrimitiveValueStatement(Word == KGVL.KEYWORD_TRUE);

            case KGVL.KEYWORD_NULL:
                return new PrimitiveValueStatement(KGVL.KEYWORD_NULL);

            case KGVL.KEYWORD_THIS:
                return new ThisStatement();

            case KGVL.KEYWORD_BASE:
                return new BaseStatement();

            case KGVL.KEYWORD_NEW:
                return ParseConstruction();

            case KGVL.KEYWORD_NAMEOF:
                return ParseNameOf();

            case KGVL.KEYWORD_TYPEOF:
                return ParseTypeOf();

            case KGVL.KEYWORD_DEFAULT:
                return ParseDefault();
        }

        /* A single parameter lambda needs no brackets, as in "x => x + 1". */
        Parser.SkipUntilNonWhitespace(null);
        if (Parser.HasStringAtIndex(Parser.DataIndex, KGVL.QUICK_METHOD_BODY))
        {
            return ParseLambdaBody(new FunctionParameter(null, new Identifier(Word),
                FunctionParameterModifier.None));
        }

        Parser.DataIndex = StartIndex;
        Parser.ReadIdentifier(null);
        return new IdentifiableAccessStatement(new Identifier(Word));
    }

    private Statement ParseNameOf()
    {
        ExpectOpenParenthesis();
        Identifier Target = new(Parser.ReadIdentifier(ErrorCreator.ExpectedNameOfTarget.CreateOptions()));
        ExpectCloseParenthesis();
        return new NameOfStatement(Target);
    }

    private Statement ParseTypeOf()
    {
        ExpectOpenParenthesis();
        TypeTargetIdentifier Target = Parser.ReadTypeTargetIdentifier(
            ErrorCreator.ExpectedTypeOfTarget.CreateOptions());
        ExpectCloseParenthesis();
        return new TypeOfStatement(Target);
    }

    /* "default" may stand alone, in which case its type comes from context. */
    private Statement ParseDefault()
    {
        Parser.SkipUntilNonWhitespace(null);
        if (Parser.GetCharAtDataIndex() != KGVL.OPEN_PARENTHESIS)
        {
            return new DefaultStatement(null);
        }

        ExpectOpenParenthesis();
        TypeTargetIdentifier Target = Parser.ReadTypeTargetIdentifier(
            ErrorCreator.ExpectedTypeOfTarget.CreateOptions());
        ExpectCloseParenthesis();
        return new DefaultStatement(Target);
    }

    private void ExpectOpenParenthesis()
    {
        Parser.SkipUntilNonWhitespace(null);
        if (Parser.GetCharAtDataIndex() != KGVL.OPEN_PARENTHESIS)
        {
            throw new SourceFileReadException(Parser,
                ErrorCreator.ExpectedOpenParenthesis.CreateOptions());
        }
        Parser.IncrementDataIndex();
        Parser.SkipUntilNonWhitespace(null);
    }

    private void ExpectCloseParenthesis()
    {
        Parser.SkipUntilNonWhitespace(null);
        if (Parser.GetCharAtDataIndex() != KGVL.CLOSE_PARENTHESIS)
        {
            throw new SourceFileReadException(Parser,
                ErrorCreator.ExpectedCloseParenthesis.CreateOptions());
        }
        Parser.IncrementDataIndex();
    }

    /* Longest match wins, so the spellings array must stay ordered longest first. */
    private string PeekOperator()
    {
        foreach (string Spelling in _operatorSpellings)
        {
            if (Parser.HasStringAtIndex(Parser.DataIndex, Spelling))
            {
                return Spelling;
            }
        }
        return string.Empty;
    }


    /* Construction. */
    private Statement ParseConstruction()
    {
        Parser.SkipUntilNonWhitespace(null);

        /* "new[] { ... }" leaves the element type to be inferred from the values. */
        if (Parser.GetCharAtDataIndex() == KGVL.OPEN_SQUARE_BRACKET)
        {
            Parser.IncrementDataIndex();
            Parser.SkipUntilNonWhitespace(null);
            if (Parser.GetCharAtDataIndex() != KGVL.CLOSE_SQUARE_BRACKET)
            {
                throw new SourceFileReadException(Parser,
                    ErrorCreator.ExpectedInferredArrayEnd.CreateOptions());
            }
            Parser.IncrementDataIndex();

            ArrayCreationStatement InferredArray = new(null);
            InferredArray.Elements = ParseBracedValueList();
            return InferredArray;
        }

        TypeTargetIdentifier CreatedType = Parser.ReadTypeTargetIdentifier(
            ErrorCreator.ExpectedConstructedType.CreateOptions());
        Parser.SkipUntilNonWhitespace(null);

        /* ReadTypeTargetIdentifier already swallowed any empty "[]" pairs as array rank, so a type
         * arriving here with a rank can only be "new int[] { ... }". */
        if (CreatedType.IsArray)
        {
            ArrayCreationStatement SizedArray = new(CreatedType);
            SizedArray.Elements = ParseBracedValueList();
            return SizedArray;
        }

        if (Parser.GetCharAtDataIndex() == KGVL.OPEN_SQUARE_BRACKET)
        {
            return ParseSizedArrayCreation(CreatedType);
        }

        ConstructorCallStatement Call = new(CreatedType);
        if (Parser.GetCharAtDataIndex() == KGVL.OPEN_PARENTHESIS)
        {
            ParseCallArguments(Call);
            Parser.SkipUntilNonWhitespace(null);
        }

        if (Parser.GetCharAtDataIndex() == KGVL.OPEN_CURLY_BRACKET)
        {
            Call.Initializer = ParseBracedValueList();
        }
        return Call;
    }

    private Statement ParseSizedArrayCreation(TypeTargetIdentifier elementType)
    {
        ArrayCreationStatement Array = new(elementType);
        Parser.IncrementDataIndex();
        Parser.SkipUntilNonWhitespace(null);

        while (Parser.GetCharAtDataIndex() != KGVL.CLOSE_SQUARE_BRACKET)
        {
            Array.AddLength(ParseAssignment());
            Parser.SkipUntilNonWhitespace(null);

            if (Parser.GetCharAtDataIndex() != KGVL.COMMA)
            {
                break;
            }
            Parser.IncrementDataIndex();
            Parser.SkipUntilNonWhitespace(null);
        }

        if (Parser.GetCharAtDataIndex() != KGVL.CLOSE_SQUARE_BRACKET)
        {
            throw new SourceFileReadException(Parser, ErrorCreator.ExpectedArrayLengthEnd.CreateOptions());
        }
        Parser.IncrementDataIndex();

        /* Any further empty brackets deepen the element type rather than adding lengths, so that
         * "new int[2][]" is two arrays of int rather than two ints. */
        int ExtraRank = 0;
        while (Parser.ReadOneArrayLevel())
        {
            ExtraRank++;
        }

        if (ExtraRank > 0)
        {
            Array.ElementType = DeepenArrayType(elementType, ExtraRank);
        }
        return Array;
    }

    private TypeTargetIdentifier DeepenArrayType(TypeTargetIdentifier elementType, int extraRank)
    {
        List<bool> Nullability = elementType.NullabilityByLevel.ToList();
        for (int i = 0; i < extraRank; i++)
        {
            Nullability.Add(false);
        }
        elementType.SetNullabilityByLevel(Nullability);
        return elementType;
    }

    /* The braced part of an object, collection or array initializer. Member initializers inside it
     * are assignments and collection elements are plain values, so both are read as expressions. */
    private StatementCollection ParseBracedValueList()
    {
        Parser.SkipUntilNonWhitespace(null);
        if (Parser.GetCharAtDataIndex() != KGVL.OPEN_CURLY_BRACKET)
        {
            throw new SourceFileReadException(Parser, ErrorCreator.ExpectedInitializerStart.CreateOptions());
        }
        Parser.IncrementDataIndex();

        StatementCollection Values = new();
        Parser.SkipUntilNonWhitespace(null);

        while (Parser.IsMoreDataAvailable && (Parser.GetCharAtDataIndex() != KGVL.CLOSE_CURLY_BRACKET))
        {
            Values.AddStatement(ParseAssignment());
            Parser.SkipUntilNonWhitespace(null);

            if (Parser.GetCharAtDataIndex() != KGVL.COMMA)
            {
                break;
            }
            Parser.IncrementDataIndex();
            Parser.SkipUntilNonWhitespace(null);
        }

        if (Parser.GetCharAtDataIndex() != KGVL.CLOSE_CURLY_BRACKET)
        {
            throw new SourceFileReadException(Parser, ErrorCreator.ExpectedInitializerEnd.CreateOptions());
        }
        Parser.IncrementDataIndex();
        return Values;
    }


    /* Lambdas and bracketed values. */
    private Statement ParseLambdaOrBracketedValue()
    {
        if (TryParseBracketedLambda(out Statement? Lambda))
        {
            return Lambda!;
        }

        Parser.IncrementDataIndex();
        Parser.SkipUntilNonWhitespace(null);
        Statement Value = ParseAssignment();
        ExpectCloseParenthesis();
        return Value;
    }

    /* "(a, b) => ..." and "(int a) => ..." only reveal themselves as lambdas at the arrow, so the
     * parameter list is read speculatively and put back if no arrow follows. */
    private bool TryParseBracketedLambda(out Statement? result)
    {
        result = null;
        int StartIndex = Parser.DataIndex;

        Parser.IncrementDataIndex();
        Parser.SkipUntilNonWhitespace(null);

        List<FunctionParameter> Parameters = new();
        while (Parser.IsMoreDataAvailable && (Parser.GetCharAtDataIndex() != KGVL.CLOSE_PARENTHESIS))
        {
            if (!TryReadLambdaParameter(out FunctionParameter? Parameter))
            {
                Parser.DataIndex = StartIndex;
                return false;
            }
            Parameters.Add(Parameter!);

            Parser.SkipUntilNonWhitespace(null);
            if (Parser.GetCharAtDataIndex() != KGVL.COMMA)
            {
                break;
            }
            Parser.IncrementDataIndex();
            Parser.SkipUntilNonWhitespace(null);
        }

        if (Parser.GetCharAtDataIndex() != KGVL.CLOSE_PARENTHESIS)
        {
            Parser.DataIndex = StartIndex;
            return false;
        }
        Parser.IncrementDataIndex();
        Parser.SkipUntilNonWhitespace(null);

        if (!Parser.HasStringAtIndex(Parser.DataIndex, KGVL.QUICK_METHOD_BODY))
        {
            Parser.DataIndex = StartIndex;
            return false;
        }

        result = ParseLambdaBody(Parameters.ToArray());
        return true;
    }

    /* A lambda parameter is either "name" or "type name". Which one it is only becomes clear after
     * reading the first token and seeing whether another identifier follows it. */
    private bool TryReadLambdaParameter(out FunctionParameter? parameter)
    {
        parameter = null;
        if (!Parser.IsIdentifierFirstChar(Parser.GetCharAtDataIndex()))
        {
            return false;
        }

        TypeTargetIdentifier FirstToken = Parser.ReadTypeTargetIdentifier(null);
        Parser.SkipUntilNonWhitespace(null);

        if (!Parser.IsIdentifierFirstChar(Parser.GetCharAtDataIndex()))
        {
            if (FirstToken.IsArray || (FirstToken.TypeArguments.Length > 0))
            {
                return false;
            }
            parameter = new(null, FirstToken.MainTarget, FunctionParameterModifier.None);
            return true;
        }

        string Name = Parser.ReadIdentifier(null);
        if (Name.Length == 0)
        {
            return false;
        }
        parameter = new(FirstToken, new Identifier(Name), FunctionParameterModifier.None);
        return true;
    }

    private Statement ParseLambdaBody(params FunctionParameter[] parameters)
    {
        Parser.SkipUntilNonWhitespace(null);
        if (!Parser.HasStringAtIndex(Parser.DataIndex, KGVL.QUICK_METHOD_BODY))
        {
            throw new SourceFileReadException(Parser, ErrorCreator.ExpectedLambdaArrow.CreateOptions());
        }
        Parser.IncrementDataIndexNTimes(KGVL.QUICK_METHOD_BODY.Length);
        Parser.SkipUntilNonWhitespace(null);

        LambdaStatement Lambda = new();
        foreach (FunctionParameter Parameter in parameters)
        {
            Lambda.Parameters.AddItem(Parameter);
        }

        if (Parser.GetCharAtDataIndex() == KGVL.OPEN_CURLY_BRACKET)
        {
            Lambda.Body.SetFrom(_statementParser.ParseStatementBody());
            return Lambda;
        }

        Lambda.IsExpressionBodied = true;
        Lambda.Body.AddStatement(ParseAssignment());
        return Lambda;
    }


    /* Literals. */
    private Statement ParseNumber()
    {
        object? Number = Parser.ReadNumber(ErrorCreator.ExpectedNumberValue.CreateOptions());
        return new PrimitiveValueStatement(Number ?? 0);
    }

    private Statement ParseCharacter()
    {
        CharConstant? Character = Parser.ReadCharacter(ErrorCreator.ExpectedCharacterValue.CreateOptions());
        return new PrimitiveValueStatement(Character ?? new CharConstant(KGVL.CHAR_NONE));
    }

    internal string ParseString()
    {
        if (Parser.GetCharAtDataIndex() != KGVL.DOUBLE_QUOTE)
        {
            throw new SourceFileReadException(Parser, ErrorCreator.ExpectedStringStart.CreateOptions());
        }
        Parser.IncrementDataIndex();

        StringBuilder Builder = new();
        while (Parser.IsMoreDataAvailable && (Parser.GetCharAtDataIndex() != KGVL.DOUBLE_QUOTE))
        {
            if (Parser.GetCharAtDataIndex() == KGVL.ESCAPE_CHAR)
            {
                Builder.Append(ReadEscapedCharacter());
                continue;
            }
            Builder.Append(Parser.GetCharAtDataIndex());
            Parser.IncrementDataIndex();
        }

        if (Parser.GetCharAtDataIndex() != KGVL.DOUBLE_QUOTE)
        {
            throw new SourceFileReadException(Parser, ErrorCreator.ExpectedStringEnd.CreateOptions());
        }
        Parser.IncrementDataIndex();
        return Builder.ToString();
    }

    /* Interpolated strings keep their literal runs and their substituted expressions as separate
     * sections, in source order. "{{" and "}}" are literal braces rather than a substitution. */
    internal Statement ParseInterpolatedString()
    {
        Parser.IncrementDataIndex();
        if (Parser.GetCharAtDataIndex() != KGVL.DOUBLE_QUOTE)
        {
            throw new SourceFileReadException(Parser, ErrorCreator.ExpectedStringStart.CreateOptions());
        }
        Parser.IncrementDataIndex();

        InterpolatedStringStatement Interpolated = new();
        StringBuilder Literal = new();

        while (Parser.IsMoreDataAvailable && (Parser.GetCharAtDataIndex() != KGVL.DOUBLE_QUOTE))
        {
            char Character = Parser.GetCharAtDataIndex();

            if (Character == KGVL.ESCAPE_CHAR)
            {
                Literal.Append(ReadEscapedCharacter());
            }
            else if (Parser.HasStringAtIndex(Parser.DataIndex, KGVL.DOUBLE_CURLY_OPEN))
            {
                Literal.Append(KGVL.INTERPOLATION_SECTION_START);
                Parser.IncrementDataIndexNTimes(KGVL.DOUBLE_CURLY_OPEN.Length);
            }
            else if (Parser.HasStringAtIndex(Parser.DataIndex, KGVL.DOUBLE_CURLY_CLOSE))
            {
                Literal.Append(KGVL.INTERPOLATION_SECTION_END);
                Parser.IncrementDataIndexNTimes(KGVL.DOUBLE_CURLY_CLOSE.Length);
            }
            else if (Character == KGVL.INTERPOLATION_SECTION_START)
            {
                if (Literal.Length > 0)
                {
                    Interpolated.AddSection(new InterpolatedStringSection(Literal.ToString()));
                    Literal.Clear();
                }
                Interpolated.AddSection(new InterpolatedStringSection(ParseInterpolationSection()));
            }
            else
            {
                Literal.Append(Character);
                Parser.IncrementDataIndex();
            }
        }

        if (Parser.GetCharAtDataIndex() != KGVL.DOUBLE_QUOTE)
        {
            throw new SourceFileReadException(Parser, ErrorCreator.ExpectedStringEnd.CreateOptions());
        }
        Parser.IncrementDataIndex();

        if (Literal.Length > 0)
        {
            Interpolated.AddSection(new InterpolatedStringSection(Literal.ToString()));
        }
        return Interpolated;
    }

    private Statement ParseInterpolationSection()
    {
        Parser.IncrementDataIndex();
        Parser.SkipUntilNonWhitespace(null);

        Statement Value = ParseAssignment();

        Parser.SkipUntilNonWhitespace(null);
        if (Parser.GetCharAtDataIndex() != KGVL.INTERPOLATION_SECTION_END)
        {
            throw new SourceFileReadException(Parser,
                ErrorCreator.ExpectedInterpolationSectionEnd.CreateOptions());
        }
        Parser.IncrementDataIndex();
        return Value;
    }

    private string ReadEscapedCharacter()
    {
        Parser.IncrementDataIndex();
        char Indicator = Parser.GetCharAtDataIndex();

        if ((Indicator != KGVL.ESCAPE_SEQUENCE_CODEPOINT_INDICATOR)
            && (Indicator != KGVL.ESCAPE_SEQUENCE_HEX_INDICATOR))
        {
            Parser.IncrementDataIndex();
            return Parser.EscapeSequenceToChar(Indicator.ToString()).ToString();
        }

        StringBuilder Sequence = new();
        Sequence.Append(Indicator);
        Parser.IncrementDataIndex();

        const int MAX_DIGIT_COUNT = 4;
        while ((Sequence.Length <= MAX_DIGIT_COUNT) && char.IsAsciiHexDigit(Parser.GetCharAtDataIndex()))
        {
            Sequence.Append(Parser.GetCharAtDataIndex());
            Parser.IncrementDataIndex();
        }

        return Parser.EscapeSequenceToChar(Sequence.ToString()).ToString();
    }
}
