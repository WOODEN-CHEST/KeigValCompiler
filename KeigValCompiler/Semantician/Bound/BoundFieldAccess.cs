using KeigValCompiler.Semantician.Member;
using KeigValCompiler.Semantician.Member.Code;
using KeigValCompiler.Semantician.Types;

namespace KeigValCompiler.Semantician.Bound;

/* A field, read or written: one of the object the receiver is, or a static one, which has no receiver. A
 * constant field's value is the expression's. */
internal sealed class BoundFieldAccess : BoundExpression
{
    // Internal fields.
    internal BoundExpression? Receiver { get; private init; }
    internal PackField Field { get; private init; }

    /* The type holding the field as it is seen where it is named, as Cache<int> holds the "Value" of
     * "Cache<int>.Value", whose type arguments decide which static field is meant. Null for a field a
     * namespace holds. */
    internal DeclaredType? ContainingType { get; private init; }

    internal override IEnumerable<BoundNode> Children =>
        (Receiver == null) ? Array.Empty<BoundNode>() : new BoundNode[] { Receiver };


    // Constructors.
    internal BoundFieldAccess(Statement syntax,
        BoundExpression? receiver,
        PackField field,
        DeclaredType? containingType,
        SemanticType type,
        ConstantValue? constantValue)
        : base(syntax, type ?? throw new ArgumentNullException(nameof(type)), constantValue)
    {
        Receiver = receiver;
        Field = field ?? throw new ArgumentNullException(nameof(field));
        ContainingType = containingType;
    }
}
