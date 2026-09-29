namespace KeigValCompiler.Semantician;

internal class PackSourceFile
{
    // Internal fields.
    internal DataPack Pack { get; private set; }
    internal PackNameSpace[] NamespaceImports => _namespaceImports.Select(import => import.NameSpace).ToArray();
    internal NamespaceImport[] ImportDirectives => _namespaceImports.ToArray();
    internal PackNameSpace[] Namespaces => _namespaces.ToArray();
    internal PackNameSpace[] AllUsedNamespaces => _namespaces.Concat(NamespaceImports).ToArray();
    internal string Path { get; private init; }
    internal SourceFileKind Kind { get; private init; }
    internal bool IsLibraryFile => Kind == SourceFileKind.Library;


    // Private fields.
    private readonly List<NamespaceImport> _namespaceImports = new();
    private readonly List<PackNameSpace> _namespaces = new();


    // Constructors.
    internal PackSourceFile(DataPack pack, string path, SourceFileKind kind)
    {
        Pack = pack ?? throw new ArgumentNullException(nameof(pack));
        Path = path ?? throw new ArgumentNullException(nameof(path));
        Kind = kind;
    }


    // Internal methods.
    internal void AddNamespaceImport(PackNameSpace packNameSpace, SourceFileOrigin origin)
    {
        _namespaceImports.Add(new(packNameSpace ?? throw new ArgumentNullException(nameof(packNameSpace)),
            origin));
    }

    internal void AddNamespace(PackNameSpace nameSpace)
    {
        _namespaces.Add(nameSpace ?? throw new ArgumentNullException(nameof(nameSpace)));
    }
}