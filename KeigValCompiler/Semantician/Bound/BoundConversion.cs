using KeigValCompiler.Semantician.Member.Code;
using KeigValCompiler.Semantician.Types;

namespace KeigValCompiler.Semantician.Bound;

/* A value converted to another type: implicitly, where a value of that type is needed, or as a cast says.
 * One which does not exist has been reported, and still has the type it was to be converted to, so that
 * nothing using it reports the mistake again. A constant converted is still one, when C# says it is. */
internal sealed class BoundConversion : BoundExpression
{
    // Internal fields.
    internal BoundExpression Operand { get; private init; }
    internal Conversion Conversion { get; private init; }

    /* Written as a cast, as opposed to made where a value of the type was needed. */
    internal bool IsExplicitlyWritten { get; private init; }

    internal override IEnumerable<BoundNode> Children => new BoundNode[] { Operand };


    // Protected fields.
    protected override bool IsErroneous => base.IsErroneous || !Conversion.DoesExist;


    // Constructors.
    internal BoundConversion(Statement syntax,
        BoundExpression operand,
        Conversion conversion,
        SemanticType type,
        bool isExplicitlyWritten,
        ConstantValue? constantValue)
        : base(syntax, type ?? throw new ArgumentNullException(nameof(type)), constantValue)
    {
        Operand = operand ?? throw new ArgumentNullException(nameof(operand));
        Conversion = conversion ?? throw new ArgumentNullException(nameof(conversion));
        IsExplicitlyWritten = isExplicitlyWritten;
    }
}
