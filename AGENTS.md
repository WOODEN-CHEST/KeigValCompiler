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

Be honest with yourself about how little exists. As of 2026-09-19:

- The **parser** is partially built — it reads types, but not function bodies.
- The **semantic/resolver layer** is a skeleton and is never invoked.
- The **datapack backend does not exist**. Not one line. The project does not
  currently emit any output at all.

Do not describe this project as working, and do not write documentation or
comments implying features exist when they do not. See
[`agents/architecture.md`](agents/architecture.md) for the detailed state.

## Build and run

```bash
dotnet build                       # from repository root
dotnet run --project KeigValCompiler -- <source dir> [dest dir]
```

Both projects target **net10.0**. There are **zero external dependencies** — no
NuGet packages in either `.csproj`. Keep it that way unless the owner agrees
otherwise; a compiler with no dependencies is a deliberate property of this
project, not an accident.

`tests/test.kgvl` is the parser fixture: a KGVL file which, once the parser is
complete, must parse with zero errors. It is corrupted at lines 72-77, where an
editing accident dropped characters (`: Interface1` missing its leading `I`,
`TestInterface3<T> ace1,` mangled) and left a duplicate `TestClass3` line with an
orphaned continuation. Repair those and the whole file parses cleanly today.

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
5. **Do not delete commented-out code** without asking. Several blocks (e.g.
   `Compiler.Test()`, `MemberParser.ParseReturnTypedMember`) are paused work,
   not dead code — they record intent.
6. **Leave stubs honest.** An unfinished method should `throw new
   NotImplementedException()`, not silently return a wrong value or do nothing.
   Empty method bodies that pretend to succeed have already cost this project
   real debugging time.

## Documents

| File | Read it when |
|---|---|
| [`agents/code-style.md`](agents/code-style.md) | Before writing or editing any C#. Always. |
| [`agents/architecture.md`](agents/architecture.md) | You need the compiler pipeline and what is/isn't built. |
| [`agents/language.md`](agents/language.md) | You are touching language syntax, semantics, or the datapack target. |

Keep these documents short. This project is too young for extensive structural
documentation, and detailed file-by-file maps go stale faster than they help.
