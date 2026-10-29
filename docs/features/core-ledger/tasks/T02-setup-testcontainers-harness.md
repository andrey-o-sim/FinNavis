---
id: T02
title: "Build the Testcontainers PostgreSQL 18 fixture for the integration suite"
layer: "tests"
deps: []
acs: []
files_hint: ["tests/FinNavis.Infrastructure.IntegrationTests/PostgresFixture.cs", "tests/FinNavis.Infrastructure.IntegrationTests/DatabaseCollection.cs", "tests/FinNavis.Infrastructure.IntegrationTests/SmokeTests.cs"]
owner: "Andrey Simonenko"
estimate: "M"
status: "done"
attempts: 1
---

# T02 — Build the Testcontainers PostgreSQL 18 fixture for the integration suite

## Why

[sad.md §7](../sad.md) and [sad.md §11](../sad.md) both say the integration harness does not exist,
and that **not one PRD §6 NFR row can be measured** until it does. [sad.md §10](../sad.md) requires
QG-1 to run against real PostgreSQL rather than a test double, because ADR-0001 made balance a
three-part sum across two tables and the thing most likely to be wrong is the SQL.

## What

- An xUnit collection fixture that starts **PostgreSQL 18** in Testcontainers once per test run and
  exposes its connection string.
- Per-test isolation: each test gets a clean database state, so test order cannot change a result.
- Delete `tests/FinNavis.Infrastructure.IntegrationTests/SmokeTests.cs` and replace it with one
  real test that proves the fixture connects.

## Definition of Done

- [ ] the fixture starts a PostgreSQL 18 container and the connection test passes with Docker
      running
- [ ] two tests running in sequence do not see each other's rows
- [ ] `SmokeTests.cs` is deleted from this project (`CLAUDE.md` Tests)
- [ ] `./build.sh test` is green
- [ ] lint + build clean, warnings are errors (`TreatWarningsAsErrors`)

## Notes

- This task does **not** apply migrations — none exist yet.
  [T12](./T12-migration-core-ledger-initial.md) extends this same fixture to run
  `Database.MigrateAsync()`, so the two share a lane on `PostgresFixture.cs`.
- Image tag pins major version 18, matching [T01](./T01-setup-compose-stack.md) and sad.md §7.
