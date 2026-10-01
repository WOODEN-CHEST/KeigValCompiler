using KeigValCompiler.Error;

namespace KeigValCompilerTest;

/* Compiles one fixture directory, as "dotnet run" would, and compares what the compiler reported with what
 * the comments at the top of its files list: the same messages, file by file, whatever their order, and the
 * counts the comments state. Each file is a test, and so is the directory as a whole, which has to report
 * nothing outside its own files. A zero-error fixture is one whose files list nothing. */
internal class FixtureTester : ICodeTester
{
    // Static fields.
    private const string SOURCE_FILE_PATTERN = "*.kgvl";
    private const string OUTSIDE_TEST_NAME = "messages outside the directory's files";


    // Private fields.
    private readonly string _directory;
    private readonly string _libraryDirectory;
    private readonly bool _isParseOnly;


    // Fields.
    public string Name => Path.GetFileName(_directory) + (_isParseOnly ? " (parse only)" : string.Empty);


    // Constructors.
    internal FixtureTester(string directory, string libraryDirectory, bool isParseOnly)
    {
        _directory = Path.GetFullPath(directory ?? throw new ArgumentNullException(nameof(directory)));
        _libraryDirectory = libraryDirectory ?? throw new ArgumentNullException(nameof(libraryDirectory));
        _isParseOnly = isParseOnly;
    }


    // Private methods.
    /* Why a file's messages are not what its comment lists, or null when they are. */
    private string? CheckFile(string filePath, List<CompilerMessage> messages, int errorCount, int warningCount)
    {
        FixtureHeader Header = FixtureHeader.Read(filePath);
        List<string> Problems = new();

        List<FixtureMessage> Missing = Subtract(Header.Entries, messages.Select(FixtureMessage.Of));
        List<FixtureMessage> Unexpected = Subtract(messages.Select(FixtureMessage.Of), Header.Entries);
        Problems.AddRange(Missing.Select(entry => $"expected but not reported: {entry}"));
        Problems.AddRange(Unexpected.Select(entry => "reported but not expected: " + messages.First(
            message => FixtureMessage.Of(message) == entry)));

        int ListedErrors = Header.Entries.Count(entry => entry.IsError);
        int ListedWarnings = Header.Entries.Count - ListedErrors;
        if ((Header.StatedFileCounts != null) && (Header.StatedFileCounts != (ListedErrors, ListedWarnings)))
        {
            Problems.Add($"the comment states {FormatCounts(Header.StatedFileCounts.Value)} for the file, but "
                + $"lists {FormatCounts((ListedErrors, ListedWarnings))}");
        }
        if ((Header.StatedDirectoryCounts != null)
            && (Header.StatedDirectoryCounts != (errorCount, warningCount)))
        {
            Problems.Add($"the comment states {FormatCounts(Header.StatedDirectoryCounts.Value)} for the "
                + $"directory, which gives {FormatCounts((errorCount, warningCount))}");
        }
        return (Problems.Count == 0) ? null : string.Join(Environment.NewLine, Problems);
    }

    /* What is in the first but not the second, counting each message as often as it occurs. */
    private List<FixtureMessage> Subtract(IEnumerable<FixtureMessage> from, IEnumerable<FixtureMessage> taken)
    {
        List<FixtureMessage> Left = from.ToList();
        foreach (FixtureMessage Taken in taken)
        {
            Left.Remove(Taken);
        }
        return Left.OrderBy(entry => entry.Line).ToList();
    }

    private string FormatCounts((int Errors, int Warnings) counts)
    {
        return $"{counts.Errors} errors and {counts.Warnings} warnings";
    }


    // Inherited methods.
    public TestResults Test()
    {
        List<CompilerMessage> Messages = TestCompilation.Compile(_directory, _libraryDirectory, _isParseOnly);
        int ErrorCount = Messages.Count(message => message.IsError);
        int WarningCount = Messages.Count - ErrorCount;

        string[] Files = Directory.GetFiles(_directory, SOURCE_FILE_PATTERN, SearchOption.AllDirectories)
            .Select(Path.GetFullPath).Order(StringComparer.Ordinal).ToArray();
        ILookup<string, CompilerMessage> MessagesByFile = Messages.ToLookup(message =>
            message.Location.HasFilePath ? Path.GetFullPath(message.Location.FilePath!) : string.Empty);

        List<FailedTestResult> Failures = new();
        foreach (string FilePath in Files)
        {
            string? Problems = CheckFile(FilePath, MessagesByFile[FilePath].ToList(), ErrorCount, WarningCount);
            if (Problems != null)
            {
                Failures.Add(new(Path.GetRelativePath(_directory, FilePath), Problems));
            }
        }

        List<CompilerMessage> Outside = Messages.Where(message => !message.Location.HasFilePath
            || !Files.Contains(Path.GetFullPath(message.Location.FilePath!))).ToList();
        if (Outside.Count > 0)
        {
            Failures.Add(new(OUTSIDE_TEST_NAME, string.Join(Environment.NewLine, Outside)));
        }
        return new(Files.Length + 1, Failures.ToArray());
    }
}
