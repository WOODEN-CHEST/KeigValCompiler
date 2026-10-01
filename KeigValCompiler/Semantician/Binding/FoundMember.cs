using KeigValCompiler.Semantician.Member;
using KeigValCompiler.Semantician.Types;

namespace KeigValCompiler.Semantician.Binding;

/* A member a lookup found, with the type holding it as seen from where the lookup began, as Base<int> holds
 * what Derived : Base<int> inherits from Base<T>, so that the member's types can be read in terms of it. The
 * holder is null for a member a namespace holds. */
internal sealed record FoundMember(PackMember Member, DeclaredType? Holder);
