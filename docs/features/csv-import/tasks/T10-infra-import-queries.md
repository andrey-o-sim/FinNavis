---
id: T10
title: "Implement the candidate snapshot and the AC-17 still-in-use check in EfImportQueries"
layer: "infra"
deps: ["T06", "T08"]
acs: ["AC-17", "AC-20", "AC-21"]
files_hint: ["src/FinNavis.Infrastructure/Queries/EfImportQueries.cs", "tests/FinNavis.Infrastructure.IntegrationTests/Queries/CandidateSnapshotTests.cs", "tests/FinNavis.Infrastructure.IntegrationTests/Queries/CreatedEntityInUseTests.cs"]
owner: "Andrey Simonenko"
estimate: "L"
status: "done"
---

# T10 — Implement the candidate snapshot and the AC-17 still-in-use check in EfImportQueries

## Why

`EfImportQueries` is the adapter behind `IImportQueries`, and that port declares two reads. Both are
built in this one task. Split across two tasks, the first one has to leave the other method as a
`NotImplementedException` stub, and `finnavis-tdd` Gate 2 refuses any stub left after GREEN. That
is how the first `finnavis-tdd-loop` run failed on 2026-11-07 (`reports/T10.md`).

**Read 1 — the candidate snapshot.**
[ADR-0003](../adr/0003-take-one-snapshot-of-candidate-movements-before-the-run-writes.md): one query
over the accounts the file names across the dates it covers, taken **before the run writes
anything**. It replaces ~1500 per-row lookups and — more importantly — it is why a run can never
see its own inserts. [sad.md §6](../sad.md) flows 2, 3 and 4 all open with it.

Satisfies [AC-20](../PRD.md) and [AC-21](../PRD.md) at the query level: the snapshot has to contain
movements an earlier run recorded **and** movements the owner typed by hand, and treat neither
differently. T14 is the matching over it; T24 is the gate.

**Read 2 — the AC-17 still-in-use check.**
[sad.md §6](../sad.md) flow 6: before an undo deletes the accounts and categories a run created, it
asks which of them *"a movement that belongs to no link of this run"* now names. That read crosses
both ledger tables and belongs to no single aggregate, so [sad.md §5](../sad.md) puts it behind
`IImportQueries` rather than in a repository.

Satisfies [AC-17](../PRD.md) — "those are removed too, **except any that a movement from outside the
load now names**". The exception is this query. T19 acts on it; T25 proves it.

## What

Two methods on `EfImportQueries` in `src/FinNavis.Infrastructure/Queries/`. Both return read models
from `Application/Abstractions/Reads/`, not Domain entities, and neither adds an index —
[data-model.md](../data-model.md) § Indexes settles that `core-ledger`'s existing indexes serve
both.

**The candidate snapshot.** Given the accounts the file names and the date range it covers, return
every transaction and every transfer that could match a row — whoever recorded them.

Two shapes of call, one query:

- a **statement load** names one account and roughly a 30-day range;
- a **history migration** names every account the file mentions across the whole range it covers.

**The still-in-use check.** Given a run, return which of its created accounts and created
categories are still named by a movement outside the run.

It is an **existence check per created entity**, not a read of the movements —
[sad.md §6](../sad.md) flags that it may not want the same index as the candidate snapshot, and
[data-model.md](../data-model.md) confirms it is served by `core-ledger`'s existing
`idx_transactions_account_id`, `idx_transactions_category_id`, `idx_transfers_source_account_id`
and `idx_transfers_destination_account_id`.

"Outside the run" means: not reachable through this run's `import_transaction_links` or
`import_transfer_links`.

## Definition of Done

Candidate snapshot:

- [ ] The snapshot contains movements an earlier run recorded and movements the owner typed by hand,
      with nothing distinguishing them (AC-20, AC-21) — an integration test seeds both and asserts
      both come back
- [ ] It is taken in one query, before any write, so it can never hold a row the run itself adds —
      the structural invariant ADR-0003 rests on
- [ ] A movement outside the date range or on another account is **not** returned
- [ ] An `EXPLAIN` assertion shows the statement-load snapshot using
      `idx_transactions_occurred_on_account_id` rather than a sequential scan, and the test **fails
      when that index is dropped**

Still-in-use check:

- [ ] An account the run created and that a movement outside the run now names is reported **in
      use** (AC-17) — an integration test seeds exactly that case
- [ ] An account named only by the run's own movements is reported **not in use**, so undo removes
      it
- [ ] A category the run created and that a hand-typed transaction now names is reported in use —
      the case `UndoImportRun_CreatedCategoryNowUsedElsewhere_IsKept` (T25) rests on
- [ ] A transfer naming a created account on **either** side counts as in use — both
      `source_account_id` and `destination_account_id` are checked
- [ ] An `EXPLAIN` assertion shows it using `core-ledger`'s four existing account and category
      indexes

Both:

- [ ] Neither query adds an index to `transactions` or `transfers` (ADR-0007)
- [ ] No `NotImplementedException` is left in `EfImportQueries`
- [ ] `./build.sh test` is green

## Notes

- **Merged from the old T10 and T11 on 2026-11-07.** They were separate because each read has its
  own index to prove. They share one class, so neither could pass Gate 2 alone. The two test files
  stay separate, one per read.
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
- **Checking only `source_account_id` is the easy bug in the still-in-use check.** A transfer names
  two accounts, and a created account can be either side. Missing one half means undo deletes an
  account a real transfer still points at, and PostgreSQL will refuse the delete at the FK — a
  failure the owner sees as a broken undo rather than a wrong answer.
- **It is an existence check, not a count.** The undo needs yes or no per created entity; returning
  the movements would pull the ledger into memory for nothing.
- **The still-in-use check is the whole of the AC-17 exception.** [data-model.md](../data-model.md)
  says the creation link table tells undo *what* the run created; this query decides *whether* each
  one may go.
