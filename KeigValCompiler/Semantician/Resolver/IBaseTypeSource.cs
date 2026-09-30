using KeigValCompiler.Semantician.Member;

namespace KeigValCompiler.Semantician.Resolver;

/* The types a type names as its bases, as declarations, for looking names up while signatures are still being
 * resolved: a lookup inside one type may need the bases of a type around it, or of a qualifier, before its
 * base list has been reached. */
internal interface IBaseTypeSource
{
    // Methods.
    /* A class's base class, if it has one, and then the interfaces it lists; an interface's or a structure's
     * interfaces. None for a type whose base list is still being resolved, since, as in C#, a type has no
     * bases while its own base list is resolved. */
    IEnumerable<PackMember> GetBaseTypes(PackMember type);

    /* Whether a type's own base list is being resolved, so that it has no bases yet. */
    bool IsResolvingBases(PackMember type);
}
