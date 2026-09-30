using KeigValCompiler.Main.Commandline;
using KeigValCompiler.Semantician;
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

    internal virtual ErrorDefinition UsingUnknownNamespace { get; } = new(10,
        CompilerMessageCategory.SourceFileRoot,
        $"The namespace \"{{0}}\" named by this \"{KGVL.KEYWORD_USING}\" directive does not exist. No file " +
        "declares it, nor any namespace inside it");


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

    internal virtual ErrorDefinition InterfaceInstanceField { get; } = new(6,
        CompilerMessageCategory.MemberGeneric,
        "The interface \"{0}\" cannot hold the instance field \"{1}\". An interface holds no data of its own, " +
        "so its only fields can be static ones and constants");

    internal virtual ErrorDefinition InterfaceInstanceConstructor { get; } = new(7,
        CompilerMessageCategory.MemberGeneric,
        "The interface \"{0}\" cannot have an instance constructor. An interface is never created itself, only " +
        "the types implementing it are, so the only constructor it can have is a static one");

    internal virtual ErrorDefinition StaticClassInstanceMember { get; } = new(8,
        CompilerMessageCategory.MemberGeneric,
        "The static class \"{0}\" cannot hold the {1} \"{2}\". A static class is never created, so everything " +
        "it holds has to be static, and it cannot have operators or indexers, which work on values of their " +
        "type");

    internal virtual ErrorDefinition ReadonlyStructMutableMember { get; } = new(9,
        CompilerMessageCategory.MemberGeneric,
        $"The readonly structure \"{{0}}\" cannot hold the {{1}} \"{{2}}\", which could be changed after the " +
        $"structure is created. Each instance field of a readonly structure has to be readonly too, a " +
        $"property which stores its own value cannot have a \"{KGVL.KEYWORD_SET}\" accessor without a body, " +
        "and an instance event cannot be held at all, since subscribing to it changes it");

    internal virtual ErrorDefinition NamespaceExplicitImplementation { get; } = new(10,
        CompilerMessageCategory.MemberGeneric,
        "The namespace \"{0}\" cannot hold the {1} \"{2}\", which implements a member of \"{3}\" explicitly. " +
        "Only a type can implement an interface");

    internal virtual ErrorDefinition ConstantTypeNotConstant { get; } = new(11,
        CompilerMessageCategory.MemberGeneric,
        $"The constant \"{{0}}\" has the type \"{{1}}\", whose values cannot be fixed when the code is " +
        $"compiled. A constant is a number, a char, a bool, a string, an enum's value, or " +
        $"\"{KGVL.KEYWORD_NULL}\" for any other reference type");

    internal virtual ErrorDefinition StructContainsItself { get; } = new(12,
        CompilerMessageCategory.MemberGeneric,
        "The {0} \"{1}\" of the structure \"{2}\" has the type \"{3}\", so \"{2}\" would hold itself by value, " +
        "through \"{4}\". A structure holds the values of its fields, and of the properties storing their own, " +
        "directly, so one holding itself would have no end");


    /* Member modifier. */
    internal virtual ErrorDefinition DuplicateModifiers { get; } = new(1,
        CompilerMessageCategory.MemberModifier,
        "Duplicate member modifier \"{0}\"");

    internal virtual ErrorDefinition ReservedKeywordBuiltIn { get; } = new(2,
        CompilerMessageCategory.MemberModifier,
        $"The modifier \"{KGVL.KEYWORD_BUILTIN}\" is reserved for compiler internal use only. It marks what " +
        "the compiler implements itself, so only the standard library's own files can use it");

    internal virtual ErrorDefinition ModifierNotAllowed { get; } = new(3,
        CompilerMessageCategory.MemberModifier,
        "The modifier \"{0}\" cannot be used on the {1} \"{2}\". Declared in the {3} \"{4}\", it can only be " +
        "marked {5}");

    internal virtual ErrorDefinition MultipleAccessModifiers { get; } = new(4,
        CompilerMessageCategory.MemberModifier,
        $"The {{0}} \"{{1}}\" has more than one access modifier. A member has one of " +
        $"\"{KGVL.KEYWORD_PUBLIC}\", \"{KGVL.KEYWORD_PROTECTED}\", \"{KGVL.KEYWORD_INTERNAL}\" and " +
        $"\"{KGVL.KEYWORD_PRIVATE}\", or one of " +
        $"the pairs \"{KGVL.KEYWORD_PROTECTED} {KGVL.KEYWORD_INTERNAL}\" and " +
        $"\"{KGVL.KEYWORD_PRIVATE} {KGVL.KEYWORD_PROTECTED}\"");

    internal virtual ErrorDefinition ClassModifierConflict { get; } = new(5,
        CompilerMessageCategory.MemberModifier,
        "The class \"{0}\" cannot be both \"{1}\" and \"{2}\". An abstract class is made to be derived from, " +
        "which a sealed class cannot be, and a static class is as good as both already, since it can be " +
        "neither created nor derived from, so it says neither");

    internal virtual ErrorDefinition AbstractVirtualConflict { get; } = new(6,
        CompilerMessageCategory.MemberModifier,
        $"The {{0}} \"{{1}}\" cannot be both \"{KGVL.KEYWORD_ABSTRACT}\" and \"{KGVL.KEYWORD_VIRTUAL}\". An " +
        "abstract member is virtual already, and has no body to be the default a derived type could keep");

    internal virtual ErrorDefinition OverrideConflict { get; } = new(7,
        CompilerMessageCategory.MemberModifier,
        $"The {{0}} \"{{1}}\" cannot be both \"{KGVL.KEYWORD_OVERRIDE}\" and \"{{2}}\". An override is virtual " +
        "already, since what it overrides is, and it replaces the inherited member rather than hiding it");

    internal virtual ErrorDefinition SealedWithoutOverride { get; } = new(8,
        CompilerMessageCategory.MemberModifier,
        $"The {{0}} \"{{1}}\" is \"{KGVL.KEYWORD_SEALED}\" but not \"{KGVL.KEYWORD_OVERRIDE}\". Sealing a " +
        "member stops the types deriving from its own from overriding it, so only a member which overrides an " +
        "inherited one can be sealed");

    internal virtual ErrorDefinition AbstractSealedConflict { get; } = new(9,
        CompilerMessageCategory.MemberModifier,
        $"The {{0}} \"{{1}}\" cannot be both \"{KGVL.KEYWORD_ABSTRACT}\" and \"{KGVL.KEYWORD_SEALED}\". An " +
        "abstract member has to be overridden, and a sealed one cannot be");

    internal virtual ErrorDefinition StaticVirtualConflict { get; } = new(10,
        CompilerMessageCategory.MemberModifier,
        "The static {0} \"{1}\" cannot be \"{2}\". Overriding goes through an object, and a static member " +
        "belongs to none, so only an interface's static members can be abstract or virtual, to be supplied by " +
        "the types implementing it");

    internal virtual ErrorDefinition ConstModifierConflict { get; } = new(11,
        CompilerMessageCategory.MemberModifier,
        "The constant \"{0}\" cannot also be \"{1}\". A constant's value is fixed when the code is compiled, " +
        "so it is static and read-only already, and nothing can set it");

    internal virtual ErrorDefinition PrivateVirtualConflict { get; } = new(12,
        CompilerMessageCategory.MemberModifier,
        "The {0} \"{1}\" is private, so it cannot be \"{2}\". Nothing outside its type can see a private " +
        "member, so nothing could override or implement it");

    internal virtual ErrorDefinition RequiredStatic { get; } = new(13,
        CompilerMessageCategory.MemberModifier,
        "The required {0} \"{1}\" cannot be static. A required member is given its value where each object is " +
        "created, so it has to belong to the object");

    internal virtual ErrorDefinition BuiltInAbstractConflict { get; } = new(14,
        CompilerMessageCategory.MemberModifier,
        $"The {{0}} \"{{1}}\" cannot be both \"{KGVL.KEYWORD_BUILTIN}\" and \"{KGVL.KEYWORD_ABSTRACT}\". The " +
        "compiler supplies a builtin member's body, and an abstract member has none");

    internal virtual ErrorDefinition StaticConstructorAccessModifier { get; } = new(15,
        CompilerMessageCategory.MemberModifier,
        "The static constructor of \"{0}\" cannot have an access modifier. Nothing calls a static constructor: " +
        "it runs by itself, before its type is first used");

    internal virtual ErrorDefinition ReadonlyStaticConflict { get; } = new(16,
        CompilerMessageCategory.MemberModifier,
        $"The static {{0}} \"{{1}}\" cannot be \"{KGVL.KEYWORD_READONLY}\". A readonly member promises not to " +
        "change the structure it is used on, and a static member is used on none");

    internal virtual ErrorDefinition ExplicitImplementationModifier { get; } = new(17,
        CompilerMessageCategory.MemberModifier,
        "The {0} \"{1}\" implements a member of \"{2}\" explicitly, and cannot be marked \"{3}\": as an " +
        "explicit implementation of it, it can only be marked {4}");

    internal virtual ErrorDefinition AbstractMemberInConcreteClass { get; } = new(18,
        CompilerMessageCategory.MemberModifier,
        "The {0} \"{1}\" is abstract, but the class \"{2}\" holding it is not. Only an abstract class can have " +
        "abstract members, since an object of any other class can be created, and its members need bodies");

    internal virtual ErrorDefinition VirtualMemberInSealedClass { get; } = new(19,
        CompilerMessageCategory.MemberModifier,
        "The {0} \"{1}\" is virtual, but the class \"{2}\" holding it is sealed, so no class can derive from " +
        "it to override the member");

    internal virtual ErrorDefinition RequiredNotSettable { get; } = new(20,
        CompilerMessageCategory.MemberModifier,
        $"The required {{0}} \"{{1}}\" cannot be set once its object is created: a readonly field can only be " +
        $"set by constructors, and a property needs a \"{KGVL.KEYWORD_SET}\" or \"{KGVL.KEYWORD_INIT}\" " +
        "accessor. A required member is given its value where each object is created, so it has to be settable");

    internal virtual ErrorDefinition ExplicitImplementationWithoutModifiers { get; } = new(21,
        CompilerMessageCategory.MemberModifier,
        "The {0} \"{1}\" implements a member of \"{2}\" explicitly, and cannot be marked \"{3}\": as an " +
        "explicit implementation of it, it can have no modifiers at all");

    internal virtual ErrorDefinition InterfaceSealedConflict { get; } = new(22,
        CompilerMessageCategory.MemberModifier,
        $"The {{0}} \"{{1}}\" cannot be both \"{KGVL.KEYWORD_SEALED}\" and \"{{2}}\". In an interface, " +
        $"\"{KGVL.KEYWORD_SEALED}\" says that a member's body is the only one, which the types implementing " +
        "the interface cannot replace, and a virtual member is made to be replaced, while a private one could " +
        "not be anyway");

    internal virtual ErrorDefinition PrivateAccessorOfAbstract { get; } = new(23,
        CompilerMessageCategory.MemberModifier,
        "The accessor \"{0}\" is private, but the {1} \"{2}\" it belongs to is abstract, so whatever supplies " +
        "the {1} could never see the accessor to supply it");

    internal virtual ErrorDefinition ReadonlyAutoSetter { get; } = new(24,
        CompilerMessageCategory.MemberModifier,
        $"The {{0}} \"{{1}}\" cannot be readonly, since a \"{KGVL.KEYWORD_SET}\" accessor without a body sets " +
        $"the value the property stores, which changes the structure. A readonly property's stored value can " +
        $"only be set by an \"{KGVL.KEYWORD_INIT}\" accessor, while the structure is being created");

    internal virtual ErrorDefinition ReadonlyInitAccessor { get; } = new(25,
        CompilerMessageCategory.MemberModifier,
        $"The accessor \"{{0}}\" cannot be \"{KGVL.KEYWORD_READONLY}\". An \"{KGVL.KEYWORD_INIT}\" accessor " +
        "sets its property or indexer while its structure is being created, which is what readonly means not " +
        "to do");

    internal virtual ErrorDefinition ReadonlyAccessorMisplaced { get; } = new(26,
        CompilerMessageCategory.MemberModifier,
        $"\"{KGVL.KEYWORD_READONLY}\" belongs on the {{1}} \"{{2}}\" rather than on its accessor \"{{0}}\". An " +
        "accessor is marked readonly only when what it belongs to is not, and has two accessors of which the " +
        "other is not readonly");

    internal virtual ErrorDefinition AccessorAccessNotNarrower { get; } = new(27,
        CompilerMessageCategory.MemberModifier,
        "The accessor \"{0}\" is {1}, which is not narrower than the {2} \"{3}\" it belongs to, which is {4}. " +
        $"An accessor's own access only ever narrows its member's; \"{KGVL.KEYWORD_PROTECTED}\" and " +
        $"\"{KGVL.KEYWORD_INTERNAL}\" are each narrower than \"{KGVL.KEYWORD_PROTECTED} {KGVL.KEYWORD_INTERNAL}\", " +
        "but not than each other");

    internal virtual ErrorDefinition AccessorAccessOnBoth { get; } = new(28,
        CompilerMessageCategory.MemberModifier,
        "The {0} \"{1}\" has an access modifier on both of its accessors. Only one of them can narrow the " +
        "member's access; the other has the member's own");

    internal virtual ErrorDefinition AccessorAccessWithoutOther { get; } = new(29,
        CompilerMessageCategory.MemberModifier,
        "The accessor \"{0}\" has an access modifier, but the {1} \"{2}\" has no other accessor. An accessor " +
        "narrows its member's access only beside another accessor which has the member's own");


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

    internal virtual ErrorDefinition EventTypeNotDelegate { get; } = new(3,
        CompilerMessageCategory.Event,
        "The event \"{0}\" has the type \"{1}\", which is not a delegate. An event holds the functions " +
        "subscribed to it, so its type has to be a delegate describing them");

    internal virtual ErrorDefinition InterfaceEventNotAbstract { get; } = new(4,
        CompilerMessageCategory.Event,
        "The event \"{0}\" of the interface \"{1}\" cannot be \"{2}\". An interface's instance event can only " +
        "be abstract, since it could only have a body in accessors of its own, and an event has none");


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

    internal virtual ErrorDefinition DuplicateEnumConstant { get; } = new(9,
        CompilerMessageCategory.Enum,
        "The enum \"{0}\" already has a constant named \"{1}\", on line {2}");


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

    internal virtual ErrorDefinition RecordBaseNotRecord { get; } = new(6,
        CompilerMessageCategory.Record,
        $"The record \"{{0}}\" cannot derive from \"{{1}}\", which is not a record. A record can derive only " +
        $"from another record, or from \"{KGVL.KEYWORD_OBJECT}\"");

    internal virtual ErrorDefinition NonRecordBaseRecord { get; } = new(7,
        CompilerMessageCategory.Record,
        "The class \"{0}\" cannot derive from the record \"{1}\". Only a record can derive from a record");

    internal virtual ErrorDefinition RecordMemberMismatch { get; } = new(8,
        CompilerMessageCategory.Record,
        "The record \"{0}\" keeps its positional parameter \"{1}\" in the member of that name, but the {2} " +
        "\"{1}\" of \"{3}\" is not a readable instance property or field of the parameter's type, \"{4}\". A " +
        "record keeps each positional parameter in a property made for it, unless it declares a property or " +
        "field of that name, or inherits a member of that name, which then has to keep the value");

    internal virtual ErrorDefinition HiddenPositionalMember { get; } = new(9,
        CompilerMessageCategory.Record,
        "The record \"{0}\" keeps its positional parameter \"{1}\" in the {2} \"{1}\" it inherits from \"{3}\", " +
        "but a member of its own of that name hides it. Rename the member, or the parameter");

    internal virtual ErrorDefinition RecordParameterByReference { get; } = new(10,
        CompilerMessageCategory.Record,
        "The positional parameter \"{0}\" of the record \"{1}\" cannot be passed by \"{2}\". A record keeps its " +
        "positional parameters' values in its properties, so they are passed as values");

    internal virtual ErrorDefinition BaseArgumentsWithoutParameterList { get; } = new(11,
        CompilerMessageCategory.Record,
        "The {0} \"{1}\" passes arguments to \"{2}\" in its base list, which only a record with a parameter " +
        "list can do: the primary constructor that list declares passes them on to the base record's " +
        $"constructor. A constructor written in the type passes its base's arguments with \"{KGVL.COLON} " +
        $"{KGVL.KEYWORD_BASE}{KGVL.OPEN_PARENTHESIS}...{KGVL.CLOSE_PARENTHESIS}\" instead");

    internal virtual ErrorDefinition BaseArgumentsNotFirst { get; } = new(12,
        CompilerMessageCategory.Record,
        "The record \"{0}\" passes arguments to \"{1}\", which is not first in its base list. Only a " +
        "record's base record takes arguments, and it is written first, before any interface");

    internal virtual ErrorDefinition BaseArgumentsToInterface { get; } = new(13,
        CompilerMessageCategory.Record,
        "The record \"{0}\" passes arguments to the interface \"{1}\". Only a base record's constructor " +
        "can be given arguments, and an interface has no constructor");

    internal virtual ErrorDefinition BaseArgumentsWithoutBaseClass { get; } = new(14,
        CompilerMessageCategory.Record,
        "The {0} \"{1}\" passes arguments to \"{2}\" in its base list, but it derives from no class, so " +
        "it has no base constructor to give them to. Only a record with a parameter list passes arguments in " +
        "its base list, to its base record's constructor");

    internal virtual ErrorDefinition RecordConstructorWithoutThis { get; } = new(15,
        CompilerMessageCategory.Record,
        "A constructor of the record \"{0}\" has to run another of the record's constructors first, with " +
        $"\"{KGVL.COLON} {KGVL.KEYWORD_THIS}{KGVL.OPEN_PARENTHESIS}...{KGVL.CLOSE_PARENTHESIS}\". A record " +
        "with a parameter list is created through its primary constructor, which every other constructor has " +
        "to lead to. Only its copy constructor, which takes a \"{0}\", and a static constructor need not");


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

    internal virtual ErrorDefinition DuplicateConstraintClause { get; } = new(10,
        CompilerMessageCategory.Generics,
        $"The generic parameter \"{{0}}\" of the {{1}} \"{{2}}\" already has a \"{KGVL.KEYWORD_WHERE}\" " +
        $"clause, so this one is not used. All of a parameter's constraints go in one clause, as in " +
        $"\"{KGVL.KEYWORD_WHERE} {{0}} {KGVL.COLON} {KGVL.KEYWORD_CLASS}{KGVL.COMMA} SomeInterface\"");

    internal virtual ErrorDefinition DuplicateGenericParameterName { get; } = new(11,
        CompilerMessageCategory.Generics,
        "The {0} \"{1}\" has more than one generic parameter named \"{2}\"");

    internal virtual ErrorDefinition GenericParameterNamedLikeHolder { get; } = new(12,
        CompilerMessageCategory.Generics,
        "The generic parameter \"{0}\" has the same name as the {1} declaring it");

    internal virtual ErrorDefinition SpecialConstraintNotFirst { get; } = new(13,
        CompilerMessageCategory.Generics,
        $"The constraint \"{{0}}\" of the generic parameter \"{{1}}\" has to be its first, and its only " +
        $"one of \"{KGVL.KEYWORD_CLASS}\", \"{KGVL.KEYWORD_STRUCT}\" and \"{KGVL.KEYWORD_NOTNULL}\"");

    internal virtual ErrorDefinition ClassConstraintNotFirst { get; } = new(14,
        CompilerMessageCategory.Generics,
        $"The class \"{{0}}\" constrains the generic parameter \"{{1}}\", so it has to be its only class " +
        $"constraint, and come before its other constraints but \"{KGVL.KEYWORD_CLASS}\", " +
        $"\"{KGVL.KEYWORD_STRUCT}\" or \"{KGVL.KEYWORD_NOTNULL}\"");

    internal virtual ErrorDefinition InvalidConstraintType { get; } = new(15,
        CompilerMessageCategory.Generics,
        $"\"{{0}}\" cannot constrain the generic parameter \"{{1}}\". A constraint is an interface, another " +
        $"generic parameter, or a class which is neither sealed, static nor \"{KGVL.KEYWORD_OBJECT}\", since " +
        "only those leave more than one type able to satisfy it");

    internal virtual ErrorDefinition DuplicateConstraint { get; } = new(16,
        CompilerMessageCategory.Generics,
        "The generic parameter \"{0}\" is constrained to \"{1}\" more than once");

    internal virtual ErrorDefinition CircularConstraint { get; } = new(17,
        CompilerMessageCategory.Generics,
        "The generic parameter \"{0}\" is constrained to itself, as in \"{1}\"");

    internal virtual ErrorDefinition ClassConstraintWithSpecial { get; } = new(18,
        CompilerMessageCategory.Generics,
        "The generic parameter \"{0}\" is constrained to the class \"{1}\", so it cannot also be " +
        "constrained to \"{2}\": the class decides already what kind of type it is");

    internal virtual ErrorDefinition ConstraintNotSatisfied { get; } = new(19,
        CompilerMessageCategory.Generics,
        "\"{0}\" cannot stand for the generic parameter \"{1}\" in \"{2}\", since it does not satisfy the " +
        "constraint \"{3}\" of \"{1}\"");

    internal virtual ErrorDefinition OverrideRestatesConstraint { get; } = new(20,
        CompilerMessageCategory.Generics,
        $"The generic parameter \"{{0}}\" of \"{{1}}\" cannot be constrained to \"{{2}}\". A function which " +
        "overrides another or implements one explicitly takes its generic parameters' constraints from it, " +
        $"so it can only say \"{KGVL.KEYWORD_CLASS}\" or \"{KGVL.KEYWORD_STRUCT}\", to tell what a " +
        $"'{KGVL.TYPE_NULLABLE_INDICATOR}' on one of them means");

    internal virtual ErrorDefinition RestatedClassConstraint { get; } = new(21,
        CompilerMessageCategory.Generics,
        $"The generic parameter \"{{0}}\" of \"{{1}}\" says \"{KGVL.KEYWORD_CLASS}\", but the one of " +
        "\"{2}\" in \"{3}\", which it takes its constraints from, is not constrained to be a reference type");

    internal virtual ErrorDefinition RestatedStructConstraint { get; } = new(22,
        CompilerMessageCategory.Generics,
        $"The generic parameter \"{{0}}\" of \"{{1}}\" says \"{KGVL.KEYWORD_STRUCT}\", but the one of " +
        "\"{2}\" in \"{3}\", which it takes its constraints from, is not constrained to be a value type which " +
        "is not nullable");

    internal virtual ErrorDefinition StructParameterAsConstraint { get; } = new(23,
        CompilerMessageCategory.Generics,
        $"The generic parameter \"{{0}}\" cannot be constrained to \"{{1}}\", which is constrained to " +
        $"\"{KGVL.KEYWORD_STRUCT}\": nothing derives from a value type, so only \"{{1}}\" itself could " +
        "satisfy it");

    internal virtual ErrorDefinition ConflictingClassConstraints { get; } = new(24,
        CompilerMessageCategory.Generics,
        "The generic parameter \"{0}\" is constrained to both the class \"{1}\" and the class \"{2}\", through " +
        "the generic parameters it is constrained to, and neither derives from the other, so no type could " +
        "satisfy both");

    internal virtual ErrorDefinition StaticClassAsTypeArgument { get; } = new(25,
        CompilerMessageCategory.Generics,
        "The {0} \"{1}\" gives the static class \"{2}\" as a type argument, in \"{3}\". A static class has no " +
        "instances, so no generic parameter can stand for it");


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

    internal virtual ErrorDefinition DuplicateParameterName { get; } = new(6,
        CompilerMessageCategory.Function,
        "The {0} \"{1}\" has more than one parameter named \"{2}\"");

    internal virtual ErrorDefinition StaticConstructorParameters { get; } = new(7,
        CompilerMessageCategory.Function,
        "The static constructor of \"{0}\" cannot have parameters. Nothing calls a static constructor to give " +
        "it arguments: it runs by itself, before its type is first used");

    internal virtual ErrorDefinition ParameterNamedLikeGenericParameter { get; } = new(8,
        CompilerMessageCategory.Function,
        "The parameter \"{0}\" of the {1} \"{2}\" has the same name as one of its generic parameters");

    internal virtual ErrorDefinition ParamsNotLast { get; } = new(9,
        CompilerMessageCategory.Function,
        $"The \"{KGVL.KEYWORD_PARAMS}\" parameter \"{{0}}\" of the {{1}} \"{{2}}\" is not its last parameter. " +
        $"A \"{KGVL.KEYWORD_PARAMS}\" parameter takes every argument left over at the end of a call, so " +
        "nothing can come after it");

    internal virtual ErrorDefinition RequiredAfterOptional { get; } = new(10,
        CompilerMessageCategory.Function,
        "The parameter \"{0}\" of the {1} \"{2}\" has no default value, but comes after \"{3}\", which has " +
        "one. An argument can only be left out at the end of a call, so a parameter with a default value is " +
        $"followed only by others with one, and by a \"{KGVL.KEYWORD_PARAMS}\" parameter");

    internal virtual ErrorDefinition DefaultValueByReference { get; } = new(11,
        CompilerMessageCategory.Function,
        "The parameter \"{0}\" of the {1} \"{2}\" cannot have a default value, since it is passed by " +
        "\"{3}\". It has to name a variable, which a value written in the declaration is not");

    internal virtual ErrorDefinition DefaultValueOnParams { get; } = new(12,
        CompilerMessageCategory.Function,
        $"The \"{KGVL.KEYWORD_PARAMS}\" parameter \"{{0}}\" of the {{1}} \"{{2}}\" cannot have a default " +
        $"value. A call giving it no arguments already passes it an empty array");

    internal virtual ErrorDefinition ParamsNotArray { get; } = new(13,
        CompilerMessageCategory.Function,
        $"The \"{KGVL.KEYWORD_PARAMS}\" parameter \"{{0}}\" of the {{1}} \"{{2}}\" has the type \"{{3}}\", " +
        $"which is not an array. A \"{KGVL.KEYWORD_PARAMS}\" parameter gathers the arguments left over at the " +
        "end of a call into an array, so its type is written as one, as in " +
        $"\"{KGVL.KEYWORD_PARAMS} {KGVL.KEYWORD_INT}" +
        $"{KGVL.OPEN_SQUARE_BRACKET}{KGVL.CLOSE_SQUARE_BRACKET} values\"");


    /* Literals. */
    internal virtual ErrorDefinition InvalidHexEscapeSequence { get; } = new(1,
        CompilerMessageCategory.Literal,
        $"The escape sequence \"{KGVL.ESCAPE_CHAR}{{0}}\" is not a valid hexadecimal character code. " +
        $"A hexadecimal escape sequence is the prefix '{KGVL.ESCAPE_SEQUENCE_HEX_INDICATOR}' followed by " +
        "one to four hexadecimal digits (0-9 and a-f)");

    internal virtual ErrorDefinition UnknownEscapeSequence { get; } = new(2,
        CompilerMessageCategory.Literal,
        $"Unknown escape sequence \"{KGVL.ESCAPE_CHAR}{{0}}\". An escape sequence is the character " +
        $"'{KGVL.ESCAPE_CHAR}' followed by one of 0, a, b, e, f, n, r, t, v, ', \" or {KGVL.ESCAPE_CHAR}, by " +
        $"the prefix '{KGVL.ESCAPE_SEQUENCE_HEX_INDICATOR}' and one to four hexadecimal digits, by the " +
        $"prefix '{KGVL.ESCAPE_SEQUENCE_CODEPOINT_INDICATOR}' and exactly four, or by the prefix " +
        $"'{KGVL.ESCAPE_SEQUENCE_LONG_CODEPOINT_INDICATOR}' and exactly eight");

    internal virtual ErrorDefinition DecimalMissingDigits { get; } = new(3,
        CompilerMessageCategory.Literal,
        "The decimal number \"{0}\" has no digits. A decimal needs at least one digit, either before " +
        $"or after its point '{KGVL.DECIMAL_SEPARATOR}'");

    internal virtual ErrorDefinition DecimalMultipleSeparators { get; } = new(4,
        CompilerMessageCategory.Literal,
        $"The decimal number \"{{0}}\" has more than one point '{KGVL.DECIMAL_SEPARATOR}'. A decimal has " +
        "at most one, between its whole part and its fraction");

    internal virtual ErrorDefinition DecimalMissingExponentDigits { get; } = new(5,
        CompilerMessageCategory.Literal,
        $"The exponent of the decimal number \"{{0}}\" has no digits. An exponent is " +
        $"'{KGVL.DECIMAL_EXPONENT}', then optionally '{KGVL.DECIMAL_EXPONENT_POSITIVE_SIGN}' or " +
        $"'{KGVL.DECIMAL_EXPONENT_NEGATIVE_SIGN}', then at least one digit");

    internal virtual ErrorDefinition DecimalUnexpectedCharacter { get; } = new(6,
        CompilerMessageCategory.Literal,
        $"The decimal number \"{{0}}\" contains the character '{{1}}' where it cannot stand. A decimal " +
        $"is digits with at most one point '{KGVL.DECIMAL_SEPARATOR}', then optionally an exponent " +
        $"'{KGVL.DECIMAL_EXPONENT}' with its own sign and digits, then optionally the suffix " +
        $"'{KGVL.SUFFIX_DECIMAL}' at the very end. There is no suffix for any other fractional type, " +
        $"because {KGVL.KEYWORD_DECIMAL} is the only one KGVL has");

    internal virtual ErrorDefinition DecimalMisplacedDigitSeparator { get; } = new(7,
        CompilerMessageCategory.Literal,
        $"The decimal number \"{{0}}\" has a digit separator '{KGVL.UNDERSCORE}' that is not between two " +
        "digits. A separator may only stand between digits of the same part of the number, so never at " +
        $"either end of it, and never next to its point '{KGVL.DECIMAL_SEPARATOR}', its exponent " +
        $"'{KGVL.DECIMAL_EXPONENT}' or its suffix '{KGVL.SUFFIX_DECIMAL}'");

    internal virtual ErrorDefinition DecimalTooLarge { get; } = new(8,
        CompilerMessageCategory.Literal,
        $"The decimal number \"{{0}}\" is too large for {KGVL.KEYWORD_DECIMAL}, whose largest finite value " +
        $"is {TwoIntDecimal.MaxValue}");

    internal virtual ErrorDefinition DecimalTooSmall { get; } = new(9,
        CompilerMessageCategory.Literal,
        $"The decimal number \"{{0}}\" is too close to zero for {KGVL.KEYWORD_DECIMAL} and could only " +
        $"become zero. The smallest {KGVL.KEYWORD_DECIMAL} above zero is {TwoIntDecimal.Epsilon}");

    internal virtual ErrorDefinition IntegerMissingDigits { get; } = new(10,
        CompilerMessageCategory.Literal,
        "The integer \"{0}\" has no digits after its base prefix \"{1}\"");

    internal virtual ErrorDefinition IntegerInvalidBinaryDigit { get; } = new(11,
        CompilerMessageCategory.Literal,
        $"The binary integer \"{{0}}\" contains the digit '{{1}}'. A binary number, written with the " +
        $"prefix \"{KGVL.PREFIX_BINARY}\", may only use the digits 0 and 1");

    internal virtual ErrorDefinition IntegerMisplacedDigitSeparator { get; } = new(12,
        CompilerMessageCategory.Literal,
        $"The integer \"{{0}}\" has a digit separator '{KGVL.UNDERSCORE}' that is not followed by a digit. " +
        "A separator may only stand before one of the number's digits, so never at its end or right " +
        "before its suffix");

    internal virtual ErrorDefinition IntegerInvalidSuffix { get; } = new(13,
        CompilerMessageCategory.Literal,
        $"The integer \"{{0}}\" ends in \"{{1}}\", which is not a suffix an integer can have. An integer " +
        $"takes no suffix, '{KGVL.SUFFIX_UNSIGNED}' for unsigned, '{KGVL.SUFFIX_LONG}' for long, or both " +
        $"in either order, and the suffix '{KGVL.SUFFIX_DECIMAL}' makes a number a {KGVL.KEYWORD_DECIMAL} " +
        "instead");

    internal virtual ErrorDefinition IntegerTooLarge { get; } = new(14,
        CompilerMessageCategory.Literal,
        $"The integer \"{{0}}\" is too large for every integer type. The largest integer is " +
        $"{ulong.MaxValue}, the largest value a {KGVL.KEYWORD_ULONG} can hold");

    internal virtual ErrorDefinition InvalidUnicodeEscapeSequence { get; } = new(15,
        CompilerMessageCategory.Literal,
        $"The escape sequence \"{KGVL.ESCAPE_CHAR}{{0}}\" is not a valid Unicode character code. A Unicode " +
        $"escape sequence is the prefix '{KGVL.ESCAPE_SEQUENCE_CODEPOINT_INDICATOR}' followed by exactly four " +
        "hexadecimal digits (0-9 and a-f)");

    internal virtual ErrorDefinition InvalidLongUnicodeEscapeSequence { get; } = new(16,
        CompilerMessageCategory.Literal,
        $"The escape sequence \"{KGVL.ESCAPE_CHAR}{{0}}\" is not a valid Unicode code point. A long Unicode " +
        $"escape sequence is the prefix '{KGVL.ESCAPE_SEQUENCE_LONG_CODEPOINT_INDICATOR}' followed by exactly " +
        "eight hexadecimal digits (0-9 and a-f), naming a code point no higher than 10FFFF");

    internal virtual ErrorDefinition CharacterNeedsSurrogatePair { get; } = new(17,
        CompilerMessageCategory.Literal,
        $"The escape sequence \"{KGVL.ESCAPE_CHAR}{{0}}\" names the code point U+{{1}}, which is above FFFF, " +
        $"so it takes two UTF-16 code units, a surrogate pair. A {KGVL.KEYWORD_CHAR} holds only one, so the " +
        "code point can only be written in a string");


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

    internal virtual ErrorDefinition ExpectedTypeTestType { get; } = new(3,
        CompilerMessageCategory.Expression,
        "Expected a type name after the \"{0}\" keyword, as in \"value {0} SomeType\"");

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

    internal virtual ErrorDefinition MisplacedThrowExpression { get; } = new(26,
        CompilerMessageCategory.Expression,
        $"\"{KGVL.KEYWORD_THROW}\" cannot be used as a value here. As in C#, a throw expression can only stand " +
        $"where its value would never be needed: as the right operand of \"{KGVL.OPERATOR_NULL_COALESCE}\", as " +
        $"either branch of a conditional value, or as the \"{KGVL.QUICK_METHOD_BODY}\" value of a member, a " +
        "lambda or a switch expression's arm. Anywhere else, throw with a statement of its own");

    internal virtual ErrorDefinition NameOfWithoutName { get; } = new(27,
        CompilerMessageCategory.Expression,
        $"\"{KGVL.KEYWORD_NAMEOF}\" gives the name of what it names, and \"{{0}}\" on its own names nothing " +
        "it could give. It takes a name, or a chain of member accesses ending in one, as in " +
        $"\"{KGVL.KEYWORD_NAMEOF}{KGVL.OPEN_PARENTHESIS}value{KGVL.MEMBER_ACCESS}Member" +
        $"{KGVL.CLOSE_PARENTHESIS}\"");


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
        "This operator cannot be overloaded. The ones which can are the unary +, -, !, ~, ++ and --, " +
        "the binary +, -, *, /, %, &, |, ^, <<, >> and >>>, the comparisons ==, !=, >, <, >= and <=, " +
        "and implicit and explicit conversions. Whether + or - is unary or binary is decided by how many " +
        "parameters it has");

    internal virtual ErrorDefinition ExpectedParameterListEnd { get; } = new(11,
        CompilerMessageCategory.ReturnTypedMembersCommon,
        "Expected '{0}' to end the parameter list");

    internal virtual ErrorDefinition ExpectedConstructorChainTarget { get; } = new(10,
        CompilerMessageCategory.ReturnTypedMembersCommon,
        $"Expected \"{KGVL.KEYWORD_THIS}\" or \"{KGVL.KEYWORD_BASE}\" to name the constructor " +
        "which runs before this one");

    internal virtual ErrorDefinition ExplicitInterfaceField { get; } = new(12,
        CompilerMessageCategory.ReturnTypedMembersCommon,
        "The field \"{0}\" is written as implementing a member of an interface, but an interface has no " +
        "fields to implement. Only functions, operators, properties and indexers can name an interface " +
        "before their own name");

    internal virtual ErrorDefinition DuplicateAccessor { get; } = new(13,
        CompilerMessageCategory.ReturnTypedMembersCommon,
        "\"{0}\" repeats an accessor this {1} already has, so it is not used. There can be at most one " +
        "accessor reading the {1}, and one setting it");

    internal virtual ErrorDefinition MissingBody { get; } = new(14,
        CompilerMessageCategory.ReturnTypedMembersCommon,
        "The {0} \"{1}\" has no body, but it needs one. Only an abstract function can leave its body out, " +
        "which an interface's own instance function is without saying so unless it is private, virtual or " +
        "sealed, and a builtin function of the standard library, whose body the compiler supplies");

    internal virtual ErrorDefinition AbstractWithBody { get; } = new(15,
        CompilerMessageCategory.ReturnTypedMembersCommon,
        "The {0} \"{1}\" is abstract, so it cannot have a body. Whatever derives from its type supplies one");

    internal virtual ErrorDefinition MemberWithoutAccessors { get; } = new(16,
        CompilerMessageCategory.ReturnTypedMembersCommon,
        "The {0} \"{1}\" has no accessors. It needs an accessor reading it, one setting it, or one of each");

    internal virtual ErrorDefinition AutoPropertyWithoutGetter { get; } = new(17,
        CompilerMessageCategory.ReturnTypedMembersCommon,
        $"The property \"{{0}}\" stores its own value, since an accessor of it has no body, but it has no " +
        $"\"{KGVL.KEYWORD_GET}\" accessor to read that value back with");

    internal virtual ErrorDefinition InitAccessorOnStaticProperty { get; } = new(18,
        CompilerMessageCategory.ReturnTypedMembersCommon,
        $"The property \"{{0}}\" cannot have an \"{KGVL.KEYWORD_INIT}\" accessor, since it belongs to no " +
        $"object: it is static, or a namespace holds it. An \"{KGVL.KEYWORD_INIT}\" accessor sets a property " +
        "while an object is being created");

    internal virtual ErrorDefinition StructConstructorBaseChain { get; } = new(19,
        CompilerMessageCategory.ReturnTypedMembersCommon,
        $"A constructor of the structure \"{{0}}\" runs " +
        $"\"{KGVL.KEYWORD_BASE}{KGVL.OPEN_PARENTHESIS}...{KGVL.CLOSE_PARENTHESIS}\" first, but a structure " +
        "derives from no type whose constructor it could run");

    internal virtual ErrorDefinition StaticConstructorChain { get; } = new(20,
        CompilerMessageCategory.ReturnTypedMembersCommon,
        $"The static constructor of \"{{0}}\" cannot run another constructor first. It runs by itself, before " +
        $"its type is first used, so it has no " +
        $"\"{KGVL.KEYWORD_THIS}{KGVL.OPEN_PARENTHESIS}...{KGVL.CLOSE_PARENTHESIS}\" or " +
        $"\"{KGVL.KEYWORD_BASE}{KGVL.OPEN_PARENTHESIS}...{KGVL.CLOSE_PARENTHESIS}\"");

    internal virtual ErrorDefinition OperatorNotPublicStatic { get; } = new(21,
        CompilerMessageCategory.ReturnTypedMembersCommon,
        $"The {{0}} of \"{{1}}\" has to be \"{KGVL.KEYWORD_PUBLIC}\" and \"{KGVL.KEYWORD_STATIC}\". An " +
        "operator belongs to its type rather than to one object of it, and can be used wherever its operands " +
        "can");

    internal virtual ErrorDefinition UnaryOperatorParameterCount { get; } = new(22,
        CompilerMessageCategory.ReturnTypedMembersCommon,
        "The {0} of \"{1}\" takes exactly one parameter, its operand, but its parameter list has {2}");

    internal virtual ErrorDefinition BinaryOperatorParameterCount { get; } = new(23,
        CompilerMessageCategory.ReturnTypedMembersCommon,
        "The {0} of \"{1}\" takes exactly two parameters, its left and right operands, but its parameter " +
        "list has {2}");

    internal virtual ErrorDefinition PlusMinusOperatorParameterCount { get; } = new(24,
        CompilerMessageCategory.ReturnTypedMembersCommon,
        "The {0} of \"{1}\" takes one parameter as a unary operator, or two as a binary one, but its parameter " +
        "list has {2}");

    internal virtual ErrorDefinition ConversionParameterCount { get; } = new(25,
        CompilerMessageCategory.ReturnTypedMembersCommon,
        "The {0} of \"{1}\" takes exactly one parameter, the value it converts, but its parameter list has " +
        "{2}");

    internal virtual ErrorDefinition OperatorParameterModifier { get; } = new(26,
        CompilerMessageCategory.ReturnTypedMembersCommon,
        $"The parameter \"{{0}}\" of the {{1}} of \"{{2}}\" cannot be \"{{3}}\". An operator is given its " +
        $"operands as values, so its parameters can only be plain or \"{KGVL.KEYWORD_IN}\"");

    internal virtual ErrorDefinition OperatorReturnsVoid { get; } = new(27,
        CompilerMessageCategory.ReturnTypedMembersCommon,
        $"The {{0}} of \"{{1}}\" returns \"{KGVL.KEYWORD_VOID}\", but an operator always produces a value");

    internal virtual ErrorDefinition ExplicitOperatorNotStatic { get; } = new(28,
        CompilerMessageCategory.ReturnTypedMembersCommon,
        $"The {{0}} of \"{{1}}\" implements an operator of \"{{2}}\" explicitly, and has to be " +
        $"\"{KGVL.KEYWORD_STATIC}\", as every operator is");

    internal virtual ErrorDefinition InterfaceOperatorNotAbstract { get; } = new(29,
        CompilerMessageCategory.ReturnTypedMembersCommon,
        $"The {{0}} of the interface \"{{1}}\" has to be \"{KGVL.KEYWORD_ABSTRACT}\" or " +
        $"\"{KGVL.KEYWORD_VIRTUAL}\". An interface cannot have equality, inequality or conversion operators " +
        "of its own, only ones the types implementing it supply");

    internal virtual ErrorDefinition InitializerWithoutStorage { get; } = new(30,
        CompilerMessageCategory.ReturnTypedMembersCommon,
        "The property \"{0}\" is given a starting value, but it does not store a value of its own for it to " +
        "start with. Only a property with an accessor without a body stores its own value, and not one which " +
        "is abstract, builtin, or an interface's instance property");

    internal virtual ErrorDefinition MissingAccessorBody { get; } = new(31,
        CompilerMessageCategory.ReturnTypedMembersCommon,
        "The accessor \"{0}\" has no body, but it needs one. An accessor can leave its body out only when what " +
        "it belongs to is abstract, which an interface's own instance property or indexer is without saying so " +
        "when none of its accessors has a body, or builtin, or when it belongs to a property which stores its " +
        "own value, which any property but an interface's instance property can. An indexer cannot store values");

    internal virtual ErrorDefinition UnaryOperatorParameterType { get; } = new(32,
        CompilerMessageCategory.ReturnTypedMembersCommon,
        "The {0} of \"{1}\" has to take a \"{2}\" as its operand: the type it is declared in or, in an " +
        "interface's abstract or virtual operator, a generic parameter constrained to the interface, a value " +
        "type's nullable form counting as the type");

    internal virtual ErrorDefinition BinaryOperatorParameterType { get; } = new(33,
        CompilerMessageCategory.ReturnTypedMembersCommon,
        "The {0} of \"{1}\" has to take a \"{2}\" as one of its operands: the type it is declared in or, in " +
        "an interface's abstract or virtual operator, a generic parameter constrained to the interface, a " +
        "value type's nullable form counting as the type");

    internal virtual ErrorDefinition ShiftOperatorParameterType { get; } = new(34,
        CompilerMessageCategory.ReturnTypedMembersCommon,
        "The {0} of \"{1}\" has to take a \"{2}\" as its left operand, the value shifted: the type it is " +
        "declared in or, in an interface's abstract or virtual operator, a generic parameter constrained to " +
        "the interface, a value type's nullable form counting as the type. The right operand, how far to " +
        "shift, can be of any type");

    internal virtual ErrorDefinition IncrementReturnType { get; } = new(35,
        CompilerMessageCategory.ReturnTypedMembersCommon,
        "The {0} of \"{1}\" has to return what it takes, \"{2}\", or a class deriving from it, since its " +
        "result replaces its operand. An interface's abstract or virtual operator taking the interface can " +
        "also return a generic parameter constrained to it");

    internal virtual ErrorDefinition ConversionNotContaining { get; } = new(36,
        CompilerMessageCategory.ReturnTypedMembersCommon,
        "The {0} of \"{1}\" has to convert from or to \"{2}\": the type it is declared in or, in an " +
        "interface's abstract or virtual conversion, a generic parameter constrained to the interface, a " +
        "value type's nullable form counting as the type");

    internal virtual ErrorDefinition ConversionInterface { get; } = new(37,
        CompilerMessageCategory.ReturnTypedMembersCommon,
        "The {0} of \"{1}\" converts from or to the interface \"{2}\". Conversions from and to interfaces " +
        "are the language's own, so none can be declared");

    internal virtual ErrorDefinition ConversionBaseOrDerived { get; } = new(38,
        CompilerMessageCategory.ReturnTypedMembersCommon,
        "The {0} of \"{1}\" converts between \"{2}\" and \"{3}\", one of which derives from the other. " +
        "Conversions between a type and the types it derives from are the language's own, so none can be " +
        "declared");

    internal virtual ErrorDefinition ConversionIdentity { get; } = new(39,
        CompilerMessageCategory.ReturnTypedMembersCommon,
        "The {0} of \"{1}\" converts \"{2}\" to itself, which needs no conversion");

    internal virtual ErrorDefinition UnpairedOperator { get; } = new(40,
        CompilerMessageCategory.ReturnTypedMembersCommon,
        $"The {{0}} of \"{{1}}\" has no matching {{2}} with the same parameter and return types. The " +
        $"operators {KGVL.OPERATOR_EQUALS} and {KGVL.OPERATOR_NOT_EQUALS}, {KGVL.OPERATOR_LESS_THAN} and " +
        $"{KGVL.OPERATOR_LARGER_THAN}, and {KGVL.OPERATOR_LESS_OR_EQUAL} and " +
        $"{KGVL.OPERATOR_LARGER_OR_EQUAL} are declared in pairs, so that either one of a pair can be used " +
        "wherever the other can");

    internal virtual ErrorDefinition StaticClassAsType { get; } = new(41,
        CompilerMessageCategory.ReturnTypedMembersCommon,
        "The {0} \"{1}\" has the type \"{2}\", which is a static class. A static class has no instances, so " +
        "nothing can hold one");

    internal virtual ErrorDefinition StaticClassAsReturnType { get; } = new(42,
        CompilerMessageCategory.ReturnTypedMembersCommon,
        "The {0} \"{1}\" returns \"{2}\", which is a static class. A static class has no instances, so nothing " +
        "can return one");

    internal virtual ErrorDefinition StaticClassAsParameterType { get; } = new(43,
        CompilerMessageCategory.ReturnTypedMembersCommon,
        "The parameter \"{0}\" of the {1} \"{2}\" has the type \"{3}\", which is a static class. A static class " +
        "has no instances, so none can be passed");

    internal virtual ErrorDefinition StaticClassAsArrayElement { get; } = new(44,
        CompilerMessageCategory.ReturnTypedMembersCommon,
        "The {0} \"{1}\" names an array of the static class \"{2}\", in \"{3}\". A static class has no " +
        "instances, so there can be no array of them");


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

    internal virtual ErrorDefinition ConstantWithoutValue { get; } = new(25,
        CompilerMessageCategory.Statement,
        $"The constant \"{{0}}\" is not given a value. A constant's value is fixed where it is declared, " +
        $"so it has to be written there, as in \"{KGVL.KEYWORD_CONST} {KGVL.KEYWORD_INT} {{0}} " +
        $"{KGVL.ASSIGN} 5{KGVL.SEMICOLON}\"");

    internal virtual ErrorDefinition ExpectedConstantDeclaration { get; } = new(26,
        CompilerMessageCategory.Statement,
        $"Expected the type and name of a constant after \"{KGVL.KEYWORD_CONST}\", as in " +
        $"\"{KGVL.KEYWORD_CONST} {KGVL.KEYWORD_INT} limit {KGVL.ASSIGN} 5{KGVL.SEMICOLON}\"");


    /* Command line. */
    internal virtual ErrorDefinition CommandlineUnknownArgument { get; } = new(1,
        CompilerMessageCategory.Commandline,
        $"Unknown argument \"{{0}}\". It starts with '{CommandlineSyntax.SHORT_NAME_PREFIX}' like a named " +
        "argument does, but the compiler accepts no argument by that name");

    internal virtual ErrorDefinition CommandlineFlagGivenValue { get; } = new(2,
        CompilerMessageCategory.Commandline,
        "The argument \"{0}\" is a flag, which is switched on just by being present, so it cannot be given " +
        "the value \"{1}\"");

    internal virtual ErrorDefinition CommandlineMissingOptionValue { get; } = new(3,
        CompilerMessageCategory.Commandline,
        $"The argument \"{{0}}\" needs a value, written either as " +
        $"\"{{0}} {CommandlineSyntax.VALUE_NAME_START}{{1}}{CommandlineSyntax.VALUE_NAME_END}\" or as " +
        $"\"{{0}}{CommandlineSyntax.VALUE_SEPARATOR}" +
        $"{CommandlineSyntax.VALUE_NAME_START}{{1}}{CommandlineSyntax.VALUE_NAME_END}\". " +
        $"A value starting with '{CommandlineSyntax.SHORT_NAME_PREFIX}' can only be written the second way, " +
        "since on its own it would be read as the name of another argument");

    internal virtual ErrorDefinition CommandlineRepeatedArgument { get; } = new(4,
        CompilerMessageCategory.Commandline,
        "The argument \"{0}\" was given more than once, but it can only be given once");

    internal virtual ErrorDefinition CommandlineUnexpectedPositionalArgument { get; } = new(5,
        CompilerMessageCategory.Commandline,
        "Unexpected argument \"{0}\". It is not named, so it was read as a positional argument, but every " +
        "positional argument the compiler takes had already been given");

    internal virtual ErrorDefinition CommandlineMissingPositionalArgument { get; } = new(6,
        CompilerMessageCategory.Commandline,
        "The required argument \"{0}\" was not given");

    internal virtual ErrorDefinition CommandlineInvalidPath { get; } = new(7,
        CompilerMessageCategory.Commandline,
        "\"{0}\", given for \"{1}\", is not a valid path");

    internal virtual ErrorDefinition CommandlinePathIsFile { get; } = new(8,
        CompilerMessageCategory.Commandline,
        "The path \"{0}\", given for \"{1}\", leads to a file, but \"{1}\" expects a directory");

    internal virtual ErrorDefinition CommandlineDirectoryNotFound { get; } = new(9,
        CompilerMessageCategory.Commandline,
        "The directory \"{0}\", given for \"{1}\", does not exist");


    /* Standard library. */
    internal virtual ErrorDefinition LibraryDirectoryNotFound { get; } = new(1,
        CompilerMessageCategory.StandardLibrary,
        "The standard library was not found in \"{0}\". Building the compiler copies it there, beside the " +
        "compiler itself. To read it from somewhere else, give that directory with \"{1}\"");

    internal virtual ErrorDefinition LibraryHasErrors { get; } = new(2,
        CompilerMessageCategory.StandardLibrary,
        "The standard library in \"{0}\" has errors, reported in its own files. The library is part of the " +
        "compiler rather than of the code being compiled, so those errors are not in your code: the " +
        "library needs fixing, or \"{1}\" needs to point at a copy of it which works");

    internal virtual ErrorDefinition LibraryTypeMissing { get; } = new(3,
        CompilerMessageCategory.StandardLibrary,
        "The standard library declares no \"{0}\", which the compiler relies on");

    internal virtual ErrorDefinition BuiltInMemberNotImplemented { get; } = new(4,
        CompilerMessageCategory.StandardLibrary,
        "\"{0}\" is builtin, but the compiler implements nothing with that signature. A builtin member has " +
        "to match one of the signatures the compiler implements exactly");

    internal virtual ErrorDefinition BuiltInImplementationNotDeclared { get; } = new(5,
        CompilerMessageCategory.StandardLibrary,
        "The compiler implements \"{0}\", but the standard library declares no builtin member with that " +
        "signature");

    internal virtual ErrorDefinition BuiltInMemberWithBody { get; } = new(6,
        CompilerMessageCategory.StandardLibrary,
        $"\"{{0}}\" is builtin, so the compiler supplies its body, and it has to be declared with " +
        $"'{KGVL.SEMICOLON}' in place of one");

    internal virtual ErrorDefinition BuiltInTypeWithInstanceField { get; } = new(7,
        CompilerMessageCategory.StandardLibrary,
        "The builtin type \"{0}\" declares the instance field \"{1}\", but the compiler decides how a " +
        "builtin type's values are stored, so it can have none");

    internal virtual ErrorDefinition BuiltInMemberOfUnknownType { get; } = new(8,
        CompilerMessageCategory.StandardLibrary,
        "\"{0}\" is builtin, but the type declaring it is not one the compiler knows by name, so nothing " +
        "can implement it");

    internal virtual ErrorDefinition BuiltInMemberUsesUnknownType { get; } = new(9,
        CompilerMessageCategory.StandardLibrary,
        "\"{0}\" is builtin, but its signature uses \"{1}\", which is not a type the compiler knows by name");


    /* Resolution. */
    internal virtual ErrorDefinition DuplicateType { get; } = new(1,
        CompilerMessageCategory.Resolution,
        "The type \"{0}\" is declared more than once. A namespace or a type can hold only one type of " +
        "each name and number of generic parameters");

    internal virtual ErrorDefinition TypeNotFound { get; } = new(2,
        CompilerMessageCategory.Resolution,
        $"No type named \"{{0}}\" was found. A type is looked for among the generic parameters and nested " +
        $"types around where it is used, in the namespace being declared and each one containing it, and " +
        $"in the namespaces imported with \"{KGVL.KEYWORD_USING}\"");

    internal virtual ErrorDefinition AmbiguousType { get; } = new(3,
        CompilerMessageCategory.Resolution,
        "\"{0}\" is declared in more than one of the namespaces this file imports, as {1}, so which one is " +
        "meant is not clear");

    internal virtual ErrorDefinition WrongTypeArgumentCount { get; } = new(4,
        CompilerMessageCategory.Resolution,
        "\"{0}\" is written with {1} type arguments, but the type it names takes {2}");

    internal virtual ErrorDefinition ExplicitInterfaceNotInterface { get; } = new(5,
        CompilerMessageCategory.Resolution,
        "\"{0}\" is written as implementing a member of \"{1}\", but that is not an interface");

    internal virtual ErrorDefinition MemberNamedLikeType { get; } = new(6,
        CompilerMessageCategory.Resolution,
        "The {0} \"{1}\" has the same name as the {2} holding it. Only a constructor can be named after a " +
        "class or structure, and only an instance member after an interface");

    internal virtual ErrorDefinition DuplicateMemberName { get; } = new(7,
        CompilerMessageCategory.Resolution,
        "The {0} \"{1}\" has the same name as the {2} declared in \"{3}\" on line {4}, and both belong to " +
        "\"{5}\". Only functions can share a name, as overloads of each other, and types, when their numbers " +
        "of generic parameters differ");

    internal virtual ErrorDefinition MultipleBaseClasses { get; } = new(8,
        CompilerMessageCategory.Resolution,
        "The class \"{0}\" derives from both \"{1}\" and \"{2}\", but a class can derive from only one other " +
        "class. It can implement any number of interfaces");

    internal virtual ErrorDefinition BaseClassNotFirst { get; } = new(9,
        CompilerMessageCategory.Resolution,
        "The class \"{0}\" derives from \"{1}\", which has to be written first, before the interfaces it " +
        "implements");

    internal virtual ErrorDefinition BaseTypeNotInterface { get; } = new(10,
        CompilerMessageCategory.Resolution,
        "The {0} \"{1}\" can derive only from interfaces, and \"{2}\" is not an interface");

    internal virtual ErrorDefinition BaseTypeNotClassOrInterface { get; } = new(11,
        CompilerMessageCategory.Resolution,
        "The class \"{0}\" can derive only from a class and from interfaces, and \"{1}\" is neither");

    internal virtual ErrorDefinition SealedBaseClass { get; } = new(12,
        CompilerMessageCategory.Resolution,
        "The class \"{0}\" cannot derive from \"{1}\", which is sealed so that no class can derive from it");

    internal virtual ErrorDefinition StaticBaseClass { get; } = new(13,
        CompilerMessageCategory.Resolution,
        "The class \"{0}\" cannot derive from \"{1}\", which is static. A static class is never created, so no " +
        "class can derive from it");

    internal virtual ErrorDefinition StaticClassWithBase { get; } = new(14,
        CompilerMessageCategory.Resolution,
        $"The static class \"{{0}}\" cannot derive from \"{{1}}\". A static class is never created, so it " +
        $"derives only from \"{KGVL.KEYWORD_OBJECT}\" and implements no interfaces");

    internal virtual ErrorDefinition CircularBase { get; } = new(15,
        CompilerMessageCategory.Resolution,
        "\"{0}\" derives from itself, as in \"{1}\". A type cannot be among its own base types");

    internal virtual ErrorDefinition MemberNamedLikeGenericParameter { get; } = new(16,
        CompilerMessageCategory.Resolution,
        "The {0} \"{1}\" has the name of a generic parameter of the {2} \"{3}\" holding it. A type's members " +
        "and its generic parameters share one space of names");

    internal virtual ErrorDefinition MemberNamedLikeNamespace { get; } = new(17,
        CompilerMessageCategory.Resolution,
        "The {0} \"{1}\" has the same name as the namespace \"{2}\", which the namespace \"{3}\" holding it " +
        "holds too. A namespace cannot hold a namespace and a member of the same name");

    internal virtual ErrorDefinition BaseTypeWithMarkers { get; } = new(18,
        CompilerMessageCategory.Resolution,
        $"The {{0}} \"{{1}}\" cannot derive from \"{{2}}\", which is written as an array or with " +
        $"'{KGVL.TYPE_NULLABLE_INDICATOR}'. What a type derives from is a class or an interface itself, though " +
        "its type arguments can be anything");

    internal virtual ErrorDefinition ExplicitInterfaceWithMarkers { get; } = new(19,
        CompilerMessageCategory.Resolution,
        $"\"{{0}}\" is written as implementing a member of \"{{1}}\", which is written as an array or with " +
        $"'{KGVL.TYPE_NULLABLE_INDICATOR}'. An explicit implementation names the interface itself, though its " +
        "type arguments can be anything");

    internal virtual ErrorDefinition DuplicateSignature { get; } = new(20,
        CompilerMessageCategory.Resolution,
        $"The {{0}} \"{{1}}\" has the same parameters as the one declared in \"{{2}}\" on line {{3}}, so a " +
        $"call could not tell them apart. Overloads have to differ in their numbers of generic " +
        $"parameters, their parameters' types, or which parameters are passed by reference; return types, " +
        $"parameter names and \"{KGVL.KEYWORD_PARAMS}\" do not count, and neither do \"{KGVL.KEYWORD_REF}\", " +
        $"\"{KGVL.KEYWORD_OUT}\" and \"{KGVL.KEYWORD_IN}\" against each other");

    internal virtual ErrorDefinition DuplicateConversion { get; } = new(21,
        CompilerMessageCategory.Resolution,
        "The {0} of \"{1}\" converts between the same types as the one declared in \"{2}\" on line {3}. " +
        "There can be only one conversion from one type to another, whether implicit or explicit");

    internal virtual ErrorDefinition NothingToOverride { get; } = new(22,
        CompilerMessageCategory.Resolution,
        $"The {{0}} \"{{1}}\" is marked \"{KGVL.KEYWORD_OVERRIDE}\", but no class \"{{2}}\" derives from " +
        "has a {0} of that name and signature which it can see and override");

    internal virtual ErrorDefinition OverrideNotVirtual { get; } = new(23,
        CompilerMessageCategory.Resolution,
        "The {0} \"{1}\" overrides the one of \"{2}\", which is not virtual, abstract or an override " +
        "itself, so it cannot be overridden");

    internal virtual ErrorDefinition OverrideSealed { get; } = new(24,
        CompilerMessageCategory.Resolution,
        "The {0} \"{1}\" overrides the one of \"{2}\", which is sealed, so that nothing deriving from " +
        "\"{2}\" can override it");

    internal virtual ErrorDefinition OverrideReturnType { get; } = new(25,
        CompilerMessageCategory.Resolution,
        "The {0} \"{1}\" has the type \"{2}\", but the one of \"{3}\" it overrides has the type \"{4}\". An " +
        "override has the same type, or, for a function, or a property or indexer without a setter, a class or " +
        "interface deriving from that type");

    internal virtual ErrorDefinition OverrideAccess { get; } = new(26,
        CompilerMessageCategory.Resolution,
        "The {0} \"{1}\" is {2}, but the one of \"{3}\" it overrides is {4}. An override keeps the access " +
        "of what it overrides");

    internal virtual ErrorDefinition OverrideMissingAccessor { get; } = new(27,
        CompilerMessageCategory.Resolution,
        "The {0} \"{1}\" overrides its \"{2}\" accessor, but the one of \"{3}\" it overrides has no such " +
        "accessor, of its own or inherited, which it can see and override");

    internal virtual ErrorDefinition AbstractMemberNotImplemented { get; } = new(28,
        CompilerMessageCategory.Resolution,
        "The class \"{0}\" is not abstract, but does not override the abstract {1} \"{2}\" it inherits from " +
        "\"{3}\". Every abstract member a class inherits needs a body, which an override in the class, or " +
        "in a class between the two, supplies");

    internal virtual ErrorDefinition InterfaceMemberNotImplemented { get; } = new(29,
        CompilerMessageCategory.Resolution,
        "The {0} \"{1}\" does not implement the {2} \"{3}\" of \"{4}\". A type implementing an interface " +
        "needs a public member matching each abstract member of it, or one implementing it explicitly");

    internal virtual ErrorDefinition InterfaceImplementationNotPublic { get; } = new(30,
        CompilerMessageCategory.Resolution,
        "The {0} \"{1}\" does not implement the {2} \"{3}\" of \"{4}\": its {2} of that name and signature " +
        "is not public, as one implementing an interface's member has to be unless it names the interface");

    internal virtual ErrorDefinition InterfaceImplementationStatic { get; } = new(31,
        CompilerMessageCategory.Resolution,
        "The {0} \"{1}\" does not implement the {2} \"{3}\" of \"{4}\": its {2} of that name and signature " +
        "is static where the interface's is not, or the other way round");

    internal virtual ErrorDefinition InterfaceImplementationType { get; } = new(32,
        CompilerMessageCategory.Resolution,
        "The {0} \"{1}\" does not implement the {2} \"{3}\" of \"{4}\": its {2} of that name and signature " +
        "has the type \"{5}\", where the interface's has \"{6}\"");

    internal virtual ErrorDefinition InterfaceImplementationAccessor { get; } = new(33,
        CompilerMessageCategory.Resolution,
        "The {0} \"{1}\" does not implement the {2} \"{3}\" of \"{4}\": the interface's has the \"{5}\" " +
        "accessor, and its own has no public one");

    internal virtual ErrorDefinition ExplicitInterfaceNotImplemented { get; } = new(34,
        CompilerMessageCategory.Resolution,
        "\"{0}\" implements a member of \"{1}\" explicitly, but \"{2}\" does not list \"{1}\" among the types " +
        "it derives from, nor an interface deriving from it. Implementing explicitly an interface which only a " +
        "base class lists takes listing it again");

    internal virtual ErrorDefinition ExplicitMemberNotInInterface { get; } = new(35,
        CompilerMessageCategory.Resolution,
        "\"{0}\" implements a member of \"{1}\" explicitly, but \"{1}\" has no {2} of that name, signature " +
        "and staticness which can be implemented: one which is neither private nor sealed, and when static, " +
        "is abstract or virtual");

    internal virtual ErrorDefinition DuplicateBaseInterface { get; } = new(36,
        CompilerMessageCategory.Resolution,
        "The {0} \"{1}\" lists \"{2}\" more than once among the types it derives from");

    internal virtual ErrorDefinition OverrideAmbiguous { get; } = new(37,
        CompilerMessageCategory.Resolution,
        "The {0} \"{1}\" could override more than one {0} of \"{2}\", such as \"{3}\", since they take the same " +
        "parameters once the generic parameters of \"{2}\" are given types, so which it overrides is unknown");

    internal virtual ErrorDefinition OverrideHiddenByOtherKind { get; } = new(38,
        CompilerMessageCategory.Resolution,
        $"The {{0}} \"{{1}}\" is marked \"{KGVL.KEYWORD_OVERRIDE}\", but the nearest member of its name, in " +
        "\"{2}\", is a {3}, which hides whatever further back it could override");

    internal virtual ErrorDefinition HidesAbstractMember { get; } = new(39,
        CompilerMessageCategory.Resolution,
        "The {0} \"{1}\" hides the abstract {2} of the same name which \"{3}\" inherits from \"{4}\". An " +
        "abstract member cannot be hidden, since nothing deriving from \"{3}\" could then give it a body");

    internal virtual ErrorDefinition AbstractAccessorNotImplemented { get; } = new(40,
        CompilerMessageCategory.Resolution,
        "The class \"{0}\" is not abstract, but does not override the \"{1}\" accessor of the abstract {2} " +
        "\"{3}\" it inherits from \"{4}\". Every abstract accessor a class inherits needs a body, which an " +
        "override in the class, or in a class between the two, supplies");

    internal virtual ErrorDefinition NoMostSpecificImplementation { get; } = new(41,
        CompilerMessageCategory.Resolution,
        "The {0} \"{1}\" does not implement the {2} \"{3}\" of \"{4}\": both \"{5}\" and \"{6}\" implement it " +
        "or make it abstract again, and neither derives from the other, so neither decides. A type " +
        "implementing both has to implement it itself");

    internal virtual ErrorDefinition ImplementationConstraints { get; } = new(42,
        CompilerMessageCategory.Resolution,
        "The {0} \"{1}\" implements the function \"{2}\" of \"{3}\" with a function whose generic parameter " +
        "\"{4}\" is constrained differently from the interface's \"{5}\". An implementation keeps the " +
        "constraints of what it implements; one implementing it explicitly takes them without restating them");

    internal virtual ErrorDefinition ExplicitImplementationType { get; } = new(43,
        CompilerMessageCategory.Resolution,
        "\"{0}\" implements the {1} \"{2}\" of \"{3}\" explicitly, but has the type \"{4}\", where the " +
        "interface's has \"{5}\"");

    internal virtual ErrorDefinition ExplicitImplementationMissingAccessor { get; } = new(44,
        CompilerMessageCategory.Resolution,
        "\"{0}\" implements the {1} of \"{2}\" explicitly, but has no \"{3}\" accessor, which the interface's " +
        "has");

    internal virtual ErrorDefinition ExplicitImplementationExtraAccessor { get; } = new(45,
        CompilerMessageCategory.Resolution,
        "\"{0}\" implements the {1} of \"{2}\" explicitly, but has the \"{3}\" accessor, which the interface's " +
        "does not");

    internal virtual ErrorDefinition UnifyingInterfaces { get; } = new(46,
        CompilerMessageCategory.Resolution,
        "The {0} \"{1}\" cannot implement both \"{2}\" and \"{3}\", since some types for its generic " +
        "parameters would make them the same interface, whose members it could then not tell apart");

    internal virtual ErrorDefinition OverrideSetterKind { get; } = new(47,
        CompilerMessageCategory.Resolution,
        $"The {{0}} \"{{1}}\" overrides with its \"{{2}}\" accessor the \"{{4}}\" accessor of the one of \"{{3}}\" " +
        $"it overrides. Both set the member, but an override's setter is \"{KGVL.KEYWORD_INIT}\" exactly when " +
        "the one it overrides is");

    internal virtual ErrorDefinition InconsistentMemberType { get; } = new(48,
        CompilerMessageCategory.Resolution,
        "The {0} \"{1}\" has the type \"{2}\", but \"{3}\", which is {4}, cannot be used everywhere the {0} can. " +
        "A declaration names only types at least as accessible as what it declares, since whatever uses it " +
        "needs those types too");

    internal virtual ErrorDefinition InconsistentReturnType { get; } = new(49,
        CompilerMessageCategory.Resolution,
        "The {0} \"{1}\" returns \"{2}\", but \"{3}\", which is {4}, cannot be used everywhere the {0} can. A " +
        "declaration names only types at least as accessible as what it declares, since whatever uses it needs " +
        "those types too");

    internal virtual ErrorDefinition InconsistentParameterType { get; } = new(50,
        CompilerMessageCategory.Resolution,
        "The parameter \"{5}\" of the {0} \"{1}\" has the type \"{2}\", but \"{3}\", which is {4}, cannot be " +
        "used everywhere the {0} can. A declaration names only types at least as accessible as what it " +
        "declares, since whatever uses it needs those types too");

    internal virtual ErrorDefinition InconsistentConstraintType { get; } = new(51,
        CompilerMessageCategory.Resolution,
        "The generic parameter \"{5}\" of the {0} \"{1}\" is constrained to \"{2}\", but \"{3}\", which is {4}, " +
        "cannot be used everywhere the {0} can. A declaration names only types at least as accessible as what " +
        "it declares, since whatever uses it needs those types too");

    internal virtual ErrorDefinition InconsistentBaseType { get; } = new(52,
        CompilerMessageCategory.Resolution,
        "The {0} \"{1}\" derives from \"{2}\", but \"{3}\", which is {4}, cannot be used everywhere the {0} " +
        "can. A class's base class and an interface's base interfaces are at least as accessible as the type, " +
        "though the interfaces a class or structure implements need not be");

    internal virtual ErrorDefinition RequiredLessAccessible { get; } = new(53,
        CompilerMessageCategory.Resolution,
        "The {0} \"{1}\" is required, but it, or a property's setter, is less accessible than the {2} \"{3}\" " +
        "holding it. " +
        "Whatever can create a \"{3}\" has to set its required members, so each of them, and its setter, is " +
        "at least as accessible as the type");

    internal virtual ErrorDefinition TypeInaccessible { get; } = new(54,
        CompilerMessageCategory.Resolution,
        $"The type \"{{0}}\" was found as \"{{1}}\", which is \"{KGVL.KEYWORD_INTERNAL}\" to the other side of " +
        "the standard library's boundary, so it cannot be used here");

    internal virtual ErrorDefinition NameSpaceUsedAsType { get; } = new(55,
        CompilerMessageCategory.Resolution,
        "\"{0}\" names the namespace \"{1}\", but a type is expected here. A namespace is only written before " +
        $"a type or namespace it holds, as in \"{{1}}{KGVL.NAMESPACE_SEPARATOR}SomeType\"");

    internal virtual ErrorDefinition TypeNotInNameSpace { get; } = new(56,
        CompilerMessageCategory.Resolution,
        "The namespace \"{0}\" holds no type or namespace named \"{1}\"");

    internal virtual ErrorDefinition NestedTypeNotFound { get; } = new(57,
        CompilerMessageCategory.Resolution,
        $"The type \"{{0}}\" declares or inherits no type named \"{{1}}\". A name written after a type and a " +
        $"'{KGVL.NAMESPACE_SEPARATOR}' is looked for among the types declared inside it and those it inherits");

    internal virtual ErrorDefinition GenericParameterQualifier { get; } = new(58,
        CompilerMessageCategory.Resolution,
        "\"{0}\" is a generic parameter, so no type can be named through it: it stands for whatever type it " +
        "is given, and which types that one declares is not known where the parameter is used");

    internal virtual ErrorDefinition NestedTypeInaccessible { get; } = new(59,
        CompilerMessageCategory.Resolution,
        "The type \"{0}\" cannot be used by the {1} \"{2}\", since it is \"{3}\" inside \"{4}\", which does " +
        "not let it be used where it is named here");

    internal virtual ErrorDefinition NameSpaceWithTypeArguments { get; } = new(60,
        CompilerMessageCategory.Resolution,
        "\"{0}\" is written with type arguments, but without them it names the namespace \"{1}\", which cannot " +
        "take any, and no type of its name takes them either");

    internal virtual ErrorDefinition TypeOrNameSpaceNotFound { get; } = new(61,
        CompilerMessageCategory.Resolution,
        $"No type or namespace named \"{{0}}\" was found. A name before a '{KGVL.NAMESPACE_SEPARATOR}' is " +
        "looked for as a type is, among the generic parameters and nested types around where it is used, the " +
        "types of the namespace being declared and each one containing it, and those of the namespaces " +
        $"imported with \"{KGVL.KEYWORD_USING}\", and also as a namespace, inside the namespace being declared " +
        "and each one containing it, and at the root");

    internal virtual ErrorDefinition CircularBaseThroughNesting { get; } = new(62,
        CompilerMessageCategory.Resolution,
        "\"{0}\" depends on itself, as in \"{1}\", where each type derives from the next or is declared " +
        "inside it. As in C#, a type depends on its bases and on the type it is declared in, so it cannot " +
        "derive from a type which depends on it");

    internal virtual ErrorDefinition ExplicitImplementationSetterKind { get; } = new(63,
        CompilerMessageCategory.Resolution,
        "\"{0}\" implements the {1} of \"{2}\" explicitly with its \"{3}\" accessor, where the interface's has " +
        $"\"{{4}}\". Both set the member, but an implementation's setter is \"{KGVL.KEYWORD_INIT}\" exactly " +
        "when the interface's is");

    internal virtual ErrorDefinition AmbiguousInheritedType { get; } = new(64,
        CompilerMessageCategory.Resolution,
        "\"{0}\" is declared in more than one of the interfaces \"{1}\" derives from, as {2}. None of those " +
        "interfaces derives from another, so which one is meant is not clear");

    internal virtual ErrorDefinition CircularBaseLookup { get; } = new(65,
        CompilerMessageCategory.Resolution,
        "\"{0}\" is not declared in the {1} \"{2}\", and what \"{2}\" inherits is not known yet, since resolving " +
        "the base list of \"{2}\" is what led to this name. As in C#, what a type inherits cannot depend on " +
        "itself, so the name is not looked for any further");


    /* Warnings. */
    internal virtual WarningDefinition DuplicateUsingDirective { get; } = new(1,
        WarningSeverity.Minor,
        $"The namespace \"{{0}}\" is already imported by an earlier \"{KGVL.KEYWORD_USING}\" directive " +
        "in this file, so this one does nothing and can be removed",
        CompilerMessageCategory.SourceFileRoot);

    internal virtual WarningDefinition GenericParameterHidesOuter { get; } = new(1,
        WarningSeverity.Normal,
        "The generic parameter \"{0}\" of the {1} \"{2}\" has the same name as a generic parameter of the {3} " +
        "\"{4}\" around it, which it hides: inside \"{2}\", \"{0}\" means only its own",
        CompilerMessageCategory.Generics);

    internal virtual WarningDefinition MemberHidesInherited { get; } = new(1,
        WarningSeverity.Normal,
        $"The {{0}} \"{{1}}\" hides the {{2}} of the same name which \"{{3}}\" inherits from \"{{4}}\". If " +
        $"hiding it is meant, mark it \"{KGVL.KEYWORD_NEW}\"",
        CompilerMessageCategory.Resolution);

    internal virtual WarningDefinition MemberHidesVirtual { get; } = new(2,
        WarningSeverity.Normal,
        $"The {{0}} \"{{1}}\" hides the virtual {{2}} of the same name which \"{{3}}\" inherits from " +
        $"\"{{4}}\". To replace it wherever it is used, mark it \"{KGVL.KEYWORD_OVERRIDE}\"; to only hide " +
        $"it, mark it \"{KGVL.KEYWORD_NEW}\"",
        CompilerMessageCategory.Resolution);

    internal virtual WarningDefinition NewHidesNothing { get; } = new(3,
        WarningSeverity.Minor,
        $"The {{0}} \"{{1}}\" is marked \"{KGVL.KEYWORD_NEW}\", but hides nothing it inherits, so " +
        $"\"{KGVL.KEYWORD_NEW}\" can be taken off",
        CompilerMessageCategory.Resolution);

    internal virtual WarningDefinition DuplicateInterfaceAnnotations { get; } = new(4,
        WarningSeverity.Normal,
        $"The {{0}} \"{{1}}\" lists \"{{2}}\", which it lists already as \"{{3}}\". The two differ only in " +
        $"their '{KGVL.TYPE_NULLABLE_INDICATOR}' annotations, so they are one interface",
        CompilerMessageCategory.Resolution);

    internal virtual WarningDefinition SwitchAfterTypeTest { get; } = new(1,
        WarningSeverity.Normal,
        $"This \"{KGVL.KEYWORD_SWITCH}\" switches on the whole of the \"{KGVL.KEYWORD_IS}\" or " +
        $"\"{KGVL.KEYWORD_AS}\" test before it, as in C#, though it reads as though it belonged to the type. " +
        $"Bracket the test, as in \"{KGVL.OPEN_PARENTHESIS}value {KGVL.KEYWORD_AS} SomeType" +
        $"{KGVL.CLOSE_PARENTHESIS} {KGVL.KEYWORD_SWITCH} ...\", to say so",
        CompilerMessageCategory.Expression);

    internal virtual WarningDefinition UnusableDefaultOnExplicitImplementation { get; } = new(1,
        WarningSeverity.Normal,
        "The default value of the parameter \"{0}\" of the {1} \"{2}\" can never be used. The {1} implements " +
        "an interface's member explicitly, so it is only ever called through the interface, and only the " +
        "interface's member's default values apply",
        CompilerMessageCategory.Function);

    internal virtual WarningDefinition UnusableDefaultOnOperator { get; } = new(2,
        WarningSeverity.Normal,
        "The default value of the parameter \"{0}\" of the {1} \"{2}\" can never be used. An operator is used " +
        "through the symbol it overloads, which always gives it every operand",
        CompilerMessageCategory.Function);

    internal virtual WarningDefinition UnusableDefaultOnSingleIndex { get; } = new(3,
        WarningSeverity.Normal,
        "The default value of the parameter \"{0}\" of the {1} \"{2}\" can never be used. The indexer has " +
        $"only this parameter, and indexing it needs a value between '{KGVL.OPEN_SQUARE_BRACKET}' and " +
        $"'{KGVL.CLOSE_SQUARE_BRACKET}'",
        CompilerMessageCategory.Function);

    internal virtual WarningDefinition StaticClassReturnedInInterface { get; } = new(1,
        WarningSeverity.Normal,
        "The {0} \"{1}\" has the type \"{2}\", which is a static class. A static class has no instances, so " +
        "nothing but null can be of its type. Anywhere but in an interface, this is an error",
        CompilerMessageCategory.ReturnTypedMembersCommon);

    internal virtual WarningDefinition StaticClassTakenInInterface { get; } = new(2,
        WarningSeverity.Normal,
        "The parameter \"{0}\" of the {1} \"{2}\" has the type \"{3}\", which is a static class. A static " +
        "class has no instances, so nothing but null can be passed. Anywhere but in an interface, this is an " +
        "error",
        CompilerMessageCategory.ReturnTypedMembersCommon);
}