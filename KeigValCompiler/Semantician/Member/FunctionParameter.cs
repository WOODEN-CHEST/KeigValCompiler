using KeigValCompiler.Semantician.Member.Code;

namespace KeigValCompiler.Semantician.Member;

internal class FunctionParameter : IIdentifiable
{
    // Fields.
    public Identifier SelfIdentifier { get; private init; }


    // Internal fields.
    internal TypeTargetIdentifier? Type { get; private init; }
    internal FunctionParameterModifier Modifiers { get; private init; }

    /* The value an argument left out takes, as in "int count = 5", which makes the parameter optional. It is
     * kept as the expression written, which has to be a constant, to be computed and checked once the
     * expressions of function bodies are resolved. */
    internal Statement? DefaultValue { get; init; }


    // Constructors.
    internal FunctionParameter(TypeTargetIdentifier? type, Identifier selfIdentifier, FunctionParameterModifier modifier)
    {
        Type = type;
        SelfIdentifier = selfIdentifier ?? throw new ArgumentNullException(nameof(selfIdentifier));
        Modifiers = modifier;
    }


    // Inherited methods.
    public override string ToString()
    {
        return $"{SelfIdentifier} (Type {Type})";
    }

    public override bool Equals(object? obj)
    {
        if (obj is FunctionParameter FunctionParam)
        {
            return SelfIdentifier.Equals(FunctionParam.SelfIdentifier) && Type.Equals(FunctionParam.Type);
        }
        return false;
    }

    public override int GetHashCode()
    {
        return SelfIdentifier.GetHashCode();
    }
}