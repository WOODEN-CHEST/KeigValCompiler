using KeigValCompiler.Semantician;

namespace KeigValCompiler.Source;

internal class PackParser
{
    // Internal fields.
    internal const string SOURCE_FILE_EXTENSION = ".kgvl";


    // Private fields.
    private readonly string _sourceDirPath;
    private readonly string _destDirPath;


    // Constructors.
    internal PackParser(string sourceDirectory, string? destDirectory)
    {
        if (sourceDirectory == null)
        {
            throw new ArgumentNullException(nameof(sourceDirectory));
        }
        if (!Directory.Exists(sourceDirectory))
        {
            throw new DirectoryNotFoundException(nameof(sourceDirectory));
        }
        _sourceDirPath = sourceDirectory;

        _destDirPath = destDirectory ?? _sourceDirPath;
        if (_destDirPath == string.Empty)
        {
            _destDirPath = _sourceDirPath;
        }
        if (!Directory.Exists(_destDirPath))
        {
            throw new DirectoryNotFoundException(nameof(_destDirPath));
        }
    }


    // Internal methods.
    internal DataPack ParsePack()
    {
        DataPack Pack = new();

        foreach (string sourceFile in Directory.GetFiles(_sourceDirPath, $"*{SOURCE_FILE_EXTENSION}", SearchOption.AllDirectories))
        {
            SourceFileParser FileParser = new(sourceFile, Pack);
            FileParser.ParseFile(Pack);
        }

        return Pack;
    }
}