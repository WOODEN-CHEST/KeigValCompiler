using KeigValCompiler.Semantician.Member;

namespace KeigValCompiler.Semantician.Library.Bindings;

/* All eight integer types, which declare the same members but for their width and sign. Their
 * conversions are in NumericConversionBindings. */
internal class IntegerBindings : ILibraryBindingProvider
{
    // Static fields.
    /* What decides the operators each declares. As in C#, the ones narrower than int have only ++ and
     * --, their other arithmetic being int's, and only the signed ones have a unary -. The 64-bit ones
     * are also the only ones whose GetHashCode is builtin, the others' being the value itself. */
    private static readonly IntegerFacts[] _integers = new IntegerFacts[]
    {
        new(LibraryTypes.Int8, true, 8),
        new(LibraryTypes.UInt8, false, 8),
        new(LibraryTypes.Int16, true, 16),
        new(LibraryTypes.UInt16, false, 16),
        new(LibraryTypes.Int32, true, 32),
        new(LibraryTypes.UInt32, false, 32),
        new(LibraryTypes.Int64, true, 64),
        new(LibraryTypes.UInt64, false, 64)
    };

    private const int NARROWEST_WITH_OWN_ARITHMETIC = 32;
    private const int WIDTH_WITH_BUILTIN_HASH_CODE = 64;


    // Private methods.
    private void AddCommonBindings(LibraryBindingTable table, SignatureBuilder member)
    {
        table.AddIntrinsic(member.StaticMethod(LibraryMemberNames.PARSE, member.Self, LibraryTypes.String),
            IntrinsicOperation.Parse);
        table.AddIntrinsic(member.StaticMethod(LibraryMemberNames.TRY_PARSE, LibraryTypes.Boolean,
            LibraryTypes.String, SignatureParameter.Out(member.Self)), IntrinsicOperation.TryParse);
        table.AddIntrinsic(member.Method(LibraryMemberNames.TO_STRING, LibraryTypes.String),
            IntrinsicOperation.ToText);

        table.AddIntrinsic(member.UnaryOperator(OverloadableOperator.Increment), IntrinsicOperation.Increment);
        table.AddIntrinsic(member.UnaryOperator(OverloadableOperator.Decrement), IntrinsicOperation.Decrement);
    }

    private void AddArithmeticBindings(LibraryBindingTable table, SignatureBuilder member, bool isSigned)
    {
        table.AddIntrinsic(member.UnaryOperator(OverloadableOperator.UnaryPlus), IntrinsicOperation.Plus);
        if (isSigned)
        {
            table.AddIntrinsic(member.UnaryOperator(OverloadableOperator.Negation), IntrinsicOperation.Negate);
        }
        table.AddIntrinsic(member.UnaryOperator(OverloadableOperator.BitwiseComplement),
            IntrinsicOperation.Complement);

        foreach (OperatorIntrinsic Binary in OperatorIntrinsics.Arithmetic.Concat(OperatorIntrinsics.Bitwise))
        {
            table.AddIntrinsic(member.BinaryOperator(Binary.Operator), Binary.Operation);
        }
        foreach (OperatorIntrinsic Shift in OperatorIntrinsics.Shifts)
        {
            table.AddIntrinsic(member.Shift(Shift.Operator), Shift.Operation);
        }
        foreach (OperatorIntrinsic Comparison in OperatorIntrinsics.Equality.Concat(OperatorIntrinsics.Ordering))
        {
            table.AddIntrinsic(member.Comparison(Comparison.Operator), Comparison.Operation);
        }
    }


    // Inherited methods.
    public void AddBindings(LibraryBindingTable table)
    {
        ArgumentNullException.ThrowIfNull(table, nameof(table));

        foreach (IntegerFacts Integer in _integers)
        {
            SignatureBuilder Member = new(Integer.Type);
            AddCommonBindings(table, Member);

            if (Integer.BitCount >= NARROWEST_WITH_OWN_ARITHMETIC)
            {
                AddArithmeticBindings(table, Member, Integer.IsSigned);
            }
            if (Integer.BitCount == WIDTH_WITH_BUILTIN_HASH_CODE)
            {
                table.AddIntrinsic(Member.Method(LibraryMemberNames.GET_HASH_CODE, LibraryTypes.Int32),
                    IntrinsicOperation.HashCode);
            }
        }
    }
}
