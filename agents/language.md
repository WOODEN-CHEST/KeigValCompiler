# KGVL — the language, and its target

## Shape of the language

KGVL is close enough to C# that a C# developer should read it without a manual.
The authoritative list of what the language currently recognises is
[`KeigValCompiler/KGVL.cs`](../KeigValCompiler/KGVL.cs); `tests/test.kgvl` is
the worked example.

Familiar from C#: namespaces and `using`; `class`, `struct`, `interface`,
`record`, `enum`, `delegate`, `event`; generics with `where` constraints;
properties and indexers; access modifiers and `static` / `abstract` / `virtual`
/ `override` / `sealed` / `readonly` / `required`; `if` / `for` / `foreach` /
`while` / `do` / `switch` / `try` / `catch` / `finally` / `throw` / `return`;
`ref` / `out` / `in` / `params`; string interpolation; `nameof` / `typeof`.

Divergences from C# worth knowing:

- **`byte` is signed, `ubyte` is unsigned.** KGVL's integer keywords are
  `byte`/`ubyte`, `short`/`ushort`, `int`/`uint`, `long`/`ulong`. Do not assume
  C#'s `sbyte`/`byte` pairing.
- **No floating point.** `decimal` is the only fractional type, and it is
  fixed-point — see below.
- **Extra modifiers** exist that C# lacks: `builtin` and `inline` (both parsed
  today, as `PackMemberModifiers.BuiltIn` and `.Inline`).
- **`raw` and `constalloc`** are reserved in `KGVL.cs` but referenced nowhere
  else — no parsing, no modifier flag, no semantics. Their intended meaning is
  not recorded anywhere; ask before assuming.
- Namespaces are file-scoped and *reassignable mid-file* — a `.kgvl` file may
  contain several `namespace X;` statements, each switching the active namespace
  for what follows.

The built-in type names, as synthesised by `DefaultInternalContentProvider`,
live in the `KGVL` namespace: `Int8`, `UInt8`, `Int16`, `UInt16`, `Int32`,
`UInt32`, `Int64`, `UInt64`, `TwoIntDecimal`, `Boolean`, `String`, `Null`. The
keywords above are shorthands registered against them in `BuiltInTypeRegistry`.

## `TwoIntDecimal`

Minecraft scoreboards hold **32-bit signed integers and nothing else**. There is
no float. `decimal` is therefore implemented as fixed-point across two integers,
in [`TwoIntDecimal.cs`](../KeigValCompiler/Semantician/TwoIntDecimal.cs) — 668
lines, mirroring the arithmetic the datapack backend will eventually have to
emit as commands. That is why it looks over-engineered for a compiler-internal
number type: it is a specification of the runtime behaviour, not a convenience.

## The target, and why it constrains everything

The backend does not exist yet, so nothing here is settled. But these properties
of Minecraft datapacks will shape the language, and they are the reason this
project is hard:

- **Integers only.** All arithmetic lowers to scoreboard operations on 32-bit
  signed ints. 64-bit types and `decimal` must be synthesised from multiple
  scoreboard values.
- **No call stack.** `function` calls exist but there are no parameters, no
  return values, and no locals. A stack must be built by hand out of storage NBT
  and a stack-pointer scoreboard.
- **Hard recursion and iteration limits.** Function call depth and commands per
  tick are capped by server configuration. Unbounded recursion and long loops
  are not merely slow, they silently stop.
- **No heap.** Objects must be modelled as entries in storage NBT (or as marker
  entities) with hand-rolled allocation and identity.
- **No exceptions, no dynamic dispatch, no closures.** `try`/`catch`, `virtual`
  and lambdas all need to be lowered to explicit branching and dispatch tables.
- **Branching is `execute if`.** There is no jump. Control flow becomes a graph
  of `.mcfunction` files invoked conditionally.

These are known properties of the platform rather than measurements taken in
this project. Before the backend is designed, they should be **verified against
the current Minecraft Java Edition version** — command syntax, the storage NBT
API, and the relevant limits have all changed across versions, and the pack
format number the compiler emits will depend on the answer.

## Open design questions

The repository owner decides these. Agents should surface them, not settle them.

- Which Minecraft version and `pack_format` is the target?
- How are objects represented — storage NBT, marker entities, or parallel
  scoreboards? This decision cascades into almost everything else.
- Is garbage collected memory in scope, or is allocation arena/static only?
- Do `virtual` / interfaces survive to the backend, or does the compiler require
  whole-program devirtualisation?
- What does the standard library look like, and how do `builtin` members bridge
  to raw commands? (`raw` is a reserved keyword and looks intended for this.)
- What is the interop story for reading and writing actual game state —
  entities, blocks, inventories?

If a task requires an answer to one of these, **ask rather than picking one.**
An assumption baked into the front-end is expensive to remove later.
