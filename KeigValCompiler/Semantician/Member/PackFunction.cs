using KeigValCompiler.Semantician.Library;
using KeigValCompiler.Semantician.Member.Code;

namespace KeigValCompiler.Semantician.Member;

internal class PackFunction : PackMember, IGenericParameterHolder, IExplicitInterfaceMember
{
    // Fields.
    public GenericTypeParameterCollection GenericParameters { get; private init; } = new();
    public TypeTargetIdentifier? ExplicitInterface { get; set; } = null;


    // Internal fields.
    internal TypeTargetIdentifier? ReturnType { get; set; } = null;
    internal FunctionParameterCollection Parameters { get; private init; } = new();
    internal StatementCollection? Statements { get; set; } = null;

    /* What the compiler implements a builtin function as, once the library is bound; null for any
     * other function. A builtin property's or indexer's accessors carry theirs. */
    internal IntrinsicOperation? Intrinsic { get; set; } = null;


    // Constructors.
    internal PackFunction(Identifier identifier, PackSourceFile sourceFile) : base(identifier, sourceFile) { }
}