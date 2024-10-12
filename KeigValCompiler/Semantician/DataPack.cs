namespace KeigValCompiler.Semantician;

internal class DataPack
{
    // Internal fields.
    internal PackSourceFile[] SourceFiles => _sourceFiles.ToArray();
    internal PackNameSpace[] NameSpaces => _sourceFiles.SelectMany(file => file.Namespaces).ToArray();


    // Private fields.
    private List<PackSourceFile> _sourceFiles = new();


    // Methods.
    public void AddSourceFile(PackSourceFile sourceFile)
    {
        _sourceFiles.Add(sourceFile ?? throw new ArgumentNullException(nameof(sourceFile)));
    }
}