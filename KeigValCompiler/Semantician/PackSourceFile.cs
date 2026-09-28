namespace KeigValCompiler.Semantician;

internal class PackSourceFile
{
    // Internal fields.
    internal DataPack Pack { get; private set; }
    internal PackNameSpace[] NamespaceImports => _namespaceImports.ToArray();
    internal PackNameSpace[] Namespaces => _namespaces.ToArray();
    internal PackNameSpace[] AllUsedNamespaces => _namespaces.Concat(_namespaceImports).ToArray();
    internal string Path { get; private init; }
    internal SourceFileKind Kind { get; private init; }
    internal bool IsLibraryFile => Kind == SourceFileKind.Library;


    // Private fields.
    private readonly List<PackNameSpace> _namespaceImports = new();
    private readonly List<PackNameSpace> _namespaces = new();


    // Constructors.
    internal PackSourceFile(DataPack pack, string path, SourceFileKind kind)
    {
        Pack = pack ?? throw new ArgumentNullException(nameof(pack));
        Path = path ?? throw new ArgumentNullException(nameof(path));
        Kind = kind;
    }


    // Internal methods.
    internal void AddNamespaceImport(PackNameSpace packNameSpace)
    {
        _namespaceImports.Add(packNameSpace ?? throw new ArgumentNullException(nameof(packNameSpace)));
    }

    internal void AddNamespace(PackNameSpace nameSpace)
    {
        _namespaces.Add(nameSpace ?? throw new ArgumentNullException(nameof(nameSpace)));
    }
}