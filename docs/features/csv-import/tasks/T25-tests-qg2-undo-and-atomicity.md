---
id: T25
title: "Quality gate QG-2 — a load is all-or-nothing, and reversible"
layer: "tests"
deps: ["T22", "T23"]
acs: ["AC-16", "AC-17", "AC-18", "AC-19", "AC-22"]
files_hint: ["tests/FinNavis.Infrastructure.IntegrationTests/Quality/UndoAndAtomicityTests.cs", "tests/FinNavis.Infrastructure.IntegrationTests/Seeding/"]
owner: "Andrey Simonenko"
estimate: "L"
status: "todo"
---

# T25 — Quality gate QG-2 — a load is all-or-nothing, and reversible

## Why

[sad.md §10](../sad.md) QG-2. It carries one of PRD §6's two numbers and four correctness tests that
no other task proves end to end.

Satisfies [AC-16](../PRD.md), [AC-17](../PRD.md), [AC-18](../PRD.md), [AC-19](../PRD.md) and
[AC-22](../PRD.md).

## What

Integration tests in `tests/FinNavis.Infrastructure.IntegrationTests/Quality/`, against PostgreSQL
18 in Testcontainers, seeded, using the synthetic export of T12. The five
[sad.md §10](../sad.md) names, verbatim:

- `LoadStatement_CommitFailsPartWay_LedgerIsUnchanged` — a fault injected at the commit; it fails
  if any row, run record or link survives (AC-22).
- `UndoImportRun_1500Rows_P95Under30s` — timed against the seeded ledger and the synthetic export.
- `UndoImportRun_MovementChangedByHand_IsRefusedAndNamesTheRows` — AC-18, and the **only** test of
  ADR-0008's fingerprint.
- `UndoImportRun_CreatedCategoryNowUsedElsewhere_IsKept` — AC-17.
- `LoadStatement_RunUndoneThenSameFileAgain_RecordsEverything` — AC-19.

The seeded ledger lives in `Seeding/`, shared with T26.

## Definition of Done

- [ ] `UndoImportRun_1500Rows_P95Under30s` meets **undo of a load of 1500 rows, p95 ≤ 30 s**
      (PRD §6, copied not rounded)
- [ ] `LoadStatement_CommitFailsPartWay_LedgerIsUnchanged` leaves no movement, no run row, no link
      and no set-aside row, and every balance unchanged (AC-22)
- [ ] `UndoImportRun_MovementChangedByHand_IsRefusedAndNamesTheRows` refuses the undo, names the
      changed rows, and asserts that **nothing was deleted** — no movement, no created account, no
      created category, and the run keeps its status (AC-18)
- [ ] That test **fails when a field is dropped from ADR-0008's list** — edit the dropped field by
      hand and assert the undo is still refused
- [ ] `UndoImportRun_CreatedCategoryNowUsedElsewhere_IsKept` keeps a created category a hand-typed
      transaction now names, and removes one nothing else names (AC-17)
- [ ] `LoadStatement_RunUndoneThenSameFileAgain_RecordsEverything` records every row again — an
      undone load does not look already recorded (AC-19)
- [ ] A clean undo returns every balance to what it would have been had the load never happened
      (AC-16)
- [ ] `./build.sh test` is green

## Notes

- **The fingerprint test is the one that goes quietly wrong.** [sad.md §10](../sad.md): *"it fails
  if the fingerprint stops covering a field the owner can edit, which is the way that record goes
  quietly wrong."* Write it so it edits each covered field in turn — a test that only edits the
  amount passes forever after `note` drops out of the hash.
- **A deleted movement is not a changed one.** [sad.md §6](../sad.md): a movement the owner deleted
  by hand is simply absent and blocks nothing. Add that case, or the first bug report will be an
  undo that refuses when it should work.
- **Shares a lane with T26 on `tests/.../Seeding/`**, where the ~20 000-movement seeded ledger both
  gates time against lives.
- **The ≤ 30 s figure cannot be measured until the Testcontainers harness exists**
  ([sad.md §10](../sad.md) Prerequisite) — `core-ledger` builds it.
- No `./build.sh check` and no lint command exist in this repo. `TreatWarningsAsErrors` makes
  `./build.sh build` the lint gate.
