using KeigValCompiler.Semantician;
using KeigValCompiler.Source.Parser;
using System.Runtime.CompilerServices;
using System.Text;


namespace KeigValCompiler.Source;


/* Yeah, this class may be a tiny bit too big.  */
internal class SourceFileParser
{
    // Internal fields.
    internal string FilePath { get; private init; }
    internal int Line { get; private set; } = 1;
    internal DataPack Pack { get; private init; }


    // Private fields.
    private string _data;
    private int _dataIndex = 0;

    private string _activeNamespace = string.Empty;
    private readonly HashSet<string> _namespaceImports = new(8);


    /* Chars. */
    private const char SEMICOLON = ';';
    private const char NAMESPACE_SEPARATOR = '.';
    private const char UNDERSCORE = '_';
    private const char OPEN_CURLY_BRACKET = '{';
    private const char CLOSE_CURLY_BRACKET = '}';


    /* Keywords. */
    private const string KEYWORD_NAMESPACE = "namespace";
    private const string KEYWORD_USING = "using";
    private const string KEYWORD_CLASS = "class";


    // Constructors.
    internal SourceFileParser(string filePath, DataPack pack)
    {
        FilePath = filePath ?? throw new ArgumentNullException(nameof(filePath));
        Pack = pack ?? throw new ArgumentNullException(nameof(pack));
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
            throw new FileReadException(this, $"File \"{FilePath}\" not found.");
        }
        catch (DirectoryNotFoundException e)
        {
            throw new FileReadException(this, $"Directory not found for file \"{FilePath}\". {e}");
        }
        catch (IOException e)
        {
            throw new FileReadException(this, $"IOException reading file \"{FilePath}\". {e}");
        }
    }


    // Private methods.
    /* Parsing stages. */




    /* Base. */
    private void ParseBase()
    {
        while (SkipUntilNonWhitespace(null))
        {
            string Keyword = ParseWord($"Expected keyword '{KEYWORD_NAMESPACE}' or '{KEYWORD_USING}'.");

            switch (Keyword)
            {
                case KEYWORD_NAMESPACE:
                    ParseNamespace();
                    break;

                case KEYWORD_USING:
                    ParseUsingStatement();
                    break;

                case KEYWORD_CLASS:
                    ParseClass();
                    break;

                default:
                    throw new FileReadException(this, $"Invalid keyword \"{Keyword}\".");
            }
        }
    }


    /* Namespace and using statement. */
    private string ParseNamespaceName()
    {
        SkipUntilNonWhitespace("Expected namespace name.");

        StringBuilder NamespaceName = new();
        bool HadSeparator = false;
        while (!(char.IsWhiteSpace(GetCharAtDataIndex()) || (GetCharAtDataIndex() == SEMICOLON)))
        {
            char Character = GetCharAtDataIndex();
            NamespaceName.Append(Character);
            IncrementDataIndex();

            if (Character == NAMESPACE_SEPARATOR)
            {
                if (HadSeparator)
                {
                    throw new FileReadException(this, $"Invalid namespace, multiple namespace separators '{NAMESPACE_SEPARATOR}'");
                }

                HadSeparator = true;
                continue;
            }

            HadSeparator = false;
            if (!char.IsLetter(Character))
            {
                throw new FileReadException(this, $"Invalid character '{Character}' in namespace. Expected only letters.");
            }
        }

        if (NamespaceName.Length == 0)
        {
            throw new FileReadException(this, $"Expected namespace name.");
        }
        else if (NamespaceName[^1] == NAMESPACE_SEPARATOR)
        {
            throw new FileReadException(this, $"Namespace ends with a namespace separator '{NAMESPACE_SEPARATOR}'");
        }
        else if (NamespaceName[0] == NAMESPACE_SEPARATOR)
        {
            throw new FileReadException(this, $"Namespace starts with a namespace separator '{NAMESPACE_SEPARATOR}'");
        }

        SkipUntil($"Missing '{SEMICOLON}' after namespace", SEMICOLON);
        IncrementDataIndex();

        return NamespaceName.ToString();
    }

    private void ParseNamespace()
    {
        _activeNamespace = ParseNamespaceName();
    }

    private void ParseUsingStatement()
    {
        _namespaceImports.Add(ParseNamespaceName());
    }


    /* Classes. */
    internal string ParseClassName()
    {
        SkipUntilNonWhitespace("Expected class name.");
        return ParseIdentifier("Expected class name.");
    }

    internal void ParseClass()
    {
        PackClass ParsedClass = new(_activeNamespace, ParseClassName());
        SkipUntil($"Expected class body opening '{OPEN_CURLY_BRACKET}'", OPEN_CURLY_BRACKET);
        IncrementDataIndex();

        bool IsClassClosed = false;
        while (!IsClassClosed)
        {
            SkipUntilNonWhitespace(null);
            if (GetCharAtDataIndex() == CLOSE_CURLY_BRACKET)
            {
                IsClassClosed = true;
                IncrementDataIndex();
            }
            else
            {
                ParseClassMember(ParsedClass);
            }
        }
    }

    internal void ParseClassMember(PackClass packClass)
    {

    }


    /* Functions. */


    /* Generic parsing methods. */
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void IncrementDataIndex()
    {
        if (_data[_dataIndex] == '\n')
        {
            Line++;
        }
        _dataIndex++;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private char GetCharAtDataIndex() => _dataIndex >= _data.Length ? '\0' : _data[_dataIndex];

    private bool SkipUntilNonWhitespace(string? exceptionMessage)
    {
        while ((_dataIndex < _data.Length) && char.IsWhiteSpace(_data[_dataIndex]))
        {
            IncrementDataIndex();
        }

        if (_dataIndex >= _data.Length)
        {
            if (exceptionMessage != null)
            {
                throw new FileReadException(this, exceptionMessage);
            }
            return false;
        }
        return true;
    }

    private string ParseWord(string? exceptionMessage)
    {
        StringBuilder Word = new();

        while ((_dataIndex < _data.Length) && char.IsLetter(GetCharAtDataIndex()))
        {
            Word.Append(GetCharAtDataIndex());
            IncrementDataIndex();
        }

        if (Word.Length == 0)
        {
            if (exceptionMessage != null)
            {
                throw new FileReadException(this, exceptionMessage);
            }
            return string.Empty;
        }
        return Word.ToString();
    }

    private string ParseIdentifier(string? exceptionMessage)
    {
        StringBuilder Identifier = new();

        while ((_dataIndex < _data.Length) && (char.IsLetterOrDigit(GetCharAtDataIndex()) || GetCharAtDataIndex() == UNDERSCORE))
        {
            Identifier.Append(GetCharAtDataIndex());
            IncrementDataIndex();
        }

        if (Identifier.Length == 0)
        {
            if (exceptionMessage != null)
            {
                throw new FileReadException(this, exceptionMessage);
            }
            return string.Empty;
        }
        else if (char.IsDigit(Identifier[0]))
        {
            throw new FileReadException(this, exceptionMessage);
        }

        return Identifier.ToString();
    }

    private string ParseUntil(string? exceptionMessage, params char[] charsToFind)
    {
        if (charsToFind == null)
        {
            throw new ArgumentNullException(nameof(charsToFind));
        }

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

        if ((ParsedText.Length == 0) && (exceptionMessage != null))
        {
            throw new FileReadException(this, exceptionMessage);
        }
        return string.Empty;
    }

    private bool SkipUntil(string? exceptionMessage, params char[] charsToFind)
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
            throw new FileReadException(this, exceptionMessage);
        }
        return false;
    }
}