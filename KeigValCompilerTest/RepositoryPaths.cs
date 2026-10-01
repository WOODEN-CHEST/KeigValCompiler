namespace KeigValCompilerTest;

/* Where the repository is, found from where the tests run, so that the fixtures and the standard library
 * are read where they are kept rather than from copies beside the tests. */
internal static class RepositoryPaths
{
    // Static fields.
    private const string SOLUTION_FILE_NAME = "KeigValCompiler.sln";


    // Internal static methods.
    internal static string FindRoot()
    {
        for (DirectoryInfo? Current = new(AppContext.BaseDirectory); Current != null; Current = Current.Parent)
        {
            if (File.Exists(Path.Combine(Current.FullName, SOLUTION_FILE_NAME)))
            {
                return Current.FullName;
            }
        }
        throw new DirectoryNotFoundException($"No \"{SOLUTION_FILE_NAME}\" was found in "
            + $"\"{AppContext.BaseDirectory}\" or any directory holding it.");
    }
}
