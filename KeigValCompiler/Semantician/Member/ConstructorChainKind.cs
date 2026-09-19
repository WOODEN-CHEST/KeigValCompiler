namespace KeigValCompiler.Semantician.Member;

/* Which constructor, if any, runs before this one's own body. */
internal enum ConstructorChainKind
{
    None,
    This,
    Base
}
