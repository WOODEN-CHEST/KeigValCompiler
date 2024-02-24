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
    internal void ParseFile()
    {
        try
        {
            _data = File.ReadAllText(FilePath);
            StripDataOfComments();
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
    private void StripDataOfComments()
    {
        _dataIndex = 0;
        StringBuilder StrippedData = new(_data.Length);

        while (_dataIndex < _data.Length) 
        {
            char Character = GetCharAtDataIndex();

            if (Character == KGVL.DOUBLE_QUOTE)
            {
                StrippedData.Append(Character);
                IncrementDataIndex();
                StrippedData.Append(ParseUntil(null, KGVL.DOUBLE_QUOTE));
                StrippedData.Append(GetCharAtDataIndex());
            }
            else if (HasStringAtIndex(_dataIndex, "//"))
            {
                SkipUntil(null, '\n');
            }
            else if (HasStringAtIndex(_dataIndex, "/*"))
            {
                SkipUntilString("Multi-comment string wasn't terminated properly.", "*/");
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

    /* Base. */
    private void ParseBase()
    {
        while (SkipUntilNonWhitespace(null))
        {
            string Keyword = ParseWord($"Expected keyword '{KGVL.KEYWORD_NAMESPACE}' or '{KGVL.KEYWORD_USING}'.");

            if (Keyword == KGVL.KEYWORD_NAMESPACE)
            {
                ParseNamespace();
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
        SkipUntilNonWhitespace("Expected namespace name.");

        StringBuilder NamespaceName = new();
        bool HadSeparator = false;

        while (!(char.IsWhiteSpace(GetCharAtDataIndex()) || (GetCharAtDataIndex() == KGVL.SEMICOLON)))
        {
            char Character = GetCharAtDataIndex();
            NamespaceName.Append(Character);
            IncrementDataIndex();

            if (Character == KGVL.NAMESPACE_SEPARATOR)
            {
                if (HadSeparator)
                {
                    throw new FileReadException(this, $"Invalid namespace, multiple namespace separators '{KGVL.NAMESPACE_SEPARATOR}'");
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
        else if (NamespaceName[^1] == KGVL.NAMESPACE_SEPARATOR)
        {
            throw new FileReadException(this, $"Namespace ends with a namespace separator '{KGVL.NAMESPACE_SEPARATOR}'");
        }
        else if (NamespaceName[0] == KGVL.NAMESPACE_SEPARATOR)
        {
            throw new FileReadException(this, $"Namespace starts with a namespace separator '{KGVL.NAMESPACE_SEPARATOR}'");
        }

        SkipUntil($"Missing '{KGVL.SEMICOLON}' after namespace", KGVL.SEMICOLON);
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
    internal void ParseClass(PackMemberModifiers modifiers, PackClass? parentClass)
    {
        if ((modifiers & (~PackMemberModifiers.Static & ~PackMemberModifiers.Abstract)) != 0)
        {
            throw new FileReadException(this, $"Class may only have the modifiers '{KGVL.KEYWORD_STATIC}' and '{KGVL.KEYWORD_ABSTRACT}'");
        }

        SkipUntilNonWhitespace("Expected class name.");
        string ClassName = ParseIdentifier("Expected class name.");

        List<string> ExtendedItems = new();
        SkipWhitespaceUntil("Expected class body, class extension or interface implementation.", KGVL.COLON, KGVL.OPEN_CURLY_BRACKET);
        if (GetCharAtDataIndex() == KGVL.COLON)
        {
            do
            {
                IncrementDataIndex();
                SkipUntilNonWhitespace("Expected class or interface name.");

                ExtendedItems.Add(ParseIdentifier("Expected class or interface name."));

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
        string Identifier = ParseIdentifier("Expected member identifier.");
        SkipUntilNonWhitespace(null);

        switch (GetCharAtDataIndex())
        {
            case KGVL.SEMICOLON:
            case KGVL.ASSIGNMENT_OPEERATOR:
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
                throw new FileReadException(this, $"Expected '{KGVL.SEMICOLON}', '{KGVL.ASSIGNMENT_OPEERATOR}'," +
                    $" '{KGVL.OPEN_PARENTHESIS}' or '{KGVL.OPEN_CURLY_BRACKET}'");
        }
    }

    private PackMemberModifiers ParseItemModifiers(PackClass? packClass, out string type)
    {
        PackMemberModifiers Modifiers = PackMemberModifiers.None;

        while (true) // While true loops are the most fun!
        {
            SkipUntilNonWhitespace("Expected member modifier or identifier");
            string Word = ParseIdentifier("Expected member modifier or identifier.");
            PackMemberModifiers NewModifier = Word switch
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

            if (NewModifier != PackMemberModifiers.None)
            {
                Modifiers = CombineModifier(Modifiers, NewModifier);
                continue;
            }

            if ((CountAccessModifiers(Modifiers) == 0) && (packClass != null))
            {
                Modifiers |= PackMemberModifiers.Private;
            }

            type = Word;

            return Modifiers;
        }
    }

    internal void ParseMember(PackClass? packClass)
    {
        PackMemberModifiers Modifiers = ParseItemModifiers(packClass ,out string LastIdentifier);

        if (LastIdentifier == KGVL.KEYWORD_CLASS)
        {
            ParseClass(Modifiers, packClass);
            return;
        }

        if (packClass == null && ((Modifiers & (PackMemberModifiers.Static | PackMemberModifiers.Abstract | PackMemberModifiers.Virtual
                | PackMemberModifiers.Override)) != 0)) // Extra modifier restrains for members outside of classes.
        {
            throw new FileReadException(this, "Members outside of classes may not be static, abstract, virtual or overridden.");
        }
        if ((packClass != null) && ((Modifiers & PackMemberModifiers.Static) == 0))
        {
            throw new FileReadException(this, "All members of a static class must be static");
        }

        ParseMemberValue(packClass, Modifiers, LastIdentifier);
    }


    /* Statements. */
    private void ParseStatement()
    {

    }


    /* Functions. */
    private void ParseFunction(PackMemberModifiers modifiers, PackClass? parentClass, string returnType)
    {
        if ((modifiers & PackMemberModifiers.Readonly) != 0)
        {
            throw new FileReadException(this, $"Functions cannot have the modifier '{KGVL.KEYWORD_READONLY}'");
        }
    }


    /* Properties. */
    private void ParseProperty(PackMemberModifiers modifiers, PackClass? parentClass)
    {
        
    }


    /* Fields. */
    private void ParseField(PackMemberModifiers modifiers, PackClass? parentClass)
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

        while ((_dataIndex < _data.Length) && (char.IsLetterOrDigit(GetCharAtDataIndex()) || GetCharAtDataIndex() == KGVL.UNDERSCORE))
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

        if (((ParsedText.Length == 0) && (exceptionMessage != null)) || !charsToFind.Contains(GetCharAtDataIndex()))
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
}