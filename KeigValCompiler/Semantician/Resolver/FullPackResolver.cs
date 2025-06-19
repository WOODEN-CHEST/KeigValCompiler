namespace KeigValCompiler.Semantician.Resolver;

internal class FullPackResolver : IPackResolver
{
    // Private fields.
    private readonly IPackResolver _namespaceResolver = new NameSpaceResolver();
    private readonly IPackResolver _parentItemResolver = new ParentItemResolver();
    private readonly IPackResolver _identifierResolver = new IdentifierResolver();


    // Inherited methods.
    public void ResolvePack(PackResolutionContext context)
    {
        ArgumentNullException.ThrowIfNull(context, nameof(context));

        // Order of method calls here must NOT be changed, or else this will incorrectly resolve things.
        _namespaceResolver.ResolvePack(context);
        _parentItemResolver.ResolvePack(context);
        _identifierResolver.ResolvePack(context);
    }
}