using KeigValCompiler.Error;
using KeigValCompiler.Semantician;
using KeigValCompiler.Source.Parser;
using System.Runtime.CompilerServices;
using System.Text;

namespace KeigValCompiler.Source;


internal class SourceFileParser
{
    // Fields.
    public string FilePath { get; private init; }
    public DataPack Pack { get; private init; }


    // Constructors.
    internal SourceFileParser(string filePath, DataPack pack)
    {
        FilePath = filePath ?? throw new ArgumentNullException(nameof(filePath));
        Pack = pack ?? throw new ArgumentNullException(nameof(pack));
    }


    // Internal methods.
    internal void ParseFile(DataPack parentPack,
        ErrorRepository errorRepository,
        ParserUtilities utilities,
        CompilerMessageCollection messages)
    {
        try
        {
            string FileData = File.ReadAllText(FilePath, Encoding.UTF8);

            SourceDataParser OriginalFileParser = new(FileData, FilePath, errorRepository);
            string StrippedFileData = new CommentStripper(OriginalFileParser, errorRepository)
                .StripCommentsFromCode(FileData);

            SourceDataParser SourceParser = new(StrippedFileData, FilePath, errorRepository);
            PackSourceFile SourceFile = new(Pack, FilePath);
            Pack.AddSourceFile(SourceFile);

            PackParsingContext Context = new()
            {
                ErrorCreator = errorRepository,
                SourceFile = SourceFile,
                Parser = SourceParser,
                Utilities = utilities,
                Messages = messages
            };

            new SourceFileRootParser(Context).ParseBase();
        }
        catch (PackContentException e)
        {
            throw CreateFileException(errorRepository.SourceFileInvalidContent, e);
        }
        catch (FileNotFoundException e)
        {
            throw CreateFileException(errorRepository.SourceFileNotFound, e);
        }
        catch (DirectoryNotFoundException e)
        {
            throw CreateFileException(errorRepository.SourceFileDirectoryNotFound, e);
        }
        catch (IOException e)
        {
            throw CreateFileException(errorRepository.SourceFileReadFailure, e);
        }
    }


    // Private methods.
    /* These failures happen before or instead of reading the file, so there is no line or column to
     * point at - only the path. The exception's own text goes into the notes. */
    private SourceFileReadException CreateFileException(ErrorDefinition definition, Exception cause)
    {
        return new(new CompilerMessage(definition.CreateOptions(FilePath),
            new CompilerMessageLocation(FilePath),
            cause.Message));
    }
}