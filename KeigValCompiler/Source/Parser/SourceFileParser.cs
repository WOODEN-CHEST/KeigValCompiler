using KeigValCompiler.Source.Exceptions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KeigValCompiler.Source;
/* The file parser is the first step. It reads a source code file and turns
 * its contents into objects in memory. Ir verifies the syntax of the code, but
 * it does not verify anything else about the code. */
internal class SourceFileParser
{

    // Internal fields.
    internal readonly string FilePath;
    internal int LineCur = 0;


    // Private fields.
    private string _data;
    private int _dataIndex = 0;
    private int DataIndex
    {
        get => _dataIndex;
        set => _dataIndex = Math.Max(0, value);
    }

    
    private readonly HashSet<string> _namespaceImports = new(8);


    // Constructors.
    internal SourceFileParser(string filePath)
    {
        FilePath = filePath ?? throw new ArgumentNullException();
    }


    // Internal methods.
    internal void ParseFile()
    {
        try
        {
            _data = File.ReadAllText(FilePath);
            ParseBase();
        }
        catch (FileNotFoundException e)
        {
            throw new FileReadException(this, "File not found.");
        }
        catch (DirectoryNotFoundException e)
        {
            throw new FileReadException(this, e.ToString());
        }
        catch (IOException e)
        {
            throw new FileReadException(this, $"IOException reading file. {e}");
        }
    }


    // Internal methods.
    /* Reading chars */
    internal bool TryReadChar(out char readChar)
    {
        if (_dataIndex >= _data.Length)
        {
            readChar = '\0';
            return false;
        }

        readChar = _data[_dataIndex++];
        return true;
    }

    internal char ReadChar(string? onExceptionMessage)
    {
        try
        {
            char Char = _data[DataIndex++];
            if (Char == '\n') LineCur++;
            return Char;
        }
        catch (IndexOutOfRangeException)
        {
            throw ThrowIncompleteFile(onExceptionMessage);
        }
    }


    /* Reading until something. */
    internal void SkipUntilNonWS(string? exceptionMessage)
    {
        char Char;
        do
        {
            Char = ReadChar(exceptionMessage);
        }
        while (char.IsWhiteSpace(Char));
        DataIndex--;
    }

    internal bool SkipUntilNonWSOrEnd()
    {
        char Char;
        do
        {
            if (!TryReadChar(out Char))
            {
                return false;
            }
        }
        while (char.IsWhiteSpace(Char));

        DataIndex--;
        return true;
    }

    internal void SkipUntilSemiColon(string? exceptionMessage)
    {
        DataIndex = _data.IndexOf(';', DataIndex);
        if (DataIndex == -1)
        {
            throw ThrowIncompleteFile(exceptionMessage);
        }
    }

    internal void SkipUntil(string? exceptionMessage, params char[] characters)
    {
        DataIndex = _data.IndexOfAny(characters);
        if (DataIndex == -1)
        {
            throw ThrowIncompleteFile(exceptionMessage);
        }
    }


    /* Reading strings. */
    internal string ParseWord(string? exceptionMessage)
    {
        StringBuilder Word = new();

        while (DataIndex < _data.Length && !char.IsWhiteSpace(_data[DataIndex]))
        {
            Word.Append(_data[DataIndex++]);
        }

        DataIndex--;
        return Word.ToString();
    }


    internal string ReadUntilSemicolon(string? exceptionMessage)
    {
        int StartIndex = DataIndex;
        SkipUntilSemiColon(exceptionMessage);
        return _data[StartIndex..(DataIndex + 1)];
    }

    internal string ReadUntil(string? exceptionMessage, params char[] characters)
    {
        int StartIndex = DataIndex;
        SkipUntil(exceptionMessage, characters);
        return _data[StartIndex..(DataIndex + 1)];
    }


    /* Exceptions. */
    internal FileReadException ThrowUnknownKeyword(string keyword, string? message)
    {
        return new FileReadException(this, $"Unknown keyword \"{keyword}\". {message}");
    }

    internal FileReadException ThrowIncompleteFile(string? exceptionMessage)
    {
        return new FileReadException(this, exceptionMessage ?? $"Incomplete file, no further information.");
    }

    // Private methods.
    private void ParseBase()
    {
        if (!SkipUntilNonWSOrEnd())
        {
            return;
        }

        string ExceptionMsg = "Expected a namespace or namespace import.";
        string Keyword = ParseWord(ExceptionMsg);

        switch (Keyword)
        {
            case "using":

                break;

            case "namespace":
                Pack
                break;

            default:
                ThrowUnknownKeyword(Keyword, ExceptionMsg);
                break;
        }
    }

    private void ParseImportStatement()
    {
        SkipUntilNonWS("Expected a namespace to import.");
    }

    private void ParseNamespaceStatement()
    {
        SkipUntilNonWS("Expected a namespace.");
        string Name = ReadUntilSemicolon("Expected a namespace.");

    }

    private string ParseNamespaceName()
    {
        return null;
    }
}