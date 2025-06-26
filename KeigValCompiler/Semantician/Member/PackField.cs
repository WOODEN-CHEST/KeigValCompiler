using KeigValCompiler.Semantician.Member.Code;

namespace KeigValCompiler.Semantician.Member;

internal class PackField : PackMember
{
    // Internal fields.
    internal TypeTargetIdentifier Type { get; set; }
    internal Statement? InitialValue { get; set; }


    // Constructors.
    public PackField(Identifier identifier, TypeTargetIdentifier fieldType, PackSourceFile sourceFile)
        : base(identifier, sourceFile)
    {
        Type = fieldType;
    }
}