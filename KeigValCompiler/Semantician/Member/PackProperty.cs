using KeigValCompiler.Semantician.Member.Code;

namespace KeigValCompiler.Semantician.Member;

internal class PackProperty : PackMember
{
    // Internal fields.
    internal Identifier Type { get; set; }
    internal Statement? InitialValue { get; set; }
    internal PackFunction? GetFunction { get;set; }
    internal PackFunction? SetFunction { get; set; }
    internal PackFunction? InitFunction { get; set; }


    // Constructors.
    internal PackProperty(Identifier identifier, PackSourceFile sourceFile) : base(identifier, sourceFile) { }
}