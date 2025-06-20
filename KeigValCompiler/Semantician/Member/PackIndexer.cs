namespace KeigValCompiler.Semantician.Member;

internal class PackIndexer : PackMember
{
    // Internal fields.
    internal Identifier Type { get; set; }
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
    public PackIndexer(Identifier identifier, PackSourceFile sourceFile) : base(identifier, sourceFile) { }
}