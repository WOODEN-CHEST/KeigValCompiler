using KeigValCompiler.Middle;
using KeigValCompiler.Source;


namespace KeigValCompiler;

public static class Compiler
{
    public static void Main(string[] args)
    {
        if (args.Length == 0)
        {
            Console.WriteLine("Missing pack source directory");
        }
        if (args[0] == null)
        {
            Console.WriteLine("Invalid source");
        }

        try
        {
            Console.WriteLine("Compiling pack...");
            CompilePack(args[0], args.Length > 1 ? args[1] : null);
            Console.WriteLine("Successfully compiled the datapack!");

        }
        catch (Exception e)
        {
            Console.WriteLine(e);
        }
    }

    internal static void CompilePack(string sourceDir, string? destDir)
    {
        DataPack Datapack = PackParser.ParsePack(sourceDir, destDir);
    }
}