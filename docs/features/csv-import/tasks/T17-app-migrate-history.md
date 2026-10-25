---
id: T17
title: "Add the MigrateHistory use case with the migration policy"
layer: "app"
deps: ["T14"]
acs: ["AC-03", "AC-10", "AC-11", "AC-12"]
files_hint: ["src/FinNavis.Application/Import/MigrateHistory/", "tests/FinNavis.Application.UnitTests/Import/MigrateHistory/"]
owner: "Andrey Simonenko"
estimate: "M"
status: "todo"
---

# T17 — Add the MigrateHistory use case with the migration policy

## Why

[sad.md §6](../sad.md) flow 4 — US-03, the one-time move of three years of history out of the old
tracker. Same pipeline as T16 with the **migration policy**, which is
[ADR-0005](../adr/0005-run-both-import-jobs-through-one-pipeline-with-two-entry-points.md) working
as intended.

Satisfies [AC-03](../PRD.md), [AC-10](../PRD.md), [AC-11](../PRD.md) and [AC-12](../PRD.md).

## What

In `src/FinNavis.Application/Import/MigrateHistory/`: the use case and the migration policy.

The three differences from a statement load, all visible in flow 4:

1. **No mapping profile and no confirmation step.** The export already names accounts and
   categories in the owner's own words.
2. **What the ledger lacks is created**, not refused — every account and category the file names
   that the ledger does not have, each recorded in a creation link so AC-17 can undo it (AC-10).
3. **A move between two of the owner's own accounts becomes a `Transfer`**, because the export names
   both sides (AC-11). A statement sets the same row aside.

Rows keep the file's own date. Every write is an insert, so a month the owner already filled in by
hand gains the loaded rows and loses nothing (AC-12).

An export naming no date, no amount **or no account per row** is refused before anything is
recorded (AC-03).

## Definition of Done

- [ ] An export naming no date, no amount or no account column is refused and which of the three is
      missing is named (AC-03) — unit tests in `tests/FinNavis.Application.UnitTests`
- [ ] Every row is recorded on the account and category it names, keeping its own date (AC-10)
- [ ] An account or category the ledger lacks is created and linked to the run, so undo can find it
      later (AC-10, AC-17)
- [ ] A row naming two of the owner's accounts becomes a `Transfer` with no category, and the
      owner's total is unchanged (AC-11)
- [ ] Rows dated in a month the owner already recorded by hand are added, and nothing hand-typed is
      changed or removed (AC-12)
- [ ] No mapping profile is read and no confirmation step is reachable
- [ ] `./build.sh test` is green

## Notes

- **Blocked by `core-ledger`'s open calendar-date-to-UTC question, and this is the task it blocks.**
  [sad.md §11](../sad.md), High: *"a monthly statement loaded under the wrong interpretation is 30
  rows to fix; the one-time move of the old history is 1500, loaded once, and fixing it afterwards
  is a migration over real recorded history."* US-02 may ship before this. Close `core-ledger`'s
  question first.
- **AC-11 may be unreachable.** PRD §8 is open on whether the export names both sides of a move
  between accounts. Default now: AC-11 applies when it does. If T01 finds it does not, flow 4 loses
  its Transfer branch — say so in `sad.md` rather than deleting it.
- **A category group is never created by an import.** The export names categories; a created
  category simply has no group, which the breakdown reports under the Default group
  ([data-model.md](../data-model.md)).
- **`import_runs.account_id` is always null for this kind.** The file names an account per row
  instead.
- **A transfer carries no `external_ref`** (`core-ledger` ADR-0006), so a transfer recorded here is
  reachable by the proximity tier alone on a later load. Costs nothing — the export is loaded once.
