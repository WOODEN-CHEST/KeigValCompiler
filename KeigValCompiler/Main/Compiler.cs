using KeigValCompiler.Semantician;
using KeigValCompiler.Semantician.Member;
using KeigValCompiler.Semantician.Member.Code;
using KeigValCompiler.Semantician.Resolver;
using KeigValCompiler.Source;
using KeigValCompiler.Source.Parser;
using System.Diagnostics;

namespace KeigValCompiler.Main;

public static class Compiler
{
    // Internal static fields.
    internal static Version CompilerVersion { get; } = new Version(1, 0, 0, 0);


    // Internal static methods.
    internal static void Main(string[] args)
    {
        if (args.Length == 0)
        {
            Console.WriteLine($"KeigVal Compiler Version {CompilerVersion}." +
                $"\nCommand-line arguments: <source directory> <destination directory (optional)>");
            return;
        }

        CompilerOptions Options;
        try
        {
            Options = new(args);
        }
        catch (CommandlineArgumentException e)
        {
            Console.WriteLine(e.Message);
            return;
        }

        try
        {
            Console.WriteLine($"Compiling pack using KeigVal compiler {CompilerVersion}.");
            Stopwatch CompilationTimeMeasurer = new();
            CompilationTimeMeasurer.Start();

            CompilePack(Options);

            CompilationTimeMeasurer.Stop();
            Console.WriteLine($"Successfully compiled the datapack in {CompilationTimeMeasurer.Elapsed}");

        }
        catch (SourceFileReadException e)
        {
            Console.WriteLine(e.Message);
        }
        catch (PackContentException e)
        {
            Console.WriteLine(e.Message);
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
        }
    }



    // Private static methods.
    private static void CompilePack(CompilerOptions options)
    {
        DataPack Pack = new PackParser(options.SourceDirectory).ParsePack();
    }

    private static void Test()
    {
        DataPack Pack = new();
        IInternalContentProvider ContentProvider = new DefaultInternalContentProvider();
        BuiltInTypeRegistry Registry = ContentProvider.AddInternalContent(Pack);

        IPackResolver Resolver = new FullPackResolver();
        IdentifierGenerator IdentifierGenerator = new();

        PackResolutionContext Context = new()
        {
            Registry = Registry,
            Pack = Pack,
            IdentifierSearcher = new DefaultIdentifierSearcher(IdentifierGenerator),
            PrimitiveResolver = new DefaultPrimitiveValueResolver(),
            IdentifierGenerator = IdentifierGenerator
        };

        PackSourceFile TestSourceFile = new(Pack, "test");
        PackNameSpace TestNameSpace = new(new("Test.Test2"));
        PackClass TestClass = new(new("TestClass"), TestSourceFile);
        PackFunction TestFunction = new(new("TestFunc"), TestSourceFile);

        VariableAssignmentStatement TestStatement1 = new(new("TestClass"), true);
        TestStatement1.Assignments.AddItem(
            new VariableAssignment(new("A"),
            new ConstructorCallStatement(new("TestClass"))));

        VariableAssignmentStatement TestStatement2 = new(new("long"), true);
        TestStatement2.Assignments.AddItem(
            new VariableAssignment(new("B"),
            new PrimitiveValueStatement("1234567890L")));

        TestFunction.Statements.AddStatement(TestStatement1);
        TestFunction.Statements.AddStatement(TestStatement2);

        PackDelegate TestDelegate = new(new("Func"), TestSourceFile);
        TestDelegate.ReturnType = new("TestClass");
        TestDelegate.Parameters.AddItem(new(new("int"), new("x"), FunctionParameterModifier.None));
        TestDelegate.Parameters.AddItem(new(new("decimal"), new("y"), FunctionParameterModifier.None));
        TestDelegate.Parameters.AddItem(new(new("Test.Test2.TestClass"), new("z"), FunctionParameterModifier.None));

        PackEvent TestEvent = new(new("RandomEvent"), new("Func"), TestSourceFile);

        PackField TestField = new(new("TestField"), new("byte"), TestSourceFile);

        PackProperty TestProperty = new(new("TestProperty"), new("ubyte"), TestSourceFile);
        TestProperty.GetFunction = new(new(string.Empty), TestSourceFile);

        TestClass.AddProperty(TestProperty);
        TestClass.AddFunction(TestFunction);
        TestClass.AddDelegate(TestDelegate);
        TestClass.AddEvent(TestEvent);
        TestClass.AddField(TestField);
        TestNameSpace.AddClass(TestClass);
        TestSourceFile.AddNamespace(TestNameSpace);
        Pack.AddSourceFile(TestSourceFile);

        Resolver.ResolvePack(Context);  
        
        Console.WriteLine("Test complete");
    }
}