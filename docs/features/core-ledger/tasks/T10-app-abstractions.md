---
id: T10
title: "Declare IUnitOfWork and the ILedgerQueries read port with its result records"
layer: "app"
deps: ["T05"]
acs: []
files_hint: ["src/FinNavis.Application/Abstractions/IUnitOfWork.cs", "src/FinNavis.Application/Abstractions/ILedgerQueries.cs", "src/FinNavis.Application/Abstractions/Reads/"]
owner: "Andrey Simonenko"
estimate: "S"
status: "todo"
---

# T10 — Declare `IUnitOfWork` and the `ILedgerQueries` read port

## Why

Derives from [ADR-0004](../adr/0004-separate-the-read-path-behind-a-query-port.md) and
[sad.md §5](../sad.md): *"Writes go through the repository ports and return domain entities. Reads
go through `ILedgerQueries` … so aggregation happens in PostgreSQL and only result rows travel
back."* Both ports are outside-world ports, so they live in `Application/Abstractions`
(`CLAUDE.md`, `ARCHITECTURE.md`).

Declaring them early unlocks the three read-path tasks and the six use-case tasks in parallel.

## What

- `IUnitOfWork` — commits a set of changes. Kept in Application, not Domain: transactions are an
  orchestration concern, not a domain rule (`CLAUDE.md`).
- `ILedgerQueries` — the read port, with the signatures the three read tasks implement:
  - balance per account, and the two existence checks flow 5 and flow 6 need before a delete
  - one page of history, with the filters and the opaque cursor
  - the period breakdown rows
- The result records these return. They are plain read rows, **not** Domain entities — that is the
  point of ADR-0004.

## Definition of Done

- [ ] both ports are declared in `Application/Abstractions`; nothing in Application references
      EF Core, HTTP or PostgreSQL (`CLAUDE.md`)
- [ ] `tests/FinNavis.ArchitectureTests` stays green — no `PackageReference` was added to
      Application
- [ ] the result records are read rows, carrying no behaviour and no entity type
- [ ] `./build.sh build` is clean, warnings are errors

## Notes

- The history cursor is typed as an **opaque string** here, never a UUID.
  [`.claude/rules/openapi.md` deviation 3](../../../../.claude/rules/openapi.md): history is a
  `UNION ALL` of two tables ordered by `(occurred_on, id)`, so one id cannot locate a page
  boundary.
- **We have no driving ports.** Presentation calls use-case classes directly — do not add an
  interface per use case (`CLAUDE.md`, sad.md §5).
