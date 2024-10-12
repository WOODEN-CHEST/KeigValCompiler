namespace KeigValCompiler.Semantician.Member;

internal abstract class Statement
{
    // Methods.
    public abstract IIdentifiable? GetReturnType();
    public abstract object? GetValue();
}