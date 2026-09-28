namespace KeigValCompiler.Semantician.Resolver;

/* Resolves declarations and binds the standard library. Function bodies are not resolved yet. */
internal class FullPackResolver : IPackResolver
{
    // Inherited methods.
    public void ResolvePack(PackResolutionContext context)
    {
        ArgumentNullException.ThrowIfNull(context, nameof(context));

        /* The order must NOT be changed: each pass relies on what the ones before it set. Types are
         * named before any is looked up, known types are found before keywords can resolve through
         * them, and signatures are resolved before members can be named after their parameter types
         * or matched against what the compiler implements. */
        new NameSpaceResolver().ResolvePack(context);
        new ParentItemResolver().ResolvePack(context);
        new TypeDeclarationResolver().ResolvePack(context);
        new KnownTypeResolver().ResolvePack(context);
        new SignatureResolver().ResolvePack(context);
        new MemberIdentifierResolver().ResolvePack(context);
        new LibraryBindingResolver().ResolvePack(context);
    }
}
