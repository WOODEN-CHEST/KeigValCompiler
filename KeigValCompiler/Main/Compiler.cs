using KeigValCompiler.Error;
using KeigValCompiler.Main.Commandline;
using KeigValCompiler.Semantician;
using KeigValCompiler.Semantician.Library;
using KeigValCompiler.Semantician.Resolver;
using KeigValCompiler.Source;
using KeigValCompiler.Source.Parser;
using System.Diagnostics;

namespace KeigValCompiler.Main;

public static class Compiler
{
    // Internal static fields.
    internal static Version CompilerVersion { get; } = new Version(1, 0, 0, 0);

    /* Process exit codes. */
    internal const int EXIT_CODE_SUCCESS = 0;
    internal const int EXIT_CODE_FAILURE = 1;


    // Internal static methods.
    internal static int Main(string[] args)
    {
        ErrorRepository ErrorCreator = new();
        CompilerArguments Arguments = new();
        CompilerMessageCollection CommandlineMessages = new();

        CommandlineParser ArgumentParser = new(Arguments.Repository, new CommandlineParsingContext()
        {
            ErrorCreator = ErrorCreator,
            Messages = CommandlineMessages
        });
        CommandlineParseResult ParseResult = ArgumentParser.Parse(args);

        /* Help is printed even over mistakes elsewhere on the command line, since it is what explains them. */
        if ((args.Length == 0) || ParseResult.IsPresent(Arguments.Help))
        {
            PrintHelp(Arguments);
            return EXIT_CODE_SUCCESS;
        }

        CompilerMessagePrinter MessagePrinter = new();
        MessagePrinter.PrintMessages(CommandlineMessages);
        MessagePrinter.PrintSummary(CommandlineMessages);
        if (CommandlineMessages.HasErrors)
        {
            Console.WriteLine($"Run the compiler with \"{Arguments.Help.DisplayName}\" to list every argument "
                + "it accepts.");
            return EXIT_CODE_FAILURE;
        }

        CompilerOptions Options = new(ParseResult, Arguments);

        try
        {
            Console.WriteLine($"Compiling pack using KeigVal compiler {CompilerVersion}.");
            Stopwatch CompilationTimeMeasurer = new();
            CompilationTimeMeasurer.Start();

            bool IsCompilationSuccessful = CompilePack(Options, Arguments, ErrorCreator);

            CompilationTimeMeasurer.Stop();
            if (!IsCompilationSuccessful)
            {
                Console.WriteLine($"Failed to compile the datapack after {CompilationTimeMeasurer.Elapsed}");
                return EXIT_CODE_FAILURE;
            }

            Console.WriteLine(Options.IsParseOnly
                ? $"Successfully parsed the pack in {CompilationTimeMeasurer.Elapsed}"
                : $"Successfully compiled the datapack in {CompilationTimeMeasurer.Elapsed}");
            return EXIT_CODE_SUCCESS;
        }
        catch (Exception e) when (e is PackContentException or SourceFileReadException)
        {
            Console.WriteLine(e.Message);
            return EXIT_CODE_FAILURE;
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            return EXIT_CODE_FAILURE;
        }
    }



    // Private static methods.
    /* Every stage reports everything it found before the next one is allowed to start. Running a
     * stage on what a failed one left behind only buries its errors under invented ones. */
    private static bool CompilePack(CompilerOptions options,
        CompilerArguments arguments,
        ErrorRepository errorCreator)
    {
        CompilerMessageCollection Messages = new();
        ParserUtilities ParserUtilities = new();
        CompilerMessagePrinter MessagePrinter = new();
        DataPack Pack = new();

        PackParser Parser = new(errorCreator, ParserUtilities, Messages);
        ParseLibrary(Parser, Pack, options, arguments, errorCreator, Messages);
        Parser.ParseDirectory(Pack, options.SourceDirectory, SourceFileKind.User);

        MessagePrinter.PrintMessages(Messages);
        MessagePrinter.PrintSummary(Messages);
        if (Messages.HasErrors || options.IsParseOnly)
        {
            return !Messages.HasErrors;
        }

        CompilerMessageCollection ResolutionMessages = ResolvePack(Pack, options, arguments, errorCreator);
        MessagePrinter.PrintMessages(ResolutionMessages);
        MessagePrinter.PrintSummary(ResolutionMessages);
        if (ResolutionMessages.HasErrors)
        {
            return false;
        }

        /* Emitting goes here, reporting before anything is written. */
        return true;
    }

    /* Its messages are a collection of their own, so that the parser's warnings are not printed again
     * with them. As with parsing, a broken library says so once, on top of its own errors. */
    private static CompilerMessageCollection ResolvePack(DataPack pack,
        CompilerOptions options,
        CompilerArguments arguments,
        ErrorRepository errorCreator)
    {
        CompilerMessageCollection Messages = new();
        BuiltInTypeRegistry Registry = new();
        PackResolutionContext Context = new()
        {
            Pack = pack,
            Registry = Registry,
            TypeSearcher = new(pack),
            TypeReader = new(Registry),
            BindingTable = DefaultLibraryBindings.CreateTable(),
            IdentifierGenerator = new(),
            ErrorCreator = errorCreator,
            Messages = Messages
        };

        new FullPackResolver().ResolvePack(Context);

        if (Context.HasLibraryErrors())
        {
            Messages.AddError(errorCreator.LibraryHasErrors.CreateOptions(options.LibraryDirectory,
                arguments.Library.DisplayName), new CompilerMessageLocation(options.LibraryDirectory), null);
        }
        return Messages;
    }

    /* The standard library is read into the same pack as the user's code, and before it, since what
     * the user's code names is looked for there too. A broken library is not the user's fault, which
     * one error of its own says. The user's code is still read afterwards, because its parse errors
     * do not depend on the library. */
    private static void ParseLibrary(PackParser parser,
        DataPack pack,
        CompilerOptions options,
        CompilerArguments arguments,
        ErrorRepository errorCreator,
        CompilerMessageCollection messages)
    {
        if (!Directory.Exists(options.LibraryDirectory))
        {
            messages.AddError(errorCreator.LibraryDirectoryNotFound.CreateOptions(options.LibraryDirectory,
                arguments.Library.DisplayName), CompilerMessageLocation.None, null);
            return;
        }

        int ErrorCountBeforeLibrary = messages.ErrorCount;
        parser.ParseDirectory(pack, options.LibraryDirectory, SourceFileKind.Library);

        if (messages.ErrorCount > ErrorCountBeforeLibrary)
        {
            messages.AddError(errorCreator.LibraryHasErrors.CreateOptions(options.LibraryDirectory,
                arguments.Library.DisplayName), new CompilerMessageLocation(options.LibraryDirectory), null);
        }
    }

    private static void PrintHelp(CompilerArguments arguments)
    {
        Console.WriteLine($"KeigVal Compiler Version {CompilerVersion}.");
        Console.WriteLine();

        CommandlineHelpPrinter HelpPrinter = new();
        HelpPrinter.PrintHelp(AppDomain.CurrentDomain.FriendlyName, arguments.Repository);
    }

    private static void Test()
    {
        //DataPack Pack = new();
        //IInternalContentProvider ContentProvider = new DefaultInternalContentProvider();
        //BuiltInTypeRegistry Registry = ContentProvider.AddInternalContent(Pack);

        //IPackResolver Resolver = new FullPackResolver();
        //IdentifierGenerator IdentifierGenerator = new();

        //PackResolutionContext Context = new()
        //{
        //    Registry = Registry,
        //    Pack = Pack,
        //    IdentifierSearcher = new DefaultIdentifierSearcher(IdentifierGenerator),
        //    PrimitiveResolver = new DefaultPrimitiveValueResolver(),
        //    IdentifierGenerator = IdentifierGenerator
        //};

        //PackSourceFile TestSourceFile = new(Pack, "test");
        //PackNameSpace TestNameSpace = new(new("Test.Test2"));
        //PackClass TestClass = new(new("TestClass"), TestSourceFile);
        //PackFunction TestFunction = new(new("TestFunc"), TestSourceFile);

        //VariableAssignmentStatement TestStatement1 = new(new("TestClass"), true);
        //TestStatement1.Assignments.AddItem(
        //    new VariableAssignment(new("A"),
        //    new ConstructorCallStatement(new("TestClass"))));

        //VariableAssignmentStatement TestStatement2 = new(new("long"), true);
        //TestStatement2.Assignments.AddItem(
        //    new VariableAssignment(new("B"),
        //    new PrimitiveValueStatement("1234567890L")));

        //TestFunction.Statements.AddStatement(TestStatement1);
        //TestFunction.Statements.AddStatement(TestStatement2);

        //PackDelegate TestDelegate = new(new("Func"), TestSourceFile);
        //TestDelegate.ReturnType = new("TestClass");
        //TestDelegate.Parameters.AddItem(new(new("int"), new("x"), FunctionParameterModifier.None));
        //TestDelegate.Parameters.AddItem(new(new("decimal"), new("y"), FunctionParameterModifier.None));
        //TestDelegate.Parameters.AddItem(new(new("Test.Test2.TestClass"), new("z"), FunctionParameterModifier.None));

        //PackEvent TestEvent = new(new("RandomEvent"), new("Func"), TestSourceFile);

        //PackField TestField = new(new("TestField"), new("byte"), TestSourceFile);

        //PackProperty TestProperty = new(new("TestProperty"), new("ubyte"), TestSourceFile);
        //TestProperty.GetFunction = new(new(string.Empty), TestSourceFile);

        //TestClass.AddProperty(TestProperty);
        //TestClass.AddFunction(TestFunction);
        //TestClass.AddDelegate(TestDelegate);
        //TestClass.AddEvent(TestEvent);
        //TestClass.AddField(TestField);
        //TestNameSpace.AddClass(TestClass);
        //TestSourceFile.AddNamespace(TestNameSpace);
        //Pack.AddSourceFile(TestSourceFile);

        //Resolver.ResolvePack(Context);  
        
        Console.WriteLine("Test complete");
    }
}