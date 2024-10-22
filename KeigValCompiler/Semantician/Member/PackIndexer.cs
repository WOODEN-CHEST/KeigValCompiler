namespace KeigValCompiler.Semantician.Member;

internal class PackIndexer : PackMember
{
    // Fields.
    public Identifier SelfIdentifier => throw new NotImplementedException();


    // Internal fields.
    internal Identifier Type { get; set; }
    internal PackFunction? GetFunction { get; set; }
    internal PackFunction? SetFunction { get; set; }


    // Constructors.
    public PackIndexer(Identifier identifier, PackSourceFile sourceFile) : base(identifier, sourceFile) { }
}