---
id: T13
title: "Register the four import adapters in the Infrastructure container"
layer: "wiring"
deps: ["T09", "T10", "T11", "T12"]
acs: []
files_hint: ["src/FinNavis.Infrastructure/DependencyInjection/", "src/FinNavis.Presentation/Program.cs", "tests/FinNavis.ArchitectureTests/"]
owner: "Andrey Simonenko"
estimate: "S"
status: "todo"
---

# T13 — Register the four import adapters in the Infrastructure container

## Why

[sad.md §5](../sad.md): *"all four new adapters are registered here — the two repositories, the
query adapter and the reader. Presentation calls this registration method; it never registers an
Infrastructure type one by one."* `CLAUDE.md` § Dependency injection says the same for the whole
repo.

`acs` is empty: wiring satisfies no acceptance criterion. It is what makes every `ports` task
runnable.

## What

Extend Infrastructure's own `DependencyInjection` entry point with the four adapters:
`EfImportRunRepository`, `EfMappingProfileRepository`, `EfImportQueries` and the CSV reader behind
`IStatementReader`. Presentation already calls that method — this task adds registrations to it, it
does not add a second call site.

Then one resolve test that asks the built container for all four ports.

## Definition of Done

- [ ] `IImportRunRepository`, `IMappingProfileRepository`, `IImportQueries` and `IStatementReader`
      all resolve from the container — **one test proves it**, rather than a reading of `Program.cs`
- [ ] Registration happens inside Infrastructure's `DependencyInjection` entry point, not type by
      type in `Program.cs`
- [ ] `tests/FinNavis.ArchitectureTests` is green — Infrastructure's `.csproj` still lists
      `Application` only, with Domain transitive
- [ ] Each adapter's lifetime matches what `core-ledger` already uses for its own repositories —
      one rule for the whole container, not two
- [ ] `./build.sh test` is green

## Notes

- **Shares a lane with T21, T22 and T23 on `src/FinNavis.Presentation/Program.cs`.** This task is
  upstream of all three, so the lane is held in order as long as it lands first.
- **`core-ledger` T17 created the entry points.** This task adds to them. If
  `src/FinNavis.Application/DependencyInjection/` does not exist, `core-ledger` has not shipped and
  nothing in this epic can run — do not create it here as a side effect.
- The resolve test is the point. A registration that compiles and throws at the first request is
  the failure this catches, and reading `Program.cs` does not.
