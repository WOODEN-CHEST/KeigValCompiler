using KeigValCompiler.Semantician;
using KeigValCompiler.Semantician.Resolver;

namespace KeigValCompiler.Main;

public static class Compiler
{
    // Internal static fields.
    internal static Version CompilerVersion { get; } = new Version(1, 0, 0, 0);


    // Internal static methods.
    internal static void Main(string[] args)
    {
        Test();
        //return;

        //if (args.Length == 0)
        //{
        //    Console.WriteLine($"KeigVal Compiler Version {CompilerVersion}." +
        //        $"\nCommand-line arguments: <source directory> <destination directory (optional)>");
        //    return;
        //}

        //CompilerOptions Options;
        //try
        //{
        //    Options = new(args);
        //}
        //catch (CommandlineArgumentException e)
        //{
        //    Console.WriteLine(e.Message);
        //    return;
        //}

        //try
        //{
        //    Console.WriteLine($"Compiling pack using KeigVal compiler {CompilerVersion}.");
        //    Stopwatch CompilationTimeMeasurer = new();
        //    CompilationTimeMeasurer.Start();

        //    CompilePack(Options);

        //    CompilationTimeMeasurer.Stop();
        //    Console.WriteLine($"Successfully compiled the datapack in {CompilationTimeMeasurer.Elapsed}");

        //}
        //catch (FileReadException e)
        //{
        //    Console.WriteLine(e.Message);
        //}
        //catch (PackContentException e)
        //{
        //    Console.WriteLine(e.Message);
        //}
        //catch (Exception e)
        //{
        //    Console.WriteLine(e);
        //}
    }



    // Private static methods.
    private static void CompilePack(CompilerOptions options)
    {
    }

    private static void Test()
    {
        DataPack Pack = new();
        IInternalContentProvider ContentProvider = new DefaultInternalContentProvider();
        BuiltInTypeRegistry Registry = ContentProvider.AddInternalContent(Pack);

        IPackResolver Resolver = new FullPackResolver();

        PackResolutionContext Context = new()
        {
            Registry = Registry,
            Pack = Pack,
            IdentifierSearcher = new DefaultIdentifierSearcher(),
            PrimitiveResolver = new DefaultPrimitiveValueResolver()
        };

        Resolver.ResolvePack(Context);
    }
}