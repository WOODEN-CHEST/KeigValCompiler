﻿using KeigValCompiler.Semantician;
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
        //PackParser Parser = new(options.SourceDirectory, options.DestinationDirectory);
        //DataPack ObjectDataPack = Parser.ParsePack();
    }

    private static void Test()
    {
        TwoIntDecimal Dec1 = new(-6);
        TwoIntDecimal Dec2 = new(2);
        TwoIntDecimal Dec3 = Dec2 * Dec1;


        //DataPack Pack = new();
        //IPackResolver Resolver = new KGVLPackResolver();
        //Resolver.ResolvePack(Pack);
    }
}