namespace KeigValCompiler;

internal static class KGVL
{
    /* Syntax. */
    public const char SEMICOLON = ';';
    public const char COLON = ':';
    public const char COMMA = ',';
    public const char NAMESPACE_SEPARATOR = '.';
    public const char UNDERSCORE = '_';
    public const char OPEN_CURLY_BRACKET = '{';
    public const char CLOSE_CURLY_BRACKET = '}';
    public const char OPEN_PARENTHESIS = '(';
    public const char CLOSE_PARENTHESIS = ')';
    public const char ASSIGNMENT_OPERATOR = '=';
    public const char STRING_INTERPOLATION_OPERATOR = '$';
    public const char ESCAPE_CHAR = '\\';
    public const char CHAR_NONE = '\0';
    public const char DASH = '-';
    public const char MEMBER_ACCESS = '.';

    public const char LINE_COMMENT_INDICATOR1 = '/';
    public const char LINE_COMMENT_INDICATOR2 = '*';
    public const string MULTI_LINE_COMMENT_START = "/*";
    public const string MULTI_LINE_COMMENT_END = "*/";
    public const string SINGLE_LINE_COMMENT_START = "//";
    public const char NEWLINE = '\n';
    public const int COMMENT_INDICATOR_LENGTH = 2;

    public const char SINGLE_QUOTE = '\'';
    public const char DOUBLE_QUOTE = '"';

    public const string QUICK_METHOD_BODY = "=>";

    public const char GENERIC_TYPE_START = '<';
    public const char GENERIC_TYPE_END = '>';

    public const char OPEN_SQUARE_BRACKET = '[';
    public const char CLOSE_SQUARE_BRACKET = ']';

    public const char TYPE_NULLABLE_INDICATOR = '?';

    public const char ESCAPE_SEQUENCE_CODEPOINT_INDICATOR = 'u';
    public const char ESCAPE_SEQUENCE_HEX_INDICATOR = 'x';

    public const char TERNARY_BRANCH_SEPARATOR = ':';
    public const char INTERPOLATION_SECTION_START = '{';
    public const char INTERPOLATION_SECTION_END = '}';


    /* Operators. Longest spellings must be tested before shorter ones which prefix them, so that
     * ">>=" is never read as ">>" followed by "=". */
    public const string OPERATOR_INCREMENT = "++";
    public const string OPERATOR_DECREMENT = "--";

    public const string OPERATOR_ADD = "+";
    public const string OPERATOR_SUBTRACT = "-";
    public const string OPERATOR_MULTIPLY = "*";
    public const string OPERATOR_DIVIDE = "/";
    public const string OPERATOR_MODULO = "%";

    public const string OPERATOR_NOT = "!";
    public const string OPERATOR_BITWISE_COMPLEMENT = "~";

    public const string OPERATOR_EQUALS = "==";
    public const string OPERATOR_NOT_EQUALS = "!=";
    public const string OPERATOR_LARGER_THAN = ">";
    public const string OPERATOR_LESS_THAN = "<";
    public const string OPERATOR_LARGER_OR_EQUAL = ">=";
    public const string OPERATOR_LESS_OR_EQUAL = "<=";

    public const string OPERATOR_CONDITIONAL_AND = "&&";
    public const string OPERATOR_CONDITIONAL_OR = "||";

    public const string OPERATOR_BITWISE_AND = "&";
    public const string OPERATOR_BITWISE_OR = "|";
    public const string OPERATOR_BITWISE_XOR = "^";

    public const string OPERATOR_LEFT_SHIFT = "<<";
    public const string OPERATOR_RIGHT_SHIFT = ">>";
    public const string OPERATOR_UNSIGNED_RIGHT_SHIFT = ">>>";

    public const string OPERATOR_NULL_COALESCE = "??";
    public const string OPERATOR_CONDITIONAL_ACCESS = "?.";

    public const string ASSIGN = "=";
    public const string ASSIGN_ADD = "+=";
    public const string ASSIGN_SUBTRACT = "-=";
    public const string ASSIGN_MULTIPLY = "*=";
    public const string ASSIGN_DIVIDE = "/=";
    public const string ASSIGN_MODULO = "%=";
    public const string ASSIGN_BITWISE_AND = "&=";
    public const string ASSIGN_BITWISE_OR = "|=";
    public const string ASSIGN_BITWISE_XOR = "^=";
    public const string ASSIGN_LEFT_SHIFT = "<<=";
    public const string ASSIGN_RIGHT_SHIFT = ">>=";
    public const string ASSIGN_UNSIGNED_RIGHT_SHIFT = ">>>=";
    public const string ASSIGN_NULL_COALESCE = "??=";


    /* Keywords. */
    public const string KEYWORD_NAMESPACE = "namespace";
    public const string KEYWORD_USING = "using";

    public const string KEYWORD_CLASS = "class";
    public const string KEYWORD_STRUCT = "struct";
    public const string KEYWORD_RECORD = "record";
    public const string KEYWORD_DELEGATGE = "delegate";
    public const string KEYWORD_INTERFACE = "interface";
    public const string KEYWORD_ENUM = "enum";
    public const string KEYWORD_EVENT = "event";
    public const string KEYWORD_STATIC = "static";
    public const string KEYWORD_PRIVATE = "private";
    public const string KEYWORD_PROTECTED = "protected";
    public const string KEYWORD_PUBLIC = "public";
    public const string KEYWORD_READONLY = "readonly";
    public const string KEYWORD_BUILTIN = "builtin";
    public const string KEYWORD_INLINE = "inline";
    public const string KEYWORD_ABSTRACT = "abstract";
    public const string KEYWORD_VIRTUAL = "virtual";
    public const string KEYWORD_OVERRIDE = "override";
    public const string KEYWORD_RAW = "raw";
    public const string KEYWORD_REQUIRED = "required";

    public const string KEYWORD_FOR = "for";
    public const string KEYWORD_FOREACH = "foreach";
    public const string KEYWORD_WHILE = "while";
    public const string KEYWORD_DO = "do";
    public const string KEYWORD_CONTINUE = "continue";
    public const string KEYWORD_BREAK = "break";
    public const string KEYWORD_YIELD = "yield";
    public const string KEYWORD_IF = "if";
    public const string KEYWORD_ELSE = "else";
    public const string KEYWORD_SWITCH = "switch";
    public const string KEYWORD_CASE = "case";
    public const string KEYWORD_DEFAULT = "default";
    public const string KEYWORD_TRY = "try";
    public const string KEYWORD_CATCH = "catch";
    public const string KEYWORD_THROW = "throw";
    public const string KEYWORD_FINALLY = "finally";
    public const string KEYWORD_RETURN = "return";
    public const string KEYWORD_CONSTALLOC = "constalloc";
    public const string KEYWORD_IS = "is";
    public const string KEYWORD_AS = "as";
    public const string KEYWORD_BASE = "base";
    public const string KEYWORD_THIS = "this";
    public const string KEYWORD_VAR = "var";

    public const string KEYWORD_IN = "in";
    public const string KEYWORD_OUT = "out";
    public const string KEYWORD_REF = "ref";
    

    public const string KEYWORD_WHERE = "where";
    public const string KEYWORD_NOTNULL = "notnull";

    public const string KEYWORD_WHEN = "when";

    public const string KEYWORD_NAMEOF = "nameof";
    public const string KEYWORD_TYPEOF = "typeof";

    public const string KEYWORD_NEW = "new";
    public const string KEYWORD_GET = "get";
    public const string KEYWORD_SET = "set";
    public const string KEYWORD_INIT = "init";
    public const string KEYWORD_OPERATOR = "operator";
    public const string KEYWORD_IMPLICIT = "implicit";
    public const string KEYWORD_EXPLICIT = "explicit";
    public const string KEYWORD_SEALED = "sealed";

    public const string KEYWORD_BYTE = "byte";
    public const string KEYWORD_UBYTE = "ubyte";
    public const string KEYWORD_SHORT = "short";
    public const string KEYWORD_USHORT = "ushort";
    public const string KEYWORD_INT = "int";
    public const string KEYWORD_UINT = "uint";
    public const string KEYWORD_LONG = "long";
    public const string KEYWORD_ULONG = "ulong";
    public const string KEYWORD_DECIMAL = "decimal";
    public const string KEYWORD_STRING = "string";
    public const string KEYWORD_BOOL = "bool";
    public const string KEYWORD_NULL = "null";
    public const string KEYWORD_FIELD = "field";
    public const string KEYWORD_VALUE = "value";
    public const string KEYWORD_PARAMS = "params";
    public const string KEYWORD_TRUE = "true";
    public const string KEYWORD_FALSE = "false";
    public const string KEYWORD_VOID = "void";

    public const string PREFIX_BINARY = "0b";
    public const string PREFIX_HEX = "0x";
    public const char SUFFIX_LONG = 'l';
    public const char SUFFIX_UNSIGNED ='u';
    public const char SUFFIX_DECIMAL ='m';
    public const char DECIMAL_SEPARATOR ='.';
    public const char DECIMAL_EXPONENT ='e';
    public const char PREFIX_HEX_CHAR ='x';


    /* Internal. */
    public const char IDENTIFIER_SEPARATOR_FUNCTION = '!';
    public const char IDENTIFIER_ACCESSOR = '&';
    public const char IDENTIFIER_OPERATOR = '%';

    public const string NAME_CLASS = "class";
    public const string NAME_STRUCT = "structure";
    public const string NAME_INTERFACE = "interface";
    public const string NAME_DELEGATE = "delegate";
    public const string NAME_ENUM = "enum";
    public const string NAME_FIELD = "field";
    public const string NAME_PROPERTY = "property";
    public const string NAME_FUNCTION = "function";
    public const string NAME_INDEXER = "indexer";
    public const string NAME_NAMESPACE = "namespace";
    public const string NAME_RECORD_CLASS = "record class";
    public const string NAME_EVENT = "event";
    public const string NAME_CONSTRUCTOR = "constructor";
    public const string NAME_OPERATOR_OVERLOAD = "operator overload";

    public const string DOUBLE_CURLY_OPEN = "{{";
    public const string DOUBLE_CURLY_CLOSE = "}}";

    /* Error messages are run through string.Format, which reads a lone brace as the start of a
     * placeholder, so a brace meant literally has to be doubled. */
    public const string OPEN_CURLY_BRACKET_IN_MESSAGE = "{{";
    public const string CLOSE_CURLY_BRACKET_IN_MESSAGE = "}}";
}