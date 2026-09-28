using KeigValCompiler.Semantician.Member;

namespace KeigValCompiler.Semantician.Library.Bindings;

/* KGVL.Boolean. && and || are not here, since they are the language's rather than operators the type
 * declares. */
internal class BooleanBindings : ILibraryBindingProvider
{
    // Inherited methods.
    public void AddBindings(LibraryBindingTable table)
    {
        ArgumentNullException.ThrowIfNull(table, nameof(table));
        SignatureBuilder Member = new(LibraryTypes.Boolean);

        table.AddIntrinsic(Member.StaticMethod(LibraryMemberNames.PARSE, Member.Self, LibraryTypes.String),
            IntrinsicOperation.Parse);
        table.AddIntrinsic(Member.StaticMethod(LibraryMemberNames.TRY_PARSE, LibraryTypes.Boolean,
            LibraryTypes.String, SignatureParameter.Out(Member.Self)), IntrinsicOperation.TryParse);

        table.AddIntrinsic(Member.UnaryOperator(OverloadableOperator.LogicalNot), IntrinsicOperation.Not);
        foreach (OperatorIntrinsic Bitwise in OperatorIntrinsics.Bitwise)
        {
            table.AddIntrinsic(Member.BinaryOperator(Bitwise.Operator), Bitwise.Operation);
        }
        foreach (OperatorIntrinsic Equality in OperatorIntrinsics.Equality)
        {
            table.AddIntrinsic(Member.Comparison(Equality.Operator), Equality.Operation);
        }
    }
}
