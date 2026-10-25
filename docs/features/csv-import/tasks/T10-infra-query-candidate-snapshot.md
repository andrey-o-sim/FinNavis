---
id: T10
title: "Implement the candidate snapshot in EfImportQueries"
layer: "infra"
deps: ["T06", "T08"]
acs: ["AC-20", "AC-21"]
files_hint: ["src/FinNavis.Infrastructure/Queries/EfImportQueries.cs", "tests/FinNavis.Infrastructure.IntegrationTests/Queries/CandidateSnapshotTests.cs"]
owner: "Andrey Simonenko"
estimate: "L"
status: "todo"
---

# T10 — Implement the candidate snapshot in EfImportQueries

## Why

[ADR-0003](../adr/0003-take-one-snapshot-of-candidate-movements-before-the-run-writes.md): one query
over the accounts the file names across the dates it covers, taken **before the run writes
anything**. It replaces ~1500 per-row lookups and — more importantly — it is why a run can never
see its own inserts. [sad.md §6](../sad.md) flows 2, 3 and 4 all open with it.

Satisfies [AC-20](../PRD.md) and [AC-21](../PRD.md) at the query level: the snapshot has to contain
movements an earlier run recorded **and** movements the owner typed by hand, and treat neither
differently. T14 is the matching over it; T24 is the gate.

## What

One method on `EfImportQueries` in `src/FinNavis.Infrastructure/Queries/`: given the accounts the
file names and the date range it covers, return every transaction and every transfer that could
match a row — whoever recorded them.

Two shapes of call, one query:

- a **statement load** names one account and roughly a 30-day range;
- a **history migration** names every account the file mentions across the whole range it covers.

It returns read models from `Application/Abstractions/Reads/`, not Domain entities, and it adds no
index — [data-model.md](../data-model.md) § Indexes settles that `core-ledger`'s existing indexes
serve it.

## Definition of Done

- [ ] The snapshot contains movements an earlier run recorded and movements the owner typed by hand,
      with nothing distinguishing them (AC-20, AC-21) — an integration test seeds both and asserts
      both come back
- [ ] It is taken in one query, before any write, so it can never hold a row the run itself adds —
      the structural invariant ADR-0003 rests on
- [ ] A movement outside the date range or on another account is **not** returned
- [ ] An `EXPLAIN` assertion shows the statement-load snapshot using
      `idx_transactions_occurred_on_account_id` rather than a sequential scan, and the test **fails
      when that index is dropped**
- [ ] The query adds no index to `transactions` or `transfers` (ADR-0007)
- [ ] `./build.sh test` is green

## Notes

- **Shares a lane with T11 on `src/FinNavis.Infrastructure/Queries/EfImportQueries.cs`.** The two
  reads are separate tasks because each has its own index to prove, but they cannot be worked at the
  same time.
- **The `EXPLAIN` assertion has an escape hatch, and it is written down.**
  [data-model.md](../data-model.md) § Indexes: if it ever reports a sequential scan for a *monthly*
  load, the index to add is `transactions(account_id, occurred_on)` — and by then `transactions`
  holds rows, so it is a `CREATE INDEX CONCURRENTLY` in its own migration, outside a transaction.
- **A sequential scan for a history migration is not a failure.** Several accounts across three
  years is close to the whole table, which PostgreSQL will read sequentially whatever index exists.
  [data-model.md](../data-model.md) says so; assert the plan for the **monthly** shape only.
- **This is the peak-memory point of the feature.** [sad.md §7](../sad.md): a three-year migration
  pulls close to the whole ledger into memory at once, about 20 000 movements, and reaches that on
  its **first** run rather than by growth. Look again past roughly 200 000 movements.
- **Transfers carry no `external_ref`** (`core-ledger` ADR-0006 put it on transactions only), so a
  transfer in the snapshot is reachable by the proximity tier alone. Noted so nobody assumes the two
  tiers are symmetric.
