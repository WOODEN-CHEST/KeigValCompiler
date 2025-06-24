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
    internal void ParseFile(DataPack parentPack)
    {
        try
        {
            string FileData = File.ReadAllText(FilePath);

            SourceDataParser OriginalFileParser = new(FileData, FilePath);
            string StrippedFileData = new CommentStripper(OriginalFileParser).StripCommentsFromCode(FileData);

            SourceDataParser SourceParser = new(StrippedFileData, FilePath);
            PackSourceFile SourceFile = new(Pack, FilePath);
            Pack.AddSourceFile(SourceFile);
            new SourceFileRootParser(SourceParser, new(), SourceFile).ParseBase();
        }
        catch (PackContentException e)
        {
            throw new SourceFileReadException($"Invalid pack content for file \"{FilePath}\": {e.Message}");
        }
        catch (FileNotFoundException e)
        {
            throw new SourceFileReadException($"File \"{FilePath}\" not found.");
        }
        catch (DirectoryNotFoundException e)
        {
            throw new SourceFileReadException($"Directory not found for file \"{FilePath}\". {e.Message}");
        }
        catch (IOException e)
        {
            throw new SourceFileReadException($"IOException reading file \"{FilePath}\". {e.Message}");
        }
    }


    // Private methods.

    /* Namespace and using statement. */



    ///* Namespace and class members. */



    ///* Statements. */



    //private void ParseValueStatement()
    //{

    //}


    ///* Functions. */
    //private void ParseFunction(PackMemberModifiers modifiers, PackClass? parentClass, string identifier, string returnType)
    //{

    //}


    ///* Properties. */
    //private void ParseProperty(PackMemberModifiers modifiers, PackClass? parentClass, string identifier)
    //{

    //}


    ///* Fields. */
    //private void ParseField(PackMemberModifiers modifiers, PackClass? parentClass, string identifier)
    //{
    //    SkipWhitespaceUntil($"Expected field value or '{KGVL.SEMICOLON}'");
    //    if (GetCharAtDataIndex() == KGVL.SEMICOLON)
    //    {

    //    }
    //}


    /* Generic parsing methods. */
}