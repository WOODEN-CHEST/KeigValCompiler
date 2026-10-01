using KeigValCompiler.Error;

namespace KeigValCompilerTest;

/* Checks the limit on how deep constants may wait on each other, on sources written for it: a chain of constants
 * each naming the next, declared after it, so that working out each needs the next worked out first. With as many
 * constants as the limit the chain compiles; with one more, resolution stops with a single error, rather than the
 * stack running out. The sources are written to a directory of their own and removed afterwards. */
internal class ConstantDepthTester : ICodeTester
{
    // Static fields.
    /* BodyBindingContext's limit, which a change there has to be matched by here. */
    private const int DEPTH_LIMIT = 100;
    private const string RESOLUTION_PREFIX = "RS";
    private const int TOO_DEEP_CODE = 67;


    // Private fields.
    private readonly string _libraryDirectory;


    // Fields.
    public string Name => "Constant depth";


    // Constructors.
    internal ConstantDepthTester(string libraryDirectory)
    {
        _libraryDirectory = libraryDirectory ?? throw new ArgumentNullException(nameof(libraryDirectory));
    }


    // Private methods.
    /* A chain of so many constants, each but the last naming the next. */
    private string WriteChain(int constantCount)
    {
        List<string> Lines = new()
        {
            "namespace Depth;",
            "public class Constants",
            "{"
        };
        for (int Index = 0; Index < (constantCount - 1); Index++)
        {
            Lines.Add($"    public const int Link{Index} = Link{Index + 1};");
        }
        Lines.Add($"    public const int Link{constantCount - 1} = 1;");
        Lines.Add("}");
        return string.Join(Environment.NewLine, Lines);
    }

    private List<CompilerMessage> CompileChain(int constantCount)
    {
        string SourceDirectory = Path.Combine(Path.GetTempPath(), $"kgvl-constants-{Guid.NewGuid():N}");
        Directory.CreateDirectory(SourceDirectory);
        try
        {
            File.WriteAllText(Path.Combine(SourceDirectory, "chain.kgvl"), WriteChain(constantCount));
            return TestCompilation.Compile(SourceDirectory, _libraryDirectory, false);
        }
        finally
        {
            Directory.Delete(SourceDirectory, true);
        }
    }


    // Inherited methods.
    public TestResults Test()
    {
        List<FailedTestResult> Failures = new();

        List<CompilerMessage> AtLimit = CompileChain(DEPTH_LIMIT);
        if (AtLimit.Count > 0)
        {
            Failures.Add(new($"a chain of {DEPTH_LIMIT} constants compiles",
                string.Join(Environment.NewLine, AtLimit)));
        }

        List<CompilerMessage> PastLimit = CompileChain(DEPTH_LIMIT + 1);
        bool IsOneTooDeep = (PastLimit.Count == 1)
            && (PastLimit[0].Definition?.Category.Prefix == RESOLUTION_PREFIX)
            && (PastLimit[0].Definition?.Code == TOO_DEEP_CODE);
        if (!IsOneTooDeep)
        {
            Failures.Add(new($"a chain of {DEPTH_LIMIT + 1} constants gives {RESOLUTION_PREFIX} {TOO_DEEP_CODE} "
                + "alone", string.Join(Environment.NewLine, PastLimit)));
        }
        return new(2, Failures.ToArray());
    }
}
