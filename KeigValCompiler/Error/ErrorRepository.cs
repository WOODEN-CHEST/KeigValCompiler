using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace KeigValCompiler.Error;

/* This class contains all errors and warning that the compiler has.
 * They're marked as virtual to that a derived class can replace them if needed, though that 
 * is pretty much only for testing purposes. */
internal class ErrorRepository
{
    // Fields.
    /* File root. */
    internal virtual ErrorDefinition RootExpectedKeyword { get; } = new(1,
        CompilerMessageCategory.SourceFileRoot,
        $"In root scope of a source-code file, expected keyword \"{KGVL.KEYWORD_NAMESPACE}\" " +
        $"to set the active namespace or \"{KGVL.KEYWORD_USING}\" to import a namespace");

    internal virtual ErrorDefinition RootNonActiveNamespace { get; } = new(2,
        CompilerMessageCategory.SourceFileRoot,
        "Unexpected keyword \"{0}\" in root scope of a source-code file, " +
        $"a working namespace needs to be set prior to member definitions with \"{KGVL.KEYWORD_NAMESPACE} X.Y.Z\"");

    internal virtual ErrorDefinition RootExpectedKeywordOrMember { get; } = new(3,
        CompilerMessageCategory.SourceFileRoot,
        $"In root scope of source-code file, expected either namespace change via " +
        $"\"{KGVL.KEYWORD_NAMESPACE}\" keyword, using directive with keyword \"{KGVL.KEYWORD_USING}\", " +
        $"or member in the namespace \"{{0}}\" of type {KGVL.NAME_CLASS}, {KGVL.NAME_STRUCT}, " +
        $"{KGVL.NAME_INTERFACE}, {KGVL.NAME_ENUM}, {KGVL.NAME_DELEGATE}, {KGVL.NAME_FUNCTION}, " +
        $"{KGVL.NAME_FIELD}, {KGVL.NAME_PROPERTY} or {KGVL.NAME_EVENT}");

    internal virtual ErrorDefinition ExpectedNamespaceForUsingDirective { get; } = new(4,
        CompilerMessageCategory.SourceFileRoot,
        $"Expected namespace identifier (e.g \"X.Y.Z\") for \"{KGVL.KEYWORD_USING}\" directive");

    internal virtual ErrorDefinition ExpectedNamespaceForSet { get; } = new(5,
        CompilerMessageCategory.SourceFileRoot,
        $"Expected a namespace identifier (e.g \"X.Y.Z\") to set the active namespace");

    internal virtual ErrorDefinition ExpectedNamespaceSectionIdentifier { get; } = new(6,
        CompilerMessageCategory.SourceFileRoot,
        "Expected namespace section identifier in incomplete namespace \"{0}\". " +
        "A section is a single identifier in a namespace's full name, " +
        "like the part \"X\" or \"Y\", or \"Z\" in the namespace \"X.Y.Z\"");

    internal virtual ErrorDefinition ExpectedNamespaceEndOrContinuation { get; } = new(7,
        CompilerMessageCategory.SourceFileRoot,
        $"Expected namespace \"{{0}}\" end with '{KGVL.SEMICOLON}' " +
        $"or continuation with '{KGVL.NAMESPACE_SEPARATOR}'");

    internal virtual ErrorDefinition NamespaceEOFTrailingContinuation { get; } = new(8,
        CompilerMessageCategory.SourceFileRoot,
        $"Expected namespace continuation with the next segment in the incomplete namespace \"{{0}}\"" +
        $" (it ends with the namespace separator '{KGVL.NAMESPACE_SEPARATOR}', " +
        $"indicating continuation, but none was found");

    internal virtual ErrorDefinition NamespaceUnexpectedChar { get; } = new(9,
        CompilerMessageCategory.SourceFileRoot,
        $"Unexpected character '{{0}}' in namespace \"{{1}}\", expected namespace end with '{KGVL.SEMICOLON}' " +
        $"or namespace continuation indicated by '{KGVL.NAMESPACE_SEPARATOR}'");


    /* Member generic. */
    internal virtual ErrorDefinition ExpectedMemberModifierOrKeyword { get; } = new(1,
        CompilerMessageCategory.MemberGeneric,
        $"Expected member modifier (like {KGVL.KEYWORD_PUBLIC} or {KGVL.KEYWORD_PROTECTED}, " +
        $"or {KGVL.KEYWORD_PRIVATE}) or a member in the {{0}} \"{{1}}\".");

    internal virtual ErrorDefinition ExpectedMember { get; } = new(2,
        CompilerMessageCategory.MemberGeneric,
        "Expected {0} member");

    internal virtual ErrorDefinition CannotHoldMemberInNamespace { get; } = new(3,
        CompilerMessageCategory.MemberGeneric,
        "A namespace \"{0}\" cannot hold a member of type {1}");

    internal virtual ErrorDefinition CannotHoldMemberInMember { get; } = new(4,
        CompilerMessageCategory.MemberGeneric,
        "A member of type {0} \"{1}\" cannot hold a member of type {2}");

    internal virtual ErrorDefinition CannotHoldMemberInUnknown { get; } = new(5,
        CompilerMessageCategory.MemberGeneric,
       "The parent member cannot hold a member of type {0}");


    /* Member modifier. */
    internal virtual ErrorDefinition DuplicateModifiers { get; } = new(1,
        CompilerMessageCategory.MemberModifier,
        "Duplicate member modifier \"{0}\"");

    internal virtual ErrorDefinition ReservedKeywordBuiltIn { get; } = new(2,
        CompilerMessageCategory.MemberModifier,
        $"The modifier \"{KGVL.KEYWORD_BUILTIN}\" is reserved for compiler internal use only");


    /* Identifiers. */
    internal virtual ErrorDefinition ExpectedTypeMemberIdentifier { get; } = new(1,
        CompilerMessageCategory.Identifiers,
        "Expected {0} member identifier");

    internal virtual ErrorDefinition ExpectedReturnTypeIdentifier { get; } = new(2,
        CompilerMessageCategory.Identifiers,
        "Expected {0} member return type identifier");

    internal virtual ErrorDefinition VoidCantHaveGenericArguments { get; } = new(3,
        CompilerMessageCategory.Identifiers,
        $"The return type \"{KGVL.KEYWORD_VOID}\" cannot have generic type arguments.");


    /* Type member common. */
    internal virtual ErrorDefinition ExpectedBodyOrExtensionOrConstraints { get; } = new(1,
        CompilerMessageCategory.TypeMemberCommon,
        $"Expected {{0}} \"{{1}}\" body start '{KGVL.DOUBLE_CURLY_OPEN}' " +
        $"or member extension ({{1}} {KGVL.COLON} T1, T2 ... Tn), " +
        $"or generic constraints ({{1}} {KGVL.KEYWORD_WHERE} T1 {KGVL.COLON} ... )");

    internal virtual ErrorDefinition ExpectedBodyOrExtension { get; } = new(2,
        CompilerMessageCategory.TypeMemberCommon,
        $"Expected {{0}} \"{{1}}\" body start '{KGVL.DOUBLE_CURLY_OPEN}' " +
        $"or member extension ({{1}} {KGVL.COLON} T1, T2 ... Tn)");

    internal virtual ErrorDefinition EOFWhileParsingMembers { get; } = new(3,
        CompilerMessageCategory.TypeMemberCommon,
        $"Expected {{0}} \"{{1}}\" body end '{KGVL.DOUBLE_CURLY_CLOSE}' or {{0}} member, got end of file");

    internal virtual ErrorDefinition ExpectedCurlyTypeEnd { get; } = new(4,
        CompilerMessageCategory.TypeMemberCommon,
        $"Expected {{0}} \"{{1}}\" body end '{KGVL.DOUBLE_CURLY_CLOSE}'");

    internal virtual ErrorDefinition ExpectedGenericParamsOrExtension { get; } = new(5,
        CompilerMessageCategory.TypeMemberCommon,
        "Expected {0} member \"{1}\" generic parameters " +
        $"({{1}}{KGVL.GENERIC_TYPE_START}T1, T2 ... Tn{KGVL.GENERIC_TYPE_END}) " +
        $"or member extension ({{1}} {KGVL.COLON} T1, T2 ... Tn)");

    internal virtual ErrorDefinition ExpectedMemberBodyStart { get; } = new(6,
        CompilerMessageCategory.TypeMemberCommon,
        $"Expected {{0}} \"{{1}}\" body start '{KGVL.DOUBLE_CURLY_OPEN}'");

    internal virtual ErrorDefinition ExpectedExtendedMemberIdentifier { get; } = new(7,
        CompilerMessageCategory.TypeMemberCommon,
        $"Expected {{0}} \"{{1}}\" extended member identifier (like \"T1\" or \"ABC\")");

    internal virtual ErrorDefinition UnexpectedExtendedMemberEnd { get; } = new(8,
        CompilerMessageCategory.TypeMemberCommon,
        $"Expected a member extension identifier because a previous comma '{KGVL.COMMA}' indicated " +
        "that more member extensions are to follow for the {0} \"{1}\"");


    /* Delegate. */
    internal virtual ErrorDefinition ExpectedDelegateParamList { get; } = new(1,
        CompilerMessageCategory.Delegate,
        $"Expected delegate \"{{0}}\" parameter list ({{0}}{KGVL.OPEN_PARENTHESIS} ... {KGVL.CLOSE_PARENTHESIS})");

    internal virtual ErrorDefinition ExpectedDelegateParamListEnd { get; } = new(2,
        CompilerMessageCategory.Delegate,
        $"Expected delegate \"{{0}}\" parameter list end '{KGVL.CLOSE_PARENTHESIS}'");

    internal virtual ErrorDefinition ExpectedDelegateDefinitionEnd { get; } = new(3,
        CompilerMessageCategory.Delegate,
        $"Expected delegate \"{{0}}\" definition end '{KGVL.SEMICOLON}'");


    /* Event */
    internal virtual ErrorDefinition ExpectedEventEnd { get; } = new(1,
        CompilerMessageCategory.Event,
        $"Expected event \"{{0}}\" definition end '{KGVL.SEMICOLON}'");

    internal virtual ErrorDefinition ExpectedEventDelegateType { get; } = new(2,
        CompilerMessageCategory.Event,
        "Expected event delegate type identifier.");


    /* Enum. */
    internal virtual ErrorDefinition EnumConstantOutOfRange { get; } = new(1,
        CompilerMessageCategory.Enum,
        "Enum constant \"{0}\" in enum type " +
        "\"{1}\" is out of the valid range " +
        $"{int.MinValue} to {int.MaxValue}, it has a value of {{2}}");

    internal virtual ErrorDefinition ExpectedEnumBodyStart { get; } = new(2,
        CompilerMessageCategory.Enum,
        $"Expected enum body start '{KGVL.DOUBLE_CURLY_OPEN}' for enum \"{{0}}\"");

    internal virtual ErrorDefinition ExpectedEnumBodyEnd { get; } = new(3,
        CompilerMessageCategory.Enum,
        $"Expected enum body end '{KGVL.DOUBLE_CURLY_CLOSE}' for enum \"{{0}}\"");

    internal virtual ErrorDefinition ExpectedEnumConstant { get; } = new(4,
        CompilerMessageCategory.Enum,
        $"Expected enum \"{{0}}\" constant identifier");

    internal virtual ErrorDefinition ExpectedEnumConstantOrEnd { get; } = new(5,
        CompilerMessageCategory.Enum,
        $"Expected enum \"{{0}}\" body end '{KGVL.DOUBLE_CURLY_CLOSE}' or an enum constant");

    internal virtual ErrorDefinition ExpectedEnumAssignmentOrNextOrEnd { get; } = new(6,
        CompilerMessageCategory.Enum,
        $"Expected value assignment for enum constant \"{{0}}\", or enum \"{{1}}\" body end " +
        $"'{KGVL.DOUBLE_CURLY_CLOSE}' or comma '{KGVL.COMMA}' followed by the next enum constant");

    internal virtual ErrorDefinition ExpectedEnumConstantValue { get; } = new(7,
        CompilerMessageCategory.Enum,
        $"Expected enum constant \"{{0}}\" value (like 123, 420 or something else) because a previous " +
        $"'{KGVL.ASSIGNMENT_OPERATOR}' character indicated that an enum constant value will follow. ");

    internal virtual ErrorDefinition UnexpctedEnumNonConstantValue { get; } = new(8,
        CompilerMessageCategory.Enum,
        $"Expected identifier for enum constant in enum \"{{0}}\" because a previous comma {KGVL.COMMA} " +
        $"indicated that an enum constant will follow.");


    /* Record. */
    internal virtual ErrorDefinition ExpectedRecordPrimaryConstructorOrBody { get; } = new(1,
        CompilerMessageCategory.Record,
        $"Expected record \"{{0}}\" " +
        $"primary constructor ({{0}}{KGVL.OPEN_PARENTHESIS} ... {KGVL.CLOSE_PARENTHESIS}) " +
        $"or body ({{0}} {KGVL.DOUBLE_CURLY_OPEN} ... {KGVL.DOUBLE_CURLY_CLOSE}) " +
        $"or member extension ({{0}} {KGVL.COLON} T1, T2 ... Tn)");

    internal virtual ErrorDefinition ExpectedRecordPrimaryConstructorOrBodyOrConstraints { get; } = new(2,
        CompilerMessageCategory.Record,
        $"Expected record \"{{0}}\" body start '{KGVL.DOUBLE_CURLY_OPEN}', primary constructor " +
        $"({{0}}{KGVL.OPEN_PARENTHESIS} ... {KGVL.CLOSE_PARENTHESIS}{KGVL.SEMICOLON}) " +
        $"or member extension ({{0}} {KGVL.COLON} T1, T2 ... Tn), " +
        $"or generic constraints ({{0}} {KGVL.KEYWORD_WHERE} T1 {KGVL.COLON} ... )");

    internal virtual ErrorDefinition ExpectedRecordPrimaryConstructorParameters { get; } = new(3,
        CompilerMessageCategory.Record,
        $"Expected record \"{{0}}\" primary constructor's parameters (T1 a, T2 b, ... Tn z) or " +
        $"parameter list end '{KGVL.CLOSE_PARENTHESIS}'");

    internal virtual ErrorDefinition ExpectedRecordPrimaryConstructorEnd { get; } = new(4,
        CompilerMessageCategory.Record,
        $"Expected record \"{{0}}\" primary constructor end '{KGVL.CLOSE_PARENTHESIS}'");

    internal virtual ErrorDefinition ExpectedRecordBodyStartOrEnd { get; } = new(5,
        CompilerMessageCategory.Record,
        $"Expected record \"{{0}}\" body start '{KGVL.DOUBLE_CURLY_OPEN}' or end '{KGVL.SEMICOLON}'");


    /* Generics. */
    internal virtual ErrorDefinition ExpectedGenericParameterEnd { get; } = new(1,
        CompilerMessageCategory.Generics,
        $"Expected generic parameter list end '{KGVL.GENERIC_TYPE_END}' for {{0}} \"{{1}}\"");

    internal virtual ErrorDefinition ExpectedGenericParameterIdentifier { get; } = new(2,
        CompilerMessageCategory.Generics,
        "Expected generic parameter identifier for {0} \"{1}\" (for example \"T\")");

    internal virtual ErrorDefinition ExpectedGenericParameterCommaOrEnd { get; } = new(3,
        CompilerMessageCategory.Generics,
        $"Expected comma '{KGVL.COMMA}' for next generic parameter identifier " +
        $"or generic parameter list end '{KGVL.GENERIC_TYPE_END}' for {{0}} \"{{1}}\"");

    internal virtual ErrorDefinition GenericParametersUnexpectedEnd { get; } = new(4,
        CompilerMessageCategory.Generics,
        $"Unexpected end of generic parameters in {{0}} \"{{1}}\". A previously placed comma '{KGVL.COMMA}' " +
        "indicated that more generic type parameters would follow, but the end of the parameter list was " +
        $"met instead '{KGVL.GENERIC_TYPE_END}'. Trailing comma perhaps?");

    internal virtual ErrorDefinition ExpectedGenericTypeNameForConstraint { get; } = new(5,
        CompilerMessageCategory.Generics,
        "Expected identifier of a generic type parameter (like \"T1\") " +
        "to be used for constraints for the {0} \"{1}\"");

    internal virtual ErrorDefinition GenericParameterNotFound { get; } = new(6,
        CompilerMessageCategory.Generics,
        "No generic parameter with the name \"{0}\" was found for the {1} \"{2}\"");

    internal virtual ErrorDefinition GenericParameterConstraintStartNotFound { get; } = new(7,
        CompilerMessageCategory.Generics,
        $"Expected '{KGVL.COLON}' to denote the start of constraints for the " +
        "type parameter \"{0}\" in the {1} \"{2}\"");

    internal virtual ErrorDefinition ExpectedGenericConstraintIdentifier { get; } = new(8,
        CompilerMessageCategory.Generics,
        "Expected generic constraint value " +
        $"(type identifier like \"T1\" or special constraint like \"{KGVL.KEYWORD_NOTNULL}\") for the " +
        "type parameter \"{0}\" in the {1} \"{2}\"");

    internal virtual ErrorDefinition UnexpectedGenericConstraintEnd { get; } = new(9,
        CompilerMessageCategory.Generics,
        $"Expected generic constraint value because a previous comma '{KGVL.COMMA}' indicated that more " +
        $"constraints are to follow, but found no constraint in the {{0}} \"{{1}}\". Trailing comma perhaps?");


    /* Return typed members common. */


    /* Comments. */
    internal virtual ErrorDefinition ExpectedMultiLineCommentEnd { get; } = new(1,
        CompilerMessageCategory.Comment,
        "Multi-line comment started on line {0} wasn't terminated properly");

    internal virtual ErrorDefinition ExpectedQuotedBlockEnd { get; } = new(2,
        CompilerMessageCategory.Comment,
        "Expected the quoted block opened with '{0}' to be closed with a matching '{0}' before the end " +
        "of the file. Comments are not stripped from inside quotes, so an unclosed quote swallows the " +
        "rest of the file");



    /* Function. */
    internal virtual ErrorDefinition ExpectedParametersRegularOrEnd { get; } = new(1,
        CompilerMessageCategory.Function,
        $"Expected {{0}} \"{{1}}\" function parameter list " +
        $"{KGVL.OPEN_PARENTHESIS}T1 a, T2 b, ... {KGVL.CLOSE_PARENTHESIS} " +
        $"or parameter list end '{KGVL.CLOSE_PARENTHESIS}'");

    internal virtual ErrorDefinition ExpectedParameterTypeOrModifier { get; } = new(2,
        CompilerMessageCategory.Function,
        "Expected {0} \"{1}\" function parameter type identifier or parameter modifier " +
        $"(\"{KGVL.KEYWORD_IN}\", \"{KGVL.KEYWORD_OUT}\" or \"{KGVL.KEYWORD_REF}\")");

    internal virtual ErrorDefinition ExpectedParameterType { get; } = new(3,
        CompilerMessageCategory.Function,
        "Expected {0} \"{1}\" function parameter type identifier");

    internal virtual ErrorDefinition ExpectedParameterIdentifier { get; } = new(4,
        CompilerMessageCategory.Function,
        "Expected {0} \"{1}\" function parameter identifier");

    internal virtual ErrorDefinition UnexpectedParameterEndError { get; } = new(5,
        CompilerMessageCategory.Function,
        $"Expected more function parameters because a previously placed comma '{KGVL.COMMA}' " +
        "indicated that more function parameters are to follow for {0} \"{1}\"");


    /* Literals. */
    internal virtual ErrorDefinition InvalidHexEscapeSequence { get; } = new(1,
        CompilerMessageCategory.Literal,
        $"The escape sequence \"{KGVL.ESCAPE_CHAR}{{0}}\" is not a valid hexadecimal character code. " +
        $"A hexadecimal escape sequence is the prefix '{KGVL.PREFIX_HEX_CHAR}' followed by hexadecimal " +
        "digits (0-9 and a-f) whose value fits into a single character");

    internal virtual ErrorDefinition UnknownEscapeSequence { get; } = new(2,
        CompilerMessageCategory.Literal,
        $"Unknown escape sequence \"{KGVL.ESCAPE_CHAR}{{0}}\". An escape sequence is the character " +
        $"'{KGVL.ESCAPE_CHAR}' followed by one of a, b, f, n, t, v, ', \" or {KGVL.ESCAPE_CHAR}, or by " +
        $"the prefix '{KGVL.PREFIX_HEX_CHAR}' and a hexadecimal character code");


    /* Source files. */
    internal virtual ErrorDefinition SourceFileInvalidContent { get; } = new(1,
        CompilerMessageCategory.SourceFile,
        "The source file \"{0}\" parsed, but what it describes cannot be put into the pack");

    internal virtual ErrorDefinition SourceFileNotFound { get; } = new(2,
        CompilerMessageCategory.SourceFile,
        "The source file \"{0}\" was listed in the source directory but could not be opened. " +
        "It was most likely moved or deleted while the compiler was running");

    internal virtual ErrorDefinition SourceFileDirectoryNotFound { get; } = new(3,
        CompilerMessageCategory.SourceFile,
        "The directory holding the source file \"{0}\" could not be found. " +
        "It was most likely moved or deleted while the compiler was running");

    internal virtual ErrorDefinition SourceFileReadFailure { get; } = new(4,
        CompilerMessageCategory.SourceFile,
        "The source file \"{0}\" could not be read. The file itself exists, so this is a problem with " +
        "the file system rather than with the code in it");


    /* Expressions. */
    internal virtual ErrorDefinition ExpectedValue { get; } = new(1,
        CompilerMessageCategory.Expression,
        "Expected a value here, but found the character '{0}', which cannot begin one. " +
        "A value is a literal, a name, a call, or any of those combined with operators");

    internal virtual ErrorDefinition ExpectedTernaryElseBranch { get; } = new(2,
        CompilerMessageCategory.Expression,
        $"Expected '{KGVL.TERNARY_BRANCH_SEPARATOR}' and the value to use when the condition is " +
        $"false. A conditional value is written \"condition {KGVL.TYPE_NULLABLE_INDICATOR} whenTrue " +
        $"{KGVL.TERNARY_BRANCH_SEPARATOR} whenFalse\" and must always supply both branches");

    internal virtual ErrorDefinition ExpectedIsCheckType { get; } = new(3,
        CompilerMessageCategory.Expression,
        $"Expected a type name after the \"{KGVL.KEYWORD_IS}\" keyword, as in \"value " +
        $"{KGVL.KEYWORD_IS} SomeType\"");

    internal virtual ErrorDefinition ExpectedMemberAccessName { get; } = new(4,
        CompilerMessageCategory.Expression,
        $"Expected the name of a member after '{KGVL.MEMBER_ACCESS}'");

    internal virtual ErrorDefinition ExpectedIndexAccessEnd { get; } = new(5,
        CompilerMessageCategory.Expression,
        $"Expected '{KGVL.CLOSE_SQUARE_BRACKET}' to close an index access");

    internal virtual ErrorDefinition ExpectedIndexAccessArgument { get; } = new(6,
        CompilerMessageCategory.Expression,
        $"An index access needs at least one index between '{KGVL.OPEN_SQUARE_BRACKET}' and " +
        $"'{KGVL.CLOSE_SQUARE_BRACKET}'");

    internal virtual ErrorDefinition UncallableStatement { get; } = new(7,
        CompilerMessageCategory.Expression,
        "Only a named member can be called, so the brackets here have nothing to call");

    internal virtual ErrorDefinition ExpectedCallArgumentsEnd { get; } = new(8,
        CompilerMessageCategory.Expression,
        $"Expected '{KGVL.CLOSE_PARENTHESIS}' to close a call's argument list, or " +
        $"'{KGVL.COMMA}' to continue it with another argument");

    internal virtual ErrorDefinition ExpectedOpenParenthesis { get; } = new(9,
        CompilerMessageCategory.Expression,
        $"Expected '{KGVL.OPEN_PARENTHESIS}'");

    internal virtual ErrorDefinition ExpectedCloseParenthesis { get; } = new(10,
        CompilerMessageCategory.Expression,
        $"Expected '{KGVL.CLOSE_PARENTHESIS}'");

    internal virtual ErrorDefinition ExpectedNameOfTarget { get; } = new(11,
        CompilerMessageCategory.Expression,
        $"Expected the name whose text \"{KGVL.KEYWORD_NAMEOF}\" should produce");

    internal virtual ErrorDefinition ExpectedTypeOfTarget { get; } = new(12,
        CompilerMessageCategory.Expression,
        "Expected a type name inside the brackets");

    internal virtual ErrorDefinition ExpectedConstructedType { get; } = new(13,
        CompilerMessageCategory.Expression,
        $"Expected the name of the type to create after the \"{KGVL.KEYWORD_NEW}\" keyword");

    internal virtual ErrorDefinition ExpectedInferredArrayEnd { get; } = new(14,
        CompilerMessageCategory.Expression,
        $"Expected '{KGVL.CLOSE_SQUARE_BRACKET}' directly after '{KGVL.OPEN_SQUARE_BRACKET}'. " +
        $"An array written as \"{KGVL.KEYWORD_NEW}[] {{ ... }}\" takes its element type from its " +
        "values, so no length belongs between the brackets");

    internal virtual ErrorDefinition ExpectedArrayLengthEnd { get; } = new(15,
        CompilerMessageCategory.Expression,
        $"Expected '{KGVL.CLOSE_SQUARE_BRACKET}' to close an array's length");

    internal virtual ErrorDefinition ExpectedInitializerStart { get; } = new(16,
        CompilerMessageCategory.Expression,
        $"Expected '{KGVL.OPEN_CURLY_BRACKET_IN_MESSAGE}' to start the list of values");

    internal virtual ErrorDefinition ExpectedInitializerEnd { get; } = new(17,
        CompilerMessageCategory.Expression,
        $"Expected '{KGVL.CLOSE_CURLY_BRACKET_IN_MESSAGE}' to end the list of values, or '{KGVL.COMMA}' to " +
        "continue it with another value");

    internal virtual ErrorDefinition ExpectedLambdaArrow { get; } = new(18,
        CompilerMessageCategory.Expression,
        $"Expected \"{KGVL.QUICK_METHOD_BODY}\" between an inline function's parameters and its body");

    internal virtual ErrorDefinition ExpectedSwitchExpressionEnd { get; } = new(19,
        CompilerMessageCategory.Expression,
        $"Expected '{KGVL.CLOSE_CURLY_BRACKET_IN_MESSAGE}' to end a switch value's branches, or '{KGVL.COMMA}' " +
        "to continue with another branch");

    internal virtual ErrorDefinition ExpectedSwitchArmArrow { get; } = new(20,
        CompilerMessageCategory.Expression,
        $"Expected \"{KGVL.QUICK_METHOD_BODY}\" between a switch value's pattern and its result");

    internal virtual ErrorDefinition ExpectedInterpolationSectionEnd { get; } = new(21,
        CompilerMessageCategory.Expression,
        $"Expected '{KGVL.INTERPOLATION_SECTION_END}' to close a substituted section of an " +
        $"interpolated string. To put a literal brace in the text, double it as " +
        $"\"{KGVL.DOUBLE_CURLY_OPEN}\" or \"{KGVL.DOUBLE_CURLY_CLOSE}\"");

    internal virtual ErrorDefinition ExpectedNumberValue { get; } = new(22,
        CompilerMessageCategory.Expression,
        "Expected a number");

    internal virtual ErrorDefinition ExpectedCharacterValue { get; } = new(23,
        CompilerMessageCategory.Expression,
        "Expected a character constant");

    internal virtual ErrorDefinition ExpectedStringStart { get; } = new(24,
        CompilerMessageCategory.Expression,
        $"Expected quote '{KGVL.DOUBLE_QUOTE}' to start a string");

    internal virtual ErrorDefinition ExpectedStringEnd { get; } = new(25,
        CompilerMessageCategory.Expression,
        $"Expected quote '{KGVL.DOUBLE_QUOTE}' to end a string");


    /* Return typed members: fields, properties, indexers, functions, constructors and operators. */
    internal virtual ErrorDefinition ExpectedFieldOrPropertyOrFunction { get; } = new(1,
        CompilerMessageCategory.ReturnTypedMembersCommon,
        $"Expected \"{{0}}\" to continue into a field, a property or a function. A field ends with " +
        $"'{KGVL.SEMICOLON}' or a starting value, a property has a " +
        $"'{KGVL.OPEN_CURLY_BRACKET_IN_MESSAGE}' block or a \"{KGVL.QUICK_METHOD_BODY}\" value, and a " +
        $"function has a '{KGVL.OPEN_PARENTHESIS}' parameter list");

    internal virtual ErrorDefinition ExpectedFunctionBody { get; } = new(2,
        CompilerMessageCategory.ReturnTypedMembersCommon,
        $"Expected a body starting with '{KGVL.OPEN_CURLY_BRACKET_IN_MESSAGE}', a single value after " +
        $"\"{KGVL.QUICK_METHOD_BODY}\", or '{KGVL.SEMICOLON}' for a member which deliberately has " +
        "no body");

    internal virtual ErrorDefinition ExpectedAccessorBlockStart { get; } = new(3,
        CompilerMessageCategory.ReturnTypedMembersCommon,
        $"Expected '{KGVL.OPEN_CURLY_BRACKET_IN_MESSAGE}' to start the accessors of a {{0}}");

    internal virtual ErrorDefinition ExpectedAccessorBlockEnd { get; } = new(4,
        CompilerMessageCategory.ReturnTypedMembersCommon,
        $"Expected '{KGVL.CLOSE_CURLY_BRACKET_IN_MESSAGE}' to end the accessors of a {{0}}");

    internal virtual ErrorDefinition ExpectedAccessor { get; } = new(5,
        CompilerMessageCategory.ReturnTypedMembersCommon,
        $"Expected \"{KGVL.KEYWORD_GET}\", \"{KGVL.KEYWORD_SET}\" or \"{KGVL.KEYWORD_INIT}\" " +
        "inside the accessors of a {0}");

    internal virtual ErrorDefinition VoidIndexer { get; } = new(6,
        CompilerMessageCategory.ReturnTypedMembersCommon,
        $"An indexer produces a value, so it cannot have the type \"{KGVL.KEYWORD_VOID}\"");

    internal virtual ErrorDefinition ExpectedOperatorKeyword { get; } = new(7,
        CompilerMessageCategory.ReturnTypedMembersCommon,
        $"Expected the keyword \"{KGVL.KEYWORD_OPERATOR}\" and the type to convert to, as in " +
        $"\"{KGVL.KEYWORD_IMPLICIT} {KGVL.KEYWORD_OPERATOR} SomeType(...)\"");

    internal virtual ErrorDefinition ExpectedConversionTargetType { get; } = new(8,
        CompilerMessageCategory.ReturnTypedMembersCommon,
        "Expected the type this conversion produces");

    internal virtual ErrorDefinition UnoverloadableOperator { get; } = new(9,
        CompilerMessageCategory.ReturnTypedMembersCommon,
        "This operator cannot be overloaded. The ones which can are " +
        "+, -, *, /, %, ++, --, ==, !=, >, <, >= and <=, along with implicit and explicit " +
        "conversions");

    internal virtual ErrorDefinition ExpectedParameterListEnd { get; } = new(11,
        CompilerMessageCategory.ReturnTypedMembersCommon,
        "Expected '{0}' to end the parameter list");

    internal virtual ErrorDefinition ExpectedConstructorChainTarget { get; } = new(10,
        CompilerMessageCategory.ReturnTypedMembersCommon,
        $"Expected \"{KGVL.KEYWORD_THIS}\" or \"{KGVL.KEYWORD_BASE}\" to name the constructor " +
        "which runs before this one");


    /* Statements. */
    internal virtual ErrorDefinition ExpectedStatementEnd { get; } = new(1,
        CompilerMessageCategory.Statement,
        "Expected '{0}' to end the statement");

    internal virtual ErrorDefinition ExpectedStatementBodyStart { get; } = new(2,
        CompilerMessageCategory.Statement,
        $"Expected '{KGVL.OPEN_CURLY_BRACKET_IN_MESSAGE}' to start a body of statements");

    internal virtual ErrorDefinition ExpectedStatementBodyEnd { get; } = new(3,
        CompilerMessageCategory.Statement,
        $"Expected '{KGVL.CLOSE_CURLY_BRACKET_IN_MESSAGE}' to end a body of statements");

    internal virtual ErrorDefinition ExpectedVariableName { get; } = new(4,
        CompilerMessageCategory.Statement,
        "Expected the name of the variable being declared");

    internal virtual ErrorDefinition ExpectedConditionStart { get; } = new(5,
        CompilerMessageCategory.Statement,
        $"Expected '{KGVL.OPEN_PARENTHESIS}' to start a condition");

    internal virtual ErrorDefinition ExpectedConditionEnd { get; } = new(6,
        CompilerMessageCategory.Statement,
        $"Expected '{KGVL.CLOSE_PARENTHESIS}' to end a condition");

    internal virtual ErrorDefinition ExpectedYieldContinuation { get; } = new(7,
        CompilerMessageCategory.Statement,
        $"Expected \"{KGVL.KEYWORD_RETURN}\" or \"{KGVL.KEYWORD_BREAK}\" after the " +
        $"\"{KGVL.KEYWORD_YIELD}\" keyword, as in \"{KGVL.KEYWORD_YIELD} {KGVL.KEYWORD_RETURN} " +
        $"value{KGVL.SEMICOLON}\" or \"{KGVL.KEYWORD_YIELD} {KGVL.KEYWORD_BREAK}{KGVL.SEMICOLON}\"");

    internal virtual ErrorDefinition ExpectedCatchClause { get; } = new(8,
        CompilerMessageCategory.Statement,
        $"A \"{KGVL.KEYWORD_TRY}\" statement needs at least one \"{KGVL.KEYWORD_CATCH}\" clause");

    internal virtual ErrorDefinition ExpectedCatchClauseStart { get; } = new(9,
        CompilerMessageCategory.Statement,
        $"Expected '{KGVL.OPEN_PARENTHESIS}' and the type of exception to catch");

    internal virtual ErrorDefinition ExpectedCaughtExceptionType { get; } = new(10,
        CompilerMessageCategory.Statement,
        "Expected the type of exception this clause catches");

    internal virtual ErrorDefinition ExpectedCatchClauseEnd { get; } = new(11,
        CompilerMessageCategory.Statement,
        $"Expected '{KGVL.CLOSE_PARENTHESIS}' to end a catch clause's exception");

    internal virtual ErrorDefinition ExpectedDoWhileCondition { get; } = new(12,
        CompilerMessageCategory.Statement,
        $"Expected the keyword \"{KGVL.KEYWORD_WHILE}\" and a condition after the body of a " +
        $"\"{KGVL.KEYWORD_DO}\" statement");

    internal virtual ErrorDefinition ExpectedForHeaderStart { get; } = new(13,
        CompilerMessageCategory.Statement,
        $"Expected '{KGVL.OPEN_PARENTHESIS}' to start a for loop's header");

    internal virtual ErrorDefinition ExpectedForEachHeaderStart { get; } = new(14,
        CompilerMessageCategory.Statement,
        $"Expected '{KGVL.OPEN_PARENTHESIS}' to start a foreach loop's header");

    internal virtual ErrorDefinition ExpectedForEachElementType { get; } = new(15,
        CompilerMessageCategory.Statement,
        $"Expected the type of a single element, or the keyword \"{KGVL.KEYWORD_VAR}\" to infer it");

    internal virtual ErrorDefinition ExpectedForEachElementName { get; } = new(16,
        CompilerMessageCategory.Statement,
        "Expected the name to give each element in turn");

    internal virtual ErrorDefinition ExpectedForEachInKeyword { get; } = new(17,
        CompilerMessageCategory.Statement,
        $"Expected the keyword \"{KGVL.KEYWORD_IN}\" between a foreach loop's element and the " +
        "collection it walks");

    internal virtual ErrorDefinition ExpectedForEachHeaderEnd { get; } = new(18,
        CompilerMessageCategory.Statement,
        $"Expected '{KGVL.CLOSE_PARENTHESIS}' to end a foreach loop's header");

    internal virtual ErrorDefinition ExpectedSwitchBodyStart { get; } = new(19,
        CompilerMessageCategory.Statement,
        $"Expected '{KGVL.OPEN_CURLY_BRACKET_IN_MESSAGE}' to start a switch statement's cases");

    internal virtual ErrorDefinition ExpectedSwitchBodyEnd { get; } = new(20,
        CompilerMessageCategory.Statement,
        $"Expected '{KGVL.CLOSE_CURLY_BRACKET_IN_MESSAGE}' to end a switch statement's cases");

    internal virtual ErrorDefinition ExpectedSwitchCaseColon { get; } = new(21,
        CompilerMessageCategory.Statement,
        $"Expected '{KGVL.COLON}' after a switch case's condition");

    internal virtual ErrorDefinition ExpectedSwitchCaseCondition { get; } = new(22,
        CompilerMessageCategory.Statement,
        $"Expected at least one condition for this case. A case matching anything else is written " +
        $"\"{KGVL.KEYWORD_DEFAULT}{KGVL.COLON}\" instead");

    internal virtual ErrorDefinition DuplicateDefaultCase { get; } = new(23,
        CompilerMessageCategory.Statement,
        $"This switch statement already has a \"{KGVL.KEYWORD_DEFAULT}\" case");

    internal virtual ErrorDefinition DefaultCaseWithConditions { get; } = new(24,
        CompilerMessageCategory.Statement,
        $"A \"{KGVL.KEYWORD_DEFAULT}\" case matches anything the other cases did not, so it cannot " +
        "have conditions of its own");


    /* Warnings. */
    internal virtual WarningDefinition DuplicateUsingDirective { get; } = new(1,
        WarningSeverity.Minor,
        $"The namespace \"{{0}}\" is already imported by an earlier \"{KGVL.KEYWORD_USING}\" directive " +
        "in this file, so this one does nothing and can be removed",
        CompilerMessageCategory.SourceFileRoot);
}