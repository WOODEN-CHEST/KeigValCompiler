using KeigValCompiler.Error;
using KeigValCompiler.Semantician;
using KeigValCompiler.Source.Parser;

namespace KeigValCompiler.Source;

internal class PackParser
{
    // Internal fields.
    internal const string SOURCE_FILE_EXTENSION = ".kgvl";


    // Private fields.
    private readonly ErrorRepository _errorRepository;
    private readonly ParserUtilities _parsingUtilities;
    private readonly CompilerMessageCollection _messages;


    // Constructors.
    internal PackParser(ErrorRepository errorRepository,
        ParserUtilities parserUtilities,
        CompilerMessageCollection messages)
    {
        ArgumentNullException.ThrowIfNull(errorRepository, nameof(errorRepository));
        ArgumentNullException.ThrowIfNull(parserUtilities, nameof(parserUtilities));
        ArgumentNullException.ThrowIfNull(messages, nameof(messages));

        _errorRepository = errorRepository;
        _parsingUtilities = parserUtilities;
        _messages = messages;
    }


    // Internal methods.
    /* Reads every source file in the directory, its subdirectories included, into the pack. The
     * standard library and the user's code are read into the same pack by separate calls, and the
     * kind given here is the only thing which tells their files apart. */
    internal void ParseDirectory(DataPack pack, string directoryPath, SourceFileKind kind)
    {
        ArgumentNullException.ThrowIfNull(pack, nameof(pack));
        ArgumentNullException.ThrowIfNull(directoryPath, nameof(directoryPath));

        if (!Directory.Exists(directoryPath))
        {
            throw new DirectoryNotFoundException(directoryPath);
        }

        /* Sorted, since the order a directory lists its files in is up to the file system, and which of
         * two declarations counts as the first must not change from one machine to the next. */
        foreach (string SourceFilePath in Directory.GetFiles(
            directoryPath, $"*{SOURCE_FILE_EXTENSION}", SearchOption.AllDirectories).Order(StringComparer.Ordinal))
        {
            /* One unreadable file must not hide what is wrong with all the others, so whatever it
             * failed with is queued and the next file is read anyway. */
            try
            {
                SourceFileParser FileParser = new(SourceFilePath, pack, kind);
                FileParser.ParseFile(pack, _errorRepository, _parsingUtilities, _messages);
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
    }
}
