using KeigValCompiler.Semantician.Member;

namespace KeigValCompiler.Semantician.Library;

/* A parameter as a signature has it: its type, and whether it is ref, out, in or params. Its name is
 * no part of it, as in C#. */
internal sealed record SignatureParameter(SignatureType Type, FunctionParameterModifier Modifier)
{
    // Internal static methods.
    internal static SignatureParameter Out(SignatureType type)
    {
        return new(type, FunctionParameterModifier.Out);
    }

    internal static SignatureParameter Ref(SignatureType type)
    {
        return new(type, FunctionParameterModifier.Ref);
    }


    // Operators.
    /* So that a binding can write a plain parameter as just its type. */
    public static implicit operator SignatureParameter(SignatureType type) =>
        new(type, FunctionParameterModifier.None);

    public static implicit operator SignatureParameter(LibraryType type) =>
        new(SignatureType.Of(type), FunctionParameterModifier.None);
}
