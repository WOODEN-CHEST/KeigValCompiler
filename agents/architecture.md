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
   |  2. RESOLVE .......................... declarations only; bodies not yet
   v
DataPack  (identifiers resolved to targets)
   |
   |  3. EMIT .............................. does not exist
   v
Minecraft datapack (.mcfunction, pack.mcmeta, ...)
```

### Stage 1 — Parse (`KeigValCompiler/Source/Parser/`)

```
PackParser              walks a directory for *.kgvl: the standard library's, then the user's
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

Resolves every declaration and binds the standard library. **Function bodies
are not resolved at all yet**: nothing inside one is looked up or checked, so
`Foo bar = Nonexistent();` in a body still compiles. `Compiler.CompilePack` runs
it after parsing, with messages of its own, and stops on any error; the
`--parse-only` flag stops before it.

`FullPackResolver` runs these passes **in a fixed order that must not change**,
each relying on what the ones before it set:

1. `NameSpaceResolver`: every namespace's identity, and every member's namespace.
2. `ParentItemResolver`: every member's `ParentItem`, the member holding it.
3. `TypeDeclarationResolver`: names every type and its generic parameters, and
   reports two types with the same name and generic parameter count in one
   namespace or type, across files.
4. `KnownTypeResolver`: finds each `LibraryTypes` entry among library files only
   and records it in `BuiltInTypeRegistry`, which is how `int` finds `KGVL.Int32`.
5. `SignatureResolver`: resolves every type written in a declaration (base
   types, constraints, member types, parameters, explicit interfaces) by pointing
   its `Identifier.Target` at the type or `GenericTypeParameter` it means.
   `TypeSearcher` looks names up as C# does: generic parameters and nested types
   around the use, innermost first; then the namespace and each containing it;
   then `using` imports, where two matches are ambiguous. A type matches by name
   and generic parameter count. Accessibility is not checked yet.
6. `MemberIdentifierResolver`: names every other member. A function's name
   carries its parameter types, so overloads stay apart.
7. `LibraryBindingResolver`: binds the library, as described below.

Errors are queued, like the parser's, so one missing type does not hide the rest.
A type which cannot be resolved is left with no `Target`, and later passes skip
it rather than report it again. Resolved names use internal separators from
`KGVL.cs` (`!`, `&`, `%`, `` ` `` for generic arity, `#` for generic parameters),
so `Foo` and `Foo<T>` differ.

The object model lives in `KeigValCompiler/Semantician/Member/` (`PackClass`,
`PackFunction`, `PackField`, …) with statements under `Member/Code/`. An
`Identifier` carries both its `SourceCodeName` and, after resolution, a
`ResolvedName` and a `Target`.

Two checks the parser cannot make are left to this stage, and are not done yet,
because they need function bodies' expressions resolved. **Enum constant values**
are stored as the expressions written for them
(`PackEnumerationConstant.ValueExpression`), since they may name other constants;
they are to be computed in declaration order, a constant without one being the
previous value plus one, and each checked to fit `int`. And **integer literals**
carry their value and C# type, but whether that value fits the type it is
assigned to is to be checked here.

### The standard library

Decided on 2026-09-28; what the library means for the language is in
[`language.md`](language.md#built-in-types-and-the-standard-library). Loading
the library, its first files, the table of what the compiler implements for it,
and matching the two are all built.

**Loading (built).** `KeigValCompiler.csproj` copies `library-stubs/` into the
build output, where `CompilerOptions.LibraryDirectory` finds it by default;
`--library <dir>` points elsewhere. `Compiler.CompilePack` has `PackParser` read
the library first and the user's sources second, into the same `DataPack`, and
each `PackSourceFile` records which it is (`SourceFileKind`). Only library files
may use `builtin`; anywhere else `MemberParser` reports `ReservedKeywordBuiltIn`.
What makes a file a library file is where the compiler loaded it from, never
anything written in it. Errors found in library files are reported as usual,
plus one `StandardLibrary` error saying they are the library's fault rather than
the user's; a missing library directory is another. The user's code is parsed
even when the library fails, since its parse errors do not depend on it.

**Binding builtin members by signature.** There are no attributes or
tags; `builtin` is the only marker. A builtin member is identified by its
resolved signature (declaring type, kind, name or operator, static or not, type,
parameters), and the compiler holds a table of the signatures it implements.
The check runs both ways: every builtin member must match one entry, and every
entry must be matched by one member. In `Semantician/Library/`:

- `LibraryTypes`: every known type, one line each, with its keyword alias. The
  only central list, and one line per type rather than per member.
- `MemberSignature`, `SignatureType` and `SignatureParameter`: a signature,
  compared by value. Arrays and nullable value types are the `Array` and
  `Nullable` library types, and a `?` on a reference type is no part of a
  signature, as in C#. `SignatureBuilder` makes a binding read like the
  declaration it matches, and `SignatureFormatter` prints signatures for
  messages.
- `Bindings/`: one file per family of types, listed in `DefaultLibraryBindings`.
  `IntegerBindings` covers all eight integer types in one loop, and
  `NumericConversionBindings` holds the implicit/explicit table as a grid, the
  same one `language.md` shows. Member names are constants at the top of the
  file that binds them, except those the language fixes, such as `ToString` and
  `Parse`, which are in `LibraryMemberNames`; operators need no names, since
  `OverloadableOperator` identifies them.
- A binding maps a signature to an `IntrinsicOperation` (`Add`, `Convert`,
  `Parse`, …). Operand types come from the signature, so one `Add` serves every
  integer width. The backend will switch on the operation and never look at
  names; it is kept apart from the bindings because it will be organised by how
  values are stored, not by library type.
- `LibraryBindingTable`: `MatchIntrinsic` looks a builtin member's signature up
  and records it as matched, and `UnmatchedSignatures` is what no member claimed.

So renaming a builtin member, or changing its parameters, means editing one line
in the family file that binds it, and renaming a type means editing its line in
`LibraryTypes`. A binding written twice, or a malformed conversion grid, throws
when the table is built, since either is a mistake in the compiler.

`LibraryBindingResolver`, the last resolution pass, does the matching.
`BuiltInSignatureReader` turns each resolved builtin member into a
`MemberSignature`, a property's or indexer's accessors each becoming one, and a
matched function's `PackFunction.Intrinsic` records its operation. Each side's
leftovers are `StandardLibrary` errors: a builtin member nothing implements, and
an implementation no member declares (left out when the library already has
other errors, which would cause it). The pass also checks that a builtin member
has no body and a builtin type no instance fields. Resolution errors in library
files get the same extra error as parse errors, saying the library is at fault.
The same descriptors are meant to find the non-builtin members the compiler
relies on, such as `Object.ToString` for interpolation and
`IEnumerator<T>.MoveNext` for `foreach`; that is still to do, with function
bodies. At the time of writing, all 308 bindings match the 308 builtin members
the library declares.

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

## Current state (as of 2026-09-29)

The build is **green**. **The parser is syntactically complete** apart from the
gaps in [`parser-gaps.md`](parser-gaps.md): it reads every construct, down to
expressions inside function bodies, and builds the full statement tree for them.
`tests/test.kgvl` exercises the grammar and parses with zero errors.

**The resolver resolves declarations and binds the standard library**, but does
not look inside function bodies: `Foo bar = Nonexistent();` in a body still
compiles. Four fixtures cover what exists, all run by hand:

| Directory | Run with | Expect |
|---|---|---|
| `tests/` | `--parse-only`, since it names undeclared types | zero errors |
| `tests-errors/` | | the parse errors its header lists |
| `tests-resolution/` | | zero errors |
| `tests-resolution-errors/` | | the resolution errors its header lists |

### Works
The grammar, apart from the parser gaps listed under the smaller known gaps
below. Types (classes, structs, interfaces, records, enums, delegates, events)
with base types which may have type arguments, their members (fields, properties
with `get`/`set`/`init`, indexers, functions, constructors with `this`/`base`
chaining, operator overloads including conversions, in interfaces too, and
explicit interface implementations), `const` fields and locals, generics with
constraints, and the statement and expression forms: precedence-correct operators, assignment,
ternary, lambdas, `switch` expressions, `new` with object/collection/array
initializers, indexing, member and conditional access, `yield`, `catch ... when`,
interpolated strings and all literal forms. The standard library is read from
`library-stubs/` before the user's code, and only it may use `builtin`. Every
type named in a declaration resolves, and every builtin member of the library
is bound to what the compiler implements for it.

### The two blocking gaps

1. **Function bodies are not resolved.** No expression has a type, no name
   inside a body is looked up, and no overload, conversion or operator is chosen.
   This is the next large piece of the resolver, and needs designing first.

2. **No test harness.** `KeigValCompilerTest` has no `ProjectReference` to the
   compiler and its `Main` prints `Hello, World!`. `ICodeTester`, `TestResults`
   and `TwoIntDecimalTester` exist but nothing runs them. The four fixtures above
   are run by hand and their output read by eye.

### Smaller known gaps
- `TwoIntDecimal`'s `%` returns wrong remainders for most operands, and `Pow`
  inherits it for the sign of huge integer powers of negative bases. `Pow` with a
  fractional exponent also loses two or three digits to the logarithm it goes
  through. All three are noted in `language.md`.
- No pattern matching beyond a bare `is SomeType`, by design.
- `raw` and `constalloc` remain reserved with no meaning.
- Parser gaps, each detailed in [`parser-gaps.md`](parser-gaps.md): qualified
  type names such as `KGVL.Int32` do not parse, nor does the `as` operator, nor
  throw expressions, nor the `\e` and `\U` escapes.
- Declarations are resolved but not yet checked against C#'s rules: accessibility,
  two members with the same signature, a class with two base classes or a struct
  deriving from one, interfaces left unimplemented, and the rules for declaring
  operators all go unreported.

## Suggested order of work

Roughly dependency-ordered; the owner decides priorities.

1. ~~Get the build green.~~ Done.
2. ~~Finish the parser.~~ Done.
3. Wire up `KeigValCompilerTest` so the four fixtures run automatically. All are
   currently checked by eye, which will not survive the resolver work.
4. ~~Decide how the built-in types and standard library are declared.~~
   Decided: see "The standard library" above. Building it, in order:
   1. ~~The language additions the library needs: `object`, `char` and `const`,
      the full set of overloadable operators, and operators in interfaces.~~
      Done.
   2. ~~Loading the library, and restricting `builtin` to it.~~ Done.
   3. The first library files: `Object`, `Boolean`, `Char`, the eight integer
      types, `Decimal`, `String`, `Array`, `Nullable`, `IEquatable`,
      `IComparable`, `IParsable`, the exceptions that compiler-inserted checks
      and `Parse` throw, and `IEnumerable`/`IEnumerator`. They must parse with
      zero errors, which makes them a second parser fixture. Done, and
      reviewed by the owner. Text conversion with options, such as a number
      base, separators or a format, is to go in a library class of its own, so
      that the primitive types keep only plain `Parse` and `ToString`.
   4. ~~`Semantician/Library/` and the binding files.~~ Done, and matched
      against the library by the resolver.

   Later, once the resolver handles static abstract members, the `KGVL.Numerics`
   generic maths interfaces.
5. The resolver.
   1. ~~Resolve declarations and bind the standard library.~~ Done: see stage 2.
   2. Check declarations against C#'s rules, as listed under the smaller known
      gaps.
   3. Resolve function bodies: expression types, names inside bodies, overloads,
      conversions and operators, with the operators the library declares on its
      built in types standing in for C#'s predefined ones. Then the enum values
      and literal ranges described under stage 2.
6. Design and prototype the datapack backend.

Step 6 is worth starting **earlier than its position suggests**, even crudely.
It is the entire unexplored risk of the project, and its constraints (see
[`language.md`](language.md)) should feed back into language design before more
front-end work hardens around assumptions the target cannot support.
