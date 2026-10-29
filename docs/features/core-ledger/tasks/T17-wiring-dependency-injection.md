---
id: T17
title: "Register every adapter and use case in the composition root"
layer: "wiring"
deps: ["T01", "T13", "T14", "T15", "T16"]
acs: []
files_hint: ["src/FinNavis.Infrastructure/DependencyInjection/", "src/FinNavis.Application/DependencyInjection/", "src/FinNavis.Presentation/Program.cs"]
owner: "Andrey Simonenko"
estimate: "S"
status: "done"
attempts: 1
---

# T17 — Register every adapter and use case in the composition root

## Why

[`CLAUDE.md`](../../../../CLAUDE.md) DI rules and [sad.md §5](../sad.md): **Presentation is the only
composition root.** Infrastructure registers its own services; Application does the same for its
own; Presentation calls those registration methods rather than registering Infrastructure types one
by one.

## What

- `Infrastructure/DependencyInjection` — one `AddInfrastructure(...)` extension registering
  `FinNavisDbContext`, the five repository adapters, `UnitOfWork` and the three `ILedgerQueries`
  implementations.
- `Application/DependencyInjection` — one `AddApplication(...)` extension registering the use-case
  classes.
- `Program.cs` calls both. The connection string comes from configuration, never from a literal.
- `IClock` gets its system implementation here.

## Definition of Done

- [ ] every port declared in `Domain/Abstractions` and `Application/Abstractions` resolves from the
      container; a resolution test proves it rather than a manual check
- [ ] Presentation does not register an Infrastructure type one by one (`CLAUDE.md`)
- [ ] no connection string, password or key is in a committed file (`CLAUDE.md` Safety)
- [ ] `tests/FinNavis.ArchitectureTests` stays green
- [ ] `./build.sh run` starts against the compose stack from [T01](./T01-setup-compose-stack.md)

## Notes

- Shares a lane with [T24](./T24-ports-error-envelope-and-registry.md) on `Program.cs`. The two are
  serialized by `implement-tasks` on the overlapping `files_hint`.
- Every endpoint task depends on this, so it is the narrow point of the DAG — keep it small and
  land it quickly.
