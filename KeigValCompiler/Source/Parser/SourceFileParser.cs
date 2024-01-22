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


    /* Syntax. */
    private const char SEMICOLON = ';';
    private const char NAMESPACE_SEPARATOR = '.';
    private const char UNDERSCORE = '_';
    private const char OPEN_CURLY_BRACKET = '{';
    private const char CLOSE_CURLY_BRACKET = '}';
    private const char OPEN_PARENTHESIS = '(';
    private const char CLOSE_PARENTHESIS = ')';
    private const char ASSIGNMENT_OPEERATOR = '=';

    private const char LINE_COMMENT_INDICATOR1 = '/';
    private const char LINE_COMMENT_INDICATOR2 = '*';
    private const string MULTI_LINE_COMMENT_START = "/*";
    private const string MULTI_LINE_COMMENT_END = "*/";
    private const string SINGLE_LINE_COMMENT_START = "//";
    private const char NEWLINE = '\n';
    private const int COMMENT_INDICATOR_LENGTH = 2;


    /* Keywords. */
    private const string KEYWORD_NAMESPACE = "namespace";
    private const string KEYWORD_USING = "using";

    private const string KEYWORD_CLASS = "class";
    private const string KEYWORD_STATIC = "static";
    private const string KEYWORD_PRIVATE = "private";
    private const string KEYWORD_PROTECTED = "protected";
    private const string KEYWORD_PUBLIC = "public";
    private const string KEYWORD_BUILTIN = "builtin";
    private const string KEYWORD_INLINE = "inline";
    private const string KEYWORD_ABSTRACT = "abstract";
    private const string KEYWORD_VIRTUAL = "virtual";
    private const string KEYWORD_OVERRIDE = "override";
    private const string KEYWORD_RAW = "raw";

    private const string KEYWORD_FOR = "for";
    private const string KEYWORD_FOREACH = "foreach";
    private const string KEYWORD_CONTINUE = "continue";
    private const string KEYWORD_BREAK = "break";
    private const string KEYWORD_IF = "if";
    private const string KEYWORD_ELSE = "else";
    private const string KEYWORD_SWITCH = "switch";
    private const string KEYWORD_CASE = "case";
    private const string KEYWORD_GOTO = "goto";
    private const string KEYWORD_LABEL = "label";


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
            _data = StripCodeOfComments(File.ReadAllText(FilePath));
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
    private string StripCodeOfComments(string code)
    {
        StringBuilder StrippedCode = new();
        StringBuilder Indicator = new();

        bool IsInsideSingleLineComment = false;
        bool IsInsideMultiLineComment = false;

        foreach (char Character in code)
        {
            if (IsInsideSingleLineComment && (Character != NEWLINE))
            {
                continue;
            }
            else if (IsInsideMultiLineComment)
            {
                if (Character is LINE_COMMENT_INDICATOR1 or LINE_COMMENT_INDICATOR2)
                {
                    Indicator.Append(Character);
                }
                else
                {
                    Indicator.Clear();
                    continue;
                }

                if (Indicator.Length == COMMENT_INDICATOR_LENGTH)
                {
                    if (Indicator.Equals(MULTI_LINE_COMMENT_END))
                    {
                        IsInsideMultiLineComment = false;
                        Indicator.Clear();
                    }
                    else
                    {
                        Indicator.Remove(0, 1);
                    }
                }

                continue;
            }
            IsInsideSingleLineComment = false;

            if (Character is LINE_COMMENT_INDICATOR1 or LINE_COMMENT_INDICATOR2)
            {
                Indicator.Append(Character);
            }
            else
            {
                StrippedCode.Append(Indicator);
                Indicator.Clear();
            }

            if (Indicator.Length == COMMENT_INDICATOR_LENGTH)
            {
                switch (Indicator.ToString())
                {
                    case SINGLE_LINE_COMMENT_START:
                        IsInsideSingleLineComment = true;
                        Indicator.Clear();
                        continue;

                    case MULTI_LINE_COMMENT_START:
                        IsInsideMultiLineComment = true;
                        Indicator.Clear();
                        continue;

                    default:
                        StrippedCode.Append(Indicator[0]);
                        Indicator.Remove(0, 1);
                        continue;
                }
            }

            if (Indicator.Length == 0)
            {
                StrippedCode.Append(Character);
            }
        }

        if (IsInsideMultiLineComment)
        {
            throw new FileReadException(this, "Multi-line comment not closed properly.");
        }

        StrippedCode.Append(Indicator);
        return StrippedCode.ToString();
    }

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

    private PackMemberModifiers CombineModifier(PackMemberModifiers appliedModifiers, PackMemberModifiers newModifier)
    {
        if ((appliedModifiers & newModifier) != 0)
        {
            throw new FileReadException(this, $"Duplicate member modifier {newModifier.ToString().ToLower()}");
        }

        appliedModifiers |= newModifier;

        int AccessModifierCount = (int)(appliedModifiers & PackMemberModifiers.Private)
            + (int)(appliedModifiers & PackMemberModifiers.Protected) + (int)(appliedModifiers & PackMemberModifiers.Public);
        if (AccessModifierCount > 1)
        {
            throw new FileReadException(this, "Too many access modifiers for member.");
        }

        if ((appliedModifiers & PackMemberModifiers.Abstract) != 0 && (appliedModifiers & PackMemberModifiers.Override) != 0)
        {
            throw new FileReadException(this, "Member cannot be both abstract and overridden.");
        }
        if ((appliedModifiers & PackMemberModifiers.Abstract) != 0 && (appliedModifiers & PackMemberModifiers.Virtual) != 0)
        {
            throw new FileReadException(this, "Member cannot be both abstract and virtual.");
        }
        if ((appliedModifiers & (PackMemberModifiers.Abstract | PackMemberModifiers.Virtual | PackMemberModifiers.Override)) != 0)
        {
            throw new FileReadException(this, "Built-in member cannot be both abstract, virtual or overridden.");
        }

        return appliedModifiers;
    }

    private PackMemberModifiers GetModifierByKeywrod(string keywrod)
    {
        return keywrod switch
        {
            KEYWORD_STATIC => PackMemberModifiers.Static,
            KEYWORD_PRIVATE => PackMemberModifiers.Private,
            KEYWORD_PROTECTED => PackMemberModifiers.Protected,
            KEYWORD_PUBLIC => PackMemberModifiers.Public,
            KEYWORD_ABSTRACT => PackMemberModifiers.Abstract,
            KEYWORD_VIRTUAL => PackMemberModifiers.Virtual,
            KEYWORD_OVERRIDE => PackMemberModifiers.Override,
            KEYWORD_BUILTIN => PackMemberModifiers.BuiltIn,
            KEYWORD_INLINE => PackMemberModifiers.Inline,
            KEYWORD_RAW => PackMemberModifiers.Raw,
            _ => PackMemberModifiers.None
        };
    }

    private void ParseClassMemberValue(PackClass packClass, PackMemberModifiers modifiers)
    {
        SkipUntilNonWhitespace($"Expected '{SEMICOLON}', '{ASSIGNMENT_OPEERATOR}', '{OPEN_PARENTHESIS}' or '{OPEN_CURLY_BRACKET}'");

        switch (GetCharAtDataIndex())
        {
            case SEMICOLON:
            case ASSIGNMENT_OPEERATOR: // Field.
                ParseField();
                break;

            case OPEN_CURLY_BRACKET: // Property.
                ParseProperty();
                break;

            default: // Function.
                ParseFunction();
                break;
        }
    }

    internal void ParseClassMember(PackClass packClass)
    {
        PackMemberModifiers Modifiers = PackMemberModifiers.None;
        string? MemberName = null;
        string? FunctionReturnType = null;

        bool ReachedMemberDefinition = false;
        while (!ReachedMemberDefinition)
        {
            SkipUntilNonWhitespace(null);
            string Word = ParseIdentifier("Expected member modifier or identifier.");
            PackMemberModifiers NewModifier = GetModifierByKeywrod(Word);

            if (NewModifier != PackMemberModifiers.None)
            {
                Modifiers = CombineModifier(Modifiers, NewModifier);
                continue;
            }

            ParseClassMemberValue(packClass, Modifiers);

            MemberName = Word;
            ReachedMemberDefinition = true;
        }

        if (MemberName == null)
        {
            throw new FileReadException(this, "Expected member identifier.");
        }

        SkipUntilNonWhitespace("");
        switch (GetCharAtDataIndex())
        {
            case SEMICOLON:
            case ASSIGNMENT_OPEERATOR:
                ParseField();
                break;

            case OPEN_PARENTHESIS:
                ParseFunction();
                break;

            case OPEN_CURLY_BRACKET:
                ParseProperty();
                break;
        }
    }


    /* Functions. */
    private void ParseFunction(PackMemberModifiers modifiers, PackClass parentClass)
    {

    }


    /* Properties. */
    private void ParseProperty()
    {
        
    }


    /* Fields. */
    private void ParseField()
    {

    }


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