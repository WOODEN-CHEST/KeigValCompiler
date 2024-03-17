using KeigValCompiler.Semantician;
using KeigValCompiler.Source.Parser;
using System.Runtime.CompilerServices;
using System.Text;


namespace KeigValCompiler.Source;


/* Yeah, this class may be a tiny bit too big.
 * Good OOP principles? Never heard of it, just put everything in a single giant monolith class. */
internal class SourceFileParser
{
    // Internal fields.
    internal string FilePath { get; private init; }
    internal int Line { get; private set; } = 1;
    internal DataPack Pack { get; private init; }
    internal PackSourceFile SourceFile { get; private set; }


    // Private fields.
    private string _data;
    private int _dataIndex = 0;

    private string _activeNamespace = string.Empty;
    private readonly HashSet<string> _namespaceImports = new(8);


    // Constructors.
    internal SourceFileParser(string filePath, DataPack pack)
    {
        FilePath = filePath ?? throw new ArgumentNullException(nameof(filePath));
        Pack = pack ?? throw new ArgumentNullException(nameof(pack));
    }


    // Internal methods.
    internal void ParseFile(DataPack parentPack)
    {
        try
        {
            SourceFile = new(parentPack);
            _data = File.ReadAllText(FilePath);
            StripDataOfComments();
            ParseBase();
        }
        catch (PackContentException e)
        {
            throw new FileReadException(this, $"Invalid pack content for file \"{FilePath}\": {e.Message}");
        }
        catch (FileNotFoundException e)
        {
            throw new FileReadException(this, $"File \"{FilePath}\" not found.");
        }
        catch (DirectoryNotFoundException e)
        {
            throw new FileReadException(this, $"Directory not found for file \"{FilePath}\". {e.Message}");
        }
        catch (IOException e)
        {
            throw new FileReadException(this, $"IOException reading file \"{FilePath}\". {e.Message}");
        }
    }

    
    // Private methods.
    /* Comments. */
    private void StripDataOfComments()
    {
        _dataIndex = 0;
        StringBuilder StrippedData = new(_data.Length);

        while (_dataIndex < _data.Length) 
        {
            char Character = GetCharAtDataIndex();

            if (Character == KGVL.STRING_INTERPOLATION_OPERATOR)
            {
                StrippedData.Append(ReadInterpolatedStringInclSyntax());
            }
            else if (Character == KGVL.DOUBLE_QUOTE)
            {
                StrippedData.Append(ReadQuotedInclQuote(KGVL.DOUBLE_QUOTE));
            }
            else if (Character == KGVL.SINGLE_QUOTE)
            {
                StrippedData.Append(ReadQuotedInclQuote(KGVL.SINGLE_QUOTE));
            }
            else if (HasStringAtIndex(_dataIndex, KGVL.SINGLE_LINE_COMMENT_START))
            {
                SkipUntil(null, '\n');
            }
            else if (HasStringAtIndex(_dataIndex, KGVL.MULTI_LINE_COMMENT_START))
            {
                SkipUntilString("Multi-comment string wasn't terminated properly.", KGVL.MULTI_LINE_COMMENT_END);
            }
            else
            {
                StrippedData.Append(Character);
            }
            IncrementDataIndex();
        }

        _data = StrippedData.ToString();
        _dataIndex = 0;
    }

    private string ReadInterpolatedStringInclSyntax()
    {
        StringBuilder ReadData = new();
        ReadData.Append(GetCharAtDataIndex());
        IncrementDataIndex();
        ReadData.Append(ReadUntilNonWhitespace(null));
        if (GetCharAtDataIndex() != KGVL.DOUBLE_QUOTE)
        {
            return ReadData.ToString();
        }

        ReadData.Append(GetCharAtDataIndex());
        IncrementDataIndex();

        bool WasInterpolationEscaped = false;
        while (GetCharAtDataIndex() != KGVL.DOUBLE_QUOTE)
        {
            char Character = GetCharAtDataIndex();
            bool HasDoubleInterpSymbol = Character == KGVL.OPEN_CURLY_BRACKET && GetCharAtDataIndex(_dataIndex + 1) == KGVL.OPEN_CURLY_BRACKET;

            if ((Character == KGVL.OPEN_CURLY_BRACKET) && !HasDoubleInterpSymbol && !WasInterpolationEscaped)
            {
                ReadData.Append(ReadInterpolation());
            }
            else
            {
                ReadData.Append(Character);
            }

            WasInterpolationEscaped = !WasInterpolationEscaped && HasDoubleInterpSymbol;
            IncrementDataIndex();
        }

        ReadData.Append(GetCharAtDataIndex());
        return ReadData.ToString();
    }

    private string ReadInterpolation()
    {
        StringBuilder Interpolation = new();
        char Character;

        while (((Character = GetCharAtDataIndex()) != KGVL.CLOSE_CURLY_BRACKET) && (_dataIndex < _data.Length))
        {
            if (Character == KGVL.SINGLE_QUOTE)
            {
                Interpolation.Append(ReadQuotedInclQuote(KGVL.SINGLE_QUOTE));
            }
            else if (Character == KGVL.STRING_INTERPOLATION_OPERATOR)
            {
                Interpolation.Append(ReadInterpolatedStringInclSyntax());
            }
            else if (Character == KGVL.DOUBLE_QUOTE)
            {
                Interpolation.Append(ReadQuotedInclQuote(KGVL.DOUBLE_QUOTE));
            }
            else
            {
                Interpolation.Append(Character);
            }
            IncrementDataIndex();
        }
        if (_dataIndex >= _data.Length)
        {
            throw new SourceFileException(this, "Expected end of interpolated string's interpolation block.");
        }
        Interpolation.Append(Character);
        return Interpolation.ToString();
    }

    private string ReadQuotedInclQuote(char targetQuote)
    {
        StringBuilder Target = new();
        Target.Append(GetCharAtDataIndex());
        IncrementDataIndex();

        bool IsInEscapeSequence = false;
        char Character;

        while (((Character = GetCharAtDataIndex()) != targetQuote || IsInEscapeSequence) && (_dataIndex < _data.Length))
        {
            IsInEscapeSequence = !IsInEscapeSequence && (Character == KGVL.ESCAPE_CHAR);
            Target.Append(Character);
            IncrementDataIndex();
        }

        if (_dataIndex >= _data.Length)
        {
            throw new SourceFileException(this, $"Expected end of quoted block with {targetQuote}");
        }

        Target.Append(Character);
        return Target.ToString();
    }


    /* Base. */
    private void ParseBase()
    {
        while (SkipUntilNonWhitespace(null))
        {
            string Keyword = ReadWord($"Expected keyword '{KGVL.KEYWORD_NAMESPACE}' or '{KGVL.KEYWORD_USING}'.");

            if (Keyword == KGVL.KEYWORD_NAMESPACE)
            {
                _activeNamespace = ParseNamespaceName();
                continue;
            }
            else if (Keyword == KGVL.KEYWORD_USING)
            {
                ParseUsingStatement();
                continue;
            }

            ReverseUntilOneAfterWhitespace(); // Go back to before keyword was parsed to allow respective functions to properly handle it.
            ParseMember(null);
        }
    }


    /* Namespace and using statement. */
    private string ParseNamespaceName()
    {
        const string EXCEPTION_MSG_NAMESPACE_NAME = "Expected namespace name.";
        SkipUntilNonWhitespace(EXCEPTION_MSG_NAMESPACE_NAME);
        StringBuilder NamespaceBuilder = new();
        bool MayNamespaceEnd = true;

        while ((GetCharAtDataIndex() != KGVL.SEMICOLON) || !MayNamespaceEnd)
        {
            NamespaceBuilder.Append(ReadIdentifier(EXCEPTION_MSG_NAMESPACE_NAME));
            SkipUntilNonWhitespace($"Expected '{KGVL.NAMESPACE_SEPARATOR}' or '{KGVL.SEMICOLON}'");

            if (GetCharAtDataIndex() == KGVL.NAMESPACE_SEPARATOR)
            {
                NamespaceBuilder.Append(KGVL.NAMESPACE_SEPARATOR);
                IncrementDataIndex();
                MayNamespaceEnd = false;
                SkipUntilNonWhitespace(EXCEPTION_MSG_NAMESPACE_NAME);
            }
            else
            {
                MayNamespaceEnd = true;
                SkipUntilNonWhitespace($"Expected '{KGVL.SEMICOLON}'");
            }
        }
        IncrementDataIndex();
        return NamespaceBuilder.ToString();
    }

    private void ParseUsingStatement()
    {
        _namespaceImports.Add(ParseNamespaceName());
    }


    /* Classes. */
    internal void ParseClass(PackMemberModifiers modifiers, PackClass? parentClass, string identifier)
    {
        if ((modifiers & (~PackMemberModifiers.Static & ~PackMemberModifiers.Abstract)) != 0)
        {
            throw new FileReadException(this, $"Class may only have the modifiers '{KGVL.KEYWORD_STATIC}' and '{KGVL.KEYWORD_ABSTRACT}'");
        }

        SkipUntilNonWhitespace("Expected class name.");
        string ClassName = ReadIdentifier("Expected class name.");

        List<string> ExtendedItems = new();
        SkipWhitespaceUntil("Expected class body, class extension or interface implementation.", KGVL.COLON, KGVL.OPEN_CURLY_BRACKET);
        if (GetCharAtDataIndex() == KGVL.COLON)
        {
            do
            {
                IncrementDataIndex();
                SkipUntilNonWhitespace("Expected class or interface name.");

                ExtendedItems.Add(ReadIdentifier("Expected class or interface name."));

                SkipWhitespaceUntil("Expected class body, class extension or interface implementation", KGVL.OPEN_CURLY_BRACKET, KGVL.COMMA);
            }
            while (GetCharAtDataIndex() != KGVL.OPEN_CURLY_BRACKET);
        }

        PackClass ParsedClass = new(_activeNamespace, ClassName, parentClass, modifiers, ExtendedItems.ToArray());
        IncrementDataIndex();
        //Pack.AddClass(ParsedClass);

        bool IsClassClosed = false;
        while (!IsClassClosed)
        {
            SkipUntilNonWhitespace(null);
            if (GetCharAtDataIndex() == KGVL.CLOSE_CURLY_BRACKET)
            {
                IsClassClosed = true;
                IncrementDataIndex();
            }
            else
            {
                ParseMember(ParsedClass);
            }
        }
    }


    /* Namespace and class members. */
    private int CountAccessModifiers(PackMemberModifiers modifiers)
    {
        return ((modifiers & PackMemberModifiers.Private) > 0 ? 1 : 0)
            + ((modifiers & PackMemberModifiers.Protected) > 0 ? 1 : 0) + ((modifiers & PackMemberModifiers.Public) > 0 ? 1 : 0);
    }

    private PackMemberModifiers CombineModifier(PackMemberModifiers appliedModifiers, PackMemberModifiers newModifier)
    {
        if ((appliedModifiers & newModifier) != 0)
        {
            throw new FileReadException(this, $"Duplicate member modifier {newModifier.ToString().ToLower()}");
        }

        appliedModifiers |= newModifier;

        if (CountAccessModifiers(appliedModifiers) > 1)
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
            throw new FileReadException(this, "Built-in member cannot be abstract, virtual or overridden.");
        }

        return appliedModifiers;
    }

    private void ParseMemberValue(PackClass? packClass, PackMemberModifiers modifiers, string type)
    {
        SkipUntilNonWhitespace("Expected member identifier.");
        string Identifier = ReadIdentifier("Expected member identifier.");
        SkipUntilNonWhitespace(null);

        switch (GetCharAtDataIndex())
        {
            case KGVL.SEMICOLON:
            case KGVL.ASSIGNMENT_OPERATOR:
                IncrementDataIndex();
                ParseField(modifiers, packClass);
                break;

            case KGVL.OPEN_CURLY_BRACKET:
                IncrementDataIndex();
                ParseProperty(modifiers, packClass);
                break;

            case KGVL.OPEN_PARENTHESIS:
                IncrementDataIndex();
                ParseFunction(modifiers, packClass, type);
                break;

            default:
                throw new FileReadException(this, $"Expected '{KGVL.SEMICOLON}', '{KGVL.ASSIGNMENT_OPERATOR}'," +
                    $" '{KGVL.OPEN_PARENTHESIS}' or '{KGVL.OPEN_CURLY_BRACKET}'");
        }
    }

    private PackMemberModifiers StringToModifier(string modifierName)
    {
        return modifierName switch
        {
            KGVL.KEYWORD_STATIC => PackMemberModifiers.Static,
            KGVL.KEYWORD_PRIVATE => PackMemberModifiers.Private,
            KGVL.KEYWORD_PROTECTED => PackMemberModifiers.Protected,
            KGVL.KEYWORD_PUBLIC => PackMemberModifiers.Public,
            KGVL.KEYWORD_READONLY => PackMemberModifiers.Readonly,
            KGVL.KEYWORD_ABSTRACT => PackMemberModifiers.Abstract,
            KGVL.KEYWORD_VIRTUAL => PackMemberModifiers.Virtual,
            KGVL.KEYWORD_OVERRIDE => PackMemberModifiers.Override,
            KGVL.KEYWORD_BUILTIN => PackMemberModifiers.BuiltIn,
            KGVL.KEYWORD_INLINE => PackMemberModifiers.Inline,
            KGVL.KEYWORD_RAW => PackMemberModifiers.Raw,
            _ => PackMemberModifiers.None
        };
}

    private PackMemberModifiers ParseMemberModifiers(PackClass? packClass)
    {
        PackMemberModifiers Modifiers = PackMemberModifiers.None;
        const string EXCEPTION_MSG_EXPECTED_MODIFIER = "Expected member modifier or identifier";

        while (true) // While true loops are the most fun!
        {
            SkipUntilNonWhitespace(EXCEPTION_MSG_EXPECTED_MODIFIER);
            string Word = ReadIdentifier(EXCEPTION_MSG_EXPECTED_MODIFIER);
            PackMemberModifiers NewModifier = StringToModifier(Word);

            if (NewModifier != PackMemberModifiers.None)
            {
                Modifiers = CombineModifier(Modifiers, NewModifier);
                continue;
            }

            ReverseUntilOneAfterWhitespace();

            return Modifiers;
        }
    }

    internal void ParseMember(PackClass? memberClass)
    {
        PackMemberModifiers Modifiers = ParseMemberModifiers(memberClass);

        string Keyword = ReadIdentifier("Expected return type or member type.");
        SkipUntilNonWhitespace("Expected member identifier.");
        string Identifier = ReadIdentifier("Expected member identifier");

        if (Keyword == KGVL.KEYWORD_CLASS)
        {
            ParseClass(Modifiers, memberClass, Identifier);
            return;
        }

        SkipUntilNonWhitespace($"Expected member value");
        if (GetCharAtDataIndex() == KGVL.OPEN_PARENTHESIS)
        {
            ParseFunction(Modifiers, memberClass, Identifier, Keyword);
        }
        else if (GetCharAtDataIndex() == KGVL.OPEN_CURLY_BRACKET)
        {
            ParseProperty(Modifiers, memberClass, Identifier);
        }
        else
        {
            ParseField(Modifiers, memberClass, Identifier);
        }
    }


    /* Statements. */
    private void ParseStatement()
    {

    }


    /* Functions. */
    private void ParseFunction(PackMemberModifiers modifiers, PackClass? parentClass, string identifier, string returnType)
    {
        if ((modifiers & PackMemberModifiers.Readonly) != 0)
        {
            throw new FileReadException(this, $"Functions cannot have the modifier '{KGVL.KEYWORD_READONLY}'");
        }
    }


    /* Properties. */
    private void ParseProperty(PackMemberModifiers modifiers, PackClass? parentClass, string identifier)
    {
        
    }


    /* Fields. */
    private void ParseField(PackMemberModifiers modifiers, PackClass? parentClass, string identifier)
    {
        SkipWhitespaceUntil($"Expected field value or '{KGVL.SEMICOLON}'");
        if (GetCharAtDataIndex() == KGVL.SEMICOLON)
        {

        }
    }


    /* Generic parsing methods. */
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void IncrementDataIndex()
    {
        if (GetCharAtDataIndex() == '\n')
        {
            Line++;
        }
        _dataIndex++;
    }

    private void IncrementDatIndexNTimes(int count)
    {
        for (int i = 0; i < count; i++) { IncrementDataIndex(); }
    }

    private void DecrementDataIndex()
    {
        _dataIndex = Math.Max(0, _dataIndex - 1);
        if (_data[_dataIndex] == '\n')
        {
            Line--;
        }
    }

    private void DecrementDataIndexNTimes(int count)
    {
        for (int i = 0; i < count; i++) { DecrementDataIndex(); }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private char GetCharAtDataIndex() => _dataIndex >= _data.Length ? '\0' : _data[_dataIndex];

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private char GetCharAtDataIndex(int index) => index >= _data.Length ? '\0' : _data[index];


    private string ReadWord(string? exceptionMessage)
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

    private string ReadIdentifier(string? exceptionMessage)
    {
        StringBuilder Identifier = new();

        while ((_dataIndex < _data.Length) && IsIndentifierChar(GetCharAtDataIndex()))
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

    private void ReverseUntilOneAfterWhitespace()
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

    private string ReadUntil(string? exceptionMessage, params char[] charsToFind)
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

        if (((ParsedText.Length == 0) || !charsToFind.Contains(GetCharAtDataIndex())) && (exceptionMessage != null))
        {
            throw new FileReadException(this, exceptionMessage);
        }
        return string.Empty;
    }

    private string ReadUntilNonWhitespace(string? exceptionMessage)
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

    private bool SkipWhitespaceUntil(string? exceptionMessage, params char[] charsToFind)
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
            else if (!char.IsWhiteSpace(GetCharAtDataIndex()))
            {
                throw new FileReadException(this, exceptionMessage);
            }
            IncrementDataIndex();
        }

        if (exceptionMessage != null)
        {
            throw new FileReadException(this, exceptionMessage);
        }
        return false;
    }

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

    private bool SkipUntilString(string? exceptionMessage, params string[] stringsToFind)
    {
        if (stringsToFind == null)
        {
            throw new ArgumentNullException(nameof(stringsToFind));
        }

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
            throw new FileReadException(this, exceptionMessage);
        }
        return false;
    }

    private bool HasStringAtIndex(int index, string stringToFind)
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

    private bool IsIndentifierChar(char character) => char.IsLetterOrDigit(character) || (character == KGVL.UNDERSCORE);
}