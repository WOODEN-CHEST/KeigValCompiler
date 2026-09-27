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
- **`params`** marks a variadic final parameter, alongside `ref`, `out` and `in`.
- **`raw` and `constalloc`** keywords.
`raw` currently does nothing, but the intended idea is to allow running "raw"
minecraft commands in the code. As of now, this isn't implemented.
`constalloc` is an optimization modifier that makes something (not yet decided if
function or field, or class, or what specifically) be compiled into code where
collection members and loop iterations are hardcoded into the mcfunction file itself.
This drastically increases the size of the output function, but can also massively optimize
the code if the "normal" code is expensive.
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
- Namespaces may contain members such as fields, properties and functions.
When imported or active, these members can be used in expressions. For example, if
a namespace that's been imported has a field `XYZ` and a function `Foo`, then a function in any member in
this file can do 
```
{
    int a = XYZ;
    XYZ++;
    if (XYZ > 5) { ... }
    Foo();
    // etc.
}
```

## Member syntax

These five constructs were undefined until 2026-09-20 and are now **identical to
C#**, with the keywords `new`, `get`, `set`, `init`, `operator`, `implicit`,
`explicit` and `sealed` added to `KGVL.cs` to support them:

```
new Thing(1)                      new Thing { Field = 1 }
new int[5]        new int[2][]    new int[] { 1, 2 }      new[] { 1, 2 }

public Thing(int a) { }           public Thing(int a, int b) : this(a) { }

public int Count { get; set; }    public int Total { get => _total; }
public int Once { get; init; }    public int Quick => _total * 2;
public int Narrow { get; private set; }

public int this[int index] { get; set; }

public static Vec operator +(Vec a, Vec b) { }
public static implicit operator int(Vec v) => v._value;
```

The overloadable operators are fixed by `OverloadableOperator`: `+ - * / %`,
unary negation, `++ --`, `== != > < >= <=`, and implicit/explicit conversions.

A constructor is recognised as a member with no return type whose name matches
the type holding it, so it needs no keyword of its own. `PackConstructor`
derives from `PackFunction` and carries the `this`/`base` chain.

## Generic calls and the `<` ambiguity

`Foo<int>(x)` and `a < b > (c)` are the same run of characters, so `<` alone
decides nothing. KGVL uses C#'s rule: after a name, the type arguments are read
speculatively and kept **only when a `(` follows the closing `>`**. Everything
else rewinds and stays a pair of comparisons.

Two tokens may follow: `(` for a generic call, and `.` for a static member of a
closed generic type.

```
Foo<int>(x)        call, one type argument
obj.Method<int>(x) call on a member
Cache<int>.Value   static member of a closed generic type
Result<int>.Ok(5)  static factory on a closed generic type
a < b > c          two comparisons -- nothing usable follows '>'
a < 5 > (c)        two comparisons -- 5 cannot start a type name
a < b > (c)        CLAIMED AS A CALL, the one wrong case
(a < b) > (c)      comparisons again, which is how to force it
```

Only `(` is genuinely ambiguous: a real comparison can be written `a < b > (c)`,
and bracketing either side takes it back. `.` costs nothing, because no valid
comparison has `>` followed by `.` — `.5` is not a number literal here and
`.name` cannot start a value.

Static members are worth the syntax because a static member of a generic type
belongs to **one instantiation**: `Cache<int>.Value` and `Cache<string>.Value`
are different storage. The backend has to monomorphise generics anyway, since
the target has no runtime type system, so this costs it nothing extra.

## `TwoIntDecimal`

Minecraft scoreboards hold **32-bit signed integers and nothing else** — there
is no hardware float. `decimal` is therefore a **software floating-point number
in base 10**, built from two ints, in
[`TwoIntDecimal.cs`](../KeigValCompiler/Semantician/TwoIntDecimal.cs), with the
mathematical functions over it in
[`TwoIntDecimalMath.cs`](../KeigValCompiler/Semantician/TwoIntDecimalMath.cs).

It is a float, not fixed-point: the exponent is stored, not implied.

| Field | Meaning |
|---|---|
| `Mantissa` (`int`) | 9 significant **decimal** digits, normalised to a magnitude in `[100_000_000, 999_999_999]`. Carries the sign. |
| `Exponent` (`int`) | Base-10 exponent, restricted to `[-999_999_999, 999_999_999]`. |

```
value = Mantissa × 10^(Exponent − 8)
```

Because the radix is 10 rather than 2, values like `0.1` are exact, which binary
IEEE-754 cannot do. It is the same family as C#'s `decimal` (also base-10
floating point) but much narrower — 9 digits against C#'s 28–29 — and unlike
C#'s `decimal` it carries the full set of IEEE-style special values:

- `NaN` — encoded as `|Mantissa| > 999_999_999`
- `PositiveInfinity` / `NegativeInfinity` — encoded as `Exponent == int.MaxValue`
- `Epsilon`, `MaxValue`, `MinValue`, plus `Zero`, `One`, `Pi`, `E` and `Tau`
- division by zero yields ±Infinity rather than throwing; `0 / 0`, `∞ − ∞` and
  `∞ × 0` yield NaN

Nothing in the type throws. Overflow saturates to an infinity, underflow flushes
to zero, and every undefined operation is a NaN. Two deliberate departures from
IEEE-754: rounding is **half away from zero** rather than half to even, because
it costs one comparison in commands instead of three, and there is **no negative
zero**, because distinguishing it would need a field the two ints do not have.

Scientific notation (`1.5e10`) parses, and `ToString` round-trips through
`TryParse` for every representable value.

The reason these two files are as long as they are is the comment on
`operator /` — *"Implemented as is in DataPacks"*. These algorithms are written
the way the datapack backend will have to emit them as commands, so **every
method a compiled program could reach at runtime uses only int `+ - * / %`, and
no intermediate ever leaves the 32-bit range**. That is what forces the
three-digit limb multiplication, the paired remainder in long division, and the
hardcoded CORDIC tables. Division and remainder are applied to non-negative
operands only, because C# truncates towards zero where a scoreboard floors and
the two agree only there.

`TryParse`, `ToString` and the `double`/`float`/`long` conversions are the
exception, and are marked as such: they turn source literals into constants and
back, which happens in the compiler and never in a datapack.

`TwoIntDecimal` holds the number and the operations that are exact — the four
operators, `%`, comparison, and `Floor`/`Ceil`/`Round`/`Truncate`/`Abs`/`Min`/
`Max`. `TwoIntDecimalMath` holds everything that approximates: roots, `Pow`,
`Exp`, the logarithms, the trigonometric and hyperbolic functions. Every
approximating function takes an **iteration count**, so the standard library can
expose the accuracy-for-commands trade to KGVL code, alongside an overload using
a default that reaches the nine digits the format holds.

Measured against `Math` over hundreds of thousands of exactly representable
inputs, everything lands within **1 to 5 units in the last place**. Three limits
come from the format rather than the iteration count, and no amount of extra
work moves them:

- Trigonometry reduces against a 9-digit `π/2`, losing about one digit per power
  of ten in `|x|`; past `1e9` nothing is left and it returns NaN rather than a
  number with no correct digits in it.
- `Pow` with a fractional exponent goes through a 9-digit logarithm, so a large
  exponent eats the low digits of the fraction — about `1e-7` relative at
  exponent magnitude 30.
- `%` is exact only while the quotient is, which means while `|a / b| < 1e9`.

`TwoIntDecimal` is a **specification of the runtime's arithmetic**, executable in
C# so it can be tested, not merely a convenience type for the compiler's own use.
Changing its behaviour changes the language's arithmetic semantics.

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
