using KeigValCompiler.Error;
using KeigValCompiler.Semantician;
using KeigValCompiler.Semantician.Member;
using KeigValCompiler.Semantician.Member.Code;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace KeigValCompiler.Source.Parser;

internal class MemberParser : AbstractParserBase
{
    // Static fields.
    /* A member ends at a semicolon; the closing bracket of the type holding it ends the whole list of
     * members, so it is left alone for the loop reading them to stop on. */
    private static readonly char[] _memberResumeChars = new char[] { KGVL.SEMICOLON };
    private static readonly char[] _memberTerminatorChars = new char[] { KGVL.CLOSE_CURLY_BRACKET };

    /* Enum constants are separated by commas rather than ended by semicolons. */
    private static readonly char[] _enumResumeChars = new char[] { KGVL.COMMA };
    private static readonly char[] _enumTerminatorChars = new char[] { KGVL.CLOSE_CURLY_BRACKET };


    // Private fields
    private readonly StatementParser _statementParser;


    // Constructors.
    public MemberParser(PackParsingContext context) : base(context)
    {
        _statementParser = new(context);
    }


    // Methods.
    internal void ParseMember(object memberHolder, string memberHolderTypeName, string memberHolderName)
    {
        PackMemberModifiers Modifiers = ParseMemberModifiers(memberHolderTypeName, memberHolderName);
        ErrorCreateOptions ExpectedMemberError = ErrorCreator.ExpectedMember.CreateOptions(memberHolderTypeName);
        Parser.SkipUntilNonWhitespace(ExpectedMemberError);
        int StartParserIndex = Parser.DataIndex;
        TypeTargetIdentifier TypeTarget = Parser.ReadTypeTargetIdentifier(ExpectedMemberError);

        string FirstSegment = TypeTarget.MainTarget.SourceCodeName;

        bool IsRecord = (Modifiers & PackMemberModifiers.Record) != PackMemberModifiers.None;
        if (FirstSegment == KGVL.KEYWORD_CLASS)
        {
            ParseClass(memberHolder, Modifiers);
        }
        else if (IsRecord)
        {
            Parser.DataIndex = StartParserIndex;
            ParseClass(memberHolder, Modifiers);
        }
        else if (FirstSegment == KGVL.KEYWORD_STRUCT)
        {
            ParseStruct(memberHolder, Modifiers);
        }
        else if (FirstSegment == KGVL.KEYWORD_DELEGATGE)
        {
            ParseDelegate(memberHolder, memberHolderTypeName, Modifiers);
        }
        else if (FirstSegment == KGVL.KEYWORD_INTERFACE)
        {
            ParseInterface(memberHolder, Modifiers);
        }
        else if (FirstSegment == KGVL.KEYWORD_EVENT)
        {
            ParseEvent(memberHolder, memberHolderName, Modifiers);
        }
        else if (FirstSegment == KGVL.KEYWORD_ENUM)
        {
            ParseEnumeration(memberHolder, memberHolderTypeName, Modifiers);
        }
        else
        {
            Parser.DataIndex = StartParserIndex;
            ParseReturnTypedMember(memberHolder, memberHolderTypeName, memberHolderName, Modifiers);
        }
    }


    // Private methods.
    private PackMemberModifiers CombineModifier(PackMemberModifiers appliedModifiers, PackMemberModifiers newModifier)
    {
        if ((appliedModifiers & newModifier) != PackMemberModifiers.None)
        {
            AddError(ErrorCreator.DuplicateModifiers.CreateOptions(newModifier));
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
            KGVL.KEYWORD_BUILTIN => ReportReservedBuiltInModifier(),
            KGVL.KEYWORD_INLINE => PackMemberModifiers.Inline,
            KGVL.KEYWORD_SEALED => PackMemberModifiers.Sealed,
            KGVL.KEYWORD_REQUIRED => PackMemberModifiers.Required,
            _ => PackMemberModifiers.None
        };
    }

    /* Writing the modifier down is what is not allowed, not the modifier itself, so the parser knows
     * exactly where it is and carries on with the modifier that the keyword names. */
    private PackMemberModifiers ReportReservedBuiltInModifier()
    {
        AddError(ErrorCreator.ReservedKeywordBuiltIn.CreateOptions());
        return PackMemberModifiers.BuiltIn;
    }

    private PackMemberModifiers ParseMemberModifiers(string memberTypeName, string memberName)
    {
        PackMemberModifiers Modifiers = PackMemberModifiers.None;

        while (true)
        {
            int StartParserIndex = Parser.DataIndex;
            Parser.SkipUntilNonWhitespace(ErrorCreator.ExpectedMemberModifierOrKeyword.CreateOptions(
                memberTypeName, memberTypeName));
            string Word = Parser.ReadIdentifier(null);
            PackMemberModifiers NewModifier = StringToModifier(Word);

            if (NewModifier != PackMemberModifiers.None)
            {
                Modifiers = CombineModifier(Modifiers, NewModifier);
                continue;
            }

            Parser.DataIndex = StartParserIndex;
            return Modifiers;
        }
    }

    private Identifier ParseMemberSelfIdentifier(string typeName)
    {
        ErrorCreateOptions ErrorOptions = ErrorCreator.ExpectedTypeMemberIdentifier.CreateOptions(typeName);
        Parser.SkipUntilNonWhitespace(ErrorOptions);
        return new(Parser.ReadIdentifier(ErrorOptions));
    }

    private TypeTargetIdentifier? ParseReturnType(string holderTypeName)
    {
        ErrorCreateOptions ErrorOptions = ErrorCreator.ExpectedReturnTypeIdentifier.CreateOptions(holderTypeName);
        Parser.SkipUntilNonWhitespace(ErrorOptions);
        TypeTargetIdentifier Name = Parser.ReadTypeTargetIdentifier(ErrorOptions);

        if (Name.MainTarget.SourceCodeName == KGVL.KEYWORD_VOID)
        {
            if (Name.TypeArguments.Length > 0)
            {
                AddError(ErrorCreator.VoidCantHaveGenericArguments.CreateOptions());
            }
            return null;
        }

        return Name;
    }

    private ErrorCreateOptions GetExtendableTypeWrongStartExceptionMessage(PackMember member,
        bool isRecord,
        bool isGenericsHolder,
        string typeName)
    {
        string Name = member.SelfIdentifier.SourceCodeName;
        if (isRecord)
        {
            return ErrorCreator.ExpectedRecordPrimaryConstructorOrBodyOrConstraints.CreateOptions(Name);
        }
        if (isGenericsHolder)
        {
            return ErrorCreator.ExpectedBodyOrExtensionOrConstraints.CreateOptions(typeName, Name);
        }
        return ErrorCreator.ExpectedBodyOrExtension.CreateOptions(typeName, Name);
    }

    private void ParseRecordPrimaryConstructor(PackClass recordClass)
    {
        string RecordName = recordClass.SelfIdentifier.SourceCodeName;
        bool IsGenericsHolder = recordClass.GenericParameters.Count > 0;

        Parser.SkipUntilNonWhitespace(GetRecordPrimaryConstructorErrorMessage(RecordName, IsGenericsHolder));

        if (Parser.GetCharAtDataIndex() != KGVL.OPEN_PARENTHESIS)
        {
            return;
        }

        Parser.IncrementDataIndex();
        Parser.SkipUntilNonWhitespace(ErrorCreator.ExpectedRecordPrimaryConstructorParameters
            .CreateOptions(RecordName));

        PackFunction Constructor = new(new(recordClass.SelfIdentifier.SourceCodeName), SourceFile);
        Constructor.Parameters.SetFrom(ParseFunctionParameters(recordClass.SelfIdentifier.SourceCodeName,
            KGVL.NAME_RECORD_CLASS, KGVL.CLOSE_PARENTHESIS));

        if (Parser.GetCharAtDataIndex() != KGVL.CLOSE_PARENTHESIS)
        {
            /* Technically impossible case due to ParseFunctionParameters already throwing an exception, but
            * I'll keep it if the behavior changes, just in case. */
            throw new SourceFileReadException(Parser, ErrorCreator.ExpectedRecordPrimaryConstructorEnd
                .CreateOptions(RecordName)); 
        }

        recordClass.AddFunction(Constructor);
        Parser.IncrementDataIndex();
    }
    
    private ErrorCreateOptions GetRecordPrimaryConstructorErrorMessage(string recordName, bool isGenericsHolder)
    {
        if (isGenericsHolder)
        {
            return ErrorCreator.ExpectedRecordPrimaryConstructorOrBodyOrConstraints.CreateOptions(recordName);
        }
        return ErrorCreator.ExpectedRecordPrimaryConstructorOrBody.CreateOptions(recordName);
    }

    private ErrorCreateOptions GetExtendableTypeEOFErrorMessage(string memberName,
        string memberTypeName,
        bool isGenericsHolder)
    {
        if (isGenericsHolder)
        {
            return ErrorCreator.ExpectedBodyOrExtensionOrConstraints.CreateOptions(memberTypeName, memberName);
        }
        return ErrorCreator.ExpectedBodyOrExtension.CreateOptions(memberTypeName, memberName);
    }

    private ErrorCreateOptions GetExpectedExtendableBodyStart(bool isRecord, string memberTypeName, string memberName)
    {
        if (isRecord)
        {
            return ErrorCreator.ExpectedRecordBodyStartOrEnd.CreateOptions(memberName);
        }
        return ErrorCreator.ExpectedMemberBodyStart.CreateOptions(memberTypeName, memberName);
    }

    private void ParseClass(object parentObject, PackMemberModifiers modifiers)
    {
        bool IsRecord = (modifiers & PackMemberModifiers.Record) != PackMemberModifiers.None;
        ParseExtendableType(parentObject,
            IsRecord ? KGVL.NAME_RECORD_CLASS : KGVL.NAME_CLASS,
            modifiers,
            KGVL.NAME_CLASS,
            (identifier) => new PackClass(identifier, SourceFile),
            (type, holder) => holder.AddClass(type));
    }

    private void ParseStruct(object parentObject, PackMemberModifiers modifiers)
    {
        ParseExtendableType(parentObject, 
            KGVL.NAME_STRUCT,
            modifiers, 
            KGVL.NAME_STRUCT,
            (identifier) => new PackStruct(identifier, SourceFile),
            (type, holder) => holder.AddStruct(type));
    }

    private void ParseInterface(object parentObject, PackMemberModifiers modifiers)
    {
        ParseExtendableType(parentObject,
            KGVL.NAME_INTERFACE,
            modifiers,
            KGVL.NAME_INTERFACE,
            (identifier) => new PackInterface(identifier, SourceFile),
            (type, holder) => holder.AddInterface(type));
    }

    /* This insane function disparately needs to be broken down. */
    private void ParseExtendableType<T>(object parentObject,
        string parentObjectTypeName,
        PackMemberModifiers modifiers,
        string typeName,
        Func<Identifier, T> typeConstructor,
        Action<T, IPackTypeHolder> addFunction) where T : PackMember
    {
        if (parentObject is not IPackTypeHolder TypeHolder)
        {
            throw CreateInvalidHolderException(parentObject, parentObjectTypeName, typeName);
        }

        Identifier Name = ParseMemberSelfIdentifier(typeName);
        SourceFileOrigin Origin = new(Parser.Line);
        T CreatedType = typeConstructor.Invoke(Name);
        CreatedType.Modifiers = modifiers;
        CreatedType.SourceFileOrigin = Origin;
        addFunction.Invoke(CreatedType, TypeHolder);

        IGenericParameterHolder? GenericsHolder = CreatedType as IGenericParameterHolder;
        if (GenericsHolder != null)
        {
            Parser.SkipUntilNonWhitespace(ErrorCreator.ExpectedGenericParamsOrExtension
                .CreateOptions(typeName, Name.SourceCodeName));
            ParseGenericParameters(Name.SourceCodeName, typeName, GenericsHolder.GenericParameters);
        }

        bool IsRecord = (modifiers & PackMemberModifiers.Record) != PackMemberModifiers.None;
        bool HasGenerics = (GenericsHolder != null) && (GenericsHolder.GenericParameters.Count > 0);
        Parser.SkipUntilNonWhitespace(GetExtendableTypeWrongStartExceptionMessage(
            CreatedType, IsRecord, HasGenerics, typeName));
        if ((CreatedType is PackClass ClassType) && IsRecord)
        {
            ParseRecordPrimaryConstructor(ClassType);
        }

        Parser.SkipUntilNonWhitespace(GetExtendableTypeEOFErrorMessage(Name.SourceCodeName, typeName, HasGenerics));
        ParseMemberExtensions((IPackMemberExtender)CreatedType, typeName, CreatedType.SelfIdentifier.SourceCodeName);
        if (GenericsHolder != null)
        {
            ParseGenericConstraints(Name.SourceCodeName, typeName, GenericsHolder.GenericParameters);
        }

        Parser.SkipUntilNonWhitespace(GetExpectedExtendableBodyStart(IsRecord, typeName, Name.SourceCodeName));
        if (IsRecord && (Parser.GetCharAtDataIndex() == KGVL.SEMICOLON))
        {
            Parser.IncrementDataIndex();
            return;
        }
        if (Parser.GetCharAtDataIndex() != KGVL.OPEN_CURLY_BRACKET)
        {
            throw new SourceFileReadException(Parser,
                GetExpectedExtendableBodyStart(IsRecord, typeName, Name.SourceCodeName));
        }

        Parser.IncrementDataIndex();

        while (Parser.SkipUntilNonWhitespace(ErrorCreator.EOFWhileParsingMembers
            .CreateOptions(typeName, CreatedType.SelfIdentifier.SourceCodeName))
            && (Parser.GetCharAtDataIndex() != KGVL.CLOSE_CURLY_BRACKET)
            && Parser.IsMoreDataAvailable)
        {
            try
            {
                ParseMember(CreatedType, typeName, Name.SourceCodeName);
            }
            catch (SourceFileReadException e)
            {
                if (!RecoverFromError(e, _memberResumeChars, _memberTerminatorChars))
                {
                    break;
                }
            }
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

    private void ParseDelegate(object parentObject, string parentObjectTypeName, PackMemberModifiers modifiers)
    {
        if (parentObject is not IPackTypeHolder DelegateHolder)
        {
            throw CreateInvalidHolderException(parentObject, parentObjectTypeName, KGVL.NAME_DELEGATE);
        }
        
        TypeTargetIdentifier? ReturnType = ParseReturnType(KGVL.NAME_DELEGATE);
        Identifier Name = ParseMemberSelfIdentifier(KGVL.NAME_DELEGATE);
        SourceFileOrigin Origin = new(Parser.Line);
        PackDelegate Delegate = new(Name, ReturnType, SourceFile)
        {
            Modifiers = modifiers,
            SourceFileOrigin = Origin,
        };

        Parser.SkipUntilNonWhitespace(null);
        ParseGenericParameters(Name.SourceCodeName, KGVL.NAME_DELEGATE, Delegate.GenericParameters);

        ErrorCreateOptions ParamError = ErrorCreator.ExpectedDelegateParamList.CreateOptions(Name.SourceCodeName);
        Parser.SkipUntilNonWhitespace(ParamError);
        if (Parser.GetCharAtDataIndex() != KGVL.OPEN_PARENTHESIS)
        {
            throw new SourceFileReadException(Parser, ParamError);
        }
        Parser.IncrementDataIndex();
        FunctionParameterCollection FunctionParameters = ParseFunctionParameters(
            Name.SourceCodeName, KGVL.NAME_DELEGATE, KGVL.CLOSE_PARENTHESIS);
        if (Parser.GetCharAtDataIndex() != KGVL.CLOSE_PARENTHESIS)
        {
            throw new SourceFileReadException(Parser,
                ErrorCreator.ExpectedDelegateParamListEnd.CreateOptions(Name.SourceCodeName));
        }
        Parser.IncrementDataIndex();

        Parser.SkipUntilNonWhitespace(null);
        ParseGenericConstraints(Name.SourceCodeName, KGVL.NAME_DELEGATE, Delegate.GenericParameters);

        ErrorCreateOptions EndError = ErrorCreator.ExpectedDelegateDefinitionEnd.CreateOptions(Name.SourceCodeName);
        Parser.SkipUntilNonWhitespace(EndError);
        if (Parser.GetCharAtDataIndex() != KGVL.SEMICOLON)
        {
            throw new SourceFileReadException(Parser, EndError);
        }
        Parser.IncrementDataIndex();

        DelegateHolder.AddDelegate(Delegate);
        Delegate.Parameters.SetFrom(FunctionParameters);
    }

    private void ParseEvent(object parentObject, string parentObjectTypeName, PackMemberModifiers modifiers)
    {
        if (parentObject is not IPackEventHolder EventHolder)
        {
            throw CreateInvalidHolderException(parentObject, parentObjectTypeName, KGVL.NAME_EVENT);
        }

        ErrorCreateOptions ExpectedDelegateError = ErrorCreator.ExpectedEventDelegateType.CreateOptions();
        Parser.SkipUntilNonWhitespace(ExpectedDelegateError);
        TypeTargetIdentifier EventDelegateType = Parser.ReadTypeTargetIdentifier(ExpectedDelegateError);
        Identifier Name = ParseMemberSelfIdentifier(KGVL.NAME_EVENT);

        ErrorCreateOptions ExpectedEndError = ErrorCreator.ExpectedEventEnd.CreateOptions(Name.SourceCodeName);
        Parser.SkipUntilNonWhitespace(ExpectedEndError);
        if (Parser.GetCharAtDataIndex() != KGVL.SEMICOLON)
        {
            throw new SourceFileReadException(Parser, ExpectedEndError);
        }
        Parser.IncrementDataIndex();

        EventHolder.AddEvent(new PackEvent(Name, EventDelegateType, SourceFile)
        {
            Modifiers = modifiers,
            SourceFileOrigin = new(Parser.Line)
        });
    }

    private void ParseEnumeration(object parentObject, string holderTypeName, PackMemberModifiers modifiers)
    {
        if (parentObject is not IPackTypeHolder EnumHolder)
        {
            throw CreateInvalidHolderException(parentObject, holderTypeName, KGVL.NAME_ENUM);
        }

        Identifier Name = ParseMemberSelfIdentifier(KGVL.NAME_ENUM);
        SourceFileOrigin Origin = new(Parser.Line);
        PackEnumeration Enum = new(Name, SourceFile)
        {
            Modifiers = modifiers,
            SourceFileOrigin = Origin,
        };

        ErrorCreateOptions BodyStartError = ErrorCreator.ExpectedEnumBodyStart.CreateOptions(Name.SourceCodeName);

        Parser.SkipUntilNonWhitespace(BodyStartError);
        if (Parser.GetCharAtDataIndex() != KGVL.OPEN_CURLY_BRACKET)
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
        ErrorCreateOptions ExpectedConstantError = ErrorCreator.ExpectedEnumConstant.CreateOptions(EnumName);

        Parser.SkipUntilNonWhitespace(ConstantOrEndError);
        int CurrentEnumValue = ENUM_STARTING_VALUE;
        bool IsValueExpected = Parser.GetCharAtDataIndex() != KGVL.CLOSE_CURLY_BRACKET;
        while (IsValueExpected)
        {
            try
            {
                CurrentEnumValue = ParseEnumValue(enumeration, EnumName, CurrentEnumValue);
            }
            catch (SourceFileReadException e)
            {
                if (!RecoverFromError(e, _enumResumeChars, _enumTerminatorChars))
                {
                    return;
                }

                /* Recovery either ate the comma before the next constant or stopped on the enum's
                 * closing bracket, so whether another constant follows is decided by what is there now. */
                Parser.SkipUntilNonWhitespace(null);
                IsValueExpected = Parser.IsIdentifierFirstChar(Parser.GetCharAtDataIndex());
                CurrentEnumValue++;
                continue;
            }

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

    private int ParseEnumValue(PackEnumeration enumeration, string enumName, int currentEnumValue)
    {
        if (!Parser.IsIdentifierFirstChar(Parser.GetCharAtDataIndex()) && (enumeration.ConstantCount > 0))
        {
            throw new SourceFileReadException(Parser, ErrorCreator.UnexpctedEnumNonConstantValue
                .CreateOptions(enumName));
        }

        ErrorCreateOptions ExpectedConstantError = ErrorCreator.ExpectedEnumConstant.CreateOptions(enumName);
        string ConstantName = Parser.ReadIdentifier(ExpectedConstantError);
        ErrorCreateOptions AssignmentOrNextOrEndError = ErrorCreator.ExpectedEnumAssignmentOrNextOrEnd
            .CreateOptions(ConstantName, enumName);

        Parser.SkipUntilNonWhitespace(AssignmentOrNextOrEndError);

        int ConstantValue = currentEnumValue;
        if (Parser.GetCharAtDataIndex() == KGVL.ASSIGNMENT_OPERATOR)
        {
            ErrorCreateOptions ExpectedValueError = ErrorCreator.ExpectedEnumConstantValue.CreateOptions(ConstantName);
            Parser.IncrementDataIndex();
            Parser.SkipUntilNonWhitespace(ExpectedValueError);
            IntegerNumber Number = Parser.ReadInteger(ExpectedValueError)!;
            ConstantValue = CastNumberToEnumConstantValue(enumeration, ConstantName, Number);
        }
        enumeration.SetConstant(ConstantName, ConstantValue);

        return ConstantValue;
    }

    private int CastNumberToEnumConstantValue(PackEnumeration enumeration, string constantName, IntegerNumber number)
    {
        /* The number is right here and the constant it belongs to is known, so there is nothing to
         * recover from. A value still has to come back, and parsing the out of range one would throw. */
        const int ENUM_ERROR_VALUE = 0;

        if (number.IsLong || number.IsUnsigned)
        {
            AddError(ErrorCreator.EnumConstantOutOfRange
                .CreateOptions(constantName, enumeration.SelfIdentifier.SourceCodeName, number.Number));
            return ENUM_ERROR_VALUE;
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

    private SourceFileReadException CreateInvalidHolderException(object holder,
        string holderTypeName,
        string targetTypeName)
    {
        if (holder is PackNameSpace NameSpace)
        {
            return new(Parser, ErrorCreator.CannotHoldMemberInNamespace
                .CreateOptions(NameSpace.SelfIdentifier.SourceCodeName, targetTypeName));
        }
        if (holder is PackMember Member)
        {
            return new(Parser, ErrorCreator.CannotHoldMemberInMember.CreateOptions(
                holderTypeName, Member.SelfIdentifier.SourceCodeName, targetTypeName));
        }
        return new(Parser, ErrorCreator.CannotHoldMemberInUnknown
            .CreateOptions(targetTypeName));
    }

    /* Everything written as an optional return type followed by a name: constructors, operator
     * overloads, indexers, functions, properties and fields. Which one it is only becomes clear
     * after the name, so the cases are tried in order of how early they can be recognised. */
    private void ParseReturnTypedMember(object memberHolder,
        string memberHolderTypeName,
        string memberHolderName,
        PackMemberModifiers modifiers)
    {
        const string NAME_RETURN_TYPE_MEMBER = "field, property or function";

        if (TryParseConversionOperator(memberHolder, memberHolderTypeName, modifiers)
            || TryParseConstructor(memberHolder, memberHolderTypeName, memberHolderName, modifiers))
        {
            return;
        }

        TypeTargetIdentifier? ReturnType = ParseReturnType(NAME_RETURN_TYPE_MEMBER);

        if (TryParseOperatorOverload(memberHolder, memberHolderTypeName, ReturnType, modifiers)
            || TryParseIndexer(memberHolder, memberHolderTypeName, ReturnType, modifiers))
        {
            return;
        }

        Identifier MemberIdentifier = ParseMemberSelfIdentifier(NAME_RETURN_TYPE_MEMBER);
        ErrorCreateOptions ErrorNoMember = ErrorCreator.ExpectedFieldOrPropertyOrFunction
            .CreateOptions(MemberIdentifier.SourceCodeName);

        Parser.SkipUntilNonWhitespace(ErrorNoMember);
        char NextChar = Parser.GetCharAtDataIndex();

        if ((ReturnType == null) || (NextChar == KGVL.GENERIC_TYPE_START)
            || (NextChar == KGVL.OPEN_PARENTHESIS))
        {
            ParseFunction(memberHolder, memberHolderTypeName, modifiers, ReturnType, MemberIdentifier);
        }
        else if (Parser.HasStringAtIndex(Parser.DataIndex, KGVL.QUICK_METHOD_BODY)
            || (NextChar == KGVL.OPEN_CURLY_BRACKET))
        {
            ParseProperty(memberHolder, memberHolderTypeName, modifiers, ReturnType, MemberIdentifier);
        }
        else if ((NextChar == KGVL.ASSIGNMENT_OPERATOR) || (NextChar == KGVL.SEMICOLON))
        {
            ParseField(memberHolder, memberHolderTypeName, modifiers, ReturnType, MemberIdentifier);
        }
        else
        {
            throw new SourceFileReadException(Parser, ErrorNoMember);
        }
    }

    /* "implicit operator SomeType(...)" and its explicit counterpart, which name their result
     * where every other member names its return type. */
    private bool TryParseConversionOperator(object memberHolder,
        string memberHolderTypeName,
        PackMemberModifiers modifiers)
    {
        int StartIndex = Parser.DataIndex;
        string Keyword = ReadKeywordOrEmpty();

        if ((Keyword != KGVL.KEYWORD_IMPLICIT) && (Keyword != KGVL.KEYWORD_EXPLICIT))
        {
            Parser.DataIndex = StartIndex;
            return false;
        }

        OverloadableOperator ConversionKind = Keyword == KGVL.KEYWORD_IMPLICIT
            ? OverloadableOperator.ImplicitCast : OverloadableOperator.ExplicitCast;

        Parser.SkipUntilNonWhitespace(null);
        if (ReadKeywordOrEmpty() != KGVL.KEYWORD_OPERATOR)
        {
            throw new SourceFileReadException(Parser, ErrorCreator.ExpectedOperatorKeyword.CreateOptions());
        }

        Parser.SkipUntilNonWhitespace(null);
        TypeTargetIdentifier TargetType = Parser.ReadTypeTargetIdentifier(
            ErrorCreator.ExpectedConversionTargetType.CreateOptions());

        AddOperatorOverload(memberHolder, memberHolderTypeName, ConversionKind,
            BuildOperatorFunction(memberHolderTypeName, modifiers, TargetType,
                new Identifier(KGVL.NAME_OPERATOR_OVERLOAD)));
        return true;
    }

    /* A constructor is a name with no return type which matches the type holding it. */
    private bool TryParseConstructor(object memberHolder,
        string memberHolderTypeName,
        string memberHolderName,
        PackMemberModifiers modifiers)
    {
        int StartIndex = Parser.DataIndex;
        Parser.SkipUntilNonWhitespace(null);

        string Name = ReadKeywordOrEmpty();
        Parser.SkipUntilNonWhitespace(null);

        if ((Name.Length == 0) || (Name != memberHolderName)
            || (Parser.GetCharAtDataIndex() != KGVL.OPEN_PARENTHESIS))
        {
            Parser.DataIndex = StartIndex;
            return false;
        }

        if (memberHolder is not IPackFunctionHolder FunctionHolder)
        {
            throw CreateInvalidHolderException(memberHolder, memberHolderTypeName, KGVL.NAME_CONSTRUCTOR);
        }

        PackConstructor Constructor = new(new Identifier(Name), SourceFile)
        {
            Modifiers = modifiers,
            SourceFileOrigin = new(Parser.Line)
        };
        Parser.IncrementDataIndex();
        Constructor.Parameters.SetFrom(ParseFunctionParameters(Name, KGVL.NAME_CONSTRUCTOR,
            KGVL.CLOSE_PARENTHESIS));
        ConsumeParameterListEnd(KGVL.CLOSE_PARENTHESIS);

        ParseConstructorChain(Constructor);
        ParseFunctionBody(Constructor, modifiers);

        FunctionHolder.AddFunction(Constructor);
        return true;
    }

    /* The ": this(...)" or ": base(...)" which runs another constructor first. */
    private void ParseConstructorChain(PackConstructor constructor)
    {
        Parser.SkipUntilNonWhitespace(null);
        if (Parser.GetCharAtDataIndex() != KGVL.COLON)
        {
            return;
        }
        Parser.IncrementDataIndex();
        Parser.SkipUntilNonWhitespace(null);

        string Keyword = ReadKeywordOrEmpty();
        constructor.ChainKind = Keyword switch
        {
            KGVL.KEYWORD_THIS => ConstructorChainKind.This,
            KGVL.KEYWORD_BASE => ConstructorChainKind.Base,
            _ => ConstructorChainKind.None
        };

        if (constructor.ChainKind == ConstructorChainKind.None)
        {
            throw new SourceFileReadException(Parser, ErrorCreator.ExpectedConstructorChainTarget.CreateOptions());
        }

        Parser.SkipUntilNonWhitespace(null);
        if (Parser.GetCharAtDataIndex() != KGVL.OPEN_PARENTHESIS)
        {
            throw new SourceFileReadException(Parser, ErrorCreator.ExpectedOpenParenthesis.CreateOptions());
        }
        Parser.IncrementDataIndex();
        Parser.SkipUntilNonWhitespace(null);

        while (Parser.IsMoreDataAvailable && (Parser.GetCharAtDataIndex() != KGVL.CLOSE_PARENTHESIS))
        {
            constructor.ChainArguments.AddStatement(_statementParser.ParseExpressionValue());
            Parser.SkipUntilNonWhitespace(null);

            if (Parser.GetCharAtDataIndex() != KGVL.COMMA)
            {
                break;
            }
            Parser.IncrementDataIndex();
            Parser.SkipUntilNonWhitespace(null);
        }

        if (Parser.GetCharAtDataIndex() != KGVL.CLOSE_PARENTHESIS)
        {
            throw new SourceFileReadException(Parser, ErrorCreator.ExpectedCloseParenthesis.CreateOptions());
        }
        Parser.IncrementDataIndex();
    }

    /* "SomeType operator +(...)", where the return type has already been read. */
    private bool TryParseOperatorOverload(object memberHolder,
        string memberHolderTypeName,
        TypeTargetIdentifier? returnType,
        PackMemberModifiers modifiers)
    {
        int StartIndex = Parser.DataIndex;
        Parser.SkipUntilNonWhitespace(null);

        if (ReadKeywordOrEmpty() != KGVL.KEYWORD_OPERATOR)
        {
            Parser.DataIndex = StartIndex;
            return false;
        }

        Parser.SkipUntilNonWhitespace(null);
        OverloadableOperator? Overloaded = ReadOverloadableOperator();
        if (Overloaded == null)
        {
            throw new SourceFileReadException(Parser, ErrorCreator.UnoverloadableOperator.CreateOptions());
        }

        AddOperatorOverload(memberHolder, memberHolderTypeName, Overloaded.Value,
            BuildOperatorFunction(memberHolderTypeName, modifiers, returnType,
                new Identifier(KGVL.NAME_OPERATOR_OVERLOAD)));
        return true;
    }

    private PackFunction BuildOperatorFunction(string memberHolderTypeName,
        PackMemberModifiers modifiers,
        TypeTargetIdentifier? returnType,
        Identifier identifier)
    {
        PackFunction Function = new(identifier, SourceFile)
        {
            Modifiers = modifiers,
            ReturnType = returnType,
            SourceFileOrigin = new(Parser.Line)
        };

        Parser.SkipUntilNonWhitespace(null);
        if (Parser.GetCharAtDataIndex() != KGVL.OPEN_PARENTHESIS)
        {
            throw new SourceFileReadException(Parser, ErrorCreator.ExpectedOpenParenthesis.CreateOptions());
        }
        Parser.IncrementDataIndex();

        Function.Parameters.SetFrom(ParseFunctionParameters(identifier.SourceCodeName,
            KGVL.NAME_OPERATOR_OVERLOAD, KGVL.CLOSE_PARENTHESIS));
        ConsumeParameterListEnd(KGVL.CLOSE_PARENTHESIS);
        ParseFunctionBody(Function, modifiers);
        return Function;
    }

    private void AddOperatorOverload(object memberHolder,
        string memberHolderTypeName,
        OverloadableOperator overloadedOperator,
        PackFunction function)
    {
        if (memberHolder is not IOperatorOverloadHolder OverloadHolder)
        {
            throw CreateInvalidHolderException(memberHolder, memberHolderTypeName,
                KGVL.NAME_OPERATOR_OVERLOAD);
        }
        OverloadHolder.OperatorOverloads.AddOverload(new(overloadedOperator, function));
    }

    /* Only the operators OverloadableOperator lists can be overloaded, so an unknown spelling here
     * is reported rather than guessed at. */
    private OverloadableOperator? ReadOverloadableOperator()
    {
        (string Spelling, OverloadableOperator Operator)[] Overloadable = new[]
        {
            (KGVL.OPERATOR_INCREMENT, OverloadableOperator.Increment),
            (KGVL.OPERATOR_DECREMENT, OverloadableOperator.Decrement),
            (KGVL.OPERATOR_EQUALS, OverloadableOperator.Equals),
            (KGVL.OPERATOR_NOT_EQUALS, OverloadableOperator.NotEquals),
            (KGVL.OPERATOR_LARGER_OR_EQUAL, OverloadableOperator.LargerOrEqual),
            (KGVL.OPERATOR_LESS_OR_EQUAL, OverloadableOperator.LessThanOrEqual),
            (KGVL.OPERATOR_ADD, OverloadableOperator.Addition),
            (KGVL.OPERATOR_SUBTRACT, OverloadableOperator.Subtraction),
            (KGVL.OPERATOR_MULTIPLY, OverloadableOperator.Multiplication),
            (KGVL.OPERATOR_DIVIDE, OverloadableOperator.Division),
            (KGVL.OPERATOR_MODULO, OverloadableOperator.Modulo),
            (KGVL.OPERATOR_LARGER_THAN, OverloadableOperator.LargerThan),
            (KGVL.OPERATOR_LESS_THAN, OverloadableOperator.LessThan)
        };

        foreach ((string Spelling, OverloadableOperator Operator) Candidate in Overloadable)
        {
            if (Parser.HasStringAtIndex(Parser.DataIndex, Candidate.Spelling))
            {
                Parser.IncrementDataIndexNTimes(Candidate.Spelling.Length);
                return Candidate.Operator;
            }
        }
        return null;
    }

    /* "SomeType this[...] { get; set; }". */
    private bool TryParseIndexer(object memberHolder,
        string memberHolderTypeName,
        TypeTargetIdentifier? returnType,
        PackMemberModifiers modifiers)
    {
        int StartIndex = Parser.DataIndex;
        Parser.SkipUntilNonWhitespace(null);

        if (ReadKeywordOrEmpty() != KGVL.KEYWORD_THIS)
        {
            Parser.DataIndex = StartIndex;
            return false;
        }

        Parser.SkipUntilNonWhitespace(null);
        if (Parser.GetCharAtDataIndex() != KGVL.OPEN_SQUARE_BRACKET)
        {
            Parser.DataIndex = StartIndex;
            return false;
        }

        if (returnType == null)
        {
            throw new SourceFileReadException(Parser, ErrorCreator.VoidIndexer.CreateOptions());
        }
        if (memberHolder is not IPackFunctionHolder FunctionHolder)
        {
            throw CreateInvalidHolderException(memberHolder, memberHolderTypeName, KGVL.NAME_INDEXER);
        }

        PackIndexer Indexer = new(new Identifier(KGVL.NAME_INDEXER), returnType, SourceFile)
        {
            Modifiers = modifiers,
            SourceFileOrigin = new(Parser.Line)
        };

        Parser.IncrementDataIndex();
        Indexer.Parameters.SetFrom(ParseFunctionParameters(KGVL.NAME_INDEXER, KGVL.NAME_INDEXER,
            KGVL.CLOSE_SQUARE_BRACKET));
        ConsumeParameterListEnd(KGVL.CLOSE_SQUARE_BRACKET);

        ParseAccessorBlock(KGVL.NAME_INDEXER,
            accessor => Indexer.GetFunction = accessor,
            accessor => Indexer.SetFunction = accessor,
            null);

        FunctionHolder.AddIndexer(Indexer);
        return true;
    }

    private void ParseFunction(object memberHolder,
        string memberHolderTypeName,
        PackMemberModifiers modifiers,
        TypeTargetIdentifier? returnType,
        Identifier identifier)
    {
        if (memberHolder is not IPackFunctionHolder FunctionHolder)
        {
            throw CreateInvalidHolderException(memberHolder, memberHolderTypeName, KGVL.NAME_FUNCTION);
        }

        PackFunction Function = new(identifier, SourceFile)
        {
            Modifiers = modifiers,
            ReturnType = returnType,
            SourceFileOrigin = new(Parser.Line)
        };

        ParseGenericParameters(identifier.SourceCodeName, KGVL.NAME_FUNCTION, Function.GenericParameters);

        Parser.SkipUntilNonWhitespace(null);
        if (Parser.GetCharAtDataIndex() != KGVL.OPEN_PARENTHESIS)
        {
            throw new SourceFileReadException(Parser, ErrorCreator.ExpectedOpenParenthesis.CreateOptions());
        }
        Parser.IncrementDataIndex();

        Function.Parameters.SetFrom(ParseFunctionParameters(identifier.SourceCodeName,
            KGVL.NAME_FUNCTION, KGVL.CLOSE_PARENTHESIS));
        ConsumeParameterListEnd(KGVL.CLOSE_PARENTHESIS);

        ParseGenericConstraints(identifier.SourceCodeName, KGVL.NAME_FUNCTION, Function.GenericParameters);
        ParseFunctionBody(Function, modifiers);

        FunctionHolder.AddFunction(Function);
    }

    /* A body is either a braced run of statements, a "=>" and a single value, or a ';' for members
     * which deliberately have none, such as abstract and built in ones. */
    private void ParseFunctionBody(PackFunction function, PackMemberModifiers modifiers)
    {
        Parser.SkipUntilNonWhitespace(null);

        if (Parser.GetCharAtDataIndex() == KGVL.SEMICOLON)
        {
            Parser.IncrementDataIndex();
            return;
        }

        if (Parser.HasStringAtIndex(Parser.DataIndex, KGVL.QUICK_METHOD_BODY))
        {
            Parser.IncrementDataIndexNTimes(KGVL.QUICK_METHOD_BODY.Length);
            Parser.SkipUntilNonWhitespace(null);

            /* The single value of a "=>" body is what the function returns, so it is stored the
             * same way an explicit return would be. */
            function.Statements = new();
            function.Statements.AddStatement(new ReturnStatement()
            {
                ReturnValue = _statementParser.ParseExpressionValue()
            });

            Parser.SkipUntilNonWhitespace(null);
            if (Parser.GetCharAtDataIndex() != KGVL.SEMICOLON)
            {
                throw new SourceFileReadException(Parser,
                    ErrorCreator.ExpectedStatementEnd.CreateOptions(KGVL.SEMICOLON));
            }
            Parser.IncrementDataIndex();
            return;
        }

        if (Parser.GetCharAtDataIndex() != KGVL.OPEN_CURLY_BRACKET)
        {
            throw new SourceFileReadException(Parser, ErrorCreator.ExpectedFunctionBody.CreateOptions());
        }

        function.Statements = _statementParser.ParseStatementBody();
    }

    private void ParseField(object memberHolder,
        string memberHolderTypeName,
        PackMemberModifiers modifiers,
        TypeTargetIdentifier returnType,
        Identifier identifier)
    {
        if (memberHolder is not IPackFieldHolder FieldHolder)
        {
            throw CreateInvalidHolderException(memberHolder, memberHolderTypeName, KGVL.NAME_FIELD);
        }

        PackField Field = new(identifier, returnType, SourceFile)
        {
            Modifiers = modifiers,
            SourceFileOrigin = new(Parser.Line)
        };

        Parser.SkipUntilNonWhitespace(null);
        if (Parser.GetCharAtDataIndex() == KGVL.ASSIGNMENT_OPERATOR)
        {
            Parser.IncrementDataIndex();
            Parser.SkipUntilNonWhitespace(null);
            Field.InitialValue = _statementParser.ParseExpressionValue();
            Parser.SkipUntilNonWhitespace(null);
        }

        if (Parser.GetCharAtDataIndex() != KGVL.SEMICOLON)
        {
            throw new SourceFileReadException(Parser,
                ErrorCreator.ExpectedStatementEnd.CreateOptions(KGVL.SEMICOLON));
        }
        Parser.IncrementDataIndex();

        FieldHolder.AddField(Field);
    }

    private void ParseProperty(object memberHolder,
        string memberHolderTypeName,
        PackMemberModifiers modifiers,
        TypeTargetIdentifier returnType,
        Identifier identifier)
    {
        if (memberHolder is not IPackFunctionHolder FunctionHolder)
        {
            throw CreateInvalidHolderException(memberHolder, memberHolderTypeName, KGVL.NAME_PROPERTY);
        }

        PackProperty Property = new(identifier, returnType, SourceFile)
        {
            Modifiers = modifiers,
            SourceFileOrigin = new(Parser.Line)
        };

        Parser.SkipUntilNonWhitespace(null);

        /* "SomeType Name => value;" is a property with nothing but a getter. */
        if (Parser.HasStringAtIndex(Parser.DataIndex, KGVL.QUICK_METHOD_BODY))
        {
            PackFunction Getter = new(new Identifier(KGVL.KEYWORD_GET), SourceFile);
            ParseFunctionBody(Getter, modifiers);
            Property.GetFunction = Getter;
            FunctionHolder.AddProperty(Property);
            return;
        }

        ParseAccessorBlock(KGVL.NAME_PROPERTY,
            accessor => Property.GetFunction = accessor,
            accessor => Property.SetFunction = accessor,
            accessor => Property.InitFunction = accessor);

        /* A property may be given a starting value, as in "public int Count { get; set; } = 3;". */
        Parser.SkipUntilNonWhitespace(null);
        if (Parser.GetCharAtDataIndex() == KGVL.ASSIGNMENT_OPERATOR)
        {
            Parser.IncrementDataIndex();
            Parser.SkipUntilNonWhitespace(null);
            Property.InitialValue = _statementParser.ParseExpressionValue();

            Parser.SkipUntilNonWhitespace(null);
            if (Parser.GetCharAtDataIndex() != KGVL.SEMICOLON)
            {
                throw new SourceFileReadException(Parser,
                    ErrorCreator.ExpectedStatementEnd.CreateOptions(KGVL.SEMICOLON));
            }
            Parser.IncrementDataIndex();
        }

        FunctionHolder.AddProperty(Property);
    }

    /* The braced "{ get; set; }" of a property or indexer. Each accessor is stored as a function
     * so that a body written for it has somewhere to live. An indexer passes null for init. */
    private void ParseAccessorBlock(string memberTypeName,
        Action<PackFunction> setGetter,
        Action<PackFunction> setSetter,
        Action<PackFunction>? setInit)
    {
        Parser.SkipUntilNonWhitespace(null);
        if (Parser.GetCharAtDataIndex() != KGVL.OPEN_CURLY_BRACKET)
        {
            throw new SourceFileReadException(Parser,
                ErrorCreator.ExpectedAccessorBlockStart.CreateOptions(memberTypeName));
        }
        Parser.IncrementDataIndex();
        Parser.SkipUntilNonWhitespace(null);

        while (Parser.IsMoreDataAvailable && (Parser.GetCharAtDataIndex() != KGVL.CLOSE_CURLY_BRACKET))
        {
            PackMemberModifiers AccessorModifiers = ParseAccessorModifiers();

            Parser.SkipUntilNonWhitespace(null);
            string Keyword = ReadKeywordOrEmpty();

            PackFunction Accessor = new(new Identifier(Keyword), SourceFile)
            {
                Modifiers = AccessorModifiers
            };
            ParseFunctionBody(Accessor, AccessorModifiers);

            if (Keyword == KGVL.KEYWORD_GET)
            {
                setGetter(Accessor);
            }
            else if (Keyword == KGVL.KEYWORD_SET)
            {
                setSetter(Accessor);
            }
            else if ((Keyword == KGVL.KEYWORD_INIT) && (setInit != null))
            {
                setInit(Accessor);
            }
            else
            {
                throw new SourceFileReadException(Parser,
                    ErrorCreator.ExpectedAccessor.CreateOptions(memberTypeName));
            }

            Parser.SkipUntilNonWhitespace(null);
        }

        if (Parser.GetCharAtDataIndex() != KGVL.CLOSE_CURLY_BRACKET)
        {
            throw new SourceFileReadException(Parser,
                ErrorCreator.ExpectedAccessorBlockEnd.CreateOptions(memberTypeName));
        }
        Parser.IncrementDataIndex();
    }

    /* An accessor may narrow its member's access, as in "{ get; private set; }". */
    private PackMemberModifiers ParseAccessorModifiers()
    {
        PackMemberModifiers Modifiers = PackMemberModifiers.None;

        while (true)
        {
            Parser.SkipUntilNonWhitespace(null);
            int StartIndex = Parser.DataIndex;

            string Keyword = ReadKeywordOrEmpty();
            PackMemberModifiers Modifier = StringToModifier(Keyword);

            if ((Keyword.Length == 0) || (Modifier == PackMemberModifiers.None))
            {
                Parser.DataIndex = StartIndex;
                return Modifiers;
            }
            Modifiers = CombineModifier(Modifiers, Modifier);
        }
    }

    /* ParseFunctionParameters stops on the character ending the list rather than consuming it, so
     * that its callers can report a missing one themselves. */
    private void ConsumeParameterListEnd(char endChar)
    {
        Parser.SkipUntilNonWhitespace(null);
        if (Parser.GetCharAtDataIndex() != endChar)
        {
            throw new SourceFileReadException(Parser,
                ErrorCreator.ExpectedParameterListEnd.CreateOptions(endChar));
        }
        Parser.IncrementDataIndex();
    }

    /* Reads the next word when there is one, without the error a bare ReadIdentifier would raise,
     * so that callers speculating on what comes next can put the cursor back instead. */
    private string ReadKeywordOrEmpty()
    {
        Parser.SkipUntilNonWhitespace(null);
        return Parser.IsIdentifierFirstChar(Parser.GetCharAtDataIndex())
            ? Parser.ReadIdentifier(null) : string.Empty;
    }

    internal void ParseMemberExtensions(IPackMemberExtender extender,
        string extenderTypeName,
        string extenderName)
    {
        ErrorCreateOptions IdentifierError = ErrorCreator.ExpectedExtendedMemberIdentifier
            .CreateOptions(extenderTypeName, extenderName);
        ErrorCreateOptions ExtendEndError = ErrorCreator.UnexpectedExtendedMemberEnd
            .CreateOptions(extenderTypeName, extenderName);

        Parser.SkipUntilNonWhitespace(null);

        bool IsAnExtensionExpected = Parser.GetCharAtDataIndex() == KGVL.COLON;
        while (IsAnExtensionExpected)
        {
            Parser.IncrementDataIndex();
            Parser.SkipUntilNonWhitespace(IdentifierError);

            if (!Parser.IsIdentifierFirstChar(Parser.GetCharAtDataIndex()) && (extender.ExtendedMemberCount > 0))
            {
                throw new SourceFileReadException(Parser, ExtendEndError);
            }

            Identifier ExtendedMember = new(Parser.ReadIdentifier(IdentifierError));
            extender.AddExtendedMember(ExtendedMember);

            Parser.SkipUntilNonWhitespace(null);
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
            KGVL.KEYWORD_PARAMS => FunctionParameterModifier.Params,
            _ => FunctionParameterModifier.None
        };
    }

    private FunctionParameterCollection ParseFunctionParameters(string memberName,
        string memberTypeName,
        char paramsListEndChar)
    {
        ErrorCreateOptions ExpectedParamOrEndError = ErrorCreator.ExpectedParametersRegularOrEnd
                .CreateOptions(memberTypeName, memberName);
        ErrorCreateOptions ExpectedTypeError = ErrorCreator.ExpectedParameterType
                .CreateOptions(memberTypeName, memberName);
        ErrorCreateOptions ExpectedIdentifierError = ErrorCreator.ExpectedParameterIdentifier
                .CreateOptions(memberTypeName, memberName);
        ErrorCreateOptions ExpectedParamTypeOrModifier = ErrorCreator.ExpectedParameterTypeOrModifier
                .CreateOptions(memberTypeName, memberName);

        FunctionParameterCollection Params = new();

        Parser.SkipUntilNonWhitespace(ExpectedParamOrEndError);
        bool IsParameterExpected = Parser.GetCharAtDataIndex() != paramsListEndChar;
        while (IsParameterExpected)
        {
            if (!Parser.IsIdentifierFirstChar(Parser.GetCharAtDataIndex()))
            {
                throw new SourceFileReadException(Parser, ErrorCreator.UnexpectedParameterEndError
                    .CreateOptions(memberTypeName, memberName));
            }

            int StartIndex = Parser.DataIndex;
            string FirstWord = Parser.ReadIdentifier(null);
            FunctionParameterModifier Modifier = StringToParamModifier(FirstWord);

            if (Modifier != FunctionParameterModifier.None)
            {
                Parser.SkipUntilNonWhitespace(ExpectedTypeError);
            }
            else
            {
                Parser.DataIndex = StartIndex;
            }

            TypeTargetIdentifier ParamType = Parser.ReadTypeTargetIdentifier(ExpectedTypeError);
            Parser.SkipUntilNonWhitespace(ExpectedIdentifierError);
            string ParamName = Parser.ReadIdentifier(ExpectedIdentifierError);
            Parser.SkipUntilNonWhitespace(ExpectedParamOrEndError);
            Params.AddItem(new(ParamType, new(ParamName), Modifier));

            char NextChar = Parser.GetCharAtDataIndex();
            IsParameterExpected = NextChar == KGVL.COMMA;
            if (IsParameterExpected)
            {
                Parser.IncrementDataIndex();
                Parser.SkipUntilNonWhitespace(ExpectedParamTypeOrModifier);
            }
        }

        return Params;
    }

    private void ParseGenericConstraints(
        string memberName,
        string memberTypeName,
        GenericTypeParameterCollection parameters)
    {
        Parser.SkipUntilNonWhitespace(null);

        int SavedIndex = Parser.DataIndex;
        while (Parser.ReadIdentifier(null) == KGVL.KEYWORD_WHERE)
        {
            Parser.SkipUntilNonWhitespace(ErrorCreator.ExpectedGenericTypeNameForConstraint
                .CreateOptions(memberTypeName, memberName));
            ParseSingleGenericParameterConstraints(memberName, memberTypeName, parameters);

            SavedIndex = Parser.DataIndex;
            Parser.SkipUntilNonWhitespace(null);
        }

        Parser.DataIndex = SavedIndex;
    }

    private void ParseSingleGenericParameterConstraints(
        string memberName,
        string memberTypeName,
        GenericTypeParameterCollection parameters)
    {
        string TypeParamName = Parser.ReadIdentifier(ErrorCreator.ExpectedGenericTypeNameForConstraint
            .CreateOptions(memberTypeName, memberName));

        GenericTypeParameter Parameter = parameters.GetBySourceCodeName(TypeParamName) ??
            throw new SourceFileReadException(Parser, ErrorCreator.GenericParameterNotFound
            .CreateOptions(TypeParamName, memberTypeName, memberName));

        ErrorCreateOptions ExpectStartError = ErrorCreator.GenericParameterConstraintStartNotFound
            .CreateOptions(TypeParamName, memberTypeName, memberName);

        Parser.SkipUntilNonWhitespace(ExpectStartError);
        if (Parser.GetCharAtDataIndex() != KGVL.COLON)
        {
            throw new SourceFileReadException(Parser, ExpectStartError);
        }
        Parser.IncrementDataIndex();

        ErrorCreateOptions ExpectedConstraintError = ErrorCreator.ExpectedGenericConstraintIdentifier
            .CreateOptions(Parameter.SelfIdentifier.SourceCodeName, memberTypeName, memberName);
        List<GenericConstraint> Constraints = new();
        bool IsConstraintExpected = true;
        while (IsConstraintExpected)
        {
            Parser.SkipUntilNonWhitespace(ExpectedConstraintError);

            if (!Parser.IsIdentifierFirstChar(Parser.GetCharAtDataIndex()) && (Constraints.Count > 0))
            {
                throw new SourceFileReadException(Parser, ErrorCreator.UnexpectedGenericConstraintEnd
                    .CreateOptions(memberTypeName, memberName));
            }

            TypeTargetIdentifier ConstraintType = Parser.ReadTypeTargetIdentifier(ExpectedConstraintError);
            SpecialGenericConstraint? SpecialConstraint = TryGetSpecialConstraint(
                ConstraintType.MainTarget.SourceCodeName);
            GenericConstraint Constraint = SpecialConstraint != null ? new(SpecialConstraint.Value) : new(ConstraintType);
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

    private bool ParseGenericParameters(string memberName,
        string memberTypeName,
        GenericTypeParameterCollection parameters)
    {
        Parser.SkipUntilNonWhitespace(null);
        if (Parser.GetCharAtDataIndex() != KGVL.GENERIC_TYPE_START)
        {
            return false;
        }
        Parser.IncrementDataIndex();

        bool IsParameterExpected = true;
        while (IsParameterExpected)
        {
            ErrorCreateOptions ExpectedIdentifierError = ErrorCreator.ExpectedGenericParameterIdentifier
                .CreateOptions(memberTypeName, memberName);

            Parser.SkipUntilNonWhitespace(ExpectedIdentifierError);
            if (Parser.GetCharAtDataIndex() == KGVL.GENERIC_TYPE_END) // Test edge case for better error messages.
            {
                throw new SourceFileReadException(Parser, ErrorCreator.GenericParametersUnexpectedEnd
                    .CreateOptions(memberTypeName, memberName));
            }

            string Identifier = Parser.ReadIdentifier(ExpectedIdentifierError);
            parameters.AddItem(new(new(Identifier), null));

            Parser.SkipUntilNonWhitespace(ErrorCreator.ExpectedGenericParameterCommaOrEnd
                    .CreateOptions(memberTypeName, memberName));

            IsParameterExpected = Parser.GetCharAtDataIndex() == KGVL.COMMA;
            if (IsParameterExpected)
            {
                Parser.IncrementDataIndex();
            }
        }

        ErrorCreateOptions ErrorExpectedParameterEnd = ErrorCreator.ExpectedGenericParameterEnd
                .CreateOptions(memberTypeName, memberName);
        Parser.SkipUntilNonWhitespace(ErrorExpectedParameterEnd);
        if (Parser.GetCharAtDataIndex() != KGVL.GENERIC_TYPE_END)
        {
            throw new SourceFileReadException(Parser, ErrorExpectedParameterEnd);
        }
        Parser.IncrementDataIndex();
        return true;
    }
}