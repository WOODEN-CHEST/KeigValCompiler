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
        bool IsRecord = (Modifiers & PackMemberModifiers.Record) != PackMemberModifiers.None;

        if (FirstSegment == KGVL.KEYWORD_CLASS)
        {
            ParseClass(memberHolder, Modifiers);
        }
        else if (IsRecord)
        {
            Parser.ReverseUntilOneAfterWhitespace();
            ParseClass(memberHolder, Modifiers);
        }
        else if (FirstSegment == KGVL.KEYWORD_STRUCT)
        {
            ParseStruct(memberHolder, Modifiers);
        }
        else if (FirstSegment == KGVL.KEYWORD_DELEGATGE)
        {
            ParseDelegate(memberHolder, Modifiers);
        }
        else if (FirstSegment == KGVL.KEYWORD_INTERFACE)
        {
            ParseInterface(memberHolder, Modifiers);
        }
        else if (FirstSegment == KGVL.KEYWORD_EVENT)
        {
            ParseEvent(memberHolder, Modifiers);
        }
        else if (FirstSegment == KGVL.KEYWORD_ENUM)
        {
            ParseEnumeration(memberHolder, Modifiers);
        }
        else
        {
            ParseReturnTypedMember(memberHolder, Modifiers, FirstSegment);
        }
    }


    // Private methods.
    private PackMemberModifiers CombineModifier(PackMemberModifiers appliedModifiers, PackMemberModifiers newModifier)
    {
        if ((appliedModifiers & newModifier) != 0)
        {
            throw new SourceFileReadException(Parser,
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
            KGVL.KEYWORD_BUILTIN => throw new SourceFileReadException(Parser,
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

    private Identifier ParseMemberIdentifier(string exceptionMsg)
    {
        Parser.SkipUntilNonWhitespace(exceptionMsg);
        return new(Parser.ReadIdentifier(exceptionMsg));
    }

    private Identifier? ParseReturnType(string exceptionMsg)
    {
        Parser.SkipUntilNonWhitespace(exceptionMsg);
        string Identifier = Parser.ReadIdentifier(exceptionMsg);
        return Identifier == KGVL.KEYWORD_VOID ? null : new(Identifier);
    }

    private void ParseClass(object parentObject, PackMemberModifiers modifiers)
    {
        ParseExtendableType(parentObject, modifiers, "class",
            (identifier) => new PackClass(identifier, SourceFile),
            (type, holder) => holder.AddClass(type));
    }

    private string GetClassWrongStartExceptionMessage(PackMember member,
        bool isRecord,
        string typeName)
    {
        string Name = member.SelfIdentifier.SourceCodeName;
        if (isRecord)
        {
            return $"Expected record body start for record {Name} (character '{KGVL.OPEN_CURLY_BRACKET}') " +
                $"or record primary constructor, or member extensions, got end of file.";
        }
        return $"Expected {typeName} body start for {typeName} {Name} " +
            $"(character '{KGVL.OPEN_CURLY_BRACKET}') or member extensions, got end of file.";
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

    private void ParseStruct(object parentObject, PackMemberModifiers modifiers)
    {
        ParseExtendableType(parentObject, modifiers, "structure",
            (identifier) => new PackStruct(identifier, SourceFile),
            (type, holder) => holder.AddStruct(type));
    }

    private void ParseExtendableType<T>(object parentObject,
        PackMemberModifiers modifiers,
        string typeName,
        Func<Identifier, T> typeConstructor,
        Action<T, IPackTypeHolder> addFunction) where T : PackMember
    {
        if (parentObject is not IPackTypeHolder TypeHolder)
        {
            throw CreateInvalidHolderException(parentObject, typeName);
        }

        Identifier Name = ParseMemberIdentifier($"Expected {typeName} identifier");
        SourceFileOrigin Origin = new(Parser.Line);
        T CreatedType = typeConstructor.Invoke(Name);
        CreatedType.Modifiers = modifiers;
        addFunction.Invoke(CreatedType, TypeHolder);
        CreatedType.SourceFileOrigin = Origin;

        bool IsRecord = (modifiers & PackMemberModifiers.Record) != PackMemberModifiers.None;
        Parser.SkipUntilNonWhitespace(GetClassWrongStartExceptionMessage(CreatedType, IsRecord, typeName));
        if ((CreatedType is PackClass ClassType) && IsRecord)
        {
            ParseRecordPrimaryConstructor(ClassType);
            if (Parser.GetCharAtDataIndex() == KGVL.SEMICOLON)
            {
                Parser.IncrementDataIndex();
                return;
            }
        }

        ParseMemberExtensions((IPackMemberExtender)CreatedType);
        Parser.IncrementDataIndex();

        while (Parser.SkipUntilNonWhitespace($"Expected {typeName} member or {typeName} body end")
            && (Parser.GetCharAtDataIndex() != KGVL.CLOSE_CURLY_BRACKET)
            && Parser.IsMoreDataAvailable)
        {
            ParseMember(CreatedType);
        }

        if (Parser.GetCharAtDataIndex() != KGVL.CLOSE_CURLY_BRACKET)
        {
            throw new SourceFileReadException(Parser,
                $"Expected end of {typeName} '{KGVL.CLOSE_CURLY_BRACKET}'");
        }
        Parser.IncrementDataIndex();
    }

    private void ParseDelegate(object parentObject, PackMemberModifiers modifiers)
    {
        if (parentObject is not IPackTypeHolder DelegateHolder)
        {
            throw CreateInvalidHolderException(parentObject, "delegate");
        }
        
        Identifier? ReturnType = ParseReturnType("Expected delegate return type identifier");
        Identifier Name = ParseMemberIdentifier("Expected delegate identifier");
        SourceFileOrigin Origin = new(Parser.Line);

        const string EXCEPTION_MSG_PARAM_LIST = "Expected delegate parameter list";
        Parser.SkipUntilNonWhitespace(EXCEPTION_MSG_PARAM_LIST);
        if (Parser.GetCharAtDataIndex() != KGVL.OPEN_PARENTHESIS)
        {
            throw new SourceFileReadException(Parser, EXCEPTION_MSG_PARAM_LIST);
        }
        Parser.IncrementDataIndex();
        FunctionParameterCollection Parameters = ParseFunctionParameters(KGVL.CLOSE_PARENTHESIS);
        if (Parser.GetCharAtDataIndex() != KGVL.CLOSE_PARENTHESIS)
        {
            throw new SourceFileReadException(Parser, "Expected end of delegate parameter list");
        }
        Parser.IncrementDataIndex();

        string ExceptionMsgEnd = $"Expected end of delegate definition '{KGVL.SEMICOLON}'";
        Parser.SkipUntilNonWhitespace(ExceptionMsgEnd);
        if (Parser.GetCharAtDataIndex() != KGVL.SEMICOLON)
        {
            throw new SourceFileReadException(Parser, ExceptionMsgEnd);
        }
        Parser.IncrementDataIndex();

        PackDelegate Delegate = new(Name, ReturnType, SourceFile)
        {
            Modifiers = modifiers,
            SourceFileOrigin = Origin,
        };
        Delegate.Parameters.SetFrom(Parameters);
        DelegateHolder.AddDelegate(Delegate);
    }

    private void ParseInterface(object parentObject, PackMemberModifiers modifiers)
    {
        ParseExtendableType(parentObject, modifiers, "interface",
            (identifier) => new PackInterface(identifier, SourceFile),
            (type, holder) => holder.AddInterface(type));
    }

    private void ParseEvent(object? parentObject, PackMemberModifiers modifiers)
    {
        if (parentObject is not IPackEventHolder EventHolder)
        {
            throw new SourceFileReadException(Parser,
                $"A member of type {Utils.MemberHolderToString(parentObject)} object cannot hold delegates");
        }
    }

    private void ParseEnumeration(object parentObject, PackMemberModifiers modifiers)
    {
        if (parentObject is not IPackTypeHolder EnumHolder)
        {
            throw CreateInvalidHolderException(parentObject, "enum");
        }

        Identifier Name = ParseMemberIdentifier("Expected enum identifier");
        SourceFileOrigin Origin = new(Parser.Line);
        string BodyNotStartExceptionMessage = $"Expected enum body start (character '{KGVL.OPEN_CURLY_BRACKET}') " +
            $"for enum \"{Name.SourceCodeName}\"";
        PackEnumeration Enum = new(Name, SourceFile)
        {
            Modifiers = modifiers,
            SourceFileOrigin = Origin,
        };

        Parser.SkipUntilNonWhitespace(BodyNotStartExceptionMessage);
        if (Parser.GetCharAtDataIndex() !=KGVL.OPEN_CURLY_BRACKET)
        {
            throw new SourceFileReadException(Parser, BodyNotStartExceptionMessage);
        }
        Parser.IncrementDataIndex();
        ParseEnumValues(Enum);
        if (Parser.GetCharAtDataIndex() != KGVL.CLOSE_CURLY_BRACKET)
        {
            throw new SourceFileReadException(Parser, 
                $"Expected enum body end (character '{KGVL.CLOSE_CURLY_BRACKET}') " +
                $"for enum \"{Name.SourceCodeName}\"");
        }
        Parser.IncrementDataIndex();
    }

    private void ParseEnumValues(PackEnumeration enumeration)
    {
        const int ENUM_STARTING_VALUE = 0;
        string ExceptionMsgNextEnumValue = $"Expected either enum body end '{KGVL.CLOSE_CURLY_BRACKET}', " +
                $"or enum value separator '{KGVL.COMMA}'";
        string ExceptionMsgEnumConstantIdentifier = "Expected enumeration constant identifier for enum " +
                $"\"{enumeration.SelfIdentifier.SourceCodeName}\"";

        Parser.SkipUntilNonWhitespace($"Expected either enum body end '{KGVL.CLOSE_CURLY_BRACKET}' " +
            $"or enum value for enum {enumeration.SelfIdentifier.SourceCodeName} " +
            $"(starts on line {enumeration.SourceFileOrigin.Line})");

        int CurrentEnumValue = ENUM_STARTING_VALUE;
        bool IsValueExpected = Parser.GetCharAtDataIndex() != KGVL.CLOSE_CURLY_BRACKET;
        while (IsValueExpected)
        {
            string ConstantName = Parser.ReadIdentifier(ExceptionMsgEnumConstantIdentifier);
            Parser.SkipUntilNonWhitespace($"Expected enum constant \"{ConstantName}\" value assignment or " +
                $"comma '{KGVL.COMMA}' for next enum constant or enum body end '{KGVL.CLOSE_CURLY_BRACKET}' " +
                $"for enum \"{enumeration.SelfIdentifier.SourceCodeName}\"");

            if (Parser.GetCharAtDataIndex() == KGVL.ASSIGNMENT_OPERATOR)
            {
                string ExceptionMsgExpectedConstant = $"Expected value for enum constant \"{ConstantName}\" " +
                    $"for enum {enumeration.SelfIdentifier.SourceCodeName}";
                Parser.IncrementDataIndex();
                Parser.SkipUntilNonWhitespace(ExceptionMsgExpectedConstant);
                GenericNumber Number = Parser.ReadInteger(ExceptionMsgExpectedConstant);
                CurrentEnumValue = CastNumberToEnumConstantValue(enumeration, ConstantName, Number);
            }
            enumeration.SetConstant(ConstantName, CurrentEnumValue);

            Parser.SkipUntilNonWhitespace(ExceptionMsgNextEnumValue);
            IsValueExpected = Parser.GetCharAtDataIndex() == KGVL.COMMA;
            if (IsValueExpected)
            {
                Parser.IncrementDataIndex();
                Parser.SkipUntilNonWhitespace(ExceptionMsgEnumConstantIdentifier);
            }
            CurrentEnumValue++;
        }
    }

    private int CastNumberToEnumConstantValue(PackEnumeration enumeration, string constantName, GenericNumber number)
    {
        if (number.IsLong || number.IsUnsigned)
        {
            throw new SourceFileReadException(Parser, $"Enum constant \"{constantName}\" in enum type " +
                $"\"{enumeration.SelfIdentifier.SourceCodeName}\" is out of the valid range " +
                $"{int.MinValue} to {int.MaxValue}, it has a value of {number.Number}");
        }

        if (number.Base == NumberBase.Binary)
        {
            return Convert.ToInt32(number.Number, 2);
        }
        else if (number.Base == NumberBase.Hexadecimal)
        {
            return Convert.ToInt32(number.Number, 16);
        }
        return int.Parse(number.Number);
    }

    private SourceFileReadException CreateInvalidHolderException(object holder, string targetTypeName)
    {
        if (holder is PackNameSpace NameSpace)
        {
            return new(Parser, $"A namespace cannot contain members of type {targetTypeName}");
        }
        if (holder is PackMember Member)
        {
            return new(Parser, $"The {Utils.MemberHolderToString(holder)} " +
                $"member \"{Member.SelfIdentifier.SourceCodeName}\" cannot hold a member of type {targetTypeName}");
        }
        return new($"The parent member holder cannot hold a member of type {targetTypeName}");
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

    internal void ParseMemberExtensions(IPackMemberExtender extender)
    {
        const string EXCEPTION_MSG_IDENTIFIER = "Expected member extension identifier.";
        char[] ExpectedChars = new char[] { KGVL.COLON, KGVL.OPEN_CURLY_BRACKET };

        Parser.SkipWhitespaceUntil("Expected member body or member extension.", ExpectedChars);

        bool IsAnExtensionExpected = Parser.GetCharAtDataIndex() == KGVL.COLON;

        while (IsAnExtensionExpected)
        {
            Parser.IncrementDataIndex();
            Parser.SkipUntilNonWhitespace(EXCEPTION_MSG_IDENTIFIER);
            Identifier ExtendedMember = new(Parser.ReadIdentifier(EXCEPTION_MSG_IDENTIFIER));
            extender.AddExtendedMember(ExtendedMember);

            Parser.SkipWhitespaceUntil("Expected class body, class extension or interface implementation",
                ExpectedChars);
            IsAnExtensionExpected = Parser.GetCharAtDataIndex() == KGVL.COMMA;
        }
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