using KeigValCompiler.Error;

namespace KeigValCompilerTest;

/* Checks the limit on how deep base lists may wait on each other, on sources written for it: a chain of
 * classes each deriving from a type the next one inherits, declared from the far end, so that resolving
 * each base list needs the next resolved first. With as many classes as the limit the chain compiles; with
 * one more, resolution stops with a single error, rather than the stack running out or every class of the
 * chain being reported. The sources are written to a directory of their own and removed afterwards. */
internal class BaseListDepthTester : ICodeTester
{
    // Static fields.
    /* SignatureResolver's limit, which a change there has to be matched by here. */
    private const int DEPTH_LIMIT = 100;
    private const string RESOLUTION_PREFIX = "RS";
    private const int TOO_DEEP_CODE = 66;


    // Private fields.
    private readonly string _libraryDirectory;


    // Fields.
    public string Name => "Base list depth";


    // Constructors.
    internal BaseListDepthTester(string libraryDirectory)
    {
        _libraryDirectory = libraryDirectory ?? throw new ArgumentNullException(nameof(libraryDirectory));
    }


    // Private methods.
    /* A chain of so many classes, each but the last deriving from a type the next one inherits. */
    private string WriteChain(int classCount)
    {
        List<string> Lines = new()
        {
            "namespace Depth;",
            "public class Holder { public class Inner : Holder { } }"
        };
        for (int Index = 0; Index < (classCount - 1); Index++)
        {
            Lines.Add($"public class Link{Index} : Link{Index + 1}.Inner {{ }}");
        }
        Lines.Add($"public class Link{classCount - 1} : Holder {{ }}");
        return string.Join(Environment.NewLine, Lines);
    }

    private List<CompilerMessage> CompileChain(int classCount)
    {
        string SourceDirectory = Path.Combine(Path.GetTempPath(), $"kgvl-depth-{Guid.NewGuid():N}");
        Directory.CreateDirectory(SourceDirectory);
        try
        {
            File.WriteAllText(Path.Combine(SourceDirectory, "chain.kgvl"), WriteChain(classCount));
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
            Failures.Add(new($"a chain of {DEPTH_LIMIT} classes compiles",
                string.Join(Environment.NewLine, AtLimit)));
        }

        List<CompilerMessage> PastLimit = CompileChain(DEPTH_LIMIT + 1);
        bool IsOneTooDeep = (PastLimit.Count == 1)
            && (PastLimit[0].Definition?.Category.Prefix == RESOLUTION_PREFIX)
            && (PastLimit[0].Definition?.Code == TOO_DEEP_CODE);
        if (!IsOneTooDeep)
        {
            Failures.Add(new($"a chain of {DEPTH_LIMIT + 1} classes gives {RESOLUTION_PREFIX} {TOO_DEEP_CODE} "
                + "alone", string.Join(Environment.NewLine, PastLimit)));
        }
        return new(2, Failures.ToArray());
    }
}
