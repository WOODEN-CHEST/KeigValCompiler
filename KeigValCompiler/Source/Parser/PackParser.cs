using KeigValCompiler.Error;
using KeigValCompiler.Semantician;
using KeigValCompiler.Source.Parser;

namespace KeigValCompiler.Source;

internal class PackParser
{
    // Internal fields.
    internal const string SOURCE_FILE_EXTENSION = ".kgvl";


    // Private fields.
    private readonly string _sourceDirPath;
    private readonly ErrorRepository _errorRepository;
    private readonly ParserUtilities _parsingUtilities;
    private readonly WarningCollection _warnings;


    // Constructors.
    internal PackParser(string sourceDirectory, 
        ErrorRepository errorRepository,
        ParserUtilities parserUtilities,
        WarningCollection warnings)
    {
        ArgumentNullException.ThrowIfNull(sourceDirectory, nameof(sourceDirectory));
        ArgumentNullException.ThrowIfNull(errorRepository, nameof(errorRepository));
        ArgumentNullException.ThrowIfNull(errorRepository, nameof(parserUtilities));
        ArgumentNullException.ThrowIfNull(warnings, nameof(warnings));

        _sourceDirPath = sourceDirectory;
        _errorRepository = errorRepository;
        _parsingUtilities = parserUtilities;
        _warnings = warnings;
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
            FileParser.ParseFile(Pack, _errorRepository, _parsingUtilities, _warnings);
        }

        return Pack;
    }
}