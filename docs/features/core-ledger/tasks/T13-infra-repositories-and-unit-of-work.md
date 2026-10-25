---
id: T13
title: "Add the five EF Core repository adapters and the unit of work"
layer: "infra"
deps: ["T12", "T10"]
acs: []
files_hint: ["src/FinNavis.Infrastructure/Repositories/", "src/FinNavis.Infrastructure/Persistence/UnitOfWork.cs", "tests/FinNavis.Infrastructure.IntegrationTests/Repositories/"]
owner: "Andrey Simonenko"
estimate: "M"
status: "todo"
---

# T13 — Add the five EF Core repository adapters and the unit of work

## Why

[sad.md §5](../sad.md): Infrastructure is the adapter layer — it implements every port, wherever it
was declared. The five repository ports come from `Domain/Abstractions`
([T06](./T06-domain-account.md), [T07](./T07-domain-category-and-group.md),
[T08](./T08-domain-transaction.md), [T09](./T09-domain-transfer.md)); `IUnitOfWork` comes from
`Application/Abstractions` ([T10](./T10-app-abstractions.md)).

## What

- `Repositories/` — one EF Core adapter per port: accounts, categories, category groups,
  transactions, transfers. Load by id, add, remove.
- `ICategoryRepository` also loads the categories pointing at a given group — AC-29 needs them
  before the group row goes ([sad.md §6 flow 10](../sad.md)), and AC-33 lists them.
- The three **name-scoped existence checks** flow 13 needs: an account by name, a category by name
  and side, a group by name and side. Each is scoped exactly as the matching unique index is
  scoped.
- `UnitOfWork` — commits a set of changes in one database transaction.

## Definition of Done

- [ ] every one of the five ports has an adapter, and each round-trips an entity through
      PostgreSQL 18 in Testcontainers
- [ ] the three name-scoped existence checks each hit the unique index, not a sequential scan
- [ ] a commit that fails rolls back every change in the unit of work — nothing is half-written
      (AC-06, sad.md §6 flow 2)
- [ ] `Infrastructure.csproj` still lists **`Application` only** — Domain comes transitively, and
      that is deliberate (`CLAUDE.md`, `ARCHITECTURE.md`)
- [ ] `tests/FinNavis.ArchitectureTests` stays green
- [ ] `./build.sh test` is green

## Notes

- Reads do **not** go through these adapters. ADR-0004 puts aggregation behind `ILedgerQueries`, so
  the breakdown and the history page never load an entity into memory
  ([T14](./T14-infra-queries-balances.md), [T15](./T15-infra-queries-history.md),
  [T16](./T16-infra-queries-breakdown.md)).
