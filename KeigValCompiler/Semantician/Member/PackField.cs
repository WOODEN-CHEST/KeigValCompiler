using KeigValCompiler.Semantician.Bound;
using KeigValCompiler.Semantician.Member.Code;

namespace KeigValCompiler.Semantician.Member;

internal class PackField : PackMember
{
    // Internal fields.
    internal TypeTargetIdentifier Type { get; set; }
    internal Statement? InitialValue { get; set; }

    /* The starting value as resolution binds it, converted to the field's type, once it is. */
    internal BoundExpression? BoundInitialValue { get; set; } = null;

    /* A constant's value, once its starting value is bound, or null when it is not a constant, or its value
     * could not be had, which has been reported. */
    internal ConstantValue? ConstantValue { get; set; } = null;


    // Constructors.
    public PackField(Identifier identifier, TypeTargetIdentifier fieldType, PackSourceFile sourceFile)
        : base(identifier, sourceFile)
    {
        Type = fieldType;
    }
}