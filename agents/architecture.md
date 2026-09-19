# Architecture and current state

Deliberately brief. The project is too early for a detailed file map, and one
would go stale within a few commits. Read the code.

## Intended pipeline

```
.kgvl source files
   |
   |  1. PARSE ............................ partially built
   v
DataPack  (in-memory object model)
   |
   |  2. RESOLVE .......................... skeleton, never invoked
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

## Current state (as of 2026-09-19)

### Works
Comment stripping; namespaces and usings; classes, structs, interfaces, records
(including primary constructors), enums, delegates, events; generic parameters
and `where` constraints; function parameter lists and modifiers; literal parsing
for integers (binary/hex/underscore-separated), decimals, chars with escape
sequences, and strings.

### The four blocking gaps

1. **No expression parser.** `StatementParser` handles every control-flow
   *keyword* (`if`, `while`, `do`, `for`, `switch`, `try`, `throw`, `return`,
   `break`, `continue`) but `ParseNonKeywordStatement()` and
   `ParseAssignmentStatement()` both throw `NotImplementedException`. There is
   no precedence-climbing or expression-tree construction anywhere.
   `OperatorStatement`, `TernaryStatement` and `StatementOperator` are defined
   but never constructed. This blocks all real code parsing.

2. **Member bodies are switched off.** In `MemberParser`,
   `ParseReturnTypedMember` is entirely commented out, and `ParseFunction`,
   `ParseField` and `ParseProperty` have empty bodies. The parser therefore
   walks type declarations and silently produces *nothing* for their contents.

3. **The resolver is orphaned.** `Compiler.CompilePack` parses and returns. The
   wiring that constructs a `PackResolutionContext` and calls `FullPackResolver`
   exists only inside the commented-out `Compiler.Test()`. Also
   `DefaultIdentifierSearcher.SearchForIdentifier` throws
   `NotImplementedException` — the actual lookup is missing.

4. **No test harness.** `KeigValCompilerTest` has no `ProjectReference` to the
   compiler and its `Main` prints `Hello, World!`. `ICodeTester`, `TestResults`
   and `TwoIntDecimalTester` exist but nothing runs them.

### Smaller known gaps
- `ParseParenthesisStatement` is half-written (empty `else`) — this is usually
  the one thing breaking the build.
- `SourceDataParser.ReadInterpolatedString` is a stub.
- `TwoIntDecimal` has several unimplemented operator/conversion members.
- `tests/test.kgvl` is corrupted around line 76.

## Suggested order of work

Roughly dependency-ordered; the owner decides priorities.

1. Get the build green.
2. Wire up `KeigValCompilerTest` and make it parse `tests/test.kgvl`. A feedback
   loop should come before more features.
3. Build the expression parser — it unblocks gap 1 and much of gap 2.
4. Restore and implement function/field/property parsing.
5. Invoke the resolver from `CompilePack`; implement `SearchForIdentifier`.
6. Design and prototype the datapack backend.

Step 6 is worth starting **earlier than its position suggests**, even crudely.
It is the entire unexplored risk of the project, and its constraints (see
[`language.md`](language.md)) should feed back into language design before more
front-end work hardens around assumptions the target cannot support.
