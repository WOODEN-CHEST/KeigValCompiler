namespace KeigValCompiler.Semantician.Member;

internal class PackIndexer : PackMember, IExplicitInterfaceMember, IPackAccessorHolder
{
    // Fields.
    public TypeTargetIdentifier? ExplicitInterface { get; set; } = null;
    public PackFunction? GetFunction { get; set; }
    public PackFunction? SetFunction { get; set; }
    public PackFunction? InitFunction { get; set; }


    // Internal fields.
    internal TypeTargetIdentifier Type { get; set; }
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
            if (InitFunction != null)
            {
                SubMembers.Add(InitFunction);
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