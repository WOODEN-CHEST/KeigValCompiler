using KeigValCompiler.Error;
using KeigValCompiler.Semantician;
using KeigValCompiler.Semantician.Member;
using KeigValCompiler.Semantician.Member.Code;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace KeigValCompiler.Source.Parser;

public class SourceDataParser
{
    // Fields.
    internal int Line { get; private set; } = 1;
    internal int DataIndex
    {
        get => _dataIndex;
        set
        {
            /* Done this way so current line number doesn't get screwed up,
            * though I agree that this is a bad solution. Better to track lines in a way that they 
            * can be indexed by the data index so that this doesn't need to be done. */
            int TargetValue = Math.Clamp(value, 0, DataLength);
            if (TargetValue > DataIndex)
            {
                IncrementDataIndexNTimes(TargetValue - DataIndex);
            }
            else
            {
                DecrementDataIndexNTimes(DataIndex - TargetValue);
            }
        }
    }
    internal string? FilePath { get; set; } = null;
    internal int DataLength => _data.Length;
    internal bool IsMoreDataAvailable => DataIndex < DataLength;


    // Private fields.
    private readonly ErrorRepository _errorRepository;
    private string _data;
    private int _dataIndex = 0;

    private char[] _numberCharsBase2 = new char[] 
        { '0', '1' };

    private char[] _numberCharsBase10 = new char[] 
        { '0', '1', '2', '3', '4', '5', '6', '7', '8', '9' };

    private char[] _numberCharsBase16 = new char[] 
        { '0', '1', '2', '3', '4', '5', '6', '7', '8', '9', 'a', 'b', 'c', 'd', 'e', 'f'};


    // Constructors.
    internal SourceDataParser(string data, string? filePath, ErrorRepository errorRepository)
    {
        _data = data;
        FilePath = filePath;
        _errorRepository = errorRepository ?? throw new ArgumentNullException(nameof(errorRepository));
    }


    // Methods.
    internal void IncrementDataIndex()
    {
        if (_dataIndex >= DataLength)
        {
            return;
        }

        if (GetCharAtDataIndex() == KGVL.NEWLINE)
        {
            Line++;
        }

        _dataIndex++;
    }

    internal void IncrementDataIndexNTimes(int count)
    {
        for (int i = 0; (i < count) && (i < DataLength); i++)
        {
            IncrementDataIndex();
        }
    }

    internal void DecrementDataIndex()
    {
        if (_dataIndex <= 0)
        {
            return;
        }

        _dataIndex--;
        if (_data[_dataIndex] == KGVL.NEWLINE)
        {
            Line--;
        }
    }

    internal void DecrementDataIndexNTimes(int count)
    {
        for (int i = 0; i < count; i++) { DecrementDataIndex(); }
    }

    internal int GetColumn(int startIndex)
    {
        // YAY, more inefficient as fuck code.
        int Index = startIndex;
        do
        {
            Index--;
        }
        while ((Index > 0) && (GetCharAtDataIndex(Index) != '\n'));

        return startIndex - Index;
    }

    internal char GetCharAtDataIndex() => _dataIndex >= DataLength ? KGVL.CHAR_NONE : _data[_dataIndex];

    internal char GetCharAtDataIndex(int index) => index >= DataLength ? KGVL.CHAR_NONE : _data[index];

    internal string ReadWord(ErrorCreateOptions? error)
    {
        StringBuilder Word = new();

        while ((_dataIndex < DataLength) && char.IsAsciiLetter(GetCharAtDataIndex()))
        {
            Word.Append(GetCharAtDataIndex());
            IncrementDataIndex();
        }

        if (Word.Length == 0)
        {
            if (error.HasValue)
            {
                throw new SourceFileReadException(this, error);
            }
            return string.Empty;
        }
        return Word.ToString();
    }

    internal string ReadIdentifier(ErrorCreateOptions? error)
    {
        StringBuilder Identifier = new();

        while ((_dataIndex < DataLength) && IsIdentifierChar(GetCharAtDataIndex()))
        {
            Identifier.Append(GetCharAtDataIndex());
            IncrementDataIndex();
        }

        if (error.HasValue && ((Identifier.Length == 0) || char.IsDigit(Identifier[0])))
        {
            string IdentifierName = Identifier.Length != 0 ? $"\"{Identifier.ToString()}\"" : "with empty name";
            throw new SourceFileReadException(this, error, $"Invalid identifier {IdentifierName}. " +
                $"Identifiers must only use ASCII a-z A-Z letters and digits 0-9, and the character '{KGVL.UNDERSCORE}'. " +
                $"They must not start with a digit and their length must be >= 1 character");
        }

        return Identifier.ToString();
    }

    internal void ReverseUntilOneAfterWhitespace()
    {
        DecrementDataIndex();
        while (_dataIndex > 0)
        {
            if (char.IsWhiteSpace(GetCharAtDataIndex()))
            {
                IncrementDataIndex();
                return;
            }
            DecrementDataIndex();
        }
    }

    internal string ReadUntil(ErrorCreateOptions? error, params char[] charsToFind)
    {
        ArgumentNullException.ThrowIfNull(charsToFind, nameof(charsToFind));

        StringBuilder ParsedText = new();

        while (_dataIndex < DataLength)
        {
            if (charsToFind.Contains(GetCharAtDataIndex()))
            {
                return ParsedText.ToString();
            }
            ParsedText.Append(GetCharAtDataIndex());
            IncrementDataIndex();
        }

        if (((ParsedText.Length == 0) || !charsToFind.Contains(GetCharAtDataIndex())) && (error.HasValue))
        {
            throw new SourceFileReadException(this, error, GetNoTargetCharsFoundNote(charsToFind));
        }
        return string.Empty;
    }

    internal string ReadUntilNonWhitespace(ErrorCreateOptions? error)
    {
        StringBuilder ParsedText = new();

        while (_dataIndex < DataLength)
        {
            if (!char.IsWhiteSpace(GetCharAtDataIndex()))
            {
                return ParsedText.ToString();
            }
            ParsedText.Append(GetCharAtDataIndex());
            IncrementDataIndex();
        }

        if (((ParsedText.Length == 0) || (_dataIndex >= DataLength)) && (error.HasValue))
        {
            throw new SourceFileReadException(this, error, "Unexpected end of file while looking for next " +
                $"non-whitespace characters.");
        }
        return string.Empty;
    }

    internal bool SkipUntil(ErrorCreateOptions? error, params char[] charsToFind)
    {
        if (charsToFind == null)
        {
            throw new ArgumentNullException(nameof(charsToFind));
        }

        while (_dataIndex < DataLength)
        {
            if (charsToFind.Contains(GetCharAtDataIndex()))
            {
                return true;
            }
            IncrementDataIndex();
        }

        if (error.HasValue)
        {
            throw new SourceFileReadException(this, error, GetNoTargetCharsFoundNote(charsToFind));
        }
        return false;
    }

    internal bool SkipWhitespaceUntil(ErrorCreateOptions? error, params char[] charsToFind)
    {
        ArgumentNullException.ThrowIfNull(charsToFind, nameof(charsToFind));

        while (_dataIndex < DataLength)
        {
            if (charsToFind.Contains(GetCharAtDataIndex()))
            {
                return true;
            }
            else if (!char.IsWhiteSpace(GetCharAtDataIndex()))
            {
                throw new SourceFileReadException(this, error, $"Invalid character '{GetCharAtDataIndex()}' found, " +
                    $"allowed characters include " +
                    $"{string.Join(", ", charsToFind.Select(value => $"'{value}'"))}.");
            }
            IncrementDataIndex();
        }

        if (error.HasValue)
        {
            throw new SourceFileReadException(this, error, GetNoTargetCharsFoundNote(charsToFind));
        }
        return false;
    }

    internal bool SkipUntilNonWhitespace(ErrorCreateOptions? error)
    {
        while ((_dataIndex < DataLength) && char.IsWhiteSpace(_data[_dataIndex]))
        {
            IncrementDataIndex();
        }

        if (_dataIndex >= DataLength)
        {
            if (error.HasValue)
            {
                throw new SourceFileReadException(this, error, "Unexpected end of file while skipping whitespace.");
            }
            return false;
        }
        return true;
    }

    internal bool SkipPastString(ErrorCreateOptions? error, params string[] stringsToFind)
    {
        ArgumentNullException.ThrowIfNull(stringsToFind, nameof(stringsToFind));

        while (_dataIndex < DataLength)
        {
            for (int i = 0; i < stringsToFind.Length; i++)
            {
                if (HasStringAtIndex(_dataIndex, stringsToFind[i]))
                {
                    IncrementDataIndexNTimes(stringsToFind[i].Length);
                    return true;
                }
            }
            IncrementDataIndex();
        }

        if (error.HasValue)
        {
            throw new SourceFileReadException(this, error);
        }
        return false;
    }

    /* Skips forward to somewhere parsing can sensibly be tried again after an error, and says what it
     * stopped on. Only characters at bracket depth zero relative to where the skip started count, so
     * the skip never stops inside something it walked into. Quoted blocks are skipped whole; comments
     * are already gone by this point.
     *
     * A bracketed block opened during the skip is a construct being abandoned, so its closing bracket
     * is a place to resume at. A closing bracket found at depth zero was never opened here, so it
     * belongs to whatever the parser is already inside of and ends the list it is reading.
     *
     * This is a guess and nothing more. It cannot know what was meant, only where it is not obviously
     * wrong to start reading again. */
    internal SyncPointKind SkipToSyncPoint(char[] resumeChars, char[] terminatorChars)
    {
        ArgumentNullException.ThrowIfNull(resumeChars, nameof(resumeChars));
        ArgumentNullException.ThrowIfNull(terminatorChars, nameof(terminatorChars));

        int BracketDepth = 0;

        while (IsMoreDataAvailable)
        {
            char Character = GetCharAtDataIndex();

            if ((Character == KGVL.DOUBLE_QUOTE) || (Character == KGVL.SINGLE_QUOTE))
            {
                SkipQuotedBlock(Character);
                continue;
            }

            if (IsOpeningBracket(Character))
            {
                BracketDepth++;
                IncrementDataIndex();
                continue;
            }

            if (IsClosingBracket(Character))
            {
                if (BracketDepth == 0)
                {
                    return SyncPointKind.Terminator;
                }

                BracketDepth--;
                if (BracketDepth == 0)
                {
                    return SyncPointKind.Resume;
                }
                IncrementDataIndex();
                continue;
            }

            if (BracketDepth == 0)
            {
                if (terminatorChars.Contains(Character))
                {
                    return SyncPointKind.Terminator;
                }
                if (resumeChars.Contains(Character))
                {
                    return SyncPointKind.Resume;
                }
            }

            IncrementDataIndex();
        }

        return SyncPointKind.None;
    }

    internal bool HasStringAtIndex(int index, string stringToFind)
    {
        if (index < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(index));
        }
        if (DataLength - index < stringToFind.Length)
        {
            return false;
        }

        for (int i = 0; i < stringToFind.Length; i++)
        {
            if (GetCharAtDataIndex(index + i) != stringToFind[i])
            {
                return false;
            }
        }

        return true;
    }

    internal bool IsIdentifierChar(char character)
    {
        return char.IsAsciiLetterOrDigit(character) || (character == KGVL.UNDERSCORE);
    }

    internal bool IsIdentifierFirstChar(char character)
    {
        return char.IsAsciiLetter(character) || (character == KGVL.UNDERSCORE);
    }

    internal bool IsValidIdentifier(string identifier)
    {
        if ((identifier.Length <= 0) || char.IsAsciiDigit(identifier[0]))
        {
            return false;
        }

        foreach (char Character in identifier)
        {
            if (!IsIdentifierChar(Character))
            {
                return false;
            }
        }
        return true;
    }

    /* Whether a number starts at the index: a digit, or a point directly followed by one, as in ".5".
     * A point followed by anything else is member access, never a number. */
    internal bool HasNumberAtIndex(int index)
    {
        char Character = GetCharAtDataIndex(index);
        if (char.IsAsciiDigit(Character))
        {
            return true;
        }
        return (Character == KGVL.DECIMAL_SEPARATOR) && char.IsAsciiDigit(GetCharAtDataIndex(index + 1));
    }

    internal IntegerNumber? ReadInteger(ErrorCreateOptions? error)
    {
        StringBuilder Number = new();

        (char[] AllowedChars, NumberBase Base) = GetNumberBase();
        char CharAtIndex = char.ToLowerInvariant(GetCharAtDataIndex());
        while (AllowedChars.Contains(CharAtIndex) || (CharAtIndex == KGVL.UNDERSCORE))
        {
            if (CharAtIndex != KGVL.UNDERSCORE)
            {
                Number.Append(GetCharAtDataIndex());
            }
            IncrementDataIndex();
            CharAtIndex = char.ToLowerInvariant(GetCharAtDataIndex());
        }

        (bool IsLong, bool IsUnsigned) = ReadNumberTypeSpecifier(Number.ToString(), Base, error);

        if (Number.Length <= 0)
        {
            if (error.HasValue)
            {
                throw new SourceFileReadException(this, error);
            }
            return null;
        }

        return new(Number.ToString(), Base, IsLong, IsUnsigned);
    }

    /* Integers are read by ReadInteger. A decimal is only delimited here and then handed to
     * TwoIntDecimal, which is the one place that decides what a valid decimal is and what it is worth.
     * A malformed decimal is still consumed whole, and what is wrong with it comes back in
     * malformedError rather than being thrown: the cursor is exactly where it should be, so the caller
     * can queue the error and carry straight on. The error argument is thrown only when there is no
     * number here at all. */
    internal object? ReadNumber(ErrorCreateOptions? error, out ErrorCreateOptions? malformedError)
    {
        malformedError = null;

        if (!HasNumberAtIndex(DataIndex))
        {
            if (error.HasValue)
            {
                throw new SourceFileReadException(this, error);
            }
            return null;
        }

        int EndIndex = FindNumberEnd(DataIndex);
        if (!IsDecimalNumber(DataIndex, EndIndex))
        {
            return ReadInteger(error);
        }
        return ReadDecimal(EndIndex, out malformedError);
    }

    internal TypeTargetIdentifier ReadTypeTargetIdentifier(ErrorCreateOptions? error)
    {
        string BaseName = ReadIdentifier(error);

        TypeTargetIdentifier[]? TypeArguments = null;
        if (GetCharAtDataIndex() == KGVL.GENERIC_TYPE_START)
        {
            TypeArguments = ReadGenericTypeArguments(error);
        }

        /* Each "[]" wraps everything read so far in one more array, and a '?' always annotates
         * whatever stands immediately to its left. So "int?[]?[]" is an array of nullable arrays
         * of nullable ints, and the levels come out innermost first. */
        List<bool> NullabilityByLevel = new() { GetIsNullable() };
        while (ReadOneArrayLevel())
        {
            NullabilityByLevel.Add(GetIsNullable());
        }

        TypeTargetIdentifier TypeTarget = new(new(BaseName), TypeArguments);
        TypeTarget.SetNullabilityByLevel(NullabilityByLevel);
        return TypeTarget;
    }

    private TypeTargetIdentifier[] ReadGenericTypeArguments(ErrorCreateOptions? error)
    {
        IncrementDataIndex();
        List<TypeTargetIdentifier> SubTypes = new();
        bool IsTypeNameExpected = true;

        while (IsTypeNameExpected)
        {
            SkipUntilNonWhitespace(error);
            TypeTargetIdentifier SubType = ReadTypeTargetIdentifier(error);
            SubTypes.Add(SubType);
            SkipUntilNonWhitespace(error);
            IsTypeNameExpected = GetCharAtDataIndex() == KGVL.COMMA;
            if (IsTypeNameExpected)
            {
                IncrementDataIndex();
            }
        }

        if (error.HasValue && (GetCharAtDataIndex() != KGVL.GENERIC_TYPE_END))
        {
            throw new SourceFileReadException(this, error,
                $"Expected generic type end '{KGVL.GENERIC_TYPE_END}'");
        }
        else if (GetCharAtDataIndex() == KGVL.GENERIC_TYPE_END)
        {
            IncrementDataIndex();
        }

        return SubTypes.ToArray();
    }

    internal CharConstant? ReadCharacter(ErrorCreateOptions? error)
    {
        if (GetCharAtDataIndex() != KGVL.SINGLE_QUOTE)
        {
            if (error != null)
            {
                throw new SourceFileReadException(this, error,
                    $"Character did not start with a quote {KGVL.SINGLE_QUOTE}");
            }
            return null;
        }
        IncrementDataIndex();

        char Character;
        if (GetCharAtDataIndex() == KGVL.ESCAPE_CHAR)
        {
            /* The backslash only marks the sequence, so it is stepped over rather than looked up. */
            IncrementDataIndex();
            Character = EscapeSequenceToChar(ReadUntil(error, KGVL.SINGLE_QUOTE));
        }
        else
        {
            Character = GetCharAtDataIndex();
            IncrementDataIndex();
        }

        if (GetCharAtDataIndex() != KGVL.SINGLE_QUOTE)
        {
            if (error != null)
            {
                throw new SourceFileReadException(this, error, 
                    $"Character \"{Character}\" did not end with a quote {KGVL.SINGLE_QUOTE}");
            }
            return null;
        }
        IncrementDataIndex();
        return new(Character);
    }

    internal char EscapeSequenceToChar(string sequence)
    {
        if (sequence.StartsWith(KGVL.PREFIX_HEX_CHAR))
        {
            try
            {
                return (char)Convert.ToUInt32(sequence.Substring(1), 16);
            }
            catch (Exception e) when (e is FormatException or ArgumentException
                or OverflowException or ArgumentOutOfRangeException)
            {
                throw new SourceFileReadException(this,
                    _errorRepository.InvalidHexEscapeSequence.CreateOptions(sequence));
            }
        }

        return sequence switch
        {
            "a" => '\a',
            "b" => '\b',
            "f" => '\f',
            "n" => '\n',
            "t" => '\t',
            "v" => '\v',
            "'" => '\'',
            "\"" => '"',
            "\\" => '\\',
            _ => throw new SourceFileReadException(this,
                _errorRepository.UnknownEscapeSequence.CreateOptions(sequence))
        };
    }


    // Private methods.
    private bool IsOpeningBracket(char character)
    {
        return (character == KGVL.OPEN_CURLY_BRACKET)
            || (character == KGVL.OPEN_PARENTHESIS)
            || (character == KGVL.OPEN_SQUARE_BRACKET);
    }

    private bool IsClosingBracket(char character)
    {
        return (character == KGVL.CLOSE_CURLY_BRACKET)
            || (character == KGVL.CLOSE_PARENTHESIS)
            || (character == KGVL.CLOSE_SQUARE_BRACKET);
    }

    /* Only used while recovering from an error, so it just needs to get past the quoted block without
     * being fooled by brackets or escaped quotes inside it. A string interpolated into another string
     * would confuse it, but by this point the file is already known to be wrong. */
    private void SkipQuotedBlock(char quote)
    {
        IncrementDataIndex();

        while (IsMoreDataAvailable)
        {
            char Character = GetCharAtDataIndex();

            if (Character == KGVL.ESCAPE_CHAR)
            {
                IncrementDataIndexNTimes(2);
                continue;
            }

            IncrementDataIndex();
            if (Character == quote)
            {
                return;
            }
        }
    }

    /* The suffix marks the number as a decimal but is not part of its value, so it is left out of the
     * text TwoIntDecimal sees. */
    private DecimalNumber ReadDecimal(int endIndex, out ErrorCreateOptions? malformedError)
    {
        malformedError = null;

        string Token = _data.Substring(DataIndex, endIndex - DataIndex);
        IncrementDataIndexNTimes(Token.Length);

        string ValueText = Token;
        if (char.ToLowerInvariant(Token[^1]) == KGVL.SUFFIX_DECIMAL)
        {
            ValueText = Token.Substring(0, Token.Length - 1);
        }

        if (!TwoIntDecimal.TryParse(ValueText, out TwoIntDecimal Value, out DecimalParseError Error))
        {
            malformedError = GetMalformedDecimalError(Token, Error);
            return new(ValueText, TwoIntDecimal.NaN);
        }
        return new(ValueText, Value);
    }

    /* Where the number starting at the index ends. The rule is loose on purpose, much like a C
     * preprocessing number: every letter, digit and underscore counts, and so do a point followed by
     * a digit and a sign directly after an exponent. Taking in everything a number could be made of,
     * rather than stopping at the first character a valid one could not contain, is what lets a
     * malformed number such as "1.5x" be reported as one, instead of the parser tripping over a stray
     * name after it.
     *
     * A point followed by anything but a digit ends the number, which is what makes "3.ToString()" and
     * "3.4.ToString()" member access on a number, as in C#. */
    private int FindNumberEnd(int startIndex)
    {
        int Index = startIndex;
        while (true)
        {
            char Character = GetCharAtDataIndex(Index);

            if (IsIdentifierChar(Character))
            {
                Index++;
                char Following = GetCharAtDataIndex(Index);
                if ((char.ToLowerInvariant(Character) == KGVL.DECIMAL_EXPONENT)
                    && ((Following == KGVL.DECIMAL_EXPONENT_POSITIVE_SIGN)
                        || (Following == KGVL.DECIMAL_EXPONENT_NEGATIVE_SIGN)))
                {
                    Index++;
                }
            }
            else if ((Character == KGVL.DECIMAL_SEPARATOR)
                && char.IsAsciiDigit(GetCharAtDataIndex(Index + 1)))
            {
                Index++;
            }
            else
            {
                return Index;
            }
        }
    }

    /* A number is a decimal when what follows its leading digits is a point, an exponent or the decimal
     * suffix. That also sends hexadecimal and binary numbers to ReadInteger, since their prefix letter
     * is none of those, which matters because "0x1e+1" has to stay an addition. Underscores are skipped
     * along with the digits so that "1_000.5" is read as the decimal it was meant to be and gets an
     * error about the underscore, rather than splitting into an integer and a member access. */
    private bool IsDecimalNumber(int startIndex, int endIndex)
    {
        int Index = startIndex;
        while ((Index < endIndex) && (char.IsAsciiDigit(GetCharAtDataIndex(Index))
            || (GetCharAtDataIndex(Index) == KGVL.UNDERSCORE)))
        {
            Index++;
        }
        if (Index >= endIndex)
        {
            return false;
        }

        char Following = char.ToLowerInvariant(GetCharAtDataIndex(Index));
        return (Following == KGVL.DECIMAL_SEPARATOR)
            || (Following == KGVL.DECIMAL_EXPONENT)
            || (Following == KGVL.SUFFIX_DECIMAL);
    }

    private ErrorCreateOptions GetMalformedDecimalError(string token, DecimalParseError error)
    {
        return error.Kind switch
        {
            DecimalParseErrorKind.Empty or DecimalParseErrorKind.MissingDigits =>
                _errorRepository.DecimalMissingDigits.CreateOptions(token),
            DecimalParseErrorKind.MultipleSeparators =>
                _errorRepository.DecimalMultipleSeparators.CreateOptions(token),
            DecimalParseErrorKind.MissingExponentDigits =>
                _errorRepository.DecimalMissingExponentDigits.CreateOptions(token),
            DecimalParseErrorKind.UnexpectedCharacter =>
                _errorRepository.DecimalUnexpectedCharacter.CreateOptions(token, token[error.Index]),
            _ => throw new ArgumentOutOfRangeException(nameof(error), $"Not a parse failure: {error}")
        };
    }

    private (char[] characters, NumberBase numberBase) GetNumberBase()
    {
        string BaseIndicator = $"{GetCharAtDataIndex()}{GetCharAtDataIndex(DataIndex + 1)}".ToLowerInvariant();
        if (BaseIndicator == KGVL.PREFIX_BINARY)
        {
            IncrementDataIndexNTimes(KGVL.PREFIX_BINARY.Length);
            return (_numberCharsBase2, NumberBase.Binary);
        }
        else if (BaseIndicator == KGVL.PREFIX_HEX)
        {
            IncrementDataIndexNTimes(KGVL.PREFIX_HEX.Length);
            return (_numberCharsBase16, NumberBase.Hexadecimal);
        }
        else
        {
            return (_numberCharsBase10, NumberBase.Decimal);
        }
    }

    private (bool IsLong, bool IsUnsigned) ReadNumberTypeSpecifier(string numberValue, 
        NumberBase numberBase,
        ErrorCreateOptions? error)
    {
        string Suffix = $"{GetCharAtDataIndex()}{GetCharAtDataIndex(DataIndex + 1)}";

        bool HasLongSpecifier = false;
        bool HasUnsignedSpecifier = false;

        foreach (char Character in Suffix)
        {
            char LowerChar = char.ToLowerInvariant(Character);
            if (LowerChar == KGVL.SUFFIX_LONG)
            {
                if (error.HasValue && HasLongSpecifier)
                {
                    throw new SourceFileReadException(this, error,
                        $"Duplicate long specifier '{KGVL.SUFFIX_LONG}' for integer {numberValue} ");
                }
                HasLongSpecifier = true;
                IncrementDataIndex();
            }
            else if (LowerChar == KGVL.SUFFIX_UNSIGNED)
            {
                if (error.HasValue && HasUnsignedSpecifier)
                {
                    throw new SourceFileReadException(this, error,
                        $"Duplicate unsigned specifier '{KGVL.SUFFIX_UNSIGNED}' for integer {numberValue}");
                }
                HasUnsignedSpecifier = true;
                IncrementDataIndex();
            }
            else
            {
                break;
            }
        }

        /* The loop above already stepped over each specifier it accepted. */
        (bool IsValueLong, bool IsValueUnsigned) = GetNumberSpecifiersBasedOnValue(numberValue, numberBase);
        return (HasLongSpecifier || IsValueLong, HasUnsignedSpecifier || IsValueUnsigned);
    }

    private (bool IsLong, bool IsUnsigned) GetNumberSpecifiersBasedOnValue(string numberValue, NumberBase numberBase)
    {
        int Base = BaseToInt(numberBase);
        if (TryParseInt(() => Convert.ToInt32(numberValue, Base)))
        {
            return (false, false);
        }
        if (TryParseInt(() => Convert.ToUInt32(numberValue, Base)))
        {
            return (false, true);
        }
        if (TryParseInt(() => Convert.ToInt64(numberValue, Base)))
        {
            return (true, false);
        }
        return (true, true);
    }

    private int BaseToInt(NumberBase numberBase)
    {
        return numberBase switch
        {
            NumberBase.Decimal => 10,
            NumberBase.Binary => 2,
            NumberBase.Hexadecimal => 16,
            _ => throw new ArgumentOutOfRangeException($"Invalid number base {numberBase} ({(int)numberBase})")
        };
    }

    private bool TryParseInt(Action action)
    {
        try
        {
            action.Invoke();
            return true;
        }
        catch (Exception e) when (e is ArgumentException or FormatException
            or ArgumentOutOfRangeException or OverflowException)
        {
            return false;
        }
    }

    private string GetNoTargetCharsFoundNote(char[] characters)
    {
        return $"Unexpected end of file while looking for one of these characters: " +
                $"[{string.Join(", ", characters)}].";
    }

    /* Consumes one "[]" pair and reports whether there was one, rewinding if not. A lone '[' that
     * is not immediately closed is left unconsumed, so that an index access like "a[i]" is never
     * mistaken for an array type. */
    internal bool ReadOneArrayLevel()
    {
        int EndIndex = DataIndex;
        SkipUntilNonWhitespace(null);

        if (GetCharAtDataIndex() != KGVL.OPEN_SQUARE_BRACKET)
        {
            DataIndex = EndIndex;
            return false;
        }
        IncrementDataIndex();
        SkipUntilNonWhitespace(null);

        if (GetCharAtDataIndex() != KGVL.CLOSE_SQUARE_BRACKET)
        {
            DataIndex = EndIndex;
            return false;
        }

        IncrementDataIndex();
        return true;
    }

    private bool GetIsNullable()
    {
        int EndIndex = DataIndex;
        SkipUntilNonWhitespace(null);
        if (GetCharAtDataIndex() == KGVL.TYPE_NULLABLE_INDICATOR)
        {
            IncrementDataIndex();
            return true;
        }
        DataIndex = EndIndex;
        return false;
    }
}