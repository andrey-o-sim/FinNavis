---
id: T05
title: "Add the ImportRun aggregate with its five child types and IImportRunRepository"
layer: "domain"
deps: ["T03"]
acs: ["AC-13", "AC-14", "AC-17", "AC-18"]
files_hint: ["src/FinNavis.Domain/Entities/ImportRun.cs", "src/FinNavis.Domain/Entities/ImportTransactionLink.cs", "src/FinNavis.Domain/Entities/ImportTransferLink.cs", "src/FinNavis.Domain/Entities/ImportCreatedAccountLink.cs", "src/FinNavis.Domain/Entities/ImportCreatedCategoryLink.cs", "src/FinNavis.Domain/Entities/SetAsideRow.cs", "src/FinNavis.Domain/Abstractions/IImportRunRepository.cs", "tests/FinNavis.Domain.UnitTests/Entities/ImportRunTests.cs"]
owner: "Andrey Simonenko"
estimate: "L"
status: "done"
---

# T05 — Add the ImportRun aggregate with its five child types and IImportRunRepository

## Why

The other aggregate of [sad.md §5](../sad.md): *"one load of one file taken as a whole"*, with three
kinds of child entity that have no life outside it. Columns and bounds come from
[data-model.md](../data-model.md) — `import_runs` and its five child tables.
[ADR-0007](../adr/0007-keep-import-bookkeeping-in-the-features-own-tables.md) is why the links are
rows in this feature's own tables rather than columns on the ledger's.

Satisfies [AC-13](../PRD.md) (the run report), [AC-14](../PRD.md) (a set-aside row is never recorded
on its own), [AC-17](../PRD.md) (the creation links are what undo needs) and [AC-18](../PRD.md)
(the fingerprint comparison that detects a hand edit).

## What

**`ImportRun`** — aggregate root. `Id`, `FileName` (≤ 255), `Kind`, `Status`, `AccountId` (nullable;
always null for `HistoryMigration`), `RecordedRowCount`, `AlreadyPresentRowCount`, `CreatedAt`, plus
its five child collections. `MarkUndone()` refuses when the run is already `Undone`.

**Five child types**, none of which gets a repository port of its own:

- `ImportTransactionLink` — `TransactionId` and the `MovementFingerprint` of T03.
- `ImportTransferLink` — `TransferId` and its fingerprint, four fields rather than five.
- `ImportCreatedAccountLink` — an account this run created (AC-10 writes it, AC-17 reads it).
- `ImportCreatedCategoryLink` — the same for a category.
- `SetAsideRow` — `RowNumber` (1-based ordinal in the file), `Reason`, `SourceCategory` (nullable,
  ≤ 200) and `RawLine` (≤ 2000).

**`IImportRunRepository`** in `Domain/Abstractions`: get one run with its children, add one, save a
status change. Reading a run's set-aside rows (AC-13) and walking its movements to undo them
(AC-16) are both reads of one `ImportRun` through this port — [sad.md §5](../sad.md) is explicit
that neither goes behind a query port.

## Definition of Done

- [ ] A run reads back both counts, and every set-aside row with its reason, row number, source
      category and raw line (AC-13) — unit tests in `tests/FinNavis.Domain.UnitTests`
- [ ] `SetAsideRow` exposes no method and no property that turns it into a movement (AC-14) — the
      type has no state and no transition
- [ ] A movement link reports a mismatch when the fingerprint it stores stops matching the movement
      it describes (AC-18)
- [ ] A created-account link and a created-category link each record what the run made, so undo can
      tell them from what the owner made years ago (AC-17)
- [ ] `MarkUndone()` on a run already `Undone` is refused, and the run is unchanged
- [ ] The entity carries **no `Failed` status**, **no `undone_at`**, and **no `MappingProfileId`** —
      each is a deliberate absence recorded in `data-model.md`
- [ ] `src/FinNavis.Domain` still references no project and no package
- [ ] `./build.sh test` is green

## Notes

- **Deliberately over the usual line.** One root plus five child types is a large task. It stays one
  task because the root holds collections of all five — splitting it produces two diffs that cannot
  compile apart.
- **No `Failed` status is the whole of AC-22.**
  [ADR-0004](../adr/0004-commit-an-entire-import-run-in-one-database-transaction.md) commits the
  whole run in one transaction, so a load that fails leaves no run row at all. A `Failed` value
  would be a row that cannot exist.
- **A set-aside row carries no foreign key into the ledger and no status column**, because there is
  no state for it to be in (AC-14). Only a later load of the same file can record it, and that load
  writes its own run.
- **`raw_line` is the confidential part.** [sad.md §11](../sad.md) flags it: shop names the owner
  did not write, kept so he can act on the row. It is never logged (`sad.md` §7, §8).
- **A set-aside row survives an undo**, because it was never in the ledger to begin with
  ([data-model.md](../data-model.md) `import_set_aside_rows`). T07 must not cascade it.
