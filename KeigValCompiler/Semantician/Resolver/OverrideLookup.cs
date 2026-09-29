using KeigValCompiler.Semantician.Member;
using KeigValCompiler.Semantician.Types;

namespace KeigValCompiler.Semantician.Resolver;

/* What looking for the member an override overrides found, in the nearest base class where the looking
 * stopped: the member overridden, and a second one when two of that class are the same member once its
 * generic parameters are given types, which leaves which is overridden unknown. Or, instead, a member of
 * that name and another kind, which hides whatever further back the override could have overridden. All
 * are null when no base class has any of these. */
internal record OverrideLookup(DeclaredType? Holder,
    DeclaredSignature? Overridden,
    DeclaredSignature? Duplicate,
    PackMember? Hiding);
