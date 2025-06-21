using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace KeigValCompiler.Source.Parser;

public class TextParser
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


    // Constructors.
    public TextParser(string data, string? filePath)
    {
        _data = data;
        FilePath = filePath;
    }


    // Methods.

    /// <summary>
    /// Increments the data index, if possible.
    /// </summary>
    public void IncrementDataIndex()
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

    /// <summary>
    /// Increments the data index N times, if possible.
    /// </summary>
    /// <param name="count">The amount of times to increment the data index.</param>
    public void IncrementDatIndexNTimes(int count)
    {
        for (int i = 0; (i < count) && (i < DataLength); i++)
        {
            IncrementDataIndex();
        }
    }

    /// <summary>
    /// Decrements the data index, if possible.
    /// </summary>
    public void DecrementDataIndex()
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

    /// <summary>
    /// Decrements the data index N times, if possible.
    /// </summary>
    /// <param name="count">The amount of times to decrement the data index.</param>
    public void DecrementDataIndexNTimes(int count)
    {
        for (int i = 0; i < count; i++) { DecrementDataIndex(); }
    }

    /// <summary>
    /// Gets the character at the current data index.
    /// </summary>
    /// <returns>The character at the current data index, or '\0' if the index is out of bounds.</returns>
    public char GetCharAtDataIndex() => _dataIndex >= _data.Length ? KGVL.CHAR_NONE : _data[_dataIndex];

    /// <summary>
    /// Gets the character at the given data index.
    /// </summary>
    /// <param name="index">The index of the character to get.</param>
    /// <returns>The character at the current data index, or '\0' if the index is out of bounds.</returns>
    public char GetCharAtDataIndex(int index) => index >= _data.Length ? KGVL.CHAR_NONE : _data[index];

    /// <summary>
    /// Reads an ASCII word starting from the current data index.
    /// </summary>
    /// <param name="exceptionMessage">The message in the exception thrown if the resulting word's length is 0.
    /// This may be <c>null</c> to prevent and exception from being thrown.</param>
    /// <returns>The read word.</returns>
    /// <exception cref="SourceFileReadException"></exception>
    public string ReadWord(string? exceptionMessage)
    {
        StringBuilder Word = new();

        while ((_dataIndex < _data.Length) && char.IsAsciiLetter(GetCharAtDataIndex()))
        {
            Word.Append(GetCharAtDataIndex());
            IncrementDataIndex();
        }

        if (Word.Length == 0)
        {
            if (exceptionMessage != null)
            {
                throw new SourceFileReadException(FilePath, Line, exceptionMessage);
            }
            return string.Empty;
        }
        return Word.ToString();
    }

    /// <summary>
    /// Reads an identifier starting from the current data index.
    /// </summary>
    /// <param name="exceptionMessage"> The message in the exception thrown if the resulting identifier's length is 0
    /// or the identifier is invalid (starts with a number).
    /// This may be <c>null</c> to prevent and exception from being thrown</param>
    /// <returns></returns>
    /// <exception cref="SourceFileReadException"></exception>
    public string ReadIdentifier(string? exceptionMessage)
    {
        StringBuilder Identifier = new();

        while ((_dataIndex < _data.Length) && IsIdentifierChar(GetCharAtDataIndex()))
        {
            Identifier.Append(GetCharAtDataIndex());
            IncrementDataIndex();
        }

        if (Identifier.Length == 0)
        {
            if (exceptionMessage != null)
            {
                throw new SourceFileReadException(FilePath, Line, exceptionMessage);
            }
            return string.Empty;
        }
        else if (char.IsDigit(Identifier[0]))
        {
            throw new SourceFileReadException(FilePath, Line, exceptionMessage);
        }

        return Identifier.ToString();
    }

    /// <summary>
    /// Reversed the current data index to point to the first non-whitespace character in the current word.
    /// If the data index before this method call points to the first whitespace character after a word,
    /// then the data index is reversed to the first character of said word.
    /// </summary>
    public void ReverseUntilOneAfterWhitespace()
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

    /// <summary>
    /// Reads a string from the current data index position until any of the characters in the
    /// <paramref name="charsToFind"/> array is encountered.
    /// </summary>
    /// <param name="exceptionMessage">The message to use in the exception which is thrown if the
    /// read text's length is 0 or no character specified in the <paramref name="charsToFind"/> array is found
    /// in the data. May be <c>null</c> for no exception to be thrown.</param>
    /// <param name="charsToFind">The array of characters to look for when detecting a stop.</param>
    /// <returns></returns>
    /// <exception cref="ArgumentNullException"></exception>
    /// <exception cref="SourceFileReadException"></exception>
    public string ReadUntil(string? exceptionMessage, params char[] charsToFind)
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

        if (((ParsedText.Length == 0) || !charsToFind.Contains(GetCharAtDataIndex())) && (exceptionMessage != null))
        {
            throw new SourceFileReadException(FilePath, Line, exceptionMessage);
        }
        return string.Empty;
    }

    /// <summary>
    /// Reads whitespace until a non-whitespace character is encountered.
    /// </summary>
    /// <param name="exceptionMessage">The message to use in the exception which is thrown if
    /// no non-whitespace character is encountered.</param>
    /// <returns>The read whitespace.</returns>
    /// <exception cref="SourceFileReadException"></exception>
    public string ReadUntilNonWhitespace(string? exceptionMessage)
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

        if (((ParsedText.Length == 0) || (_dataIndex >= _data.Length)) && (exceptionMessage != null))
        {
            throw new SourceFileReadException(FilePath, Line, exceptionMessage);
        }
        return string.Empty;
    }

    public bool SkipUntil(string? exceptionMessage, params char[] charsToFind)
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

        if (exceptionMessage != null)
        {
            throw new SourceFileReadException(FilePath, Line, exceptionMessage);
        }
        return false;
    }

    public bool SkipWhitespaceUntil(string? exceptionMessage, params char[] charsToFind)
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
                throw new SourceFileReadException(FilePath, Line, exceptionMessage);
            }
            IncrementDataIndex();
        }

        if (exceptionMessage != null)
        {
            throw new SourceFileReadException(FilePath, Line, exceptionMessage);
        }
        return false;
    }

    public bool SkipUntilNonWhitespace(string? exceptionMessage)
    {
        while ((_dataIndex < _data.Length) && char.IsWhiteSpace(_data[_dataIndex]))
        {
            IncrementDataIndex();
        }

        if (_dataIndex >= _data.Length)
        {
            if (exceptionMessage != null)
            {
                throw new SourceFileReadException(FilePath, Line, exceptionMessage);
            }
            return false;
        }
        return true;
    }

    public bool SkipPastString(string? exceptionMessage, params string[] stringsToFind)
    {
        ArgumentNullException.ThrowIfNull(stringsToFind, nameof(stringsToFind));

        while (_dataIndex < _data.Length)
        {
            for (int i = 0; i < stringsToFind.Length; i++)
            {
                if (HasStringAtIndex(_dataIndex, stringsToFind[i]))
                {
                    IncrementDatIndexNTimes(stringsToFind[i].Length);
                    return true;
                }
            }
            IncrementDataIndex();
        }

        if (exceptionMessage != null)
        {
            throw new SourceFileReadException(FilePath, Line, exceptionMessage);
        }
        return false;
    }

    public bool HasStringAtIndex(int index, string stringToFind)
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

    public bool IsIdentifierChar(char character)
    {
        return char.IsAsciiLetterOrDigit(character) || (character == KGVL.UNDERSCORE);
    }
}