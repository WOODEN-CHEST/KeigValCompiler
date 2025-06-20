namespace KeigValCompiler.Semantician.Resolver;

internal class FullPackResolver : IPackResolver
{
    // Inherited methods.
    public void ResolvePack(PackResolutionContext context)
    {
        ArgumentNullException.ThrowIfNull(context, nameof(context));

        // Order of method calls here must NOT be changed, or else this will incorrectly resolve things.
        new NameSpaceResolver().ResolvePack(context);
        new ParentItemResolver().ResolvePack(context);
        new IdentifierResolver(context.IdentifierGenerator).ResolvePack(context);
    }
}