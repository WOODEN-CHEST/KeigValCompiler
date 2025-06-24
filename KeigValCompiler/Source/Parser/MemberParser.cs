using KeigValCompiler.Semantician;
using KeigValCompiler.Semantician.Member;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KeigValCompiler.Source.Parser;

internal class MemberParser : AbstractParserBase
{
    // Constructors.
    public MemberParser(SourceDataParser parser, ParserUtilities utils, PackSourceFile sourceFile)
        : base(parser, utils, sourceFile) { }


    // Methods.
    internal void ParseMember(object memberHolder)
    {
        PackMemberModifiers Modifiers = ParseMemberModifiers();
        string FirstSegment = Parser.ReadIdentifier($"Expected {Utils.MemberHolderToString(memberHolder)} member.");
        bool IsRecord = (Modifiers | PackMemberModifiers.Record) != PackMemberModifiers.None;

        if ((FirstSegment == KGVL.KEYWORD_CLASS) || IsRecord)
        {
            if (IsRecord)
            {
                Parser.ReverseUntilOneAfterWhitespace();
            }
            ParseClass(memberHolder!, Modifiers);
        }
        else if (FirstSegment == KGVL.KEYWORD_STRUCT)
        {

        }
        else if (FirstSegment == KGVL.KEYWORD_DELEGATGE)
        {

        }
        else if (FirstSegment == KGVL.KEYWORD_INTERFACE)
        {

        }
        else if (FirstSegment == KGVL.KEYWORD_EVENT)
        {

        }
        else if (FirstSegment == KGVL.KEYWORD_ENUM)
        {

        }
        else
        {
            
        }
    }


    // Private methods.
    private PackMemberModifiers CombineModifier(PackMemberModifiers appliedModifiers, PackMemberModifiers newModifier)
    {
        if ((appliedModifiers & newModifier) != 0)
        {
            throw new SourceFileReadException(Parser.FilePath, Parser.Line,
                $"Duplicate member modifier {newModifier.ToString().ToLower()}");
        }

        appliedModifiers |= newModifier;

        return appliedModifiers;
    }

    private PackMemberModifiers StringToModifier(string modifierName)
    {
        return modifierName switch
        {
            KGVL.KEYWORD_STATIC => PackMemberModifiers.Static,
            KGVL.KEYWORD_PRIVATE => PackMemberModifiers.Private,
            KGVL.KEYWORD_PROTECTED => PackMemberModifiers.Protected,
            KGVL.KEYWORD_PUBLIC => PackMemberModifiers.Public,
            KGVL.KEYWORD_RECORD => PackMemberModifiers.Record,
            KGVL.KEYWORD_READONLY => PackMemberModifiers.Readonly,
            KGVL.KEYWORD_ABSTRACT => PackMemberModifiers.Abstract,
            KGVL.KEYWORD_VIRTUAL => PackMemberModifiers.Virtual,
            KGVL.KEYWORD_OVERRIDE => PackMemberModifiers.Override,
            KGVL.KEYWORD_BUILTIN => throw new SourceFileReadException(Parser.FilePath, Parser.Line,
                $"keyword \"{KGVL.KEYWORD_BUILTIN}\" is reserved only for compiler's internal usage"),
            KGVL.KEYWORD_INLINE => PackMemberModifiers.Inline,
            _ => PackMemberModifiers.None
        };
    }

    private PackMemberModifiers ParseMemberModifiers()
    {
        PackMemberModifiers Modifiers = PackMemberModifiers.None;
        const string EXCEPTION_MSG = "Expected member modifier or keyword.";

        while (true)
        {
            Parser.SkipUntilNonWhitespace(EXCEPTION_MSG);
            string Word = Parser.ReadWord(null);
            PackMemberModifiers NewModifier = StringToModifier(Word);

            if (NewModifier != PackMemberModifiers.None)
            {
                Modifiers = CombineModifier(Modifiers, NewModifier);
                continue;
            }

            Parser.ReverseUntilOneAfterWhitespace();
            return Modifiers;
        }
    }

    private void ParseClass(object parentObject, PackMemberModifiers modifiers)
    {
        const string EXCEPTION_MSG_EXPECTED_IDENTIFIER = "Expected class identifier";
        if (parentObject is not IPackTypeHolder ClassHolder)
        {
            throw new SourceFileReadException(Parser.FilePath, Parser.Line,
                $"A member of type {Utils.MemberHolderToString(parentObject)} object cannot hold classes");
        }

        Parser.SkipUntilNonWhitespace(EXCEPTION_MSG_EXPECTED_IDENTIFIER);
        string Identifier = Parser.ReadIdentifier(EXCEPTION_MSG_EXPECTED_IDENTIFIER);
        PackClass ParsedClass = new(new(Identifier), SourceFile) { Modifiers = modifiers };
        ClassHolder.AddClass(ParsedClass);

        bool IsRecordClass = (modifiers & PackMemberModifiers.Record) != PackMemberModifiers.None;
        Parser.SkipUntilNonWhitespace(GetClassWrongStartExceptionMessage(IsRecordClass));
        if (IsRecordClass)
        {
            ParseRecordPrimaryConstructor(ParsedClass);
            if (Parser.GetCharAtDataIndex() == KGVL.SEMICOLON)
            {
                Parser.IncrementDataIndex();
                return;
            }
        }
        Parser.SkipUntilNonWhitespace("Expected class body");
        Parser.IncrementDataIndex();

        while (Parser.SkipUntilNonWhitespace("Expected class member or class body end")
            && (Parser.GetCharAtDataIndex() != KGVL.CLOSE_CURLY_BRACKET)
            && (Parser.IsMoreDataAvailable))
        {
            ParseMember(ParsedClass);
        }

        if (Parser.GetCharAtDataIndex() != KGVL.CLOSE_CURLY_BRACKET)
        {
            throw new SourceFileReadException(Parser.FilePath, Parser.Line, 
                $"Expected end of class '{KGVL.CLOSE_CURLY_BRACKET}'");
        }
        Parser.IncrementDataIndex();
    }

    private string GetClassWrongStartExceptionMessage(bool isRecord)
    {
        if (isRecord)
        {
            return "Expected record body start or record primary constructor or member extensions.";
        }
        return "Expected class body start of member extensions.";
    }

    private void ParseRecordPrimaryConstructor(PackClass recordClass)
    {
        Parser.SkipUntilNonWhitespace("Expected record primary constructor or body.");
        if (Parser.GetCharAtDataIndex() != KGVL.OPEN_PARENTHESIS)
        {
            return;
        }

        Parser.IncrementDataIndex();
        PackFunction Constructor = new(new(recordClass.SelfIdentifier.SourceCodeName), SourceFile);
        Constructor.Parameters.SetFrom(ParseFunctionParameters(KGVL.CLOSE_PARENTHESIS));

        recordClass.AddFunction(Constructor);
        Parser.IncrementDataIndex();
    }

    private void ParseStruct(PackMemberModifiers modifiers)
    {
        throw new NotImplementedException();
    }

    private void ParseDelegate(PackMemberModifiers modifiers)
    {
        throw new NotImplementedException();
    }

    private void ParseInterface(PackMemberModifiers modifiers)
    {
        throw new NotImplementedException();
    }

    private void ParseEvent(PackMemberModifiers modifiers)
    {
        throw new NotImplementedException();
    }

    private void ParseEnumeration(PackMemberModifiers modifiers)
    {
        throw new NotImplementedException();
    }

    private void ParseReturnTypedMember(object memberHolder, PackMemberModifiers modifiers, string returnType)
    {
        const string EXCEPTION_MSG_IDENTIFIER = "Expected member identifier";
        Parser.SkipUntilNonWhitespace(EXCEPTION_MSG_IDENTIFIER);
        string Identifier = Parser.ReadIdentifier(EXCEPTION_MSG_IDENTIFIER);

        Parser.SkipUntilNonWhitespace("Expected member value");
        char NextCharacter = Parser.GetCharAtDataIndex(Parser.DataIndex + 1);

        if (NextCharacter == KGVL.OPEN_PARENTHESIS)
        {
            
        }
        else if (NextCharacter == KGVL.ASSIGNMENT_OPERATOR)
        {
            bool IsQuickGetBody = $"{NextCharacter}{Parser.GetCharAtDataIndex(Parser.DataIndex + 2)}" == KGVL.QUICK_METHOD_BODY;
            if (IsQuickGetBody || (NextCharacter == KGVL.OPEN_CURLY_BRACKET))
            {
                
            }
            else
            {
                
            }
        }
    }

    private void ParseFunction(PackMemberModifiers modifiers, string returnType, string identifier)
    {
        throw new NotImplementedException();
    }

    private void ParseField(PackMemberModifiers modifiers, string returnType, string identifier)
    {
        throw new NotImplementedException();
    }

    private void ParseProperty(PackMemberModifiers modifiers, string returnType, string identifier)
    {
        throw new NotImplementedException();
    }

    internal Identifier[] ParseMemberExtensions()
    {
        const string EXCEPTION_MSG_IDENTIFIER = "Expected member extension identifier.";
        char[] ExpectedChars = new char[] { KGVL.COLON, KGVL.OPEN_CURLY_BRACKET };

        List<Identifier> ExtendedItems = new();

        Parser.SkipWhitespaceUntil("Expected member body or member extension.", ExpectedChars);

        bool IsAnExtensionExpected = Parser.GetCharAtDataIndex() == KGVL.COLON;

        while (IsAnExtensionExpected)
        {
            Parser.IncrementDataIndex();
            Parser.SkipUntilNonWhitespace(EXCEPTION_MSG_IDENTIFIER);
            Identifier ExtendedMember = new(Parser.ReadIdentifier(EXCEPTION_MSG_IDENTIFIER));
            ExtendedItems.Add(ExtendedMember);

            Parser.SkipWhitespaceUntil("Expected class body, class extension or interface implementation",
                ExpectedChars);
            IsAnExtensionExpected = Parser.GetCharAtDataIndex() == KGVL.COMMA;
        }

        return ExtendedItems.ToArray();
    }

    private FunctionParameterModifier StringToParamModifier(string value)
    {
        return value switch
        {
            KGVL.KEYWORD_IN => FunctionParameterModifier.In,
            KGVL.KEYWORD_OUT => FunctionParameterModifier.Out,
            KGVL.KEYWORD_REF => FunctionParameterModifier.Ref,
            _ => FunctionParameterModifier.None
        };
    }

    private FunctionParameterCollection ParseFunctionParameters(char paramListEndChar)
    {
        const string EXCEPTION_MSG_IDENTIFIER = "Expected parameter name";
        const string EXCEPTION_MSG_TYPE = "Expected parameter type";
        const string EXCEPTION_MSG_TYPE_OR_MODIFIER = "Expected parameter type or parameter modifier";
        FunctionParameterCollection Params = new();

        Parser.SkipUntilNonWhitespace("Expected parameters or parameter end");

        bool IsParameterExpected = Parser.GetCharAtDataIndex() != paramListEndChar;

        while (IsParameterExpected)
        {
            string FirstWord = Parser.ReadIdentifier(EXCEPTION_MSG_TYPE_OR_MODIFIER);
            FunctionParameterModifier Modifier = StringToParamModifier(FirstWord);

            if (Modifier != FunctionParameterModifier.None)
            {
                Parser.SkipUntilNonWhitespace(EXCEPTION_MSG_TYPE);
            }
            else
            {
                Parser.ReverseUntilOneAfterIdentifier();
            }

            string ParamType = Parser.ReadIdentifier(EXCEPTION_MSG_TYPE);
            Parser.SkipUntilNonWhitespace(EXCEPTION_MSG_IDENTIFIER);
            string ParamName = Parser.ReadIdentifier(EXCEPTION_MSG_IDENTIFIER);
            Parser.SkipUntilNonWhitespace("Expected end of parameters or another parameter");
            Params.AddItem(new(new(ParamType), new(ParamName), Modifier));

            char NextChar = Parser.GetCharAtDataIndex();
            IsParameterExpected = NextChar == KGVL.COMMA;
            if (IsParameterExpected)
            {
                Parser.IncrementDataIndex();
                Parser.SkipUntilNonWhitespace(EXCEPTION_MSG_TYPE_OR_MODIFIER);
            }
        }

        return Params;
    }
}