namespace KeigValCompiler.Semantician.Resolver;

internal class PackResolutionContext
{
    // Fields.
    internal required IIdentifierSearcher IdentifierSearcher
    {
        get => _identifierSearcher;
        init => _identifierSearcher = value ?? throw new ArgumentNullException(nameof(value));
    }

    internal required IPrimitiveValueResolver PrimitiveResolver
    {
        get => _primitiveValueResolver;
        init => _primitiveValueResolver = value ?? throw new ArgumentNullException(nameof(value));
    }

    internal required BuiltInTypeRegistry Registry
    {
        get => _registry;
        init => _registry = value ?? throw new ArgumentNullException(nameof(value));
    }

    internal required DataPack Pack
    {
        get => _dataPack;
        init => _dataPack = value ?? throw new ArgumentNullException(nameof(value));
    }



    // Private fields.
    private readonly IIdentifierSearcher _identifierSearcher;
    private readonly IPrimitiveValueResolver _primitiveValueResolver;
    private readonly BuiltInTypeRegistry _registry;
    private readonly DataPack _dataPack;
}