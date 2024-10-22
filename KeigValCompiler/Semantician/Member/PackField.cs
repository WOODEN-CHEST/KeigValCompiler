using KeigValCompiler.Semantician.Member.Code;

namespace KeigValCompiler.Semantician.Member;

internal class PackField : PackMember
{
    // Internal fields.
    internal Identifier Type { get; set; }
    internal Statement? InitialValue { get; set; }


    // Constructors.
    public PackField(Identifier identifier, PackSourceFile sourceFile) : base(identifier, sourceFile) { }
}