using KeigValCompiler.Semantician.Member.Code;
using KeigValCompiler.Semantician.Types;

namespace KeigValCompiler.Semantician.Bound;

/* A bound value, or what stands before a '.' and is not one: a type or a namespace. Its type is null for a
 * value with no type of its own, which takes one from where it is used, as the literal null and "default"
 * written alone do, and for a namespace. A value which could not be bound has the error type. */
internal abstract class BoundExpression : BoundNode
{
    // Internal fields.
    internal SemanticType? Type { get; private init; }

    /* The value known at compile time, as C# defines constant expressions, or null for one which is not. */
    internal ConstantValue? ConstantValue { get; private init; }


    // Protected fields.
    protected override bool IsErroneous => Type is ErrorType;


    // Constructors.
    internal BoundExpression(Statement syntax, SemanticType? type, ConstantValue? constantValue)
        : base(syntax ?? throw new ArgumentNullException(nameof(syntax)))
    {
        Type = type;
        ConstantValue = constantValue;
    }
}
