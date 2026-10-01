using KeigValCompiler.Semantician.Member;
using KeigValCompiler.Semantician.Member.Code;
using KeigValCompiler.Semantician.Types;

namespace KeigValCompiler.Semantician.Bound;

/* A property, read or written: one of the object the receiver is, or a static one, which has no receiver. */
internal sealed class BoundPropertyAccess : BoundExpression
{
    // Internal fields.
    internal BoundExpression? Receiver { get; private init; }
    internal PackProperty Property { get; private init; }

    /* The type holding the property as it is seen where it is named; null for one a namespace holds. */
    internal DeclaredType? ContainingType { get; private init; }

    /* The accessor which reads or writes it, as it is used: the getter, or the setter, written "set" or
     * "init". An override declaring only some of its accessors inherits the others, as in C#, so this may
     * belong to a property the named one overrides. Null where a property with no setter is given its value
     * in a constructor, which writes what the property stores directly. */
    internal PackFunction? Accessor { get; private init; }

    internal override IEnumerable<BoundNode> Children =>
        (Receiver == null) ? Array.Empty<BoundNode>() : new BoundNode[] { Receiver };


    // Constructors.
    internal BoundPropertyAccess(Statement syntax,
        BoundExpression? receiver,
        PackProperty property,
        DeclaredType? containingType,
        PackFunction? accessor,
        SemanticType type) : base(syntax, type ?? throw new ArgumentNullException(nameof(type)), null)
    {
        Receiver = receiver;
        Property = property ?? throw new ArgumentNullException(nameof(property));
        ContainingType = containingType;
        Accessor = accessor;
    }
}
