

namespace KeigValCompiler.Semantician.Member;

internal class PackFunction : PackMember
{
    // Internal fields.
    internal FunctionParameterCollection Parameters { get; private init; }
    internal StatementCollection Statements { get; private init; }
    internal GenericTypeParameterCollection GenericParameters { get; private init; }


    // Private fields.
    private Dictionary<string, FunctionParameter> _parameters = new();


    // Constructors.
    internal PackFunction(Identifier identifier,
        PackMemberModifiers modifiers,
        PackSourceFile sourceFile,
        PackNameSpace nameSpace,
        Identifier parentItem,
        FunctionParameterCollection parameters,
        GenericTypeParameter[]? genericParameters,
        StatementCollection statements)
        : base(identifier, modifiers, sourceFile, nameSpace, parentItem)
    {
        Parameters = parameters ?? throw new ArgumentNullException(nameof(parameters));
        Statements = statements ?? throw new ArgumentNullException(nameof(statements));
        GenericParameters = new(genericParameters ?? Array.Empty<GenericTypeParameter>());
    }
}