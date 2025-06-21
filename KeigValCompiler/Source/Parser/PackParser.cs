using KeigValCompiler.Semantician;

namespace KeigValCompiler.Source;

internal class PackParser
{
    // Internal fields.
    internal const string SOURCE_FILE_EXTENSION = ".kgvl";


    // Private fields.
    private readonly string _sourceDirPath;


    // Constructors.
    internal PackParser(string sourceDirectory)
    {
        ArgumentNullException.ThrowIfNull(sourceDirectory, nameof(sourceDirectory));
        _sourceDirPath = sourceDirectory;
    }


    // Internal methods.
    internal DataPack ParsePack()
    {
        DataPack Pack = new();

        if (!Directory.Exists(_sourceDirPath))
        {
            throw new DirectoryNotFoundException(nameof(_sourceDirPath));
        }

        foreach (string sourceFile in Directory.GetFiles(
            _sourceDirPath, $"*{SOURCE_FILE_EXTENSION}", SearchOption.AllDirectories))
        {
            SourceFileParser FileParser = new(sourceFile, Pack);
            FileParser.ParseFile(Pack);
        }

        return Pack;
    }
}