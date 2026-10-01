using KeigValCompiler.Semantician.Bound;
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

    /* Written with "=>" and a value, which Statements holds as the return statement it amounts to, or as the
     * throw statement a throw expression amounts to. A function returning nothing runs the value instead, as
     * the statement it would be written as, so a "return" with a value there is not an error. */
    internal bool IsExpressionBodied { get; set; } = false;

    /* The body as resolution binds it, once it is, or null for a function without one. */
    internal BoundBlock? BoundBody { get; set; } = null;

    /* The "value" a property's or an indexer's setter is given, which it does not declare, with the type of
     * what it sets; made when the setter's body is bound, and null for any other function. */
    internal FunctionParameter? ValueParameter { get; set; } = null;

    /* What the compiler implements a builtin function as, once the library is bound; null for any
     * other function. A builtin property's or indexer's accessors carry theirs. */
    internal IntrinsicOperation? Intrinsic { get; set; } = null;


    // Constructors.
    internal PackFunction(Identifier identifier, PackSourceFile sourceFile) : base(identifier, sourceFile) { }
}