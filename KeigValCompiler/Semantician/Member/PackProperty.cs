using KeigValCompiler.Semantician.Member.Code;

namespace KeigValCompiler.Semantician.Member;

internal class PackProperty : PackMember
{
    // Internal fields.
    internal TypeTargetIdentifier Type { get; set; }
    internal Statement? InitialValue { get; set; }
    internal PackFunction? GetFunction { get;set; }
    internal PackFunction? SetFunction { get; set; }
    internal PackFunction? InitFunction { get; set; }
    internal override IEnumerable<PackMember> SubMembers
    {
        get
        {
            List<PackMember> SubMembers = new();

            if (GetFunction != null)
            {
                SubMembers.Add(GetFunction);
            }
            if (SetFunction != null)
            {
                SubMembers.Add(SetFunction);
            }
            if (InitFunction != null)
            {
                SubMembers.Add(InitFunction);
            }

            return SubMembers.ToArray();
        }
    }
    internal override IEnumerable<PackMember> AllSubMembers => SubMembers;


    // Constructors.
    internal PackProperty(Identifier identifier, TypeTargetIdentifier type, PackSourceFile sourceFile)
        : base(identifier, sourceFile)
    {
        Type = type;
    }
}