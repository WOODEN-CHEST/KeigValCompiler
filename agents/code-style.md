# Code style

These conventions are **consistently applied across all ~8,000 lines** of this
codebase. They are not suggestions and several of them deliberately contradict
common C# style. Follow them exactly.

## Naming

### Local variables are PascalCase

This is the convention agents get wrong most often. Every local variable and
every `foreach` variable is **PascalCase**, like a property:

```csharp
StringBuilder NamespaceBuilder = new();
PackNameSpace? NameSpace = SourceFile.Pack.TryGetNamespace(fullName);
int SeparatorIndex = Name.LastIndexOf(KGVL.NAMESPACE_SEPARATOR);

foreach (PackMember TypeMember in NameSpace.AllMembers)
foreach (char Character in sourceCodeName)
```

There are **zero** camelCase locals in the codebase. Parameters, by contrast,
are ordinary `camelCase`. That contrast is the point: at a glance you can tell a
parameter from a local.

### The rest

| Kind | Convention | Example |
|---|---|---|
| Local variable | `PascalCase` | `PackParser Parser = new(...)` |
| Parameter | `camelCase` | `string sourceCodeName` |
| Private field | `_camelCase` | `_activeNamespace`, `_errorRepository` |
| Property | `PascalCase` | `SourceFile`, `DataIndex` |
| Method | `PascalCase` | `ParseNamespaceName` |
| Constant | `UPPER_SNAKE_CASE` | `SEMICOLON`, `KEYWORD_CLASS` |
| Type | `PascalCase` | `SourceFileRootParser` |
| Interface | `I` + `PascalCase` | `IPackResolver` |

## Never use `var`

The codebase contains **zero** uses of `var`. Always write the explicit type,
even when it is long or obvious from the right-hand side:

```csharp
// Correct
PackMemberModifiers CombinedModifiers = ParseMemberModifiers(typeName, memberName);
Dictionary<string, PackMember> ShorthandTypes = new();

// Wrong
var combinedModifiers = ParseMemberModifiers(typeName, memberName);
```

Target-typed `new()` on the right-hand side **is** used and is preferred:
`StatementCollection Statements = new();`

## File layout

- **One type per file**, named after the type.
- **Directory path mirrors the namespace.** `KeigValCompiler/Semantician/Member/Code/IfStatement.cs`
  is `namespace KeigValCompiler.Semantician.Member.Code`.
- **File-scoped namespaces** (`namespace X;`), used in all 118 files. Never
  brace-scoped.
- **Files are saved with a UTF-8 BOM.** Every existing `.cs` file starts with
  `EF BB BF`. Preserve it when editing; new files should have it too.
- Unused `using` blocks from the Visual Studio file template (`System.Linq`,
  `System.Threading.Tasks`, …) are common. Leave them alone in files you are not
  otherwise restructuring — removing them creates noise diffs.

## Section comments

Members are grouped by accessibility and kind, with a section comment above each
group. This is used in nearly every file and the ordering is conventional:

```csharp
internal class SourceFileRootParser : AbstractParserBase
{
    // Internal fields.
    internal string FilePath { get; private init; }

    // Private fields.
    private PackNameSpace? _activeNamespace;

    // Constructors.
    public SourceFileRootParser(PackParsingContext context) : base(context) { }

    // Internal methods.
    internal void ParseBase() { }

    // Private methods.
    private string ParseNamespaceName(bool isUsingDirective) { }

    // Inherited methods.
    public override string ToString() { }
}
```

Section headers in use, in their usual order: `// Static fields.`,
`// Internal fields.` / `// Fields.`, `// Private fields.`, `// Constructors.`,
`// Internal methods.` / `// Methods.`, `// Private methods.`,
`// Inherited methods.`, `// Operators.`. Always singular-to-plural as written,
always ending in a period.

Within a long section, sub-groups use a block comment:

```csharp
    // Private methods.
    /* Primitives. */
    private PackStruct CreateBoolStruct(...) { }

    /* Record. */
    private void ParseRecordPrimaryConstructor(...) { }
```

## Formatting

- **Allman braces** — opening brace on its own line, always. Single-statement
  `if` bodies still get braces.
- **Four spaces**, no tabs.
- Line length is kept near **~110 characters**; long expressions wrap with the
  continuation indented one level.
- **Parenthesise the operands of `&&` and `||`**, even when precedence makes it
  unnecessary:
  ```csharp
  while ((Keyword == KGVL.KEYWORD_CASE) || (Keyword == KGVL.KEYWORD_DEFAULT))
  if ((ReturnType == null) || (NextChar == KGVL.GENERIC_TYPE_START))
  ```

## Null handling

`<Nullable>enable</Nullable>` is on. Guard clauses use two forms:

```csharp
// When assigning — use the ?? throw form (53 uses).
FilePath = filePath ?? throw new ArgumentNullException(nameof(filePath));

// When only validating — use ThrowIfNull (16 uses).
ArgumentNullException.ThrowIfNull(context, nameof(context));
```

Constructors validate all reference parameters. `required` init properties are
used for context objects (`PackParsingContext`, `PackResolutionContext`).

## Accessibility

Default to `internal`, not `public`. The compiler is a single assembly and
almost nothing is genuinely public API. Use `private` for anything not needed
outside the type.

## Language syntax constants

**Never hardcode a KGVL syntax character or keyword literal.** Every one lives
in [`KeigValCompiler/KGVL.cs`](../KeigValCompiler/KGVL.cs):

```csharp
// Correct
if (Parser.GetCharAtDataIndex() != KGVL.SEMICOLON)
if (Keyword == KGVL.KEYWORD_CLASS)

// Wrong
if (Parser.GetCharAtDataIndex() != ';')
if (Keyword == "class")
```

Add a new constant to `KGVL.cs` rather than inlining a literal. Note `KGVL.cs`
separates `/* Syntax. */`, `/* Keywords. */` and `/* Internal. */` sections —
put new constants in the right one.

## Parser errors

Every message the compiler prints comes from a definition in
[`ErrorRepository`](../KeigValCompiler/Error/ErrorRepository.cs), which gives it
a stable code within its `CompilerMessageCategory` and a reusable message. To add
one, add an `internal virtual ErrorDefinition` (or `WarningDefinition`) property
with the next free code in its category, then reference it. They are `virtual` so
tests can override them. Never write the message text at the place it is raised —
the *notes* argument is for detail the definition cannot know, not for the
message itself.

There are two kinds of error, and picking the wrong one costs error quality:

**The parser still knows where it is** — the construct was understood, it is just
wrong. Queue it and carry straight on, so one mistake does not hide the rest of
the file:

```csharp
AddError(ErrorCreator.DuplicateModifiers.CreateOptions(newModifier));
```

**The parser no longer knows where it is.** Throw, and let it unwind to the
nearest recovery point:

```csharp
throw new SourceFileReadException(Parser,
    ErrorCreator.RootNonActiveNamespace.CreateOptions(ExtractedKeyword));
```

A recovery point is a loop over a repeatable construct which catches
`SourceFileReadException` and calls `AbstractParserBase.RecoverFromError`. That
queues the message and skips ahead to somewhere the next construct could start.
If you add one, read the contract on `RecoverFromError` first: the loop **must**
break when it sees one of the `terminatorChars`, because that is the one case
where recovery returns without having moved the cursor, and a loop which
continues anyway spins forever.

**Known deviation:** `StatementParser.cs` has ~30 calls of the shape
`new SourceFileReadException(Parser, null, "some literal message")`. That
overload puts the text in the *notes* field with an empty error message — it is
a placeholder from unfinished work, **not** the convention. Do not copy it into
new code, and migrate those calls to `ErrorRepository` when you touch them. It is
the only reason `SourceFileReadException` still accepts a null
`ErrorCreateOptions`; once that file is done, make the parameter non-nullable and
the compiler will refuse any future message written in place.

Error messages in this codebase are long, specific and explain what the compiler
expected and why. Match that register; terse messages like `"Unexpected token"`
are out of place here.
