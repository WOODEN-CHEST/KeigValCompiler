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

`DefaultInternalContentProvider` synthesises the built-in `KGVL` namespace
(`Int8`…`UInt64`, `TwoIntDecimal`, `Boolean`, `String`, `Null`) into the pack
before resolution, and records them in `BuiltInTypeRegistry`.

### Stage 3 — Emit

**Nothing exists.** There is no reference anywhere in the codebase to
`.mcfunction`, `pack.mcmeta`, scoreboards, or writing any output file.
`CompilerOptions.DestinationDirectory` is validated and never read.

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

## Current state (as of 2026-09-19)

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
overloads including conversions), generics with constraints, and every
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
- `TwoIntDecimal` has several unimplemented operator/conversion members.
- No pattern matching beyond a bare `is SomeType`, by design.
- `raw` and `constalloc` remain reserved with no meaning.

## Suggested order of work

Roughly dependency-ordered; the owner decides priorities.

1. ~~Get the build green.~~ Done.
2. ~~Finish the parser.~~ Done.
3. Wire up `KeigValCompilerTest` so the two fixtures run automatically. Both are
   currently checked by eye, which will not survive the resolver work.
4. Decide how the built-in types and standard library are declared, since the
   resolver needs somewhere to resolve `KGVL.String` *to*. Stub `.kgvl` files
   parsed by the compiler itself are the leading idea.
5. Get `Semantician/Resolver/**` compiling again and invoke it from
   `CompilePack`; implement `SearchForIdentifier`.
6. Design and prototype the datapack backend.

Step 6 is worth starting **earlier than its position suggests**, even crudely.
It is the entire unexplored risk of the project, and its constraints (see
[`language.md`](language.md)) should feed back into language design before more
front-end work hardens around assumptions the target cannot support.
