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
   |  2. RESOLVE .......................... declarations; bodies in part
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

Resolves every declaration, binds the standard library, and binds function
bodies into a bound tree, **so far only in part** (step A of 5.3, see "Binding
function bodies" below): what is not bound yet is kept as a node which counts as
having errors, so nothing in it is checked or reported. `Compiler.CompilePack`
runs it after parsing, with messages of its own, and stops on any error; the
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
   Every type's base list is resolved first, each when first needed, since a
   lookup may need the bases of a type around the use or of a qualifier before
   the pass reaches them; while a type's own base list is being resolved it
   has no bases, as in C#, which also ends loops. A lookup which finds nothing
   in a class or structure in that state ends there with RS 65, as C# ends it
   with CS0146, since what the type inherits could have been the answer; for
   an interface in that state the lookup goes on, as in C#. Base lists waiting
   on each other more than 100 deep, which no real program does, stop
   resolution with RS 66 (`ResolutionStoppedException`, which
   `FullPackResolver` catches), rather than run out of stack or report the
   whole chain. Everything else follows.
   `TypeSearcher` looks names up as C# does: generic parameters around the
   use, and the nested types each type around it declares or inherits,
   innermost first (a class inherits its base classes' nested types, the
   nearest first, and an interface its base interfaces', where two which
   neither hides are ambiguous, and one deriving from itself derives from
   nothing; nothing is inherited from an implemented interface); a type's
   own base list, constraints and a record's parameter
   list are outside it, and see only its generic parameters there. Then, at
   the namespace and each containing it, a namespace of the name inside it
   and then a type it holds, and a root namespace of the name; then `using`
   imports, where two matches are ambiguous. A type matches by name and
   generic parameter count. A namespace's type the use cannot see, in
   practice the library's `internal` types from user code, is passed over,
   and reported as inaccessible when nothing else matched. Where one
   namespace holds both a namespace and a type of a name, the user's code
   finds its own over the library's, as Roslyn prefers what a compilation
   declares to what it imports, and a clash on one side, which
   `DeclarationNameChecker` reports, finds the type, and then the namespace
   for what follows it, so that only the clash is reported. The standard
   library is looked at as its own assembly, and sees only its own
   namespaces and types. A qualified name, such as `KGVL.Collections.List<int>`
   or `Outer<int>.Inner`, is a chain of `TypeTargetIdentifier.Qualifier`s,
   each with its own type arguments, resolved from the first: each names a
   namespace, whose identifier then carries its full name, or a type, in
   which the next name is looked for, a namespace inside a namespace before a
   type. The lookup tells base interfaces apart by declaration, so a type
   found through an interface's bases is looked for again among them as the
   type model reads them: `IA<int>` and `IA<string>` are two interfaces, and
   a name both declare is ambiguous (RS 64, C#'s CS0104).
   `SemanticTypeReader` takes a nested type's containing type from its
   qualifier, or from the type around the use it was inherited through
   (`TypeTargetIdentifier.InheritedThrough`), as that type sees the base
   declaring it, so `Outer<int>.Inner` and `Outer<string>.Inner` differ, and
   `Inner` inside a class deriving from `Outer<int>` is `Outer<int>.Inner`.
   That base is read along the way the lookup went to it, which
   `SignatureResolver` records (`TypeTargetIdentifier.InheritedPath`), since
   reading afterwards could not tell which base lists were still being
   resolved at the time, and each such name is read once; a holder with no
   generic parameters, nor any type around it, is taken as it is. A base on
   the way which cannot be read leaves the nested type unread too, since that
   has been reported. A
   nested type the use cannot access, as `AccessDomains.IsAccessibleFrom`
   decides it with the bases the lookup asks `SignatureResolver` for, and
   only when its access needs them, is passed over as C# passes over it, and
   reported (C#'s CS0122) and left unresolved when nothing else matched, so
   that the checks after it skip it, as Roslyn skips a type in error.
6. `MemberIdentifierResolver`: names every other member. A function's name
   carries its parameter types, so overloads stay apart.
7. `LibraryBindingResolver`: binds the library, as described below.
8. `RecordPropertyResolver`: gives each record a property for each positional
   parameter, as C# does, unless it declares or inherits a member keeping that
   value; see `language.md`. It runs last because which to make depends on
   inherited members, and it names what it makes. A made property has
   `IsSynthesized` set, and the checks of the types a member writes
   (`ConstraintChecker`, `StaticClassUsageChecker`) skip it, since its type is
   the parameter's, checked with the parameter. The checks of overriding and
   implementing treat it as any other property.

Then the checks of declarations against C#'s rules, which need all of the above.
`MemberRelations` answers what they share: what holds a member, what kind of
member it is, its access once C#'s defaults are applied, and how messages name
it.

- `ModifierChecker`, first: which modifiers each kind of member can have where
  it is declared, from a table per holder (namespace, class, static class,
  structure, interface, property or indexer), which pairs conflict, that every
  operator is `static` and, outside an interface and unless it implements one
  explicitly, written `public`, and C#'s rules for accessors and readonly
  members, an accessor's own access included: on one of two accessors only
  (an override's other accessor may be inherited), and strictly narrower than
  its member's. An invalid mix of access modifiers counts as public, as
  Roslyn reads it (`MemberRelations.IsValidAccess`). A modifier not allowed at all is reported and then **taken off the
  member**, as Roslyn does, into `PackMember.RejectedModifiers`, so the checks
  after it see only what the member may have and do not report what follows
  from a wrong modifier. After resolution, `PackMember.Modifiers` therefore
  holds only the written modifiers the member may have. Implied ones are never
  added: a default access, a `const`'s `static`, or an interface member's
  `abstract`. `MemberRelations.GetEffectiveAccess` and
  `MemberRelations.IsAbstract` answer those.
- `ImportChecker`: a `using` naming a namespace nothing declares, which as in C#
  exists if a namespace inside it does (`DataPack.GetExistingNamespaceNames`).
- `MemberPlacementChecker`: what a namespace, an interface, a static class or a
  readonly structure cannot hold, and an event whose type is not a delegate.
- `MemberBodyChecker`: which functions and accessors need a body and which
  cannot have one, properties storing their own value, and what constructors
  can run first: as in C#, each constructor a record with a parameter list
  declares runs another first with `: this(...)`, but for its copy
  constructor, taking the record by value, and a static one. Only that
  `: this(...)` is written is checked, not where the chain ends.
- `ParameterChecker`: what may follow what in a parameter list, as in C#: a
  `params` parameter comes last, and after a parameter with a default value
  only others with one and a `params` one. A `params` parameter's type is an
  array, written `int[]` or `Array<int>` (FN 13). A `ref`, `out` or `params`
  parameter cannot have a default, and a default a member can never use (on
  an explicit implementation, an operator, or an indexer of one parameter) is
  warned about. A default which cannot be had does not also make the
  parameters after it misplaced, unlike in Roslyn, so that it is reported
  once.
- `OperatorDeclarationChecker`: an operator's parameter count and parameter
  modifiers, its return type, and an interface's equality and conversion
  operators, which have to be abstract or virtual.
- `InheritanceChecker`: what each kind of type derives from, cycles, an
  interface listed twice (only a warning when the two differ only in `?`, as
  in C#), and a record's base arguments given to an interface. A cycle is as
  C# defines it: a class depends on its base class, an interface on its base
  interfaces, and either on the type it is declared in, so `class O : O.N`
  with `N` declared in `O` is one; a sealed or static base, which is reported,
  is dropped, as Roslyn drops it. As in Roslyn, a class's base class is the
  first of its bases which is not an interface: one which did not resolve, or
  is no class, keeps that place, leaving the class with none but `object` in
  `TypeHierarchy`, and a class after it is not taken for the base class. A
  base written as an array or with `?` (RS 18, C#'s CS1521) is left out of
  this pass's other checks; as Roslyn does, a class's base class and an
  interface's base interfaces written with `?` are still bases everywhere
  else, read as the type named, while a class's or a structure's interfaces
  written so are none (`SemanticTypeReader.GetWrittenBaseClassName`,
  `MemberRelations.GetBaseDeclarations`).
- `DeclarationNameChecker`: names that cannot coincide, from two enum constants
  of one name to a field named like a function beside it, a type's generic
  parameter, or a namespace.

The checks after these compare types, through the model in
`Semantician/Types/`. `TypeHierarchy` gives a type's base classes and every
interface it implements, in terms of its own type arguments, and answers
whether a type is a reference or value type or derives from another.
`GenericConstraintReader` gives a generic parameter's constraints as types: an
override's or explicit implementation's parameters take theirs from what they
override or implement, as in C#, which is why `InheritedConstraintResolver`
links each to its source before the type-comparing checks run.
`GenericParameterType.IsValueType` still reads only written constraints; code
which needs the inherited ones asks `TypeHierarchy`. A `DeclaredSignature`, read
by `DeclaredSignatureReader`, is what C# compares two functions, indexers or
operators by: generic parameter count, parameter types with generic parameters
compared by position, and which are passed by reference. `InheritedMembers`
holds C#'s rules for what an override overrides, which members hide which (by
name, generic parameter count and signature), which accessors an override
inherits, and what an explicit implementation implements.

- `DuplicateSignatureChecker`: two members of one type or namespace with the
  same signature, and two conversions between the same types.
- `OperatorTypeChecker`: that an operator takes or converts its own type (in an
  interface's abstract or virtual operator, a parameter constrained to the
  interface counts), what `++` and `--` return, what a conversion cannot
  convert between, and the operators declared in pairs.
- `OverrideChecker`: what an override overrides, which may be hidden by a
  nearer member of another kind or ambiguous; its access, type (with covariant
  returns) and accessors, own or inherited; abstract members and accessors left
  unoverridden; and hiding, warned about with and without `new` as in C#, and
  an error for an abstract member.
- `InterfaceImplementationChecker`: every abstract interface member, static
  ones included, implemented implicitly (the nearest suitable member, with the
  interface's accessors and constraints) or explicitly; default
  implementations and re-abstraction decided by the most specific interface;
  every explicit implementation naming an interface its type lists and a
  member of it which can be implemented, with that member's type and
  accessors; and no two listed interfaces which could unify.
- `ConstraintChecker`: the constraint lists themselves, constraints cycling
  through a declaration's generic parameters, conflicting classes reached
  through other parameters, overrides not restating theirs and agreeing on
  `class` and `struct`, and every type argument in a declaration satisfying
  its parameter's constraints, and in a body as it is bound.
- `FieldTypeChecker`: `const` field types, and structures holding themselves,
  worked out by declaration: which generic parameters each structure holds,
  then which structures hold which.
- `StaticClassUsageChecker`: a static class used as the type of a value. As
  in C#, only a warning in an interface's signatures, and allowed as a
  delegate's return type. The types written in bodies are checked as they are
  bound, through `BodyBinder.CheckWrittenType`, which also checks their
  constraints.
- `AccessibilityChecker`: a type less accessible than a member naming it in
  its declaration (C#'s inconsistent accessibility, a class's base class and
  an interface's base interfaces included), and required members, and their
  setters, as accessible as their type. `AccessDomains` compares two
  members' accessibility domains as Roslyn's `IsAsRestrictive` does, and
  answers what derives from what by declaration through an
  `IBaseTypeSource`: `SignatureResolver` while signatures are resolved,
  `ResolvedBaseTypes` afterwards.

Last, `BodyResolver` binds every body, as the next section describes.

### Binding function bodies (step 5.3, in progress)

Decided with the owner on 2026-10-01 (`language.md`, "Function bodies"):
resolution builds a separate **bound tree** from each body's parse tree, which
stays as written. The bound tree is in `Semantician/Bound/`: `BoundNode`, with
`BoundExpression` (a `SemanticType` or none, for the literal null and
`default` alone, and a `ConstantValue` when it is a constant) and
`BoundStatement`, each keeping the parse tree's `Statement` it came from, for
its line. A node has errors when a mistake in it or below it was reported;
nothing more is reported about it, and `ErrorType` (in `Semantician/Types/`)
is the type of a value which could not be bound, so that one mistake makes one
message, as Roslyn's error types do. A local is a `LocalSymbol`; parameters
stay `FunctionParameter`s, a setter's `value` being made on demand
(`PackFunction.ValueParameter`). Results live on the members:
`PackFunction.BoundBody`, `PackField.BoundInitialValue` and `ConstantValue`,
`PackProperty.BoundInitialValue`.

`BodyResolver` binds constant fields first, in declaration order, so that a
loop of constants is reported once, at its first, as C# reports it (CS0110),
then every other body, the library's included, which have to bind cleanly. The
binding itself is in `Semantician/Binding/`:

- `BodyBindingContext`: what every body shares, and fields' starting values,
  bound when first needed, since a constant may be named before the pass
  reaches it. A constant may wait on others only so deep
  (`MAX_CONSTANT_DEPTH`, 100), past which resolution stops with one error
  rather than run out of stack; `ConstantDepthTester` checks the limit.
- `BodyBinder`: one body, with what its names depend on (the type around it,
  whether there is a `this`, what it returns, the locals and parameters in
  scope, through `BlockScope` and `ParameterScope`), and where its messages
  go: the line of the statement or value they are about.
- `NameBinder`: names, in the decided order, and member access through a
  namespace, a type or a value, C#'s "Color Color" rule included (§12.8.7.2:
  the member after the '.' decides which is meant, and one neither has is
  reported as missing from the value, as Roslyn reports it).
  `MemberLookup` is C#'s member lookup (§12.5) in a type, with hiding, arity
  and ambiguity, and the members a KGVL namespace holds. A function which
  overrides is left out, the one it overrides standing for it, but a property
  or event which overrides is found, as Roslyn finds it, and an accessor it
  does not declare is looked for where it overrides
  (`InheritedMembers.FindAccessor`). A name not found in a type whose bases
  did not all resolve, or which derives from itself, is not reported, since it
  may be inherited from what is missing (`TypeHierarchy.HasUnknownBases`).
- `ExpressionBinder`: literals (their C# types), `this`, `base`, `default`,
  assignment (what can be assigned, readonly fields, `init`, get-only
  properties, values of value types which are not variables), reading a
  property through its getter, and converting a value to the type it is
  needed as, reporting a conversion which does not exist as C# reports it.
- `ConversionClassifier`: C#'s conversions (§10), all of them but lambdas'
  and method groups': the library's builtin conversion operators on its
  built-in types are C#'s predefined numeric and nullable conversions, so they
  count as standard conversions around a user-defined one and never as
  user-defined ones; the rest are the language's, user-defined conversions
  following Roslyn where it differs from the specification. No array
  covariance (decided 2026-10-01).
- `StatementBinder`: blocks, with C#'s local scope rules, local declarations
  (`var` and `const` included), expression statements and `return`.
- `ConstantFolder`: constants through conversions; the rest is step B.
- `TypeNameResolver` (in `Resolver/`): the type-name lookup and its errors,
  which `SignatureResolver` and bodies share, reporting where it is told to.

What is not bound yet becomes a `BoundNotYetSupported` value or a
`BoundNotYetSupportedStatement`, never an error: operators, compound
assignment and `??=`, `?:`, casts, calls and everything callable, `new`,
arrays and indexing, lambdas, `?.`, `??`, `is`/`as`, throw expressions,
`nameof`, `typeof`, interpolated strings, switch expressions, enum constants,
events, members reached through a generic parameter, and every statement but
blocks, declarations, expression statements and `return`. In an iterator,
`return` is left for later too, with `yield`. The last step deletes both node
kinds.

Bodies are bound even when declarations have errors, unlike the C# compiler,
which stops after them (`language.md`, "Function bodies").

What remains unchecked in declarations: whether `notnull` is satisfied, which
is nullability checking; constructor chains which come back round to where
they started (C#'s CS0516 and CS0768), which need overload resolution; what a
record's copy constructor runs first (CS8868), and that it is public or
protected (CS8878). Function bodies are checked only as far as they are bound.
See "Suggested order of work".

Errors are queued, like the parser's, so one missing type does not hide the rest.
A type which cannot be resolved is left with no `Target`, and later passes skip
it rather than report it again. Resolved names use internal separators from
`KGVL.cs` (`!`, `&`, `%`, `` ` `` for generic arity, `#` for generic parameters),
so `Foo` and `Foo<T>` differ.

The object model lives in `KeigValCompiler/Semantician/Member/` (`PackClass`,
`PackFunction`, `PackField`, …) with statements under `Member/Code/`. An
`Identifier` carries both its `SourceCodeName` and, after resolution, a
`ResolvedName` and a `Target`.

Three checks the parser cannot make are left to this stage, two not done yet,
because they need function bodies' expressions resolved. **Enum constant values**
are stored as the expressions written for them
(`PackEnumerationConstant.ValueExpression`), since they may name other constants;
they are to be computed in declaration order, a constant without one being the
previous value plus one, and each checked to fit `int`. **Default parameter
values** are stored the same way (`FunctionParameter.DefaultValue`), and each is
to be checked to be a constant which converts to its parameter's type. And
**integer literals** carry their value, which body binding gives C#'s type and
checks, as C# does, to fit an integer type it converts to as a constant
(CS0031).

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

`LibraryBindingResolver`, the seventh resolution pass, does the matching.
`BuiltInSignatureReader` turns each resolved builtin member into a
`MemberSignature`, a property's or indexer's accessors each becoming one, and a
matched function's `PackFunction.Intrinsic` records its operation; a setter is
matched by the setter's signature whether it is written `set` or `init`, since
both store the value, though messages show it as written. Each side's
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

## Current state (as of 2026-10-01)

The build is **green**. **The parser is syntactically complete** apart from the
gaps in [`parser-gaps.md`](parser-gaps.md): it reads every construct, down to
expressions inside function bodies, and builds the full statement tree for them.
`tests/test.kgvl` exercises the grammar and parses with zero errors.

**The resolver resolves declarations, binds the standard library, and checks
declarations against C#'s rules**, apart from the gaps listed below, and binds
function bodies in part (step A of 5.3): names, member access, literals,
conversions, locals, assignment and `return`. Seven fixtures cover what
exists, and `KeigValCompilerTest` runs them all (`dotnet run --project
KeigValCompilerTest`), comparing every message with the headers, along with
the type model and the limits on base lists and constants:

| Directory | Run with | Expect |
|---|---|---|
| `tests/` | `--parse-only`, since it names undeclared types | zero errors |
| `tests-errors/` | | the parse errors its header lists |
| `tests-resolution/` | | zero errors |
| `tests-resolution-errors/` | | the resolution errors its header lists |
| `tests-declaration-errors/` | | the errors and warnings each file's header lists |
| `tests-bodies/` | | zero errors |
| `tests-body-errors/` | | the errors each file's header lists, each compared with C#'s |

### Works
The grammar, apart from the parser gaps listed under the smaller known gaps
below. Types (classes, structs, interfaces, records, enums, delegates, events)
with base types which may have type arguments, their members (fields, properties
and indexers with `get`/`set`/`init` or a `=>` getter, functions, constructors
with `this`/`base` chaining, a record's arguments to its base record, operator
overloads including conversions, in interfaces too, and explicit interface
implementations), parameters with default values, `const` fields and locals,
generics with constraints, and the statement and expression forms:
precedence-correct operators, assignment, ternary, `is` and `as`, throw
expressions, lambdas, `switch` expressions, `new` with object/collection/array
initializers, indexing, member and conditional access, `nameof` of a member
chain, `yield`, `catch ... when`, interpolated strings and all literal forms.
The standard library is read from `library-stubs/` before the user's code, and
only it may use `builtin`. Every type named in a declaration resolves, every
builtin member of the library is bound to what the compiler implements for it,
and declarations are checked for modifiers, what may hold what, bodies, the
shape and types of operators, inheritance, names, parameters, signatures,
overriding and hiding, interface implementation, generic constraints, field
types, static classes used as types, and accessibility.

### The blocking gap

1. **Function bodies are only partly resolved.** Step A of 5.3 binds names,
   member access, literals, conversions, locals, assignment and `return`;
   operators, calls, object creation, lambdas and most statements are not
   bound yet, and nothing inside them is checked. Its decisions were made with
   the owner on 2026-10-01 and are in `language.md`, "Function bodies".

`TwoIntDecimalTester` is still a stub: nothing tests `TwoIntDecimal` yet.

### Smaller known gaps
- `TwoIntDecimal`'s `%` returns wrong remainders for most operands, and `Pow`
  inherits it for the sign of huge integer powers of negative bases. `Pow` with a
  fractional exponent also loses two or three digits to the logarithm it goes
  through. All three are noted in `language.md`.
- No pattern matching beyond a bare `is SomeType`, by design.
- `raw` and `constalloc` remain reserved with no meaning.
- Parser gaps, each detailed in [`parser-gaps.md`](parser-gaps.md): an event's
  `add` and `remove` accessors do not parse, nor does a lambda parameter's
  default value, nor named and `ref` arguments passed on by `: base(...)`,
  `: this(...)` or a record's base list, nor an unbound generic type such as
  `typeof(List<>)`.
- Roslyn's warnings for a namespace and a type of one name on the two sides of
  the standard library's boundary (CS0435 to CS0437) are not given, though
  the name finds what Roslyn finds: the user's own namespace or type.
- Declarations are checked against C#'s rules apart from `notnull`
  constraints, which wait for nullability checking, constructor chains which
  loop, which need overload resolution, and what a record's copy constructor
  runs first and its access.
- Where two base lists each need what the other's type inherits, the one
  resolved first sees the other with no bases yet, as C# does, but the other
  then sees the first complete, so fewer errors are given, and which ones
  depends on the order of declarations. Roslyn fails both sides. The code is
  rejected either way.
- A chain of thousands of classes each deriving from the next, declared from
  the far end, takes long (3000 take about 36 seconds); this was so before
  nested types were looked up through bases.
- The parser accepts keywords as the names of locals, such as `int static = 1;`,
  which C# rejects.
- A declaration as the single statement of an `if` or a loop without braces,
  as in `if (x) int a = 1;`, is not reported (C#'s CS1023): the parse tree
  does not keep whether a body was braced.
- A `default:` sharing a switch section with `case` labels, as in
  `case 1: default:`, does not parse, and a `default` section's place among
  the others is not kept.
- Differences from Roslyn in bodies, all in code rejected either way, found by
  the recheck of step A on 2026-10-01: a loop of more than 100 constants is
  reported as too deep (RS 67) rather than as a loop (CS0110); a constant
  naming one whose value failed gets nothing more, even where C# reports
  CS0134 for boxing it; type arguments on a generic parameter written as a type
  (`T<int> x;`) give RS 2 where C# gives CS0307; a second, independent mistake
  in one statement is sometimes not reported (CS0176 after CS0120, CS0201 after
  the value's own error, CS0819 with CS0822 and CS0818 with CS0819); and two
  equal messages on one line show once, since messages have no column.
- About 20,000 assignments chained in one statement, or 5,000 nested brackets,
  overflow the stack, in the binder and the parser respectively, where Roslyn
  reports CS8078.

## Suggested order of work

Roughly dependency-ordered; the owner decides priorities.

1. ~~Get the build green.~~ Done.
2. ~~Finish the parser.~~ Done.
3. ~~Wire up `KeigValCompilerTest` so the five fixtures run automatically.~~
   Done on 2026-10-01. Add each new fixture directory to its `Program`.
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
   2. Check declarations against C#'s rules, in this order, agreed with the
      owner on 2026-09-29:
      1. ~~What needs nothing new: modifiers, what may hold what, bodies,
         operator shape, inheritance, names, and `using`.~~ Done: see stage 2.
      2. ~~A semantic type model, shared with function bodies.~~ Done, in
         `Semantician/Types/`, and used by the checks of stage 2:
         `SemanticType` is a `DeclaredType` (a declaration with its type
         arguments, and for a nested type the type holding it, since
         `Outer<int>.Inner` and `Outer<string>.Inner` differ) or a
         `GenericParameterType`, compared by value. `T[]` is `Array<T>` and a
         value type's `T?` is `Nullable<T>`; a reference type's `?` is kept as
         an annotation for nullability checking but plays no part in equality.
         A `TypeSubstitution` replaces generic parameters, by a type's
         arguments or, to compare two functions, by position.
         `SemanticTypeReader` (on the resolution context) builds one on demand
         from a resolved `TypeTargetIdentifier`, a declaration's instance type,
         and a type's written base types. `SignatureType` stays as it is for
         the library bindings.
      3. ~~What needs types compared: duplicate signatures, operator parameter
         types and pairs, overriding, hiding and `new`, interface
         implementation, generic constraints and their type arguments, an
         interface listed twice, `const` field types, and structures holding
         themselves.~~ Done: see stage 2. As in C#, an override's or explicit
         implementation's generic parameters take their constraints from what
         they override or implement, which cannot be restated.
         `SemanticTypeReader` reads a `?` on one of them as `Nullable<T>`,
         unless it is written with a `class` constraint, as C# does.
         `GenericConstraintReader` gives the inherited constraints to the
         checks, and `TypeHierarchy` answers with them;
         `GenericParameterType.IsValueType` does not, so function bodies
         should ask `TypeHierarchy`.
      4. ~~Accessibility: lookup skipping what cannot be seen, accessors' own
         access, a member exposing a type less accessible than itself, and
         required members.~~ Done: see stage 2.
      5. ~~The parser gaps in [`parser-gaps.md`](parser-gaps.md).~~ Done on
         2026-09-30, qualified type names included, but for those recorded
         there for later: an event's accessors, lambda defaults and named
         chain arguments.
      6. ~~Nested types of base classes in lookup.~~ Done on 2026-09-30: see
         `SignatureResolver` and `TypeSearcher` under stage 2.
   3. Resolve function bodies into a separate bound tree, as decided with the
      owner on 2026-10-01 (`language.md`, "Function bodies"), in steps:
      A. the bound tree, scopes, names, literals, locals, simple assignment,
         `return`, and the whole conversion classifier (done on 2026-10-01);
         B. casts, operators and constants, with the enum
         values, parameter defaults and literal ranges described under stage 2;
      C. member lookup, overload resolution, type inference, object creation,
         lambdas and constructor chains, which unblocks the chain checks left
         unchecked above; D. statements and C#'s flow analysis, with C#'s
         warning for a record's positional parameter which nothing reads;
      E. nullable warnings, as their own step. The operators the library
      declares on its built-in types stand in for C#'s predefined ones.
6. Design and prototype the datapack backend.

Step 6 is worth starting **earlier than its position suggests**, even crudely.
It is the entire unexplored risk of the project, and its constraints (see
[`language.md`](language.md)) should feed back into language design before more
front-end work hardens around assumptions the target cannot support.
