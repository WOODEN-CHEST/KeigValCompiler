using KeigValCompiler.Error;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace KeigValCompiler.Source.Parser;

public class SourceDataParser
{
    // Fields.
    internal int Line { get; private set; } = 1;
    internal int DataIndex
    {
        get => _dataIndex;
        set => _dataIndex = Math.Clamp(value, 0, _data.Length);
    }
    internal string? FilePath { get; set; } = null;
    internal int DataLength => _data.Length;
    internal bool IsMoreDataAvailable => DataIndex < DataLength;


    // Private static fields.


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
        if (_dataIndex >= _data.Length)
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

    internal char GetCharAtDataIndex() => _dataIndex >= _data.Length ? KGVL.CHAR_NONE : _data[_dataIndex];

    internal char GetCharAtDataIndex(int index) => index >= _data.Length ? KGVL.CHAR_NONE : _data[index];

    internal string ReadWord(ErrorCreateOptions? error)
    {
        StringBuilder Word = new();

        while ((_dataIndex < _data.Length) && char.IsAsciiLetter(GetCharAtDataIndex()))
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

        while ((_dataIndex < _data.Length) && IsIdentifierChar(GetCharAtDataIndex()))
        {
            Identifier.Append(GetCharAtDataIndex());
            IncrementDataIndex();
        }

        if (Identifier.Length == 0)
        {
            if (error.HasValue)
            {
                throw new SourceFileReadException(this, error);
            }
            return string.Empty;
        }
        else if (char.IsDigit(Identifier[0]))
        {
            throw new SourceFileReadException(this, error, $"Invalid identifier \"{Identifier.ToString()}\", " +
                $"Identifiers must only use ASCII a-z A-Z letters and digits 0-9, and must not start with a digit.");
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
    internal void ReverseUntilOneAfterIdentifier()
    {
        DecrementDataIndex();
        while (_dataIndex > 0)
        {
            if (!IsIdentifierChar(GetCharAtDataIndex()))
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

        while (_dataIndex < _data.Length)
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

        while (_dataIndex < _data.Length)
        {
            if (!char.IsWhiteSpace(GetCharAtDataIndex()))
            {
                return ParsedText.ToString();
            }
            ParsedText.Append(GetCharAtDataIndex());
            IncrementDataIndex();
        }

        if (((ParsedText.Length == 0) || (_dataIndex >= _data.Length)) && (error.HasValue))
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

        while (_dataIndex < _data.Length)
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

        while (_dataIndex < _data.Length)
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
        while ((_dataIndex < _data.Length) && char.IsWhiteSpace(_data[_dataIndex]))
        {
            IncrementDataIndex();
        }

        if (_dataIndex >= _data.Length)
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

        while (_dataIndex < _data.Length)
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
        if (_data.Length - index < stringToFind.Length)
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

    internal GenericNumber ReadInteger(ErrorCreateOptions? error)
    {
        StringBuilder Number = new();

        (char[] AllowedChars, NumberBase Base) = GetNumberBase();
        char CharAtIndex = GetCharAtDataIndex();
        while (AllowedChars.Contains(CharAtIndex) || (CharAtIndex == KGVL.UNDERSCORE))
        {
            if (CharAtIndex != KGVL.UNDERSCORE)
            {
                Number.Append(GetCharAtDataIndex());
            }
            IncrementDataIndex();
            CharAtIndex = GetCharAtDataIndex();
        }

        (bool IsLong, bool IsUnsigned) = ReadNumberTypeSpecifier(Number.ToString(), error);

        if (Number.Length <= 0)
        {
            throw new SourceFileReadException(this, error);
        }

        return new(Number.ToString(), Base, IsLong, IsUnsigned);
    }


    // Private methods.
    private (char[] characters, NumberBase numberBase) GetNumberBase()
    {
        string BaseIndicator = $"{GetCharAtDataIndex()}{GetCharAtDataIndex(DataIndex + 1)}";
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

    private (bool IsLong, bool IsUnsigned) ReadNumberTypeSpecifier(string numberValue, ErrorCreateOptions? error)
    {
        string Suffix = $"{GetCharAtDataIndex()}{GetCharAtDataIndex(DataIndex + 1)}";

        bool HasLongSpecifier = false;
        bool HasUnsignedSpecifier = false;

        foreach (char Character in Suffix)
        {
            if (Character == KGVL.SUFFIX_LONG)
            {
                if (HasLongSpecifier)
                {
                    throw new SourceFileReadException(this, error,
                        $"Duplicate long specifier '{KGVL.SUFFIX_LONG}' for integer {numberValue} ");
                }
                HasLongSpecifier = true;
                IncrementDataIndex();
            }
            else if (Character == KGVL.SUFFIX_UNSIGNED)
            {
                if (HasUnsignedSpecifier)
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
        (bool IsValueLong, bool IsValueUnsigned) = GetNumberSpecifiersBasedOnValue(numberValue);
        return (HasLongSpecifier || IsValueLong, HasUnsignedSpecifier || IsValueUnsigned);
    }

    private (bool IsLong, bool IsUnsigned) GetNumberSpecifiersBasedOnValue(string numberValue)
    {
        if (int.TryParse(numberValue, out _))
        {
            return (false, false);
        }
        if (uint.TryParse(numberValue, out _))
        {
            return (false, true);
        }
        if (long.TryParse(numberValue, out _))
        {
            return (true, false);
        }
        return (true, true);
    }

    private string GetNoTargetCharsFoundNote(char[] characters)
    {
        return $"Unexpected end of file while looking for one of these characters: " +
                $"[{string.Join(", ", characters)}].";
    }
}