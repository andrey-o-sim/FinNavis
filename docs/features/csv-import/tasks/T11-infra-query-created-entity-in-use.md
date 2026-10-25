---
id: T11
title: "Implement the AC-17 still-in-use check in EfImportQueries"
layer: "infra"
deps: ["T06", "T08"]
acs: ["AC-17"]
files_hint: ["src/FinNavis.Infrastructure/Queries/EfImportQueries.cs", "tests/FinNavis.Infrastructure.IntegrationTests/Queries/CreatedEntityInUseTests.cs"]
owner: "Andrey Simonenko"
estimate: "M"
status: "todo"
---

# T11 — Implement the AC-17 still-in-use check in EfImportQueries

## Why

[sad.md §6](../sad.md) flow 6: before an undo deletes the accounts and categories a run created, it
asks which of them *"a movement that belongs to no link of this run"* now names. That read crosses
both ledger tables and belongs to no single aggregate, so [sad.md §5](../sad.md) puts it behind
`IImportQueries` rather than in a repository.

Satisfies [AC-17](../PRD.md) — "those are removed too, **except any that a movement from outside the
load now names**". The exception is this query.

## What

One method on `EfImportQueries`: given a run, return which of its created accounts and created
categories are still named by a movement outside the run.

It is an **existence check per created entity**, not a read of the movements —
[sad.md §6](../sad.md) flags that it may not want the same index as the candidate snapshot, and
[data-model.md](../data-model.md) confirms it is served by `core-ledger`'s existing
`idx_transactions_account_id`, `idx_transactions_category_id`, `idx_transfers_source_account_id`
and `idx_transfers_destination_account_id`.

"Outside the run" means: not reachable through this run's `import_transaction_links` or
`import_transfer_links`.

## Definition of Done

- [ ] An account the run created and that a movement outside the run now names is reported **in
      use** (AC-17) — an integration test seeds exactly that case
- [ ] An account named only by the run's own movements is reported **not in use**, so undo removes
      it
- [ ] A category the run created and that a hand-typed transaction now names is reported in use —
      the case `UndoImportRun_CreatedCategoryNowUsedElsewhere_IsKept` (T25) rests on
- [ ] A transfer naming a created account on **either** side counts as in use — both
      `source_account_id` and `destination_account_id` are checked
- [ ] The query adds no index; an `EXPLAIN` assertion shows it using `core-ledger`'s four existing
      account and category indexes
- [ ] `./build.sh test` is green

## Notes

- **Shares a lane with T10 on `src/FinNavis.Infrastructure/Queries/EfImportQueries.cs`.** Do the two
  in sequence; the DAG lets them start at the same time, and the file does not.
- **Checking only `source_account_id` is the easy bug here.** A transfer names two accounts, and a
  created account can be either side. Missing one half means undo deletes an account a real transfer
  still points at, and PostgreSQL will refuse the delete at the FK — a failure the owner sees as a
  broken undo rather than a wrong answer.
- **It is an existence check, not a count.** The undo needs yes or no per created entity; returning
  the movements would pull the ledger into memory for nothing.
- **This query is the whole of the AC-17 exception.** [data-model.md](../data-model.md) says the
  creation link table tells undo *what* the run created; this query decides *whether* each one may
  go.
