# KGVL — the language, and its target

## Shape of the language

KGVL is close enough to C# that a C# developer should read it without a manual.
The authoritative list of what the language currently recognises is
[`KeigValCompiler/KGVL.cs`](../KeigValCompiler/KGVL.cs); `tests/test.kgvl` is
the worked example.

Familiar from C#: namespaces and `using`; `class`, `struct`, `interface`,
`record`, `enum`, `delegate`, `event`; generics with `where` constraints;
properties and indexers; access modifiers and `static` / `abstract` / `virtual`
/ `override` / `sealed` / `readonly` / `required` / `const` / `new`; `if` / `for` / `foreach` /
`while` / `do` / `switch` / `try` / `catch` / `finally` / `throw` / `return`;
throw expressions such as `x ?? throw new E()`, where C# allows them; `is` and
`as` with a type; `ref` / `out` / `in` / `params`, and default parameter values;
string interpolation; `nameof` / `typeof`.

Divergences from C# worth knowing:

- **`byte` is signed, `ubyte` is unsigned.** KGVL's integer keywords are
  `byte`/`ubyte`, `short`/`ushort`, `int`/`uint`, `long`/`ulong`. Do not assume
  C#'s `sbyte`/`byte` pairing.
- **No binary floating point.** There is no `float` or `double`. `decimal` is
  the only fractional type, and it is a *base-10* floating-point number — see
  below.
- **Extra modifiers** exist that C# lacks: `builtin` and `inline` (both parsed
  today, as `PackMemberModifiers.BuiltIn` and `.Inline`). `builtin` is only
  for the standard library's own files — see
  [Built-in types and the standard library](#built-in-types-and-the-standard-library).
  `inline` forces a function's code to be inlined wherever it is called. It can
  help optimisation, especially where the compiler can simplify the inlined code
  for the arguments at a particular call site, at the cost of datapack size and
  duplicated commands. It is accepted on every kind of function: methods,
  constructors, operators and accessors (decided 2026-09-29). Nothing acts on it
  yet, since the backend does not exist.
- **Access modifiers** are C#'s, `internal` included (added 2026-09-29), with
  the same defaults. The two "assemblies" `internal` separates are the standard
  library and the code being compiled, so the library can keep helpers from
  user code; later, user-made libraries or runtime datapacks could be more.
  What a namespace holds, types and KGVL's own fields, properties, functions and
  events alike, can only be `public` or `internal`, as C#'s top-level types can.
  `protected internal` and `private protected` mean what they do in C#.
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
  Whether such a member may, must, or must not be written `static` is not
  decided; see the open questions. The compiler accepts it either way for now.
- `using A;` imports what namespace `A` holds, as in C# (decided 2026-09-29):
  its types and every member it holds, fields, properties, functions and events,
  since a KGVL namespace holds those too. It does **not** import the namespaces
  nested in `A`, so after `using A;`, `B.C` does not find `A.B.C`: reaching it
  takes `using A.B;` and then `C`, or the full name `A.B.C`. There is no
  `global::` (decided 2026-09-29).
- A type can be named in full, with the namespaces and types holding it, as in
  `KGVL.Collections.Generic.IEnumerable<int>`, anywhere a type is written, and
  a type before a `.` can have type arguments of its own, as in C#'s
  `Outer<int>.Inner` (decided 2026-09-30). As in C#, the first name is looked
  up from the innermost namespace outwards, and at each a namespace of that
  name is found before a type. Where both are declared there, the user's code
  finds its own, as Roslyn prefers what a compilation declares to what it
  imports: a namespace of its own hides a library type, and a type of its own
  a library namespace. The standard library, compiled as though on its own,
  never sees the user's namespaces. As in C#, a keyword such as `this`, `int`
  or `string` never stands before a `.` in a type's name.

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

public int this[int index] { get => _items[index]; set => _items[index] = value; }

public static Vec operator +(Vec a, Vec b) { }
public static implicit operator int(Vec v) => v._value;
```

An indexer, like a property, may be written with only a `=>` getter, and may have
`init` in place of `set`.

The overloadable operators are C#'s, fixed by `OverloadableOperator`: unary
`+ - ! ~ ++ --`, binary `+ - * / % & | ^ << >> >>>`, `== != > < >= <=`, and
implicit/explicit conversions. Whether a `+` or `-` is unary or binary is decided
by how many parameters it declares. C#'s `operator true` and `operator false` are
left out: they exist only so that `&&` and `||` can work on custom types, and
would bring many rules for little use. The rules for declaring them are also C#'s,
to be checked by the resolver: an operator is `public static`, takes a parameter
of the type declaring it, and `==`/`!=`, `<`/`>` and `<=`/`>=` are declared in
pairs. User code can overload all of them. Interfaces may declare operators too,
which with `static abstract` is how the standard library's generic maths
interfaces are written.

A constructor is recognised as a member with no return type whose name matches
the type holding it, so it needs no keyword of its own. `PackConstructor`
derives from `PackFunction` and carries the `this`/`base` chain.

**Explicit interface implementation** is C#'s: a member names the interface whose
member it implements before its own name, and is then reached only through that
interface. It is what lets `Int32` have both `const int MaxValue` and the static
`MaxValue` property that `IMinMaxValue<int>` asks for.

```
static int IMinMaxValue<int>.MaxValue => MaxValue;
IEnumerator<int> IEnumerable<int>.GetEnumerator() { ... }
int IList<int>.this[int index] { get => ...; }
static T IAdd<T>.operator +(T left, T right) => ...;
static explicit IConvert<T>.operator int(T value) => ...;
```

Functions, operators, properties and indexers can do this; the parser reports a
field which tries. The parser stores the interface on the member
(`IExplicitInterfaceMember.ExplicitInterface`) and checks nothing else: C#'s
rules, such as no access modifier on such a member, are the resolver's.

**Records** get a property from each positional parameter, as in C# (decided
2026-09-29): `record Point(int X, int Y)` has public `X` and `Y` properties with
`get` and `init`, each starting with its parameter's value. A positional record
passes its base record's constructor arguments in its base list, as in
`record Point3(int X, int Y, int Z) : Point(X, Y)`, which only the first entry
can take and only a record with a parameter list can give; its primary
constructor keeps them as a `base` chain. A record declaring a
property or field of that name keeps the value there instead, and so does one
inheriting a member of that name declaring no generic parameters, unless it is an
abstract property, which the made property overrides. What keeps it has to be an
instance field, or an instance property declaring a `get` of any access, of the
parameter's type, as C# checks it, and an inherited one cannot be hidden by a
member of the record's own. Positional parameters cannot be `ref` or `out`.
`RecordPropertyResolver` makes the properties. Nothing else of C#'s records
exists yet: no `Equals`, `ToString`, `Deconstruct` or `with`.

**Hiding** is C#'s, `new` modifier included (added 2026-09-29): a member with the
name of an inherited one hides it, and is to be written `new` to say that is
meant. As in C#, hiding without `new` is a warning, and so is `new` hiding
nothing, while a class's member hiding an abstract one is an error. All three
are the resolver's (`OverrideChecker`).

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
and bracketing either side takes it back. `.` costs nothing: followed by a name
it cannot start a value, and followed by a digit it is a number such as `.5`,
which leaves `a < b > .5` a pair of comparisons, as in C#.

Static members are worth the syntax because a static member of a generic type
belongs to **one instantiation**: `Cache<int>.Value` and `Cache<string>.Value`
are different storage. The backend has to monomorphise generics anyway, since
the target has no runtime type system, so this costs it nothing extra.

## Casts and the `(` ambiguity

`(x)-y` could be a cast of `-y` or a subtraction, and KGVL decides as C# does.
What stands in the brackets can only be a type if it is a type keyword such as
`int`, or ends in the `>` of type arguments, the `]` of an array or the `?` of a
nullable marker; then it is a cast whatever value follows. A bare name could as
well be a value, so it is a cast only if what follows cannot continue an
expression: a name, a literal, `(`, `!` or `~`, but not the words `is`, `as` and
`switch`, and not `!=`.

```
(int)-x            cast -- int is only ever a type
(int?)-x           cast -- so is anything ending in '?', ']' or '>'
(x)-y              subtraction -- x could be a value
(x)(-y)            cast, which is how to force it for a bare name
(x)y               cast -- a name cannot continue an expression
(x) is T           a test of x, not a cast of a value named "is"
```

## Numeric literals

Number literals follow C#. A decimal is `1.5`, `.5`, `1.5e-3` or `5m`; an integer
is decimal, hexadecimal (`0xFF`) or binary (`0b1010`), optionally suffixed `u`,
`l` or both. Digit separators stand between digits (`1_000_000`, `1_000.5`) and
may also follow a prefix (`0x_FF`). A point not followed by a digit is member
access, so `3.ToString()` and `3.4.ToString()` call a method on the number.

An integer literal's type is the first of `int`, `uint`, `long` and `ulong` that
holds its value, narrowed to the unsigned ones by `u` and to the long ones by
`l`. Hexadecimal and binary are no exception: `0xFFFFFFFF` is a `uint`.

**Out-of-range literals are compile errors.** A decimal is checked as it is
parsed, since its range does not depend on where it is used: one above
`MaxValue`, or a nonzero one closer to zero than `Epsilon`, is an error. So is an
integer too large even for `ulong`. Whether an integer fits the type it is
assigned to, as in `byte b = 300`, depends on that type, so that check belongs to
the validation stage and does not exist yet.

**Escape sequences** in char and string literals are C#'s: `\0 \a \b \e \f \n \r
\t \v \' \" \\`, `\x` with one to four hexadecimal digits, `\u` with exactly
four, and `\U` with exactly eight, naming a code point up to U+10FFFF. As in C#,
`\x` takes as many hex digits as follow, up to four, so `"\x41BC"` is one char,
and a `\U` code point above U+FFFF is a surrogate pair, two chars, in a string,
and an error in a char literal, which holds one.

## Built-in types and the standard library

Decided on 2026-09-28. The resolver loads the library, binds its builtin
members to what the compiler implements, and checks declarations, but what
depends on function bodies, such as conversions, operators and boxing, is not
enforced yet; see [`architecture.md`](architecture.md).

**Where it lives.** The built-in types and the standard library are ordinary
`.kgvl` files under [`library-stubs/`](../library-stubs/), laid out like .NET's:
a directory per namespace and a file per type, named after it. The compiler
parses them before the user's code, and only they may use `builtin`. User code
may still declare its own types in the `KGVL` namespace, under the normal rules.

**`builtin`.** On a member it means the compiler supplies the body, so the
member is written with `;` in place of one. On a type it means the compiler
decides how its values are stored, so the type declares no instance fields.
Everything else in a library file is ordinary KGVL, and should be: `Clamp`
written on top of builtin `<` and `>` is readable by users, so only operations
which need hand-written commands are builtin. Types such as `decimal` are
builtin so that the compiler can recognise and optimise them.

**Keyword aliases.**

| Keyword | Type | Keyword | Type |
|---|---|---|---|
| `byte` | `KGVL.Int8` | `ubyte` | `KGVL.UInt8` |
| `short` | `KGVL.Int16` | `ushort` | `KGVL.UInt16` |
| `int` | `KGVL.Int32` | `uint` | `KGVL.UInt32` |
| `long` | `KGVL.Int64` | `ulong` | `KGVL.UInt64` |
| `decimal` | `KGVL.Decimal` | `char` | `KGVL.Char` |
| `bool` | `KGVL.Boolean` | `string` | `KGVL.String` |
| `object` | `KGVL.Object` | | |

`T?` on a value type is `KGVL.Nullable<T>`; on a reference type it is only an
annotation, as in C#. `T[]` is `KGVL.Array<T>`, a generic class unlike C#'s
`System.Array`, since without a runtime type system a generic one is simpler.

The annotation is kept, not thrown away (decided 2026-09-29): the owner wants the
compiler to be able to check nullability, which helps debugging. It plays no part
in whether two types are the same, as in C#, so `Foo(string)` and `Foo(string?)`
are the same signature and cannot both be declared, and an override or an
interface implementation may differ from what it overrides in annotations only.
How strict nullability checking in function bodies should be is to be decided
with them.

**`object` and boxing.** Every type derives from `KGVL.Object`, which declares
`ToString`, `Equals` and `GetHashCode`. Structs and enums are boxed when
converted to `object` or to an interface type, as in C#. Generic code does not
box: generics are monomorphised, so a call through a constraint on `T` goes
straight to the member of the type argument. How a box, or any value, is stored
is chosen by the compiler case by case. Structs are generally scoreboards or
storage NBT; classes may also use entities, items and other game state.

**Integers behave like C#'s**, unless that would cost hundreds of commands per
operation:

- `+ - *`, `++ --`, unary `-` and narrowing conversions wrap to the type's width.
- `/` and `%` truncate towards zero. Scoreboards floor, so negative operands
  need a correction.
- Dividing by zero throws `DivideByZeroException`.
- A shift count is masked to the type's width, so `x << 33` is `x << 1` for an
  `int`.

Scoreboards have no bitwise operations, so `&`, `|`, `^` and bit counting are
the likeliest to reach that cost, along with 64-bit `/` and `%`. Their meaning
stays C#'s; making them cheap, for instance special-casing `x & 0xFF`, is the
backend's problem.

**`char`** is a 16-bit UTF-16 code unit, as in C# and in Java, whose strings
Minecraft's are. In a datapack a `char` is obtained by indexing a string.

**Conversions between primitives** are declared in the library as `implicit`
and `explicit` operators on the source type, not built into the language. One is
implicit only when it cannot lose information. `decimal` holds 9 digits, so
`int`, `uint`, `long` and `ulong` convert to it explicitly:

```
from \ to  Int8 UInt8 Int16 UInt16 Int32 UInt32 Int64 UInt64 Char Decimal
Int8        -    E     I     E      I     E      I     E      E    I
UInt8       E    -     I     I      I     I      I     I      E    I
Int16       E    E     -     E      I     E      I     E      E    I
UInt16      E    E     E     -      I     I      I     I      E    I
Int32       E    E     E     E      -     E      I     E      E    E
UInt32      E    E     E     E      E     -      I     I      E    E
Int64       E    E     E     E      E     E      -     E      E    E
UInt64      E    E     E     E      E     E      E     -      E    E
Char        E    E     E     I      I     I      I     I      -    I
Decimal     E    E     E     E      E     E      E     E      E    -
```

Boxing and unboxing are the exception: they are rules of the language, as in C#,
and are declared nowhere.

**`const`** follows C#: on fields and locals, with a value fixed at compile time
of a primitive, string or enum type, and implicitly static. The value is
required, and a `const` without one is a parse error.

**Static abstract and static virtual interface members** follow C#, including
operators declared in interfaces, so the library can offer C#'s generic maths
interfaces (`INumber<TSelf>` and the rest). Because generics are monomorphised,
a call through one becomes a direct call for each instantiation, with no
dispatch at runtime.

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

`TryParse` is also the **one parser for decimal literals** in KGVL source. The
source parser only finds where a literal ends and hands its text over, so the
grammar lives in one place, and a malformed literal is reported with the reason
`TryParse` gives. The one thing a literal may contain that a string parsed at
runtime may not, digit separators, is opted into through `DecimalParseOptions`.
Text naming a number out of range does not parse, and `Parse` throws
`OverflowException` for it; arithmetic is unaffected and still saturates to an
infinity or zero.

The reason these two files are as long as they are is the comment on
`operator /` — *"Implemented as is in DataPacks"*. These algorithms are written
the way the datapack backend will have to emit them as commands, so **every
method a compiled program could reach at runtime uses only int `+ - * / %`, and
no intermediate ever leaves the 32-bit range**. That is what forces the
three-digit limb multiplication, the paired remainder in long division, and the
hardcoded CORDIC tables. Division and remainder are applied to non-negative
operands only, because C# truncates towards zero where a scoreboard floors and
the two agree only there.

`TryParse`, `Parse`, `ToString` and the `double`/`float`/`long` conversions are
the exception, and are marked as such: they turn source literals into constants and
back, which happens in the compiler and never in a datapack.

`TwoIntDecimal` holds the number and the operations that need no iteration —
the four operators, `%`, comparison, and `Floor`/`Ceil`/`Round`/`Truncate`/`Abs`/
`Min`/`Max`. `TwoIntDecimalMath` holds everything that approximates: roots, `Pow`,
`Exp`, the logarithms, the trigonometric and hyperbolic functions. Every
approximating function takes an **iteration count**, so the standard library can
expose the accuracy-for-commands trade to KGVL code, alongside an overload using
a default that reaches the nine digits the format holds.

Measured against `Math` over hundreds of thousands of exactly representable
inputs, everything lands within **1 to 5 units in the last place**, except:

- Trigonometry reduces its argument against a 9-digit `π/2`, losing about one
  digit per power of ten in `|x|`; past `1e9` it returns NaN rather than a number
  with no correct digits in it. A longer `π` split across several ints would
  recover the lost digits, at the cost of multi-word arithmetic in the reduction.
- `Pow` with a fractional exponent goes through a 9-digit logarithm, so a large
  result eats the low digits of the fraction — about `1e-7` relative for results
  near `1e30`.
- **`%` is wrong, not merely imprecise.** It computes `a - b * Truncate(a / b)`
  with every step rounded to 9 digits, so the remainder loses its low digits
  whenever `b` times the quotient needs more than 9, and a quotient that rounds
  up across a whole number breaks it outright: `8.99999999 % 3` gives
  `-0.00000001`. An exact remainder, one digit of long division per unit of
  exponent difference, is the fix.
- `Pow` decides the sign of a negative base with `% 2`, so it inherits that bug:
  integer powers of `1e9` and above can come out with the wrong sign.

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
  scoreboards? This decision cascades into almost everything else. Only the
  direction is decided: the compiler chooses per case, as described under
  [Built-in types and the standard library](#built-in-types-and-the-standard-library).
  The concrete schemes are open.
- Is garbage collected memory in scope, or is allocation arena/static only?
- What does `T?` mean for a type parameter which could be a value type or a
  reference type? In C# it is `T` itself for a value type and an annotation for a
  reference type. The library avoids it for now: `IEquatable<T>.Equals` takes
  `T`, not C#'s `T?`.
- Do `virtual` / interfaces survive to the backend, or does the compiler require
  whole-program devirtualisation?
- The standard library's shape is decided (see above), and `builtin` members are
  implemented in the compiler. Still open: whether `raw` is also a way to write
  some of them as commands inside the library itself.
- What is the interop story for reading and writing actual game state —
  entities, blocks, inventories?
- May the fields, properties, functions and events a namespace holds be written
  `static`, must they be, or must they not be? They belong to no object either
  way. C# has nothing to compare: its nearest relative, a `const`, is static
  already and may not say so. The compiler accepts both for now.

If a task requires an answer to one of these, **ask rather than picking one.**
An assumption baked into the front-end is expensive to remove later.
