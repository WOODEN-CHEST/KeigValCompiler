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
- **No binary floating point.** There is no `float` or `double`. `decimal` is
  the only fractional type, and it is a *base-10* floating-point number — see
  below.
- **Extra modifiers** exist that C# lacks: `builtin` and `inline` (both parsed
  today, as `PackMemberModifiers.BuiltIn` and `.Inline`).
- **`raw` and `constalloc`** are reserved in `KGVL.cs` but referenced nowhere
  else — no parsing, no modifier flag, no semantics. Their intended meaning is
  not recorded anywhere; ask before assuming.
- **Arrays are jagged and nullable at every level.** A type of array depth N has
  N + 1 independently nullable positions, so `int?[]?[]?` is a nullable array of
  nullable arrays of nullable ints. The reading rule is that each `[]` wraps
  everything to its left in one more array, and a `?` always annotates whatever
  stands immediately to its left — so the leftmost `[]` is the *innermost*
  array. This differs from C#, where the leftmost rank specifier is the
  outermost; the KGVL rule was chosen because it makes every `?` position
  unambiguous. `TypeTargetIdentifier.NullabilityByLevel` stores these innermost
  first. True multidimensional arrays (`int[,]`) are not parsed.
- Namespaces are file-scoped and *reassignable mid-file* — a `.kgvl` file may
  contain several `namespace X;` statements, each switching the active namespace
  for what follows.

The built-in type names, as synthesised by `DefaultInternalContentProvider`,
live in the `KGVL` namespace: `Int8`, `UInt8`, `Int16`, `UInt16`, `Int32`,
`UInt32`, `Int64`, `UInt64`, `TwoIntDecimal`, `Boolean`, `String`, `Null`. The
keywords above are shorthands registered against them in `BuiltInTypeRegistry`.

## `TwoIntDecimal`

Minecraft scoreboards hold **32-bit signed integers and nothing else** — there
is no hardware float. `decimal` is therefore a **software floating-point number
in base 10**, built from two ints, in
[`TwoIntDecimal.cs`](../KeigValCompiler/Semantician/TwoIntDecimal.cs).

It is a float, not fixed-point: the exponent is stored, not implied.

| Field | Meaning |
|---|---|
| `Mantissa` (`int`) | 9 significant **decimal** digits, normalised to a magnitude in `[100_000_000, 999_999_999]`. Carries the sign. |
| `Exponent` (`int`) | Base-10 exponent. |

```
value = Mantissa × 10^(Exponent − 8)
```

Because the radix is 10 rather than 2, values like `0.1` are exact, which binary
IEEE-754 cannot do. It is the same family as C#'s `decimal` (also base-10
floating point) but much narrower — 9 digits against C#'s 28–29 — and unlike
C#'s `decimal` it carries the full set of IEEE-style special values:

- `NaN` — encoded as `|Mantissa| > 999_999_999`
- `PositiveInfinity` / `NegativeInfinity` — encoded as `Exponent == int.MaxValue`
- `Epsilon`, `MaxValue`, `MinValue`, plus `Pi`, `E` and `Tau`
- division by zero yields ±Infinity rather than throwing

Scientific notation (`1.5e10`) parses and round-trips through `ToString`.

The 668 lines are not over-engineering. The comment on `operator /` —
*"Implemented as is in DataPacks (which is why it is so complex)"* — is the key
to the whole file: these algorithms are written the way the datapack backend
will have to emit them as commands. `TwoIntDecimal` is a **specification of the
runtime's arithmetic**, executable in C# so it can be tested, not merely a
convenience type for the compiler's own use. Changing its behaviour changes the
language's arithmetic semantics.

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
