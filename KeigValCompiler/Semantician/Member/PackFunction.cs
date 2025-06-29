using KeigValCompiler.Semantician.Member.Code;

namespace KeigValCompiler.Semantician.Member;

internal class PackFunction : PackMember, IGenericParameterHolder
{
    // Fields.
    public GenericTypeParameterCollection GenericParameters { get; private init; } = new();


    // Internal fields.
    internal TypeTargetIdentifier? ReturnType { get; set; } = null;
    internal FunctionParameterCollection Parameters { get; private init; } = new();
    internal StatementCollection? Statements { get; set; } = null;


    // Constructors.
    internal PackFunction(Identifier identifier, PackSourceFile sourceFile) : base(identifier, sourceFile) { }
}