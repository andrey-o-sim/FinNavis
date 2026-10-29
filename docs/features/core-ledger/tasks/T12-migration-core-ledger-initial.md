---
id: T12
title: "Generate the EF Core migration and diff it against the staged SQL spec"
layer: "migration"
deps: ["T11", "T02"]
acs: []
files_hint: ["docs/features/core-ledger/migrations/", "src/FinNavis.Infrastructure/Persistence/Migrations/", "tests/FinNavis.Infrastructure.IntegrationTests/PostgresFixture.cs"]
owner: "Andrey Simonenko"
estimate: "M"
status: "done"
attempts: 1
---

# T12 — Generate the EF Core migration and diff it against the staged SQL spec

## Why

[`.claude/rules/migrations.md`](../../../../.claude/rules/migrations.md) states how this repo
differs from the SQL-first baseline: the five staged `.up.sql` / `.down.sql` pairs under
[`../migrations/`](../migrations/) are a **reference spec, never applied**. EF Core owns the real
migration, and `dotnet ef migrations script` output is compared against the spec.

## What

- Run `./build.sh migrate CoreLedgerInitial`. One migration for all five tables: this is a
  greenfield schema, and EF Core's `ModelSnapshot` makes five separate migrations pointless churn.
- Run `dotnet ef migrations script` and compare the output against the five staged pairs
  (`20261018120000_create_accounts` through `20261018120004_create_transfers`). **A difference
  means either the configuration or the spec is wrong — resolve it before applying.**
- Extend `PostgresFixture` from [T02](./T02-setup-testcontainers-harness.md) to apply migrations
  when the container starts.

## Definition of Done

- [ ] the generated script matches the staged spec on every table, column, type, bound, index and
      FK; any difference is resolved in the configuration or the spec before this task closes
- [ ] the migration applies cleanly against PostgreSQL 18 in Testcontainers, and `dotnet ef
      database update 0` reverts it cleanly
- [ ] the Testcontainers fixture applies migrations, so every later integration test starts from a
      real schema
- [ ] the timestamps of the EF migration and the staged spec differ — **that is expected**
      (`.claude/rules/migrations.md`)
- [ ] `./build.sh test` is green

## Notes

- **`layer: migration` is serialized** by `implement-tasks` into the ordered migration sequence.
  This is the only migration task in the epic.
- Never edit an applied migration. Add a new one (`CLAUDE.md`).
- This task shares a lane with [T02](./T02-setup-testcontainers-harness.md) on
  `PostgresFixture.cs`.
