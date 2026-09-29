# AGENTS.md

Guidance for AI agents working in this repository. Read this file first, then the
document in [`agents/`](agents/) that matches your task.

## What this project is

**KeigValCompiler** compiles **KGVL** (KeigVal Language) — a statically typed,
C#-like language — into **Minecraft Java Edition datapacks**.

The point of the project is to let you write ordinary object-oriented code
(classes, generics, properties, exceptions) and have it lowered onto Minecraft's
command system, which offers none of those things. The backend must synthesise
objects, a call stack, and arithmetic out of scoreboards, storage NBT and
`.mcfunction` files.

This is a personal, exploratory project. There is no deadline, no user base, and
no backwards-compatibility obligation. Language design decisions are still open
and are made by the repository owner, not by agents.

## Project status: early and incomplete

Be honest with yourself about how little exists. As of 2026-09-29:

- The **parser is complete** for the language's syntax, including function
  bodies, expressions and operator precedence, apart from the gaps listed in
  [`agents/parser-gaps.md`](agents/parser-gaps.md). It checks little about what
  it reads beyond what only it can see: repeated modifiers, accessors and
  `where` clauses, and `builtin` outside the standard library.
- The **resolver** resolves the types named in declarations, binds the
  standard library, and checks declarations against those of C#'s rules which
  need no types compared, but does not look inside function bodies at all:
  nothing in a body is looked up or type checked.
- The **datapack backend does not exist**. Not one line. The project does not
  currently emit any output at all.

Do not describe this project as working, and do not write documentation or
comments implying features exist when they do not. See
[`agents/architecture.md`](agents/architecture.md) for the detailed state.

## Build and run

```bash
dotnet build                       # from repository root
dotnet run --project KeigValCompiler -- <source dir> [--output <dir>]
dotnet run --project KeigValCompiler -- --help    # every argument the compiler accepts
```

Paths may be relative to the working directory.

Both projects target **net10.0**. There are **zero external dependencies** — no
NuGet packages in either `.csproj`. Keep it that way unless the owner agrees
otherwise; a compiler with no dependencies is a deliberate property of this
project, not an accident.

`tests/test.kgvl` is the parser fixture: a KGVL file which, once the parser is
complete, must parse with zero errors. It does so today, and must keep doing so —
treat a new parse error there as a regression. It names types it never declares,
so run it with `--parse-only`.

`tests-errors/recovery.kgvl` is the opposite fixture: every error in it is
deliberate, and it exists to check that the compiler reports all of them in one
run rather than stopping at the first. Its header comment lists what it should
produce. It sits outside `tests/` because a source directory is read
recursively. `tests-resolution/` and `tests-resolution-errors/` are the same
pair for the resolver: the first must compile with zero errors, and the second
must report what its header lists. `tests-declaration-errors/` is a second
error fixture for the resolver, for its checks of declarations, with each file's
header listing what that file reports. None of the fixtures are automated — you
run them and read the output.

The `KeigValCompilerTest` project exists but is **not wired up** — it has no
`ProjectReference` to the compiler and its `Main` prints `Hello, World!`. There
is effectively no automated test coverage.

## Rules

1. **Match the existing code style exactly.** It is unusual and deliberate —
   PascalCase local variables, never `var`, mandatory section comments. Read
   [`agents/code-style.md`](agents/code-style.md) *before* writing any C#. Code
   that does not match will be rejected.
2. **Build before you claim anything works.** Run `dotnet build` and report the
   real result. The build is frequently red mid-refactor; say so plainly.
3. **Do not invent language semantics.** If a KGVL feature is undefined, ask
   rather than deciding. See [`agents/language.md`](agents/language.md).
4. **Do not "fix" unrelated warnings or reformat untouched code.** The warning
   list is known. Drive-by changes bury the real diff.
5. **Do not delete commented-out code** without asking. Some blocks, such as
   `Compiler.Test()`, are paused work rather than dead code — they record intent.
6. **Leave stubs honest.** An unfinished method should `throw new
   NotImplementedException()`, not silently return a wrong value or do nothing.
   Empty method bodies that pretend to succeed have already cost this project
   real debugging time.
7. **Do not run git commands which mutate files.** Those will be run by me manually. Commands which only query
   contents, history, changes or are read-only are fine.

## Documents

| File | Read it when |
|---|---|
| [`agents/code-style.md`](agents/code-style.md) | Before writing or editing any C#. Always. |
| [`agents/architecture.md`](agents/architecture.md) | You need the compiler pipeline and what is/isn't built. |
| [`agents/language.md`](agents/language.md) | You are touching language syntax, semantics, or the datapack target. |

Keep these documents short. This project is too young for extensive structural
documentation, and detailed file-by-file maps go stale faster than they help.
