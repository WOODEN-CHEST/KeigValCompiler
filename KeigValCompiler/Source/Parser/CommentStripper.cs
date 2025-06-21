using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KeigValCompiler.Source.Parser;

public class CommentStripper
{
    // Private fields.
    private readonly TextParser _parser;


    // Constructors.
    public CommentStripper(TextParser parser)
    {
        _parser = parser ?? throw new ArgumentNullException(nameof(parser));
    }

    // Methods.
    public string StripCommentsFromCode(string code)
    {
        _parser.DataIndex = 0;
        StringBuilder StrippedData = new(_parser.DataLength);

        while (_parser.DataIndex < _parser.DataLength)
        {
            char Character = _parser.GetCharAtDataIndex();

            if (Character == KGVL.DOUBLE_QUOTE)
            {
                StrippedData.Append(ReadQuotedInclQuote(KGVL.DOUBLE_QUOTE));
            }
            else if (Character == KGVL.SINGLE_QUOTE)
            {
                StrippedData.Append(ReadQuotedInclQuote(KGVL.SINGLE_QUOTE));
            }
            else if (_parser.HasStringAtIndex(_parser.DataIndex, KGVL.SINGLE_LINE_COMMENT_START))
            {
                _parser.SkipUntil(null, KGVL.NEWLINE);
                StrippedData.Append(_parser.GetCharAtDataIndex());
                _parser.IncrementDataIndex();
            }
            else if (_parser.HasStringAtIndex(_parser.DataIndex, KGVL.MULTI_LINE_COMMENT_START))
            {
                _parser.SkipPastString("Multi-comment string wasn't terminated properly.", KGVL.MULTI_LINE_COMMENT_END);
                continue;
            }
            else
            {
                StrippedData.Append(Character);
                _parser.IncrementDataIndex();
            }
        }

        return StrippedData.ToString();
    }


    // Private methods.

    /* For not comments are not allowed in interpolated strings. */

    //private string ReadInterpolatedStringInclSyntax()
    //{
    //    StringBuilder ReadData = new();
    //    ReadData.Append(_parser.GetCharAtDataIndex());
    //    _parser.IncrementDataIndex();
    //    ReadData.Append(_parser.ReadUntilNonWhitespace(null));
    //    if (_parser.GetCharAtDataIndex() != KGVL.DOUBLE_QUOTE)
    //    {
    //        return ReadData.ToString();
    //    }

    //    ReadData.Append(_parser.GetCharAtDataIndex());
    //    _parser.IncrementDataIndex();

    //    bool WasInterpolationEscaped = false;
    //    while (_parser.GetCharAtDataIndex() != KGVL.DOUBLE_QUOTE)
    //    {
    //        char Character = GetCharAtDataIndex();
    //        bool HasDoubleInterpSymbol = (Character == KGVL.OPEN_CURLY_BRACKET)
    //            && (GetCharAtDataIndex(_dataIndex + 1) == KGVL.OPEN_CURLY_BRACKET);

    //        if ((Character == KGVL.OPEN_CURLY_BRACKET) && !HasDoubleInterpSymbol && !WasInterpolationEscaped)
    //        {
    //            ReadData.Append(ReadInterpolation());
    //        }
    //        else
    //        {
    //            ReadData.Append(Character);
    //        }

    //        WasInterpolationEscaped = !WasInterpolationEscaped && HasDoubleInterpSymbol;
    //        IncrementDataIndex();
    //    }

    //    ReadData.Append(GetCharAtDataIndex());

    //    return ReadData.ToString();
    //}

    //private string ReadInterpolation()
    //{
    //    StringBuilder Interpolation = new();
    //    char Character;

    //    while (((Character = GetCharAtDataIndex()) != KGVL.CLOSE_CURLY_BRACKET) && (_dataIndex < _data.Length))
    //    {
    //        if (Character == KGVL.SINGLE_QUOTE)
    //        {
    //            Interpolation.Append(ReadQuotedInclQuote(KGVL.SINGLE_QUOTE));
    //        }
    //        else if (Character == KGVL.STRING_INTERPOLATION_OPERATOR)
    //        {
    //            Interpolation.Append(ReadInterpolatedStringInclSyntax());
    //        }
    //        else if (Character == KGVL.DOUBLE_QUOTE)
    //        {
    //            Interpolation.Append(ReadQuotedInclQuote(KGVL.DOUBLE_QUOTE));
    //        }
    //        else
    //        {
    //            Interpolation.Append(Character);
    //        }
    //        IncrementDataIndex();
    //    }
    //    if (_dataIndex >= _data.Length)
    //    {
    //        throw new SourceFileException(this, "Expected end of interpolated string's interpolation block.");
    //    }
    //    Interpolation.Append(Character);
    //    return Interpolation.ToString();
    //}

    private string ReadQuotedInclQuote(char targetQuote)
    {
        StringBuilder Target = new();
        Target.Append(_parser.GetCharAtDataIndex());
        _parser.IncrementDataIndex();

        bool IsInEscapeSequence = false;
        char Character;

        Character = _parser.GetCharAtDataIndex();
        while (((Character != targetQuote) || IsInEscapeSequence) && _parser.IsMoreDataAvailable)
        {
            IsInEscapeSequence = !IsInEscapeSequence && (Character == KGVL.ESCAPE_CHAR);
            Target.Append(Character);
            _parser.IncrementDataIndex();
            Character = _parser.GetCharAtDataIndex();
        }

        if (!_parser.IsMoreDataAvailable)
        {
            throw new SourceFileReadException($"Expected end of quoted block with {targetQuote}");
        }

        Target.Append(Character);
        _parser.IncrementDataIndex();
        return Target.ToString();
    }
}