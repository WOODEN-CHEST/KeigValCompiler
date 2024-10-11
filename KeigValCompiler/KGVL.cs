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

    public const char LINE_COMMENT_INDICATOR1 = '/';
    public const char LINE_COMMENT_INDICATOR2 = '*';
    public const string MULTI_LINE_COMMENT_START = "/*";
    public const string MULTI_LINE_COMMENT_END = "*/";
    public const string SINGLE_LINE_COMMENT_START = "//";
    public const char NEWLINE = '\n';
    public const int COMMENT_INDICATOR_LENGTH = 2;

    public const char SINGLE_QUOTE = '\'';
    public const char DOUBLE_QUOTE = '"';


    /* Keywords. */
    public const string KEYWORD_NAMESPACE = "namespace";
    public const string KEYWORD_USING = "using";

    public const string KEYWORD_CLASS = "class";
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

    public const string KEYWORD_FOR = "for";
    public const string KEYWORD_FOREACH = "foreach";
    public const string KEYWORD_CONTINUE = "continue";
    public const string KEYWORD_BREAK = "break";
    public const string KEYWORD_IF = "if";
    public const string KEYWORD_ELSE = "else";
    public const string KEYWORD_SWITCH = "switch";
    public const string KEYWORD_CASE = "case";
    public const string KEYWORD_DEFAULT = "default";
    public const string KEYWORD_GOTO = "goto";
    public const string KEYWORD_LABEL = "label";
    public const string KEYWORD_TRY = "try";
    public const string KEYWORD_CATCH = "catch";
    public const string KEYWORD_THROW = "throw";
    public const string KEYWORD_FINALLY = "finally";
    public const string KEYWORD_CONSTALLOC = "constalloc";

    public const string KEYWORD_SBYTE = "sbyte";
    public const string KEYWORD_BYTE = "byte";
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


    /* Internal. */
    public const char IDENTIFIER_SEPARATOR_FUNCTION = '!';
    public const char IDENTIFIER_SEPARATOR_OVERLOAD = '=';
}