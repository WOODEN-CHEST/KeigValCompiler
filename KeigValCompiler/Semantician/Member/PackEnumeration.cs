namespace KeigValCompiler.Semantician.Member;

internal class PackEnumeration : PackMember, IPackType
{
    // Fields.
    public IEnumerable<PackFunction> Constructors => Enumerable.Empty<PackFunction>();


    // Internal fields.
    /* In declaration order, which working out the values depends on. Constants with the same name are
     * all kept, so that the validation stage can report them. */
    internal IReadOnlyList<PackEnumerationConstant> Constants => _constants;
    internal int ConstantCount => _constants.Count;


    // Private fields.
    private readonly List<PackEnumerationConstant> _constants = new();


    // Constructors.
    public PackEnumeration(Identifier identifier, PackSourceFile sourceFile) : base(identifier, sourceFile) { }



    // Methods.
    public void AddConstant(PackEnumerationConstant constant)
    {
        ArgumentNullException.ThrowIfNull(constant, nameof(constant));
        _constants.Add(constant);
    }

    public void RemoveConstant(PackEnumerationConstant constant)
    {
        _constants.Remove(constant);
    }
}