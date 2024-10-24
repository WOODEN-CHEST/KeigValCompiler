using KeigValCompiler.Semantician.Member.Code;

namespace KeigValCompiler.Semantician.Member;

internal class PackFunction : PackMember, IGenericParameterHolder
{
    // Fields.
    public GenericTypeParameterCollection GenericParameters { get; private init; } = new();


    // Internal fields.
    internal Identifier ReturnType { get; set; }
    internal FunctionParameterCollection Parameters { get; private init; } = new();
    internal StatementCollection Statements { get; private init; } = new();
    

    // Private fields.
    private Dictionary<string, FunctionParameter> _parameters = new();


    // Constructors.
    internal PackFunction(Identifier identifier,  PackSourceFile sourceFile) : base(identifier, sourceFile) { }
}