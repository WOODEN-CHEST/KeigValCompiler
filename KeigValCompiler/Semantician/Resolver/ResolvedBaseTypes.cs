using KeigValCompiler.Semantician.Member;

namespace KeigValCompiler.Semantician.Resolver;

/* The bases each type names, as declarations, once every base list is resolved, for the checks after
 * SignatureResolver: what the lookup saw, now that no base list is still being resolved. */
internal sealed class ResolvedBaseTypes : IBaseTypeSource
{
    // Inherited methods.
    public IEnumerable<PackMember> GetBaseTypes(PackMember type)
    {
        ArgumentNullException.ThrowIfNull(type, nameof(type));

        return MemberRelations.GetBaseDeclarations(type);
    }

    public bool IsResolvingBases(PackMember type)
    {
        ArgumentNullException.ThrowIfNull(type, nameof(type));

        return false;
    }
}
