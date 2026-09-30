using KeigValCompiler.Semantician.Member.Code;

namespace KeigValCompiler.Semantician.Member;

internal class PackProperty : PackMember, IExplicitInterfaceMember, IPackAccessorHolder
{
    // Fields.
    public TypeTargetIdentifier? ExplicitInterface { get; set; } = null;
    public PackFunction? GetFunction { get; set; }
    public PackFunction? SetFunction { get; set; }
    public PackFunction? InitFunction { get; set; }


    // Internal fields.
    internal TypeTargetIdentifier Type { get; set; }
    internal Statement? InitialValue { get; set; }

    /* Made by the compiler rather than written, as a record's property for one of its positional
     * parameters is. */
    internal bool IsSynthesized { get; init; } = false;
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