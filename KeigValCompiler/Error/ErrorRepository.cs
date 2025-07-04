﻿using System;
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
        $"Expected record \"{{1}}\" body start '{KGVL.DOUBLE_CURLY_OPEN}' or end '{KGVL.SEMICOLON}'");


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
}