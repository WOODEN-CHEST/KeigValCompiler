namespace KeigValCompiler.Semantician.Member;

internal class FunctionParameter : IIdentifiable
{
    // Fields.
    public Identifier SelfIdentifier { get; private init; }


    // Internal fields.
    internal TypeTargetIdentifier Type { get; private init; }
    internal FunctionParameterModifier Modifiers { get; private init; }


    // Constructors.
    internal FunctionParameter(TypeTargetIdentifier type, Identifier selfIdentifier, FunctionParameterModifier modifier)
    {
        Type = type ?? throw new ArgumentNullException(nameof(type));
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