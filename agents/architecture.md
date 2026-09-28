# Architecture and current state

Deliberately brief. The project is too early for a detailed file map, and one
would go stale within a few commits. Read the code.

## Intended pipeline

```
.kgvl source files
   |
   |  1. PARSE ............................ complete (syntax only)
   v
DataPack  (in-memory object model)
   |
   |  2. RESOLVE .......................... excluded from the build
   v
DataPack  (identifiers resolved to targets)
   |
   |  3. EMIT .............................. does not exist
   v
Minecraft datapack (.mcfunction, pack.mcmeta, ...)
```

### Stage 1 — Parse (`KeigValCompiler/Source/Parser/`)

```
PackParser              walks the source directory for *.kgvl
  SourceFileParser      reads one file
    CommentStripper     removes comments (interpolation-aware)
    SourceFileRootParser  namespaces and using directives
      MemberParser        types and their members
        StatementParser   function bodies
```

`SourceDataParser` is the shared cursor over the stripped source text — index,
line tracking, and the `Read*` / `Skip*` primitives every parser above is built
on. `AbstractParserBase` gives the sub-parsers access to the shared
`PackParsingContext`.

### Stage 2 — Resolve (`KeigValCompiler/Semantician/Resolver/`)

**This whole directory is excluded from compilation** by
`<Compile Remove="Semantician\Resolver\**" />` in the `.csproj`. It is not merely
uninvoked — it does not build. It references `MemberRetrieveStatement`, a class
that does not exist, and `Statement.SubStatements`, which was removed and later
replaced by `Children`. Expect a wave of errors when re-enabling it.

`FullPackResolver` runs three passes **in a fixed order that must not change**:
`NameSpaceResolver` → `ParentItemResolver` → `IdentifierResolver`.

The object model lives in `KeigValCompiler/Semantician/Member/` (`PackClass`,
`PackFunction`, `PackField`, …) with statements under `Member/Code/`. An
`Identifier` carries both its `SourceCodeName` and, after resolution, a
`ResolvedName` and a `Target`.

Two checks the parser cannot make are left to this stage, because they need
names or types. **Enum constant values** are stored as the expressions written
for them (`PackEnumerationConstant.ValueExpression`), since they may name other
constants; this stage computes them in declaration order, a constant without
one being the previous value plus one, and checks each fits `int`. And **integer
literals** carry their value and C# type, but whether that value fits the type
it is assigned to is checked here.

`DefaultInternalContentProvider` synthesises the built-in `KGVL` namespace
(`Int8`…`UInt64`, `TwoIntDecimal`, `Boolean`, `String`, `Null`) into the pack
before resolution, and records them in `BuiltInTypeRegistry`. It is to be
replaced by the standard library described next, and removed.

### The standard library (planned, not built)

Decided on 2026-09-28; what the library means for the language is in
[`language.md`](language.md#built-in-types-and-the-standard-library). None of
this exists yet except `library-stubs/KGVL/Int32.kgvl`, an empty start.

**Loading.** `library-stubs/` is copied to the build output and parsed before
the user's sources, into the same `DataPack`; a `--library <dir>` option points
elsewhere. Each `PackSourceFile` records whether it came from the library or
from the user. Only library files may use `builtin`; anywhere else
`MemberParser` keeps reporting `ReservedKeywordBuiltIn`. What makes a file a
library file is where the compiler loaded it from, never anything written in it.
Errors in library files get a category of their own, since they are never the
user's fault.

**Resolving.** Stage 2 runs in this order:

1. Collect every declaration, library and user.
2. Look up each known type (`KGVL.Int32`, `KGVL.String`, …) by full name, among
   library files only, into `BuiltInTypeRegistry`. This has to precede step 3,
   because the library's own signatures say `int`.
3. Resolve signatures: member types, parameters, base types, constraints.
4. Bind builtin members, as below. Stop here if the library has any error.
5. Resolve and check function bodies.

**Binding builtin members by signature.** There are no attributes or tags;
`builtin` is the only marker. A builtin member is identified by its resolved
signature (declaring type, kind, name or operator, static or not, return type,
parameters), and the compiler holds a table of the signatures it implements.
The check runs both ways: every builtin member must match one entry, and every
entry must be matched by one member. The planned layout, in
`Semantician/Library/`:

- `LibraryTypes`: every known type, one line each, with its keyword alias. The
  only central list, and one line per type rather than per member.
- `MemberSignature`: a signature, compared by value. `SignatureBuilder` makes a
  binding read like the declaration it matches, and `SignatureFormatter` prints
  signatures in error messages.
- `Bindings/`: one file per family of types. `IntegerBindings` covers all eight
  integer types in one loop, `NumericConversionBindings` holds the conversion
  table, and so on. Member names are constants at the top of the file that binds
  them; operators need no names, since `OverloadableOperator` identifies them.
- A binding maps a signature to an `IntrinsicOperation` (`Add`, `Convert`,
  `Parse`, …). Operand types come from the signature, so one `Add` serves every
  integer width. The backend will switch on the operation and never look at
  names; it is kept apart from the bindings because it will be organised by how
  values are stored, not by library type.

So renaming a builtin member, or changing its parameters, means editing one line
in the family file that binds it, and renaming a type means editing its line in
`LibraryTypes`. The same `MemberSignature` descriptors find the non-builtin
members the compiler relies on, such as `Object.ToString` for interpolation and
`IEnumerator<T>.MoveNext` for `foreach`. `BuiltInTypeRegistry` becomes a map from
each known type to its parsed `PackMember`.

### Stage 3 — Emit

**Nothing exists.** There is no reference anywhere in the codebase to
`.mcfunction`, `pack.mcmeta`, scoreboards, or writing any output file.
The `--output` argument fills `CompilerOptions.DestinationDirectory`, which
nothing reads yet.

## Command line

`Main/Commandline/` is a small argument-parsing framework: flags, options (a
value each, optionally repeatable) and positional arguments, each with a value
parser that converts and validates its text. The compiler's own arguments are
declared and registered in `Main/CompilerArguments.cs` and read into
`CompilerOptions`; the `--help` text is generated from what is registered.
Command-line mistakes are ordinary `ErrorRepository` definitions (category
`CL`), all reported together before compilation starts.

## Error reporting

Errors and warnings are queued into one `CompilerMessageCollection`, shared by
every stage through its context object, and printed together at the end of the
stage that produced them. A stage which produced any error stops the
compilation, because running the next stage on what a failed one left behind
buries its errors under invented ones.

Inside a stage the parser recovers and keeps going, so one mistake does not hide
the rest of the file. Recovery is panic-mode: the throw unwinds to the nearest
loop over a repeatable construct, which queues the message and skips ahead to
somewhere the next construct could plausibly start. Those loops are the per-file
loop in `PackParser`, `SourceFileRootParser.ParseBase`, the member loop in
`MemberParser.ParseExtendableType`, `MemberParser.ParseEnumValues`, and
`StatementParser.ParseStatementBody`, which recovers at the next `;` so that one
broken statement does not cost the rest of the function.

Recovery is a heuristic and only claims three things: it terminates, the first
error in a file is accurate, and later code is still reached. Whether the second
error in a file is useful or noise depends on the input. After a recovered error
the object model holds a partially built, possibly nonsensical tree, which is
the other reason the next stage must not run.

## Current state (as of 2026-09-28)

The build is **green** and **the parser is syntactically complete**: it reads
every construct the language has, all the way down to expressions inside
function bodies, and builds the full statement tree for them. `tests/test.kgvl`
exercises the whole grammar and parses with zero errors.

The parser only reads. It resolves no names, checks no types and validates
nothing — `Foo bar = Nonexistent();` parses happily. That is the resolver's job
and the resolver does not compile yet.

### Works
Everything in the grammar. Types (classes, structs, interfaces, records, enums,
delegates, events), their members (fields, properties with `get`/`set`/`init`,
indexers, functions, constructors with `this`/`base` chaining, operator
overloads including conversions, in interfaces too), `const` fields and locals,
generics with constraints, and every
statement and expression form: precedence-correct operators, assignment,
ternary, lambdas, `switch` expressions, `new` with object/collection/array
initializers, indexing, member and conditional access, `yield`, `catch ... when`,
interpolated strings and all literal forms.

### The two blocking gaps

1. **The resolver is excluded from the build.** See stage 2 above.
   `Compiler.CompilePack` parses and returns; the wiring that constructs a
   `PackResolutionContext` and calls `FullPackResolver` exists only inside the
   commented-out `Compiler.Test()`. Also
   `DefaultIdentifierSearcher.SearchForIdentifier` throws
   `NotImplementedException` — the actual lookup is missing.

2. **No test harness.** `KeigValCompilerTest` has no `ProjectReference` to the
   compiler and its `Main` prints `Hello, World!`. `ICodeTester`, `TestResults`
   and `TwoIntDecimalTester` exist but nothing runs them. The two fixtures,
   `tests/test.kgvl` and `tests-errors/recovery.kgvl`, are run by hand and their
   output read by eye.

### Smaller known gaps
- `TwoIntDecimal`'s `%` returns wrong remainders for most operands, and `Pow`
  inherits it for the sign of huge integer powers of negative bases. `Pow` with a
  fractional exponent also loses two or three digits to the logarithm it goes
  through. All three are noted in `language.md`.
- No pattern matching beyond a bare `is SomeType`, by design.
- `raw` and `constalloc` remain reserved with no meaning.
- `PackClass.AllSubMembers` lists every operator function twice:
  `MemberContainer.AllMembers` already adds them, and `PackClass` adds them
  again. `PackStruct` and `PackInterface` do not.
- `MemberContainer.Members` leaves out enums, so anything walking `SubMembers`,
  such as `ParentItemResolver`, never reaches a nested enum.
- An unoverloadable operator (`operator &&`, `operator true`) is reported
  correctly, but recovery then resumes after the parameter list, as
  `SkipToSyncPoint` does after any bracketed group it skipped, so the body after
  it produces a second, spurious "Expected class member" error.

## Suggested order of work

Roughly dependency-ordered; the owner decides priorities.

1. ~~Get the build green.~~ Done.
2. ~~Finish the parser.~~ Done.
3. Wire up `KeigValCompilerTest` so the two fixtures run automatically. Both are
   currently checked by eye, which will not survive the resolver work.
4. ~~Decide how the built-in types and standard library are declared.~~
   Decided: see "The standard library" above. Building it, in order:
   1. ~~The language additions the library needs: `object`, `char` and `const`,
      the full set of overloadable operators, and operators in interfaces.~~
      Done.
   2. Loading the library, and restricting `builtin` to it.
   3. The first library files: `Object`, `Boolean`, `Char`, the eight integer
      types, `Decimal`, `String`, `Array`, `Nullable`, `IEquatable`,
      `IComparable`, `IParsable`, the exceptions that compiler-inserted checks
      and `Parse` throw, and `IEnumerable`/`IEnumerator`. They must parse with
      zero errors, which makes them a second parser fixture.
   4. `Semantician/Library/` and the binding files. These can only be checked
      once step 5 runs.

   Later, once the resolver handles static abstract members, the `KGVL.Numerics`
   generic maths interfaces.
5. Get `Semantician/Resolver/**` compiling again and invoke it from
   `CompilePack`; implement `SearchForIdentifier`.
6. Design and prototype the datapack backend.

Step 6 is worth starting **earlier than its position suggests**, even crudely.
It is the entire unexplored risk of the project, and its constraints (see
[`language.md`](language.md)) should feed back into language design before more
front-end work hardens around assumptions the target cannot support.
