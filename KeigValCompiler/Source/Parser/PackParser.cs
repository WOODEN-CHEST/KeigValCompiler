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
    private readonly CompilerMessageCollection _messages;


    // Constructors.
    internal PackParser(string sourceDirectory, 
        ErrorRepository errorRepository,
        ParserUtilities parserUtilities,
        CompilerMessageCollection messages)
    {
        ArgumentNullException.ThrowIfNull(sourceDirectory, nameof(sourceDirectory));
        ArgumentNullException.ThrowIfNull(errorRepository, nameof(errorRepository));
        ArgumentNullException.ThrowIfNull(errorRepository, nameof(parserUtilities));
        ArgumentNullException.ThrowIfNull(messages, nameof(messages));

        _sourceDirPath = sourceDirectory;
        _errorRepository = errorRepository;
        _parsingUtilities = parserUtilities;
        _messages = messages;
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
            /* One unreadable file must not hide what is wrong with all the others, so whatever it
             * failed with is queued and the next file is read anyway. */
            try
            {
                SourceFileParser FileParser = new(sourceFile, Pack);
                FileParser.ParseFile(Pack, _errorRepository, _parsingUtilities, _messages);
            }
            catch (SourceFileReadException e)
            {
                _messages.Add(e.CompilerMessage);
            }
            catch (SourceFileAbortException)
            {
                /* Hit the error limit for this file. Its errors are already queued. */
            }
        }

        return Pack;
    }
}