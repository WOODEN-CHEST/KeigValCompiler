﻿using KeigValCompiler.Error;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KeigValCompiler.Source.Parser;

internal class CommentStripper
{
    // Private fields.
    private readonly SourceDataParser _parser;
    private readonly ErrorRepository _errorRepository;


    // Constructors.
    internal CommentStripper(SourceDataParser parser, ErrorRepository errors)
    {
        _parser = parser ?? throw new ArgumentNullException(nameof(parser));
        _errorRepository = errors ?? throw new ArgumentNullException(nameof(errors));
    }

    // Methods.
    public string StripCommentsFromCode(string code)
    {
        _parser.DataIndex = 0;
        StringBuilder StrippedData = new(_parser.DataLength);

        ParseCode(StrippedData, false);

        return StrippedData.ToString();
    }


    // Private methods.
    private void ParseCode(StringBuilder strippedData, bool isInterpolation)
    {
        bool IsMoreDataAvailable = true;
        while (IsMoreDataAvailable)
        {
            char Character = _parser.GetCharAtDataIndex();

            if (Character == KGVL.DOUBLE_QUOTE)
            {
                ReadQuotedInclQuote(strippedData, KGVL.DOUBLE_QUOTE, false);
            }
            else if (Character == KGVL.SINGLE_QUOTE)
            {
                ReadQuotedInclQuote(strippedData, KGVL.SINGLE_QUOTE, false);
            }
            else if ((Character == KGVL.STRING_INTERPOLATION_OPERATOR) &&
                (_parser.GetCharAtDataIndex(_parser.DataIndex + 1) == KGVL.DOUBLE_QUOTE))
            {
                _parser.IncrementDataIndex();
                strippedData.Append(KGVL.STRING_INTERPOLATION_OPERATOR);
                ReadQuotedInclQuote(strippedData, KGVL.DOUBLE_QUOTE, true);
            }
            else if (_parser.HasStringAtIndex(_parser.DataIndex, KGVL.SINGLE_LINE_COMMENT_START))
            {
                _parser.SkipUntil(null, KGVL.NEWLINE);
                strippedData.Append(KGVL.NEWLINE);
                _parser.IncrementDataIndex();
            }
            else if (_parser.HasStringAtIndex(_parser.DataIndex, KGVL.MULTI_LINE_COMMENT_START))
            {
                int StartLine = _parser.Line;
                _parser.SkipPastString(_errorRepository.ExpectedMultiLineCommentEnd
                    .CreateOptions(StartLine), KGVL.MULTI_LINE_COMMENT_END);
                int EndLine = _parser.Line;
                strippedData.Append(KGVL.NEWLINE, EndLine - StartLine);
            }
            else
            {
                strippedData.Append(Character);
                _parser.IncrementDataIndex();
            }

            IsMoreDataAvailable = _parser.IsMoreDataAvailable && (!isInterpolation 
                || (Character != KGVL.CLOSE_CURLY_BRACKET));
        }
    }


    private void ReadQuotedInclQuote(StringBuilder strippedData, char targetEndQuote, bool isInterpolated)
    {
        strippedData.Append(_parser.GetCharAtDataIndex());
        _parser.IncrementDataIndex();

        bool IsInEscapeSequence = false;
        bool IsInterpolationSuggested = false;
        char Character;

        Character = _parser.GetCharAtDataIndex();
        while (((Character != targetEndQuote) || IsInEscapeSequence) && _parser.IsMoreDataAvailable)
        {
            IsInEscapeSequence = !IsInEscapeSequence && (Character == KGVL.ESCAPE_CHAR);

            if (IsInterpolationSuggested && (Character != KGVL.OPEN_CURLY_BRACKET))
            {
                ParseCode(strippedData, true);
            }
            else
            {
                strippedData.Append(Character);
                _parser.IncrementDataIndex();
            }

            IsInterpolationSuggested = isInterpolated
                    && !IsInterpolationSuggested
                    && (Character == KGVL.OPEN_CURLY_BRACKET);

            Character = _parser.GetCharAtDataIndex();
        }

        if (!_parser.IsMoreDataAvailable)
        {
            throw new SourceFileReadException($"Expected end of quoted block with {targetEndQuote}");
        }

        strippedData.Append(Character);
        _parser.IncrementDataIndex();
    }
}