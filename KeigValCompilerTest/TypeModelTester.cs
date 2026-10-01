using KeigValCompiler.Semantician.Library;
using KeigValCompiler.Semantician.Member;
using KeigValCompiler.Semantician.Resolver;
using KeigValCompiler.Semantician.Types;

namespace KeigValCompilerTest;

/* Checks the semantic type model's answers against TypeModel/model.kgvl, resolved with the standard
 * library: how written types read, what they print as, when two are the same type, and what substituting
 * type arguments gives. Also that every type written in a declaration of the library or the model, whose
 * names all resolved, reads to a type. */
internal class TypeModelTester : ICodeTester
{
    // Private fields.
    private readonly string _libraryDirectory;
    private readonly string _sourceDirectory;
    private readonly List<FailedTestResult> _failures = new();
    private int _testCount = 0;


    // Fields.
    public string Name => "Type model";


    // Constructors.
    internal TypeModelTester(string libraryDirectory, string sourceDirectory)
    {
        _libraryDirectory = libraryDirectory ?? throw new ArgumentNullException(nameof(libraryDirectory));
        _sourceDirectory = sourceDirectory ?? throw new ArgumentNullException(nameof(sourceDirectory));
    }


    // Private methods.
    private void Expect(bool condition, string testName, string reason)
    {
        _testCount++;
        if (!condition)
        {
            _failures.Add(new(testName, reason));
        }
    }

    private void ExpectText(SemanticType? type, string expected, string testName)
    {
        Expect(type?.ToString() == expected, testName, $"read as \"{type}\", not \"{expected}\"");
    }

    private void ExpectLibraryType(SemanticType? type, LibraryType expected, string testName)
    {
        LibraryType? Actual = (type as DeclaredType)?.LibraryType;
        Expect(Actual == expected, testName, $"\"{type}\" is {Actual?.ToString() ?? "no library type"}");
    }

    private void CheckWrittenTypes(PackResolutionContext context)
    {
        List<TypeTargetIdentifier> Unreadable = context.Pack.Members.SelectMany(MemberRelations.GetWrittenTypes)
            .Where(IsFullyResolved).Where(written => context.TypeReader.Read(written) == null).ToList();
        Expect(Unreadable.Count == 0, "every resolved type written in a declaration reads to a type",
            "unreadable: " + string.Join(", ", Unreadable));
    }

    private void CheckModel(PackResolutionContext context)
    {
        SemanticTypeReader Reader = context.TypeReader;
        PackClass Outer = FindType<PackClass>(context, "Outer");
        SemanticType? Plain = ReadField(Outer, "Plain", Reader);
        SemanticType? Annotated = ReadField(Outer, "Annotated", Reader);
        SemanticType? Text = ReadField(Outer, "Text", Reader);
        SemanticType? MaybeText = ReadField(Outer, "MaybeText", Reader);
        SemanticType? Number = ReadField(Outer, "Number", Reader);
        SemanticType? MaybeNumber = ReadField(Outer, "MaybeNumber", Reader);
        SemanticType? Jagged = ReadField(Outer, "Jagged", Reader);
        SemanticType? Nested = ReadField(Outer, "Nested", Reader);
        DeclaredType? Closed = ReadField(Outer, "Closed", Reader) as DeclaredType;

        ExpectText(Plain, "T", "T reads as T");
        ExpectText(Annotated, "T?", "an unconstrained T? is an annotated T");
        ExpectText(Text, "string", "string prints as written");
        ExpectText(MaybeText, "string?", "string? prints as written");
        Expect((Text != null) && Text.Equals(MaybeText), "string and string? are the same type", "they differ");
        Expect(Text?.GetHashCode() == MaybeText?.GetHashCode(), "string and string? hash alike", "they do not");
        ExpectText(MaybeNumber, "int?", "int? prints as written");
        ExpectLibraryType(MaybeNumber, LibraryTypes.Nullable, "int? is Nullable<int>");
        Expect((Number != null) && !Number.Equals(MaybeNumber), "int and int? differ", "they are the same");
        ExpectText(Jagged, "int?[]?[]", "int?[]?[] prints as written");
        ExpectLibraryType(Jagged, LibraryTypes.Array, "an array is Array<...>");
        ExpectText(Nested, "Outer<T>.Inner", "a nested type carries the type around it");
        ExpectText(Closed, "Outer<int>", "Outer<int> prints as written");

        PackStruct Pair = FindType<PackStruct>(context, "Pair");
        ExpectLibraryType(ReadField(Pair, "Maybe", Reader), LibraryTypes.Nullable,
            "a struct-constrained TFirst? is Nullable<TFirst>");

        DeclaredType Instance = Reader.GetInstanceType(Outer);
        SemanticType? EqualsParameter = ReadType(FindFunction(Outer, "Equals").Parameters.First().Type, Reader);
        ExpectText(Instance, "Outer<T>", "the instance type is Outer<T>");
        Expect(Instance.Equals(EqualsParameter), "the instance type equals a written Outer<T>",
            $"\"{EqualsParameter}\" differs");

        if (Closed != null)
        {
            CheckSubstitution(Closed, Plain, Annotated, Nested, Reader);
        }
        else
        {
            Expect(false, "Outer<int> reads to a declared type",
                "it does not, so substitution was not checked");
        }
        CheckByPosition(Outer, Reader);

        PackFunction MakerMake = FindFunction(FindType<PackClass>(context, "Maker"), "Make");
        PackFunction MadeMake = FindFunction(FindType<PackClass>(context, "Made"), "Make");
        ExpectLibraryType(ReadType(MakerMake.ReturnType, Reader), LibraryTypes.Nullable,
            "a struct-constrained T? return is Nullable<T>");
        ExpectLibraryType(ReadType(MadeMake.ReturnType, Reader), LibraryTypes.Nullable,
            "an override's own T? is Nullable<T> too, as in C#");

        HashSet<SemanticType> Distinct = new(context.Pack.Members.SelectMany(MemberRelations.GetWrittenTypes)
            .Where(IsFullyResolved).Select(Reader.Read).OfType<SemanticType>());
        int DistinctHashes = Distinct.Select(type => type.GetHashCode()).Distinct().Count();
        Expect(DistinctHashes > (Distinct.Count / 2), "hashes spread over distinct types",
            $"{DistinctHashes} hashes over {Distinct.Count} distinct types");
    }

    private void CheckSubstitution(DeclaredType closed,
        SemanticType? plain,
        SemanticType? annotated,
        SemanticType? nested,
        SemanticTypeReader reader)
    {
        TypeSubstitution Substitution = TypeSubstitution.Of(closed);
        ExpectText(plain?.Substitute(Substitution), "int", "T in Outer<int> is int");
        ExpectText(annotated?.Substitute(Substitution), "int",
            "T? in Outer<int> is int, the annotation dropped on a value type");
        ExpectText(nested?.Substitute(Substitution), "Outer<int>.Inner",
            "Inner in Outer<int> is Outer<int>.Inner");
        Expect((nested != null) && !nested.Substitute(Substitution).Equals(nested),
            "Outer<int>.Inner differs from Outer<T>.Inner", "they are the same");

        string Bases = string.Join(", ", reader.GetWrittenBaseTypes(closed));
        Expect(Bases == "IEquatable<Outer<int>>", "Outer<int> derives from IEquatable<Outer<int>>",
            $"its bases are \"{Bases}\"");
    }

    private void CheckByPosition(PackClass outer, SemanticTypeReader reader)
    {
        PackFunction First = FindFunction(outer, "First");
        PackFunction Second = FindFunction(outer, "Second");
        TypeSubstitution ByPosition = TypeSubstitution.ByPosition(Second.GenericParameters,
            First.GenericParameters);
        List<SemanticType?> FirstTypes = First.Parameters.Select(parameter => ReadType(parameter.Type, reader))
            .ToList();
        List<SemanticType?> SecondTypes = Second.Parameters.Select(
            parameter => ReadType(parameter.Type, reader)?.Substitute(ByPosition)).ToList();
        Expect(FirstTypes.SequenceEqual(SecondTypes), "First<TA>(TA, Outer<TA>) and Second<TB>(TB, Outer<TB>) "
            + "match by position",
            $"\"{string.Join(", ", FirstTypes)}\" and \"{string.Join(", ", SecondTypes)}\"");

        SemanticType? FirstParameter = FirstTypes.FirstOrDefault();
        SemanticType? SecondParameter = ReadType(Second.Parameters.First().Type, reader);
        Expect((FirstParameter != null) && !FirstParameter.Equals(SecondParameter), "TA and TB differ before that",
            "they are the same");
    }

    private T FindType<T>(PackResolutionContext context, string name) where T : PackMember
    {
        return context.Pack.Types.OfType<T>().First(type => !type.SourceFile.IsLibraryFile
            && (type.SelfIdentifier.SourceCodeName == name));
    }

    private PackFunction FindFunction(IPackFunctionHolder holder, string name)
    {
        return holder.Functions.First(function => function.SelfIdentifier.SourceCodeName == name);
    }

    private SemanticType? ReadField(IPackFieldHolder holder, string name, SemanticTypeReader reader)
    {
        return reader.Read(holder.Fields.First(field => field.SelfIdentifier.SourceCodeName == name).Type);
    }

    private SemanticType? ReadType(TypeTargetIdentifier? type, SemanticTypeReader reader)
    {
        return (type == null) ? null : reader.Read(type);
    }

    /* Whether every name in a written type was resolved, so that it has to read to a type. */
    private bool IsFullyResolved(TypeTargetIdentifier type)
    {
        return (type.MainTarget.Target != null) && type.TypeArguments.All(IsFullyResolved);
    }


    // Inherited methods.
    public TestResults Test()
    {
        _failures.Clear();
        _testCount = 0;

        PackResolutionContext Context = TestCompilation.Resolve(_libraryDirectory, _sourceDirectory);
        Expect(!Context.Messages.HasErrors, "the model resolves with no errors",
            string.Join(Environment.NewLine, Context.Messages));
        CheckWrittenTypes(Context);
        CheckModel(Context);
        return new(_testCount, _failures.ToArray());
    }
}
