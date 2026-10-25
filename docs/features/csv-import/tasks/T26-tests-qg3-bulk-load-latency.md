---
id: T26
title: "Quality gate QG-3 — a bulk load fits inside one evening's patience"
layer: "tests"
deps: ["T22"]
acs: ["AC-10"]
files_hint: ["tests/FinNavis.Infrastructure.IntegrationTests/Quality/MigrateHistoryLatencyTests.cs", "tests/FinNavis.Infrastructure.IntegrationTests/Seeding/"]
owner: "Andrey Simonenko"
estimate: "M"
status: "todo"
---

# T26 — Quality gate QG-3 — a bulk load fits inside one evening's patience

## Why

[sad.md §10](../sad.md) QG-3, carrying PRD §6's other number. The one-time move of the old history
should not cost an evening of waiting — it is the load that closes the old tracker, and
[PRD §7](../PRD.md) makes "old tool still open" the kill metric for the whole move.

Satisfies [AC-10](../PRD.md) at scale: every row recorded on the account and category it names, with
what the ledger lacks created.

## What

One timed integration test in `tests/FinNavis.Infrastructure.IntegrationTests/Quality/`:
`MigrateHistory_1500RowsSeveralAccounts_P95Under60s`, against PostgreSQL 18 in Testcontainers, the
synthetic export of T12, and a ledger seeded with about **20 000 movements** — `core-ledger`'s own
sizing of three years of history.

Alongside it, an **`EXPLAIN` assertion** that the candidate-snapshot query uses an index on account
and date rather than a sequential scan.

## Definition of Done

- [ ] `MigrateHistory_1500RowsSeveralAccounts_P95Under60s` meets **load the old history (~1500 rows,
      several accounts), p95 ≤ 60 s** (PRD §6, copied not rounded)
- [ ] The ledger it runs against is seeded with ~20 000 movements, not an empty database
- [ ] Every row of the export is recorded on the account and category it names, and the accounts and
      categories the ledger lacked are created (AC-10)
- [ ] The `EXPLAIN` assertion reports an index scan for the **monthly** snapshot shape and **fails
      when the index is dropped**
- [ ] The export it runs against is synthetic — invented merchants and amounts, real shape
- [ ] `./build.sh test` is green

## Notes

- **The `EXPLAIN` assertion is the point of this task, not decoration.**
  [sad.md §10](../sad.md): nothing else in this feature would fail if the index were missing — its
  absence would show up as a slow evening instead of a failing test.
- **Assert the plan for the monthly shape, not the migration shape.**
  [data-model.md](../data-model.md) § Indexes: several accounts across three years is close to the
  whole table, which PostgreSQL will read sequentially whatever index exists, and *"PRD §6's ≤ 60 s
  is not in danger from this"*. A test asserting an index scan there would fail for a correct
  system.
- **The escape hatch is written down.** If the assertion ever reports a sequential scan for a
  monthly load, the index to add is `transactions(account_id, occurred_on)` — and by then
  `transactions` holds rows, so it is a `CREATE INDEX CONCURRENTLY` in its own migration, outside a
  transaction (`.claude/rules/migrations.md`).
- **This is the feature's peak-memory run.** [sad.md §7](../sad.md): a three-year migration pulls
  close to the whole ledger into memory at once, and reaches that on its **first** run rather than
  by growth.
- **Shares a lane with T25 on `tests/.../Seeding/`.**
