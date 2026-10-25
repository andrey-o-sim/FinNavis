---
id: T24
title: "Quality gate QG-1 — nothing is recorded twice"
layer: "tests"
deps: ["T22"]
acs: ["AC-15", "AC-20", "AC-21"]
files_hint: ["tests/FinNavis.Infrastructure.IntegrationTests/Quality/DuplicateSafetyTests.cs"]
owner: "Andrey Simonenko"
estimate: "M"
status: "todo"
---

# T24 — Quality gate QG-1 — nothing is recorded twice

## Why

[sad.md §10](../sad.md) QG-1. The gate behind [PRD §2](../PRD.md)'s third goal: *"the owner can hand
the same file over again without fear."* [sad.md §11](../sad.md) carries the matching High risk —
a false duplicate match deletes real spending silently, and nothing is shown to the owner when a row
is judged already there.

Satisfies [AC-15](../PRD.md), [AC-20](../PRD.md) and [AC-21](../PRD.md), proved end to end rather
than at the query (T10) or matcher (T14) level.

## What

Three integration tests in `tests/FinNavis.Infrastructure.IntegrationTests/Quality/`, against
PostgreSQL 18 in Testcontainers with a seeded ledger and the synthetic sample files of T12. They run
against a real database on purpose: the candidate snapshot is a SQL query over an account and a date
range, so a test double would prove nothing about which rows it actually selects.

- `LoadStatement_SameFileTwice_RecordsNothingTheSecondTime` — AC-20.
- `LoadStatement_FileCarriesTwoIdenticalRows_RecordsBoth` — the structural invariant of ADR-0003.
- `LoadStatement_RowOverlapsAHandTypedMovement_CountsItAsAlreadyThere` — AC-21.

Plus the AC-15 case: a row set aside for an uncovered category lands on the second load once the
rule is added, while the rows the first load recorded do not land again.

## Definition of Done

- [ ] `LoadStatement_SameFileTwice_RecordsNothingTheSecondTime` passes, and the second run reports
      the rows as **already there** rather than set aside (AC-20)
- [ ] `LoadStatement_FileCarriesTwoIdenticalRows_RecordsBoth` passes — **two rows naming two
      separate real spends are both recorded**
- [ ] `LoadStatement_RowOverlapsAHandTypedMovement_CountsItAsAlreadyThere` passes, with the matching
      movement typed by hand so it has no `external_ref` and only the proximity tier can find it
      (AC-21)
- [ ] A row set aside for an uncovered category is recorded on the second load after the rule is
      added, and the first load's rows are not recorded again (AC-15)
- [ ] `LoadStatement_FileCarriesTwoIdenticalRows_RecordsBoth` **fails** when the candidate snapshot
      is made to see the run's own writes — the assertion is not decoration
- [ ] **No number is asserted.** PRD §6 sets none for this gate, and inventing one would make §10
      look more measured than it is
- [ ] `./build.sh test` is green

## Notes

- **The mirror case matters as much as the duplicate case.** [sad.md §10](../sad.md): *"two rows
  naming two separate real spends are both recorded, because `GLOSSARY.md` defines them as two real
  spends rather than a duplicate."* This is the test that fails the moment anything lets the
  snapshot see the run's own writes — which is the regression that would silently drop real
  spending.
- **Two of the three can be written before T01 lands and one cannot.**
  [sad.md §10](../sad.md) states the split: the first two do not depend on the proximity tier's
  parameters; `LoadStatement_RowOverlapsAHandTypedMovement_CountsItAsAlreadyThere` **is** the
  proximity tier. If T01 is still open, write the first two and mark the third blocked rather than
  guessing a window.
- **The fixtures are synthetic.** No real statement and no real export enters the repository
  (`CLAUDE.md` Safety).
- Runs in parallel with T25 and T26 — all three fan out from T22, and only T25 and T26 share the
  seeding folder.
