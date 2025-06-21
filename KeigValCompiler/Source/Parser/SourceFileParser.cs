using KeigValCompiler.Semantician;
using KeigValCompiler.Source.Parser;
using System.Runtime.CompilerServices;
using System.Text;

namespace KeigValCompiler.Source;


internal class SourceFileParser
{
    // Fields.
    public string FilePath { get; private init; }
    public DataPack Pack { get; private init; }


    // Private fields.
    private PackSourceFile _sourceFile;
    private TextParser _parser;

    private PackNameSpace? _activeNamespace;


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
            string FileData = File.ReadAllText(FilePath);

            TextParser OriginalFileParser = new(FileData, FilePath);
            string StrippedFileData = new CommentStripper(OriginalFileParser).StripCommentsFromCode(FileData);

            _parser = new(StrippedFileData, FilePath);
            _sourceFile = new(Pack, FilePath);
            Pack.AddSourceFile(_sourceFile);
            ParseBase();
        }
        catch (PackContentException e)
        {
            throw new SourceFileReadException($"Invalid pack content for file \"{FilePath}\": {e.Message}");
        }
        catch (FileNotFoundException e)
        {
            throw new SourceFileReadException($"File \"{FilePath}\" not found.");
        }
        catch (DirectoryNotFoundException e)
        {
            throw new SourceFileReadException($"Directory not found for file \"{FilePath}\". {e.Message}");
        }
        catch (IOException e)
        {
            throw new SourceFileReadException($"IOException reading file \"{FilePath}\". {e.Message}");
        }
    }


    // Private methods.
    /* Base. */
    private void ParseBase()
    {
        while (_parser.SkipUntilNonWhitespace(null))
        {
            string Keyword = _parser.ReadWord($"Expected keyword '{KGVL.KEYWORD_NAMESPACE}' or '{KGVL.KEYWORD_USING}'.");

            if (Keyword == KGVL.KEYWORD_NAMESPACE)
            {
                _activeNamespace = GetOrCreateNamespace(ParseNamespaceName(), false);
                continue;
            }
            else if (Keyword == KGVL.KEYWORD_USING)
            {
                ParseUsingStatement();
                continue;
            }
            else if (_activeNamespace == null)
            {
                throw new SourceFileReadException(FilePath, _parser.Line, "Expected namespace or using statement.");
            }

            //ReverseUntilOneAfterWhitespace(); // Go back to before keyword was parsed to allow respective functions to properly handle it.
            //ParseMember(null);
        }
    }


    /* Namespace and using statement. */
    private PackNameSpace GetOrCreateNamespace(string fullName, bool isImport)
    {
        PackNameSpace? NameSpace = Pack.TryGetNamespace(fullName);
        if (NameSpace != null)
        {
            return NameSpace;
        }

        NameSpace = new(new(fullName));

        if (isImport)
        {
            _sourceFile.AddNamespaceImport(NameSpace);
        }
        else
        {
            _sourceFile.AddNamespace(NameSpace);
        }
            
        return NameSpace;
    }

    private string ParseNamespaceName()
    {
        const string EXCEPTION_MSG_NAMESPACE_NAME = "Expected namespace name.";
        _parser.SkipUntilNonWhitespace(EXCEPTION_MSG_NAMESPACE_NAME);

        StringBuilder NamespaceBuilder = new();
        bool IsMoreNamespaceExpected = true;

        while ((_parser.GetCharAtDataIndex() != KGVL.SEMICOLON) || IsMoreNamespaceExpected)
        {
            NamespaceBuilder.Append(_parser.ReadIdentifier(EXCEPTION_MSG_NAMESPACE_NAME));
            _parser.SkipUntilNonWhitespace($"Expected '{KGVL.NAMESPACE_SEPARATOR}' or '{KGVL.SEMICOLON}' " +
                "(Namespace continuation or end)");

            char CharAfterIdentifier = _parser.GetCharAtDataIndex();
            if (CharAfterIdentifier == KGVL.NAMESPACE_SEPARATOR)
            {
                NamespaceBuilder.Append(KGVL.NAMESPACE_SEPARATOR);
                _parser.IncrementDataIndex();
                IsMoreNamespaceExpected = true;
                _parser.SkipUntilNonWhitespace(EXCEPTION_MSG_NAMESPACE_NAME);
            }
            else if (CharAfterIdentifier == KGVL.SEMICOLON)
            {
                IsMoreNamespaceExpected = false;
            }
            else
            {
                throw new SourceFileReadException($"Expected '{KGVL.SEMICOLON}' " +
                    $"after namespace end, got '{CharAfterIdentifier}'");
            }
        }

        _parser.IncrementDataIndex();
        return NamespaceBuilder.ToString();
    }

    private void ParseUsingStatement()
    {
        _sourceFile.AddNamespaceImport(GetOrCreateNamespace(ParseNamespaceName(), true));
    }


    ///* Classes. */
    //internal string[] ParseClassExtensions()
    //{
    //    List<string> ExtendedItems = new();
    //    SkipWhitespaceUntil("Expected class body, class extension or interface implementation.", KGVL.COLON, KGVL.OPEN_CURLY_BRACKET);
    //    if (GetCharAtDataIndex() == KGVL.COLON)
    //    {
    //        do
    //        {
    //            IncrementDataIndex();
    //            SkipUntilNonWhitespace("Expected extended class or interface name.");
    //            ExtendedItems.Add(ReadIdentifier("Expected extended class or interface name."));
    //            SkipWhitespaceUntil("Expected class body, class extension or interface implementation", KGVL.OPEN_CURLY_BRACKET, KGVL.COMMA);
    //        }
    //        while (GetCharAtDataIndex() != KGVL.OPEN_CURLY_BRACKET);
    //    }

    //    return ExtendedItems.ToArray();
    //}

    //internal void ParseClass(PackMemberModifiers modifiers, PackClass? parentClass, string identifier)
    //{
    //    string[] ExtendedItems = ParseClassExtensions();
    //    PackClass ParsedClass;

    //    if (parentClass != null)
    //    {
    //        ParsedClass = new(identifier, modifiers, parentClass, ExtendedItems);
    //    }
    //    else
    //    {
    //        ParsedClass = new(identifier, modifiers, _activeNamespace.Name, SourceFile, ExtendedItems);
    //    }

    //    IncrementDataIndex();
    //    _activeNamespace.AddClass(ParsedClass);

    //    bool IsClassClosed = false;
    //    while (!IsClassClosed)
    //    {
    //        SkipUntilNonWhitespace(null);
    //        if (GetCharAtDataIndex() == KGVL.CLOSE_CURLY_BRACKET)
    //        {
    //            IsClassClosed = true;
    //            IncrementDataIndex();
    //        }
    //        else
    //        {
    //            ParseMember(ParsedClass);
    //        }
    //    }
    //}


    ///* Namespace and class members. */
    //private PackMemberModifiers CombineModifier(PackMemberModifiers appliedModifiers, PackMemberModifiers newModifier)
    //{
    //    if ((appliedModifiers & newModifier) != 0)
    //    {
    //        throw new FileReadException(this, $"Duplicate member modifier {newModifier.ToString().ToLower()}");
    //    }

    //    appliedModifiers |= newModifier;

    //    return appliedModifiers;
    //}

    //private PackMemberModifiers StringToModifier(string modifierName)
    //{
    //    return modifierName switch
    //    {
    //        KGVL.KEYWORD_STATIC => PackMemberModifiers.Static,
    //        KGVL.KEYWORD_PRIVATE => PackMemberModifiers.Private,
    //        KGVL.KEYWORD_PROTECTED => PackMemberModifiers.Protected,
    //        KGVL.KEYWORD_PUBLIC => PackMemberModifiers.Public,
    //        KGVL.KEYWORD_READONLY => PackMemberModifiers.Readonly,
    //        KGVL.KEYWORD_ABSTRACT => PackMemberModifiers.Abstract,
    //        KGVL.KEYWORD_VIRTUAL => PackMemberModifiers.Virtual,
    //        KGVL.KEYWORD_OVERRIDE => PackMemberModifiers.Override,
    //        KGVL.KEYWORD_BUILTIN => PackMemberModifiers.BuiltIn,
    //        KGVL.KEYWORD_INLINE => PackMemberModifiers.Inline,
    //        _ => PackMemberModifiers.None
    //    };
    //}

    //private PackMemberModifiers ParseMemberModifiers(PackClass? packClass)
    //{
    //    PackMemberModifiers Modifiers = PackMemberModifiers.None;
    //    const string EXCEPTION_MSG_EXPECTED_MODIFIER = "Expected member modifier.";

    //    while (true) // While true loops are the most fun!
    //    {
    //        SkipUntilNonWhitespace(EXCEPTION_MSG_EXPECTED_MODIFIER);
    //        string Word = ReadIdentifier(EXCEPTION_MSG_EXPECTED_MODIFIER);
    //        PackMemberModifiers NewModifier = StringToModifier(Word);

    //        if (NewModifier != PackMemberModifiers.None)
    //        {
    //            Modifiers = CombineModifier(Modifiers, NewModifier);
    //            continue;
    //        }

    //        ReverseUntilOneAfterWhitespace();

    //        return Modifiers;
    //    }
    //}

    //internal void ParseMember(PackClass? memberClass)
    //{
    //    PackMemberModifiers Modifiers = ParseMemberModifiers(memberClass);

    //    string Keyword = ReadIdentifier("Expected return type or property / field type.");
    //    SkipUntilNonWhitespace("Expected member identifier.");
    //    string Identifier = ReadIdentifier("Expected member identifier");

    //    if (Keyword == KGVL.KEYWORD_CLASS)
    //    {
    //        ParseClass(Modifiers, memberClass, Identifier);
    //        return;
    //    }

    //    SkipUntilNonWhitespace($"Expected member value");
    //    if (GetCharAtDataIndex() == KGVL.OPEN_PARENTHESIS)
    //    {
    //        ParseFunction(Modifiers, memberClass, Identifier, Keyword);
    //    }
    //    else if (GetCharAtDataIndex() == KGVL.OPEN_CURLY_BRACKET)
    //    {
    //        ParseProperty(Modifiers, memberClass, Identifier);
    //    }
    //    else
    //    {
    //        ParseField(Modifiers, memberClass, Identifier);
    //    }
    //}


    ///* Statements. */
    //private void ParseValueStatement()
    //{

    //}


    ///* Functions. */
    //private void ParseFunction(PackMemberModifiers modifiers, PackClass? parentClass, string identifier, string returnType)
    //{

    //}


    ///* Properties. */
    //private void ParseProperty(PackMemberModifiers modifiers, PackClass? parentClass, string identifier)
    //{

    //}


    ///* Fields. */
    //private void ParseField(PackMemberModifiers modifiers, PackClass? parentClass, string identifier)
    //{
    //    SkipWhitespaceUntil($"Expected field value or '{KGVL.SEMICOLON}'");
    //    if (GetCharAtDataIndex() == KGVL.SEMICOLON)
    //    {

    //    }
    //}


    /* Generic parsing methods. */

}