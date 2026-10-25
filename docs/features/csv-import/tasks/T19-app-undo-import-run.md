---
id: T19
title: "Add the UndoImportRun use case"
layer: "app"
deps: ["T05", "T09", "T11"]
acs: ["AC-16", "AC-17", "AC-18", "AC-19"]
files_hint: ["src/FinNavis.Application/Import/UndoImportRun/", "tests/FinNavis.Application.UnitTests/Import/UndoImportRun/"]
owner: "Andrey Simonenko"
estimate: "L"
status: "todo"
---

# T19 — Add the UndoImportRun use case

## Why

[sad.md §6](../sad.md) flow 6 — US-06, *"so that one bad rule does not cost me an evening"*. It is
the in-product way back from a load that went wrong, and [sad.md §7](../sad.md) says the nightly
`pg_dump` exists only for the two cases this cannot cover.

Satisfies [AC-16](../PRD.md), [AC-17](../PRD.md), [AC-18](../PRD.md) and [AC-19](../PRD.md).

## What

In `src/FinNavis.Application/Import/UndoImportRun/`, in this order:

1. **Load the run with its links**, and the movements those links point at.
2. **Refuse if the run is already `Undone`**, and say so. Nothing changes.
3. **Refuse if any movement no longer matches the fingerprint stored when the load wrote it**
   ([ADR-0008](../adr/0008-detect-post-import-hand-edits-with-a-stored-content-fingerprint.md)),
   and name the changed rows back. **No movement is deleted, no created account or category is
   removed, and the run keeps its status** (AC-18).
4. **Ask `IImportQueries`** which of the run's created accounts and categories a movement from
   outside the run now names (T11).
5. **Mark the run `Undone`**, delete its movements and the created accounts and categories nobody
   else names, and commit — one `IUnitOfWork`, one transaction (AC-16, AC-17).

The run row stays with status `Undone` but its movements do not, so handing the same file over
again records everything afresh (AC-19).

## Definition of Done

- [ ] A second undo of the same run is refused and the run is unchanged — unit tests in
      `tests/FinNavis.Application.UnitTests`
- [ ] A movement whose fingerprint no longer matches blocks the **whole** undo, the changed rows are
      named back, and nothing at all is deleted (AC-18)
- [ ] A clean undo removes every movement the run recorded, and every balance returns to what it
      would have been (AC-16)
- [ ] A created account or category that a movement outside the run now names is **kept**; one that
      nothing else names is removed (AC-17)
- [ ] After an undo the run's movements are gone, so the same file loads every row again (AC-19)
- [ ] The whole undo commits in one `IUnitOfWork`
- [ ] `src/FinNavis.Application` references no EF Core
- [ ] `./build.sh test` is green

## Notes

- **A movement the owner deleted by hand blocks nothing.** It is simply absent, and there is nothing
  left to remove ([sad.md §6](../sad.md)). Only a *changed* movement blocks the undo. Getting this
  backwards makes AC-18 refuse an undo that should work.
- **The deletes are real deletes, not reversing entries.** Inherited from `core-ledger` ADR-0003 for
  the whole ledger, and not decided here.
- **Check the fingerprints before deleting anything.** The order in the flow is not cosmetic: AC-18
  requires that a refused undo leaves the run exactly as it was, which is only true if nothing has
  been deleted by the time the mismatch is found.
- **ADR-0008's field list has a revisit rule.** [sad.md §11](../sad.md): the day a movement gains a
  field the owner can edit, AC-18 silently stops noticing edits to it. T25 is the test that fails
  when that happens; T07's comment is what warns the person making the change.
- **The set-aside rows survive.** They were never in the ledger, so an undo has nothing to take back
  from them ([data-model.md](../data-model.md)).
- **The undo of 1500 rows has a budget: p95 ≤ 30 s** (PRD §6). It is measured in T25, not here.
