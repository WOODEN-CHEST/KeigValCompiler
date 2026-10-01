using KeigValCompiler.Main;

namespace KeigValCompilerTest;

/* Runs every tester, or those whose names contain one of the arguments, prints each failure with why it
 * failed, and ends with how many tests passed. Exits with 1 when any failed, so that a script can tell. A
 * tester not written yet says so and is skipped. */
internal class Program
{
    // Static fields.
    private const int EXIT_CODE_SUCCESS = 0;
    private const int EXIT_CODE_FAILURE = 1;
    private const string FAILURE_INDENT = "      ";


    // Private static methods.
    private static int Main(string[] args)
    {
        string Root = RepositoryPaths.FindRoot();
        string Library = Path.Combine(Root, CompilerOptions.DEFAULT_LIBRARY_DIRECTORY_NAME);
        ICodeTester[] Testers = new ICodeTester[]
        {
            new FixtureTester(Path.Combine(Root, "tests"), Library, true),
            new FixtureTester(Path.Combine(Root, "tests-errors"), Library, false),
            new FixtureTester(Path.Combine(Root, "tests-resolution"), Library, false),
            new FixtureTester(Path.Combine(Root, "tests-resolution-errors"), Library, false),
            new FixtureTester(Path.Combine(Root, "tests-declaration-errors"), Library, false),
            new FixtureTester(Path.Combine(Root, "tests-bodies"), Library, false),
            new FixtureTester(Path.Combine(Root, "tests-body-errors"), Library, false),
            new TypeModelTester(Library, Path.Combine(Root, "KeigValCompilerTest", "TypeModel")),
            new BaseListDepthTester(Library),
            new ConstantDepthTester(Library),
            new TwoIntDecimalTester()
        };

        int TestCount = 0;
        int FailureCount = 0;
        foreach (ICodeTester Tester in Testers.Where(tester => (args.Length == 0)
            || args.Any(filter => tester.Name.Contains(filter, StringComparison.OrdinalIgnoreCase))))
        {
            TestResults Results;
            try
            {
                Results = Tester.Test();
            }
            catch (NotImplementedException)
            {
                Console.WriteLine($"{Tester.Name}: not written yet, skipped");
                continue;
            }

            TestCount += Results.TestCount;
            FailureCount += Results.FailResults.Length;
            Console.WriteLine($"{Tester.Name}: {Results.TestCount - Results.FailResults.Length} of "
                + $"{Results.TestCount} passed");
            foreach (FailedTestResult Failure in Results.FailResults)
            {
                Console.WriteLine($"  FAILED {Failure.TestName}");
                Console.WriteLine(FAILURE_INDENT + Failure.Reason.Replace(Environment.NewLine,
                    Environment.NewLine + FAILURE_INDENT));
            }
        }

        Console.WriteLine((FailureCount == 0) ? $"All {TestCount} tests passed."
            : $"{FailureCount} of {TestCount} tests failed.");
        return (FailureCount == 0) ? EXIT_CODE_SUCCESS : EXIT_CODE_FAILURE;
    }
}
