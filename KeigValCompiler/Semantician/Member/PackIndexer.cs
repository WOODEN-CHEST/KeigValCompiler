namespace KeigValCompiler.Semantician.Member;

internal class PackIndexer : PackMember, IExplicitInterfaceMember
{
    // Fields.
    public TypeTargetIdentifier? ExplicitInterface { get; set; } = null;


    // Internal fields.
    internal TypeTargetIdentifier Type { get; set; }
    internal PackFunction? GetFunction { get; set; }
    internal PackFunction? SetFunction { get; set; }
    internal FunctionParameterCollection Parameters { get; } = new();
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

            return SubMembers.ToArray();
        }
    }
    internal override IEnumerable<PackMember> AllSubMembers => SubMembers;


    // Constructors.
    public PackIndexer(Identifier identifier, 
        TypeTargetIdentifier type,
        PackSourceFile sourceFile) : base(identifier, sourceFile)
    {
        Type = type;
    }
}