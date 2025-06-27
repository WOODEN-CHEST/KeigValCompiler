using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace KeigValCompiler.Error;

internal class ErrorRepository
{
    // Fields.
    internal virtual ErrorDefinition RootExpectedKeyword { get; }  = new(1,
        $"In root scope of a source-code file, expected keyword \"{KGVL.KEYWORD_NAMESPACE}\" " +
        $"or \"{KGVL.KEYWORD_USING}\"");

    internal virtual ErrorDefinition RootNonActiveNamespace { get; } = new(2,
        "\"Unexpected keyword \"{0}\", " +
        "a working namespace needs to be set prior to member definitions");

    internal virtual ErrorDefinition RootInvalidNamespace { get; } = new(3,
        "Expected namespace name");

    internal virtual ErrorDefinition NamespaceEndOrContinuation { get; } = new(4,
        "Incomplete namespace \"{0}\". Expected namespace end " +
        $"'{KGVL.SEMICOLON}' or continuation '{KGVL.NAMESPACE_SEPARATOR}'");

    internal virtual ErrorDefinition ExpectedMemberModifierOrKeyword { get; } = new(5,
        "Expected member modifier or keyword");

    internal virtual ErrorDefinition ExpectedMember { get; } = new(6,
        "Expected {0} member");

    internal virtual ErrorDefinition DuplicateModifiers { get; } = new(7,
        "Duplicate member modifier \"{0}\"");

    internal virtual ErrorDefinition ReservedKeywordBuiltIn { get; } = new(8,
        $"The keyword \"{KGVL.KEYWORD_BUILTIN}\" is reserved for compiler internal use only");

    internal virtual ErrorDefinition ExpectedTypeMemberIdentifier { get; } = new(9,
        "Expected {0} identifier");

    internal virtual ErrorDefinition ExpectedReturnTypeIdentifier { get; } = new(10,
        "Expected {0} return type identifier");

    internal virtual ErrorDefinition ExpectedRecordPrimaryConstructorOrBody { get; } = new(11,
        $"Expected record primary constructor {{0}}{KGVL.OPEN_PARENTHESIS} ... {KGVL.CLOSE_PARENTHESIS} " +
        $"or record body {{0}} {KGVL.DOUBLE_CURLY_OPEN} ... {KGVL.DOUBLE_CURLY_CLOSE}");

    internal virtual ErrorDefinition EOFWhileParsingRecord { get; } = new(12,
        $"Expected record \"{{0}}\" body start '{KGVL.DOUBLE_CURLY_OPEN}', primary constructor " +
        $"({{0}}{KGVL.OPEN_PARENTHESIS} ... {KGVL.CLOSE_PARENTHESIS};) " +
        $"or member extension, or generic constraints ({{0}} {KGVL.KEYWORD_WHEN} T1 {KGVL.COLON} ... )");

    internal virtual ErrorDefinition EOFWhileParsingType { get; } = new(13,
        $"Expected {{0}} \"{{1}}\" body start '{KGVL.DOUBLE_CURLY_OPEN}' " +
        $"or member extension, or generic constraints ({{1}} {KGVL.KEYWORD_WHEN} T1 {KGVL.COLON} ... )");

    internal virtual ErrorDefinition EOFWhileParsingMembers { get; } = new(14,
        $"Expected {{0}} \"{{1}}\" body end '{KGVL.DOUBLE_CURLY_CLOSE}' or {{0}} member, got end of file");

    internal virtual ErrorDefinition ExpectedCurlyTypeEnd { get; } = new(15,
        $"Expected {{0}} \"{{1}}\" end '{KGVL.DOUBLE_CURLY_CLOSE}'");

    internal virtual ErrorDefinition ExpectedDelegateParamList { get; } = new(16,
        $"Expected delegate \"{{0}}\" parameter list {KGVL.OPEN_PARENTHESIS} ... {KGVL.CLOSE_PARENTHESIS}");

    internal virtual ErrorDefinition ExpectedDelegateParamListEnd { get; } = new(17,
        $"Expected delegate \"{{0}}\" parameter list end '{KGVL.CLOSE_PARENTHESIS}'");

    internal virtual ErrorDefinition ExpectedDelegateDefinitionEnd { get; } = new(18,
        $"Expected delegate \"{{0}}\" definition end '{KGVL.SEMICOLON}'");

    internal virtual ErrorDefinition CannotHoldMemberInNamespace { get; } = new(19,
        "A namespace \"{0}\" cannot hold a member of type {1}");

    internal virtual ErrorDefinition CannotHoldMemberInMember { get; } = new(20,
        "A member of type {0} \"{1}\" cannot hold a member of type {2}");

    internal virtual ErrorDefinition CannotHoldMemberInUnknown { get; } = new(21,
       "The parent member cannot hold a member of type {0}");

    internal virtual ErrorDefinition ExpectedMemberExtensionOrBodyOrConstraints { get; } = new(22,
        $"Expected member \"{{0}}\" body '{KGVL.DOUBLE_CURLY_OPEN}' " +
        $"or member extension ({{0}} {KGVL.COLON} T1{KGVL.COMMA} T2{KGVL.COMMA} ... Tn), " +
        $"or member generic type constraints ({{0}} {KGVL.KEYWORD_WHEN} {KGVL.COLON} ... )");

    internal virtual ErrorDefinition ExpectedMemberExtension { get; } = new(23,
        "Expected extended member identifier for member \"{0}\"");

    internal virtual ErrorDefinition ExpectedParametersOrEnd { get; } = new(24,
        $"Expected member \"{{0}}\" function parameter list " +
        $"{KGVL.OPEN_PARENTHESIS}T1 a, T2 b, ... {KGVL.CLOSE_PARENTHESIS} " +
        $"or parameter list end '{KGVL.CLOSE_PARENTHESIS}'");

    internal virtual ErrorDefinition ExpectedParameterTypeOrModifier { get; } = new(25,
        $"Expected member \"{{0}}\" function parameter type identifier or parameter modifier " +
        $"(\"{KGVL.KEYWORD_IN}\", \"{KGVL.KEYWORD_OUT}\" or \"{KGVL.KEYWORD_REF}\")");

    internal virtual ErrorDefinition ExpectedParameterType { get; } = new(26,
        "Expected member \"{0}\" function parameter type identifier");

    internal virtual ErrorDefinition ExpectedParameterIdentifier { get; } = new(27,
        "Expected member \"{0}\" function parameter identifier");

    internal virtual ErrorDefinition EnumConstantOutOfRange { get; } = new(28,
        "Enum constant \"{0}\" in enum type " +
        "\"{1}\" is out of the valid range " +
        $"{int.MinValue} to {int.MaxValue}, it has a value of {{2}}");

    internal virtual ErrorDefinition ExpectedEnumBodyStart { get; } = new(29,
        $"Expected enum body start '{KGVL.DOUBLE_CURLY_OPEN}' for enum \"{{0}}\"");

    internal virtual ErrorDefinition ExpectedEnumBodyEnd { get; } = new(30,
        $"Expected enum body end '{KGVL.DOUBLE_CURLY_CLOSE}' for enum \"{{0}}\"");

    internal virtual ErrorDefinition ExpectedEnumConstant { get; } = new(31,
        $"Expected enum \"{{0}}\" constant identifier");

    internal virtual ErrorDefinition ExpectedEnumConstantOrEnd { get; } = new(32,
        $"Expected enum \"{{0}}\" body end '{KGVL.DOUBLE_CURLY_CLOSE}' or an enum constant");

    internal virtual ErrorDefinition ExpectedEnumAssignmentOrNextOrEnd { get; } = new(33,
        $"Expected value assignment for constant \"{{0}}\", or enum \"{{1}}\" body end " +
        $"'{KGVL.DOUBLE_CURLY_CLOSE}' or comma '{KGVL.COMMA}' followed by the next enum constant");

    internal virtual ErrorDefinition ExpectedEnumConstantValue { get; } = new(34,
        "Expected enum constant \"{0}\" value");

    internal virtual ErrorDefinition ExpectedMultiLineCommentEnd { get; } = new(35,
        "Multi-line comment started on line {0} wasn't terminated properly");

    internal virtual ErrorDefinition ExpectedGenericTypeConstraint { get; } = new(36,
        "Expected identifier of a generic type parameter to apply constraints to " +
        "for the type \"{0}\"");

    internal virtual ErrorDefinition GenericParameterNotFound { get; } = new(37,
        "No generic parameter with the name \"{0}\" was found for the type \"{1}\" " +
        "as listed in the generic type parameter constraint.");

    internal virtual ErrorDefinition GenericParameterConstraintStartNotFound { get; } = new(38,
        $"Expected '{KGVL.COLON}' to denote the start of constraints for the " +
        "type parameter \"{0}\" in member \"{1}\"");

    internal virtual ErrorDefinition ExpectedGenericConstraintIdentifier { get; } = new(39,
        "Expected generic constraint value (identifier or special constraint) for the " +
        "type parameter \"{0}\"");

    internal virtual ErrorDefinition ExpectedGenericParameterEnd { get; } = new(40,
        $"Expected generic parameter list end '{KGVL.GENERIC_TYPE_END}' for member \"{{0}}\"");

    internal virtual ErrorDefinition ExpectedGenericParameterIdentifier { get; } = new(41,
        "Expected generic parameter identifier for member \"{0}\"");

    internal virtual ErrorDefinition ExpectedGenericParameterCommaOrEnd { get; } = new(42,
        $"Expected comma '{KGVL.COMMA}' for next generic parameter identifier " +
        $"or generic parameter list end '{KGVL.GENERIC_TYPE_END}' for member \"{{0}}\"");

    internal virtual ErrorDefinition ExpectedMemberBodyStart { get; } = new(43,
        $"Expected member \"{{0}}\" body start '{KGVL.DOUBLE_CURLY_OPEN}'");

    internal virtual ErrorDefinition ExpectedGenericParmsOrExtension { get; } = new(44,
        "Expected member \"{0}\" generic parameters " +
        $"({{0}}{KGVL.GENERIC_TYPE_START}T1, T2 ... Tn{KGVL.GENERIC_TYPE_END}) " +
        $"or member extension ({{0}} {KGVL.COLON} T1, T2 ... Tn)");

    internal virtual ErrorDefinition ExpectedEventDelegateType { get; } = new(45,
        "Expected event delegate type identifier.");

    internal virtual ErrorDefinition ExpectedEventEnd { get; } = new(45,
        $"Expected event \"{{0}}\" definition end '{KGVL.SEMICOLON}'");
}