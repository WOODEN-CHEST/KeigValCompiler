using KeigValCompiler.Semantician.Member;

namespace KeigValCompiler.Semantician.Resolver;

internal class PackResolutionContext
{
    // Fields.
    internal required IIdentifierSearcher IdentifierSearcher { get; init; }
    internal required IPrimitiveValueResolver PrimitiveResolver { get; init; }
    internal required BuiltInTypeRegistry Registry { get; init; }
    internal required DataPack Pack { get; init; }
    internal required IdentifierGenerator IdentifierGenerator { get; init; }
}