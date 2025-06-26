using KeigValCompiler.Error;
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
    public MemberParser(PackParsingContext context) : base(context) { }


    // Methods.
    internal void ParseMember(object memberHolder)
    {
        PackMemberModifiers Modifiers = ParseMemberModifiers();
        string FirstSegment = Parser.ReadIdentifier(ErrorCreator.ExpectedMember.CreateOptions(
            Utils.MemberHolderToString(memberHolder)));
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
            throw new SourceFileReadException(Parser, ErrorCreator.DuplicateModifiers.CreateOptions(newModifier));
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
                ErrorCreator.ReservedKeywordBuiltIn.CreateOptions()),
            KGVL.KEYWORD_INLINE => PackMemberModifiers.Inline,
            _ => PackMemberModifiers.None
        };
    }

    private PackMemberModifiers ParseMemberModifiers()
    {
        PackMemberModifiers Modifiers = PackMemberModifiers.None;

        while (true)
        {
            Parser.SkipUntilNonWhitespace(ErrorCreator.ExpectedMemberModifierOrKeyword.CreateOptions());
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

    private Identifier ParseTypeMemberIdentifier(string typeName)
    {
        ErrorCreateOptions ErrorOptions = ErrorCreator.ExpectedTypeMemberIdentifier.CreateOptions(typeName);
        Parser.SkipUntilNonWhitespace(ErrorOptions);
        return new(Parser.ReadIdentifier(ErrorOptions));
    }

    private TypeTargetIdentifier? ParseReturnType(string holderTypeName)
    {
        ErrorCreateOptions ErrorOptions = ErrorCreator.ExpectedReturnTypeIdentifier.CreateOptions(holderTypeName);
        Parser.SkipUntilNonWhitespace(ErrorOptions);
        TypeTargetIdentifier Name = Utils.ParseTypeTargetIdentifier(Parser, ErrorOptions);
        return Name.MainTarget!.SourceCodeName == KGVL.KEYWORD_VOID ? null : Name;
    }

    private void ParseClass(object parentObject, PackMemberModifiers modifiers)
    {
        ParseExtendableType(parentObject, modifiers, KGVL.NAME_CLASS,
            (identifier) => new PackClass(identifier, SourceFile),
            (type, holder) => holder.AddClass(type));
    }

    private ErrorCreateOptions GetClassWrongStartExceptionMessage(PackMember member,
        bool isRecord,
        string typeName)
    {
        string Name = member.SelfIdentifier.SourceCodeName;
        if (isRecord)
        {
            return ErrorCreator.EOFWhileParsingRecord.CreateOptions(Name);
        }
        return ErrorCreator.EOFWhileParsingType.CreateOptions(typeName, Name);
    }

    private void ParseRecordPrimaryConstructor(PackClass recordClass)
    {
        Parser.SkipUntilNonWhitespace(ErrorCreator.ExpectedRecordPrimaryConstructorOrBody.CreateOptions(
            recordClass.SelfIdentifier.SourceCodeName));

        if (Parser.GetCharAtDataIndex() != KGVL.OPEN_PARENTHESIS)
        {
            return;
        }

        Parser.IncrementDataIndex();
        PackFunction Constructor = new(new(recordClass.SelfIdentifier.SourceCodeName), SourceFile);
        Constructor.Parameters.SetFrom(ParseFunctionParameters(KGVL.CLOSE_PARENTHESIS, recordClass.SelfIdentifier));

        recordClass.AddFunction(Constructor);
        Parser.IncrementDataIndex();
    }

    private void ParseStruct(object parentObject, PackMemberModifiers modifiers)
    {
        ParseExtendableType(parentObject, modifiers, KGVL.NAME_STRUCT,
            (identifier) => new PackStruct(identifier, SourceFile),
            (type, holder) => holder.AddStruct(type));
    }

    /* This insane function disparately needs to be broken down. */
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

        Identifier Name = ParseTypeMemberIdentifier(typeName);
        SourceFileOrigin Origin = new(Parser.Line);
        T CreatedType = typeConstructor.Invoke(Name);
        CreatedType.Modifiers = modifiers;
        addFunction.Invoke(CreatedType, TypeHolder);
        CreatedType.SourceFileOrigin = Origin;

        Parser.SkipUntilNonWhitespace(null);
        IGenericParameterHolder? GenericsHolder = CreatedType as IGenericParameterHolder;
        if (GenericsHolder != null)
        {
            ParseGenericParameters(Name, GenericsHolder.GenericParameters);
        }

        bool IsRecord = (modifiers & PackMemberModifiers.Record) != PackMemberModifiers.None;
        Parser.SkipUntilNonWhitespace(GetClassWrongStartExceptionMessage(CreatedType, IsRecord, typeName));
        if ((CreatedType is PackClass ClassType) && IsRecord)
        {
            ParseRecordPrimaryConstructor(ClassType);
        }

        ParseMemberExtensions((IPackMemberExtender)CreatedType, CreatedType.SelfIdentifier);
        if (GenericsHolder != null)
        {
            ParseGenericConstraints(Name, GenericsHolder.GenericParameters, GetExtendableTypeLimitChars(IsRecord));
        }

        if (IsRecord && (Parser.GetCharAtDataIndex() == KGVL.SEMICOLON))
        {
            Parser.IncrementDataIndex();
            return;
        }
        if (Parser.GetCharAtDataIndex() != KGVL.OPEN_CURLY_BRACKET)
        {
            throw new SourceFileReadException(Parser, ErrorCreator.ExpectedMemberBodyStart
                .CreateOptions(Name.SourceCodeName));
        }

        Parser.IncrementDataIndex();

        while (Parser.SkipUntilNonWhitespace(ErrorCreator.EOFWhileParsingMembers
            .CreateOptions(typeName, CreatedType.SelfIdentifier.SourceCodeName))
            && (Parser.GetCharAtDataIndex() != KGVL.CLOSE_CURLY_BRACKET)
            && Parser.IsMoreDataAvailable)
        {
            ParseMember(CreatedType);
        }

        if (Parser.GetCharAtDataIndex() != KGVL.CLOSE_CURLY_BRACKET)
        {
            throw new SourceFileReadException(Parser, ErrorCreator.ExpectedCurlyTypeEnd
                .CreateOptions(typeName, CreatedType.SelfIdentifier.SourceCodeName));
        }
        Parser.IncrementDataIndex();
    }

    private char[] GetExtendableTypeLimitChars(bool isRecord)
    {
        List<char> Chars = new()
        {
            KGVL.OPEN_CURLY_BRACKET
        };

        if (isRecord)
        {
            Chars.Add(KGVL.SEMICOLON);
        }

        return Chars.ToArray();
    }

    private void ParseDelegate(object parentObject, PackMemberModifiers modifiers)
    {
        if (parentObject is not IPackTypeHolder DelegateHolder)
        {
            throw CreateInvalidHolderException(parentObject, KGVL.NAME_DELEGATE);
        }
        
        TypeTargetIdentifier? ReturnType = ParseReturnType(KGVL.NAME_DELEGATE);
        Identifier Name = ParseTypeMemberIdentifier(KGVL.NAME_DELEGATE);
        SourceFileOrigin Origin = new(Parser.Line);

        ErrorCreateOptions ParamError = ErrorCreator.ExpectedDelegateParamList.CreateOptions(Name.SourceCodeName);
        Parser.SkipUntilNonWhitespace(ParamError);
        if (Parser.GetCharAtDataIndex() != KGVL.OPEN_PARENTHESIS)
        {
            throw new SourceFileReadException(Parser, ParamError);
        }
        Parser.IncrementDataIndex();
        FunctionParameterCollection Parameters = ParseFunctionParameters(KGVL.CLOSE_PARENTHESIS, Name);
        if (Parser.GetCharAtDataIndex() != KGVL.CLOSE_PARENTHESIS)
        {
            throw new SourceFileReadException(Parser,
                ErrorCreator.ExpectedDelegateParamListEnd.CreateOptions(Name.SourceCodeName));
        }
        Parser.IncrementDataIndex();

        ErrorCreateOptions EndError = ErrorCreator.ExpectedDelegateDefinitionEnd.CreateOptions(Name.SourceCodeName);
        Parser.SkipUntilNonWhitespace(EndError);
        if (Parser.GetCharAtDataIndex() != KGVL.SEMICOLON)
        {
            throw new SourceFileReadException(Parser, EndError);
        }
        Parser.IncrementDataIndex();

        PackDelegate Delegate = new(Name, ReturnType, SourceFile)
        {
            Modifiers = modifiers,
            SourceFileOrigin = Origin,
        };
        DelegateHolder.AddDelegate(Delegate);
        Delegate.Parameters.SetFrom(Parameters);
    }

    private void ParseInterface(object parentObject, PackMemberModifiers modifiers)
    {
        ParseExtendableType(parentObject, modifiers, KGVL.NAME_INTERFACE,
            (identifier) => new PackInterface(identifier, SourceFile),
            (type, holder) => holder.AddInterface(type));
    }

    private void ParseEvent(object? parentObject, PackMemberModifiers modifiers)
    {

    }

    private void ParseEnumeration(object parentObject, PackMemberModifiers modifiers)
    {
        if (parentObject is not IPackTypeHolder EnumHolder)
        {
            throw CreateInvalidHolderException(parentObject, KGVL.NAME_ENUM);
        }

        Identifier Name = ParseTypeMemberIdentifier(KGVL.NAME_ENUM);
        SourceFileOrigin Origin = new(Parser.Line);
        PackEnumeration Enum = new(Name, SourceFile)
        {
            Modifiers = modifiers,
            SourceFileOrigin = Origin,
        };

        ErrorCreateOptions BodyStartError = ErrorCreator.ExpectedEnumBodyStart.CreateOptions(Name.SourceCodeName);

        Parser.SkipUntilNonWhitespace(BodyStartError);
        if (Parser.GetCharAtDataIndex() !=KGVL.OPEN_CURLY_BRACKET)
        {
            throw new SourceFileReadException(Parser, BodyStartError);
        }
        Parser.IncrementDataIndex();
        ParseEnumValues(Enum);
        if (Parser.GetCharAtDataIndex() != KGVL.CLOSE_CURLY_BRACKET)
        {
            throw new SourceFileReadException(Parser,
                ErrorCreator.ExpectedEnumBodyEnd.CreateOptions(Name.SourceCodeName));
        }
        Parser.IncrementDataIndex();
    }

    private void ParseEnumValues(PackEnumeration enumeration)
    {
        const int ENUM_STARTING_VALUE = 0;
        string EnumName = enumeration.SelfIdentifier.SourceCodeName;

        ErrorCreateOptions ConstantOrEndError = ErrorCreator.ExpectedEnumConstantOrEnd.CreateOptions(EnumName);

        Parser.SkipUntilNonWhitespace(ConstantOrEndError);
        int CurrentEnumValue = ENUM_STARTING_VALUE;
        bool IsValueExpected = Parser.GetCharAtDataIndex() != KGVL.CLOSE_CURLY_BRACKET;
        while (IsValueExpected)
        {
            ErrorCreateOptions ExpectedConstantError = ErrorCreator.ExpectedEnumConstant.CreateOptions(EnumName);
            string ConstantName = Parser.ReadIdentifier(ExpectedConstantError);
            ErrorCreateOptions AssignmentOrNextOrEndError = ErrorCreator.ExpectedEnumAssignmentOrNextOrEnd
                .CreateOptions(ConstantName, EnumName);

            Parser.SkipUntilNonWhitespace(AssignmentOrNextOrEndError);

            if (Parser.GetCharAtDataIndex() == KGVL.ASSIGNMENT_OPERATOR)
            {
                ErrorCreateOptions ExpectedValueError = ErrorCreator.ExpectedEnumConstantValue.CreateOptions(ConstantName);
                string ExceptionMsgExpectedConstant = $"Expected value for enum constant \"{ConstantName}\" " +
                    $"for enum {enumeration.SelfIdentifier.SourceCodeName}";
                Parser.IncrementDataIndex();
                Parser.SkipUntilNonWhitespace(ExpectedValueError);
                GenericNumber Number = Parser.ReadInteger(ExpectedValueError);
                CurrentEnumValue = CastNumberToEnumConstantValue(enumeration, ConstantName, Number);
            }
            enumeration.SetConstant(ConstantName, CurrentEnumValue);

            Parser.SkipUntilNonWhitespace(ConstantOrEndError);
            IsValueExpected = Parser.GetCharAtDataIndex() == KGVL.COMMA;
            if (IsValueExpected)
            {
                Parser.IncrementDataIndex();
                Parser.SkipUntilNonWhitespace(ExpectedConstantError);
            }
            CurrentEnumValue++;
        }
    }

    private int CastNumberToEnumConstantValue(PackEnumeration enumeration, string constantName, GenericNumber number)
    {
        if (number.IsLong || number.IsUnsigned)
        {
            throw new SourceFileReadException(Parser, ErrorCreator.EnumConstantOutOfRange
                .CreateOptions(constantName, enumeration.SelfIdentifier.SourceCodeName, number.Number));
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
            return new(Parser, ErrorCreator.CannotHoldMemberInNamespace
                .CreateOptions(NameSpace.SelfIdentifier.SourceCodeName, targetTypeName));
        }
        if (holder is PackMember Member)
        {
            return new(Parser, ErrorCreator.CannotHoldMemberInMember.CreateOptions(
                Utils.MemberHolderToString(holder), Member.SelfIdentifier.SourceCodeName, targetTypeName));
        }
        return new(Parser, ErrorCreator.CannotHoldMemberInUnknown
            .CreateOptions(targetTypeName));
    }

    private void ParseReturnTypedMember(object memberHolder, PackMemberModifiers modifiers, string returnType)
    {
        //const string EXCEPTION_MSG_IDENTIFIER = "Expected member identifier";
        //Parser.SkipUntilNonWhitespace(EXCEPTION_MSG_IDENTIFIER);
        //string Identifier = Parser.ReadIdentifier(EXCEPTION_MSG_IDENTIFIER);

        //Parser.SkipUntilNonWhitespace("Expected member value");
        //char NextCharacter = Parser.GetCharAtDataIndex(Parser.DataIndex + 1);

        //if (NextCharacter == KGVL.OPEN_PARENTHESIS)
        //{
            
        //}
        //else if (NextCharacter == KGVL.ASSIGNMENT_OPERATOR)
        //{
        //    bool IsQuickGetBody = $"{NextCharacter}{Parser.GetCharAtDataIndex(Parser.DataIndex + 2)}" == KGVL.QUICK_METHOD_BODY;
        //    if (IsQuickGetBody || (NextCharacter == KGVL.OPEN_CURLY_BRACKET))
        //    {
                
        //    }
        //    else
        //    {
                
        //    }
        //}
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

    internal void ParseMemberExtensions(IPackMemberExtender extender, Identifier extenderName)
    {
        char[] ExpectedChars = new char[] { KGVL.COLON, KGVL.OPEN_CURLY_BRACKET, KGVL.KEYWORD_WHEN[0], KGVL.SEMICOLON };

        ErrorCreateOptions ExtendOrBodyError = ErrorCreator.ExpectedMemberExtensionOrBody
            .CreateOptions(extenderName.SourceCodeName);
        ErrorCreateOptions ExtendError = ErrorCreator.ExpectedMemberExtension
            .CreateOptions(extenderName.SourceCodeName);

        Parser.SkipWhitespaceUntil(ExtendOrBodyError, ExpectedChars);

        bool IsAnExtensionExpected = Parser.GetCharAtDataIndex() == KGVL.COLON;

        while (IsAnExtensionExpected)
        {
            Parser.IncrementDataIndex();
            Parser.SkipUntilNonWhitespace(ExtendError);
            Identifier ExtendedMember = new(Parser.ReadIdentifier(ExtendError));
            extender.AddExtendedMember(ExtendedMember);

            Parser.SkipWhitespaceUntil(ExtendOrBodyError, ExpectedChars);
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

    private FunctionParameterCollection ParseFunctionParameters(char paramListEndChar, Identifier funcHolderName)
    {
        ErrorCreateOptions ExpectTypeOrModifierError = ErrorCreator.ExpectedParameterTypeOrModifier
                .CreateOptions(funcHolderName.SourceCodeName);
        ErrorCreateOptions ExpectTypeOrEndError = ErrorCreator.ExpectedParametersOrEnd
                .CreateOptions(funcHolderName.SourceCodeName);
        ErrorCreateOptions ExpectIdentifierError = ErrorCreator.ExpectedParameterIdentifier
                .CreateOptions(funcHolderName.SourceCodeName);
        ErrorCreateOptions ExpectTypeError = ErrorCreator.ExpectedParameterType
                .CreateOptions(funcHolderName.SourceCodeName);

        FunctionParameterCollection Params = new();
        Parser.SkipUntilNonWhitespace(ExpectTypeOrEndError);
        bool IsParameterExpected = Parser.GetCharAtDataIndex() != paramListEndChar;

        while (IsParameterExpected)
        {
            string FirstWord = Parser.ReadIdentifier(null);
            FunctionParameterModifier Modifier = StringToParamModifier(FirstWord);

            if (Modifier != FunctionParameterModifier.None)
            {
                Parser.SkipUntilNonWhitespace(ExpectTypeError);
            }
            else
            {
                Parser.ReverseUntilOneAfterIdentifier();
            }

            TypeTargetIdentifier ParamType = Utils.ParseTypeTargetIdentifier(Parser, ExpectTypeError);
            Parser.SkipUntilNonWhitespace(ExpectIdentifierError);
            string ParamName = Parser.ReadIdentifier(ExpectIdentifierError);
            Parser.SkipUntilNonWhitespace(ExpectTypeOrEndError);
            Params.AddItem(new(ParamType, new(ParamName), Modifier));

            char NextChar = Parser.GetCharAtDataIndex();
            IsParameterExpected = NextChar == KGVL.COMMA;
            if (IsParameterExpected)
            {
                Parser.IncrementDataIndex();
                Parser.SkipUntilNonWhitespace(ExpectTypeOrModifierError);
            }
        }

        return Params;
    }

    private void ParseGenericConstraints(
        Identifier memberName,
        GenericTypeParameterCollection parameters, 
        params char[] endCharacters)
    {
        Parser.SkipUntilNonWhitespace(null);

        while (Parser.HasStringAtIndex(Parser.DataIndex, KGVL.KEYWORD_WHEN))
        {
            Parser.IncrementDataIndexNTimes(KGVL.KEYWORD_WHEN.Length);
            Parser.SkipUntilNonWhitespace(ErrorCreator.ExpectedGenericTypeConstraint
                .CreateOptions(memberName.SourceCodeName));
            ParseSingleGenericParameterConstraints(memberName, parameters, endCharacters);

            Parser.SkipUntilNonWhitespace(null);
        }
    }

    private void ParseSingleGenericParameterConstraints(
        Identifier memberName,
        GenericTypeParameterCollection parameters,
        params char[] endCharacters)
    {
        string TypeName = Parser.ReadIdentifier(ErrorCreator.ExpectedGenericTypeConstraint
            .CreateOptions(memberName.SourceCodeName));
        GenericTypeParameter? Parameter = parameters.GetBySourceCodeName(TypeName);
        if (Parameter == null)
        {
            throw new SourceFileReadException(Parser, ErrorCreator.GenericParameterNotFound
                .CreateOptions(TypeName, memberName.SourceCodeName));
        }

        ErrorCreateOptions ExpectStartError = ErrorCreator.GenericParameterConstraintStartNotFound
            .CreateOptions(TypeName, memberName.SourceCodeName);
        Parser.SkipUntilNonWhitespace(ExpectStartError);
        if (Parser.GetCharAtDataIndex() != KGVL.COLON)
        {
            throw new SourceFileReadException(Parser, ExpectStartError);
        }
        Parser.IncrementDataIndex();

        ErrorCreateOptions ExpectedConstraintError = ErrorCreator.ExpectedGenericConstraintIdentifier
            .CreateOptions(Parameter.SelfIdentifier.SourceCodeName);
        List<GenericConstraint> Constraints = new();
        bool IsConstraintExpected = true;
        while (IsConstraintExpected)
        {
            Parser.SkipUntilNonWhitespace(ExpectedConstraintError);
            TypeTargetIdentifier ConstraintType = Utils.ParseTypeTargetIdentifier(Parser, ExpectedConstraintError);
            SpecialGenericConstraint? SpecialConstraint = TryGetSpecialConstraint(
                ConstraintType.MainTarget!.SourceCodeName);
            GenericConstraint Constraint = SpecialConstraint != null
                ? new(SpecialConstraint.Value) : new(ConstraintType);
            Constraints.Add(Constraint);

            Parser.SkipUntilNonWhitespace(null);
            IsConstraintExpected = Parser.GetCharAtDataIndex() == KGVL.COMMA;
            if (IsConstraintExpected)
            {
                Parser.IncrementDataIndex();
            }
        }

        Parameter.Constraints = Constraints.ToArray();
    }

    private SpecialGenericConstraint? TryGetSpecialConstraint(string constraintName)
    {
        return constraintName switch
        {
            KGVL.KEYWORD_CLASS => SpecialGenericConstraint.Class,
            KGVL.KEYWORD_STRUCT => SpecialGenericConstraint.Struct,
            KGVL.KEYWORD_NOTNULL => SpecialGenericConstraint.NotNull,
            _ => null
        };
    }

    private void ParseGenericParameters(Identifier memberName, GenericTypeParameterCollection parameters)
    {
        Parser.SkipUntilNonWhitespace(null);
        if (Parser.GetCharAtDataIndex() != KGVL.GENERIC_TYPE_START)
        {
            return;
        }
        Parser.IncrementDataIndex();

        bool IsParameterExpected = true;
        while (IsParameterExpected)
        {
            Parser.SkipUntilNonWhitespace(ErrorCreator.ExpectedGenericParameterCommaOrEnd
                    .CreateOptions(memberName.SourceCodeName));
            string Identifier = Parser.ReadIdentifier(ErrorCreator.ExpectedGenericParameterIdentifier
                .CreateOptions(memberName.SourceCodeName));
            parameters.AddItem(new(new(Identifier), null));
            IsParameterExpected = Parser.GetCharAtDataIndex() == KGVL.COMMA;
            if (IsParameterExpected)
            {
                Parser.IncrementDataIndex();
            }
        }

        ErrorCreateOptions ErrorExpectedParameterEnd = ErrorCreator.ExpectedGenericParameterEnd
                .CreateOptions(memberName.SourceCodeName);
        Parser.SkipUntilNonWhitespace(ErrorExpectedParameterEnd);
        if (Parser.GetCharAtDataIndex() != KGVL.GENERIC_TYPE_END)
        {
            throw new SourceFileReadException(Parser, ErrorExpectedParameterEnd);
        }
        Parser.IncrementDataIndex();    
    }
}