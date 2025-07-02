using KeigValCompiler.Error;
using KeigValCompiler.Semantician;
using KeigValCompiler.Semantician.Member;
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
    private string _data;
    private int _dataIndex = 0;

    private char[] _numberCharsBase2 = new char[] 
        { '0', '1' };

    private char[] _numberCharsBase10 = new char[] 
        { '0', '1', '2', '3', '4', '5', '6', '7', '8', '9' };

    private char[] _numberCharsBase16 = new char[] 
        { '0', '1', '2', '3', '4', '5', '6', '7', '8', '9', 'a', 'b', 'c', 'd', 'e', 'f'};


    // Constructors.
    public SourceDataParser(string data, string? filePath)
    {
        _data = data;
        FilePath = filePath;
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

    internal DecimalNumber? ReadDecimal(ErrorCreateOptions? error)
    {
        string? DecimalString = ParseDecimalNumber(error);

        if ((DecimalString == null) || !TwoIntDecimal.TryParse(DecimalString, out _))
        {
            if (error.HasValue)
            {
                throw new SourceFileReadException(this, error, $"Invalid decimal number {DecimalString}");
            }
            return null;
        }
         
        return new(DecimalString);
    }

    internal object? ReadNumber(ErrorCreateOptions? error)
    {
        int StartIndex = DataIndex;

        DecimalNumber? DecNumber = ReadDecimal(null);
        if (DecNumber != null)
        {
            return DecNumber;
        }

        DataIndex = StartIndex;
        IntegerNumber? IntNumber = ReadInteger(null);
        if (IntNumber != null)
        {
            return IntNumber;
        }
        
        if (error.HasValue)
        {
            throw new SourceFileReadException(this, error, "Invalid number.");
        }
        return null;
    }

    internal TypeTargetIdentifier ReadTypeTargetIdentifier(ErrorCreateOptions? error)
    {
        string BaseName = ReadIdentifier(error);
        if (GetCharAtDataIndex() != KGVL.GENERIC_TYPE_START)
        {
            return new TypeTargetIdentifier(new(BaseName), null) { IsNullable = GetIsNullable() };
        }

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

        return new(new(BaseName), SubTypes.ToArray()) { IsNullable = GetIsNullable() };
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
            Character = EscapeSequenceToChar(ReadUntil(error, KGVL.SINGLE_QUOTE));
        }
        else
        {
            Character = GetCharAtDataIndex();
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

    internal InterpolatedString? ReadInterpolatedString(ErrorCreateOptions? error)
    {
        throw new NotImplementedException();
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
                throw new SourceFileReadException(this, null, $"Invalid character in hex notation \"{sequence}\".");
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
            _ => throw new SourceFileReadException(this, null, $"Unknown escape sequence \"\\{sequence}\"")
        };
    }


    // Private methods.
    private string? ParseDecimalNumber(ErrorCreateOptions? error)
    {
        StringBuilder Number = new StringBuilder();

        bool HasDecimalIndicator = false;
        bool HasSeparator = false;
        bool HasExponent = false;

        char Character = char.ToLowerInvariant(GetCharAtDataIndex());
        while (!HasDecimalIndicator && (char.IsAsciiDigit(Character) 
            || (Character == KGVL.DECIMAL_SEPARATOR) 
            || (Character == KGVL.DECIMAL_EXPONENT)
            || (Character == KGVL.SUFFIX_DECIMAL)))
        {
            if (Character == KGVL.DECIMAL_SEPARATOR)
            {
                if (HasSeparator)
                {
                    if ((Number.Length > 0) && (Number[^1] == KGVL.DECIMAL_SEPARATOR))
                    {
                        if (error.HasValue)
                        {
                            throw new SourceFileReadException(this, error, $"Multiple decimal separators " +
                                $"'{KGVL.DECIMAL_SEPARATOR}' are not allowed (Number \"{Number}\")");
                        }
                        return null;
                    }
                    return Number.ToString();
                }
                HasSeparator = true;
            }
            else if (Character == KGVL.DECIMAL_EXPONENT)
            {
                if (HasExponent)
                {
                    if (error.HasValue)
                    {
                        throw new SourceFileReadException(this, error, $"Multiple exponend indicators " +
                            $"'{KGVL.DECIMAL_EXPONENT}' are not allowed (Number \"{Number}\")");
                    }
                    return null;
                }
                HasExponent = true;
            }
            else if (Character == KGVL.SUFFIX_DECIMAL)
            {
                if ((Number.Length == 0) || (Number[^1] == KGVL.DECIMAL_SEPARATOR) || (Number[^1] == KGVL.DECIMAL_EXPONENT))
                {
                    if (error.HasValue)
                    {
                        throw new SourceFileReadException(this, error, $"The decimal number suffix {KGVL.SUFFIX_DECIMAL} " +
                            $"is used incorrectly. It must not appear after a separator '{KGVL.DECIMAL_SEPARATOR}' " +
                            $"or an exponent indicator '{KGVL.DECIMAL_EXPONENT}' (Number \"{Number}\")");
                    }
                }
            }

            Number.Append(Character);
            IncrementDataIndex();
            Character = GetCharAtDataIndex();
        }

        if ((Number.Length == 0) || (Number[^1] == KGVL.DECIMAL_EXPONENT))
        {
            if (error != null)
            {
                throw new SourceFileReadException(this, error, $"Invalid decimal number \"{Number}\". " +
                    $"Decimal numbers must not be empty or end with an exponent indicator '{KGVL.DECIMAL_EXPONENT}'");
            }
        }

        return Number.ToString();
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

        int SpecifierLength = (HasLongSpecifier ? 1 : 0) + (HasUnsignedSpecifier ? 1 : 0);
        IncrementDataIndexNTimes(SpecifierLength);
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