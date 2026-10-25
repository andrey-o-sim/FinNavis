---
id: T15
title: "Add the InspectFile and ConfirmMappingProfile use cases"
layer: "app"
deps: ["T04", "T06", "T09", "T12"]
acs: ["AC-01", "AC-02", "AC-03", "AC-04"]
files_hint: ["src/FinNavis.Application/Import/InspectFile/", "src/FinNavis.Application/Import/ConfirmMappingProfile/", "tests/FinNavis.Application.UnitTests/Import/InspectFile/", "tests/FinNavis.Application.UnitTests/Import/ConfirmMappingProfile/"]
owner: "Andrey Simonenko"
estimate: "M"
status: "todo"
---

# T15 — Add the InspectFile and ConfirmMappingProfile use cases

## Why

The confirm step of [sad.md §6](../sad.md) flow 1 — US-01. Two use cases rather than one because
[ADR-0001](../adr/0001-never-store-the-uploaded-file.md) keeps nothing between the two calls: the
file is handed over to be inspected, and the rules come back separately.

Satisfies [AC-01](../PRD.md), [AC-02](../PRD.md), [AC-03](../PRD.md) and [AC-04](../PRD.md). They
sit in one task because inspect exists only to feed confirm — [sad.md §5](../sad.md) says so, and
it is why both endpoints live under `MappingProfiles/`.

## What

**`InspectFile`** — read the file through `IStatementReader`, report the categories it carries and
whether a profile already exists for its shape. Records nothing and stores nothing. Refuses when the
file names no date or no amount, saying which is missing (AC-03).

**`ConfirmMappingProfile`** — take the `shape_key` that inspect returned, **not the file again**
([api-sync-report.md](../contracts/api-sync-report.md), decision 1), plus one rule per category the
owner wants filed. Load the categories the rules point at; refuse the whole confirm if any is not in
the ledger, naming it back and **creating nothing** (AC-02). Otherwise build the `MappingProfile`
and commit (AC-01).

A category the owner leaves without a rule is not a gap — its rows are set aside later (AC-08,
AC-14). There is no third kind of rule.

Re-confirming an existing shape **replaces its rules wholesale** and changes nothing already
recorded (AC-04).

## Definition of Done

- [ ] Inspect lists the file's own categories and says whether rules already exist for its shape —
      unit tests in `tests/FinNavis.Application.UnitTests`
- [ ] A file naming no date or no amount is refused and the missing one is named (AC-03)
- [ ] A rule pointing at a category the ledger does not have refuses the whole confirm, names the
      category back, and creates nothing (AC-02)
- [ ] After a successful confirm the rules are found by shape key, so the next file of that shape
      needs no confirmation (AC-01)
- [ ] Re-confirming replaces every rule and leaves every already-recorded movement with the category
      it was given (AC-04)
- [ ] Confirm takes the `shape_key`, not the file — the file is not uploaded a third time
- [ ] `src/FinNavis.Application` references no EF Core
- [ ] `./build.sh test` is green

## Notes

- **AC-04 is proved here behaviourally and in T04 structurally.** T04 asserts no movement can point
  at a rule; this task asserts that re-confirming leaves recorded rows untouched. Both are needed —
  the structural one is what keeps the behavioural one true after a refactor.
- **AC-02's message says "with that name" while the rule points at a category by id.**
  [api-sync-report.md](../contracts/api-sync-report.md) keeps the PRD §10 wording verbatim on
  purpose — that is the sentence the owner reads. The id and the file's own category text go into
  `details`.
- **Two uploads per new bank is the price ADR-0001 named**, and confirm carrying the shape key
  instead of the file is what keeps it at two.
- Depends on T09 and T12 because the use cases call the real profile repository and the real
  reader — the same shape `core-ledger`'s use-case tasks take.
