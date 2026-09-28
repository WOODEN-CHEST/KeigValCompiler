using KeigValCompiler.Semantician.Member;

namespace KeigValCompiler.Semantician.Library.Bindings;

/* KGVL.Decimal. Its conversions are in NumericConversionBindings. */
internal class DecimalBindings : ILibraryBindingProvider
{
    // Static fields.
    private const string NAME_FLOOR = "Floor";
    private const string NAME_CEILING = "Ceiling";
    private const string NAME_TRUNCATE = "Truncate";
    private const string NAME_ROUND = "Round";

    /* The functions which approximate, each declared twice: as listed here, and with an int number of
     * iterations after the parameters listed. */
    private static readonly ApproximationFunction[] _approximations = new ApproximationFunction[]
    {
        new("Sqrt", IntrinsicOperation.Sqrt, LibraryTypes.Decimal),
        new("Cbrt", IntrinsicOperation.Cbrt, LibraryTypes.Decimal),
        new("RootN", IntrinsicOperation.RootN, LibraryTypes.Decimal, LibraryTypes.Int32),
        new("Pow", IntrinsicOperation.Pow, LibraryTypes.Decimal, LibraryTypes.Decimal),
        new("Exp", IntrinsicOperation.Exp, LibraryTypes.Decimal),
        new("Exp10", IntrinsicOperation.Exp10, LibraryTypes.Decimal),
        new("Log", IntrinsicOperation.Log, LibraryTypes.Decimal),
        new("Log", IntrinsicOperation.LogBase, LibraryTypes.Decimal, LibraryTypes.Decimal),
        new("Log2", IntrinsicOperation.Log2, LibraryTypes.Decimal),
        new("Log10", IntrinsicOperation.Log10, LibraryTypes.Decimal),
        new("Sin", IntrinsicOperation.Sin, LibraryTypes.Decimal),
        new("Cos", IntrinsicOperation.Cos, LibraryTypes.Decimal),
        new("Tan", IntrinsicOperation.Tan, LibraryTypes.Decimal),
        new("Asin", IntrinsicOperation.Asin, LibraryTypes.Decimal),
        new("Acos", IntrinsicOperation.Acos, LibraryTypes.Decimal),
        new("Atan", IntrinsicOperation.Atan, LibraryTypes.Decimal),
        new("Atan2", IntrinsicOperation.Atan2, LibraryTypes.Decimal, LibraryTypes.Decimal),
        new("Sinh", IntrinsicOperation.Sinh, LibraryTypes.Decimal),
        new("Cosh", IntrinsicOperation.Cosh, LibraryTypes.Decimal),
        new("Tanh", IntrinsicOperation.Tanh, LibraryTypes.Decimal)
    };


    // Private methods.
    private void AddRoundingBindings(LibraryBindingTable table, SignatureBuilder member)
    {
        table.AddIntrinsic(member.StaticMethod(NAME_FLOOR, member.Self, member.Self), IntrinsicOperation.Floor);
        table.AddIntrinsic(member.StaticMethod(NAME_CEILING, member.Self, member.Self),
            IntrinsicOperation.Ceiling);
        table.AddIntrinsic(member.StaticMethod(NAME_TRUNCATE, member.Self, member.Self),
            IntrinsicOperation.Truncate);
        table.AddIntrinsic(member.StaticMethod(NAME_ROUND, member.Self, member.Self), IntrinsicOperation.Round);
        table.AddIntrinsic(member.StaticMethod(NAME_ROUND, member.Self, member.Self, LibraryTypes.Int32),
            IntrinsicOperation.RoundToDecimals);
    }

    private void AddApproximationBindings(LibraryBindingTable table, SignatureBuilder member)
    {
        foreach (ApproximationFunction Approximation in _approximations)
        {
            SignatureParameter[] Parameters = Approximation.Parameters.ToArray();
            SignatureParameter[] WithIterations = Parameters
                .Append<SignatureParameter>(LibraryTypes.Int32).ToArray();

            table.AddIntrinsic(member.StaticMethod(Approximation.Name, member.Self, Parameters),
                Approximation.Operation);
            table.AddIntrinsic(member.StaticMethod(Approximation.Name, member.Self, WithIterations),
                Approximation.Operation);
        }
    }

    private void AddOperatorBindings(LibraryBindingTable table, SignatureBuilder member)
    {
        table.AddIntrinsic(member.UnaryOperator(OverloadableOperator.UnaryPlus), IntrinsicOperation.Plus);
        table.AddIntrinsic(member.UnaryOperator(OverloadableOperator.Negation), IntrinsicOperation.Negate);
        table.AddIntrinsic(member.UnaryOperator(OverloadableOperator.Increment), IntrinsicOperation.Increment);
        table.AddIntrinsic(member.UnaryOperator(OverloadableOperator.Decrement), IntrinsicOperation.Decrement);

        foreach (OperatorIntrinsic Arithmetic in OperatorIntrinsics.Arithmetic)
        {
            table.AddIntrinsic(member.BinaryOperator(Arithmetic.Operator), Arithmetic.Operation);
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
        SignatureBuilder Member = new(LibraryTypes.Decimal);

        table.AddIntrinsic(Member.StaticMethod(LibraryMemberNames.PARSE, Member.Self, LibraryTypes.String),
            IntrinsicOperation.Parse);
        table.AddIntrinsic(Member.StaticMethod(LibraryMemberNames.TRY_PARSE, LibraryTypes.Boolean,
            LibraryTypes.String, SignatureParameter.Out(Member.Self)), IntrinsicOperation.TryParse);
        table.AddIntrinsic(Member.Method(LibraryMemberNames.TO_STRING, LibraryTypes.String),
            IntrinsicOperation.ToText);
        table.AddIntrinsic(Member.Method(LibraryMemberNames.GET_HASH_CODE, LibraryTypes.Int32),
            IntrinsicOperation.HashCode);

        AddRoundingBindings(table, Member);
        AddApproximationBindings(table, Member);
        AddOperatorBindings(table, Member);
    }
}
