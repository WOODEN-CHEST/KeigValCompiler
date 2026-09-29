using KeigValCompiler.Semantician.Member.Code;

namespace KeigValCompiler.Semantician.Member;

/* Derives from PackFunction so that constructors travel through the collections and resolution
 * passes that already handle functions. A constructor's ReturnType is always null and its
 * SelfIdentifier is the name of the type it constructs. */
internal class PackConstructor : PackFunction
{
    // Fields.
    internal ConstructorChainKind ChainKind { get; set; } = ConstructorChainKind.None;

    /* Arguments passed to the chained constructor, as in the "a" of ": this(a)". Empty when
     * ChainKind is None. */
    internal StatementCollection ChainArguments { get; } = new();

    /* A record's primary constructor, written as a parameter list after the record's name, which has
     * no body of its own. */
    internal bool IsPrimary { get; init; } = false;


    // Constructors.
    internal PackConstructor(Identifier identifier, PackSourceFile sourceFile)
        : base(identifier, sourceFile) { }
}
