---
id: T33
title: "Prove QG-3: report stability when categories and groups are reshaped"
layer: "tests"
deps: ["T30", "T27", "T26"]
acs: ["AC-13", "AC-18", "AC-19", "AC-20", "AC-28", "AC-29", "AC-34"]
files_hint: ["tests/FinNavis.Infrastructure.IntegrationTests/Quality/BreakdownStabilityTests.cs"]
owner: "Andrey Simonenko"
estimate: "M"
status: "todo"
---

# T33 — Prove QG-3: report stability when categories and groups are reshaped

## Why

[sad.md §10 QG-3](../sad.md) and [PRD §2](../PRD.md): *"the figure for each individual category in
a closed period never changes; group figures follow the current grouping."* This is quality goal 3
and the product promise the owner's current tool cannot keep.

## What

The three integration tests [sad.md §10](../sad.md) names, against seeded data in Testcontainers:

- `Breakdown_CategoryMovedBetweenGroups_CategoryTotalUnchanged_GroupTotalsFollow` — read the
  breakdown, move the category, read again, compare (AC-13).
- `Breakdown_GroupTotalsPlusDefaultGroup_EqualPeriodTotal` — on the spending side and the earning
  side alike (AC-19).
- `Breakdown_PeriodWithTransfers_CountsNoTransfer` (AC-20).

Plus the two reshaping cases the amendment added, which belong to the same quality goal:

- renaming a group leaves every category figure unchanged and the breakdown reports the new name
  (AC-28);
- removing a group leaves every category's history intact and reports them under the Default group
  from then on (AC-29);
- renaming a category leaves its closed-period figure unchanged (AC-34).

## Definition of Done

- [ ] all three §10-named tests pass against seeded data in Testcontainers
- [ ] `Breakdown_GroupRenamed_CategoryFiguresUnchanged` passes (AC-28)
- [ ] `Breakdown_GroupRemoved_CategoriesReportedUnderDefaultGroup` passes, and no `Default` row
      exists in `category_groups` afterwards (AC-29)
- [ ] `Breakdown_CategoryRenamed_ClosedPeriodFigureUnchanged` passes (AC-34)
- [ ] the breakdown shape matches the contract, spending and earning kept apart (AC-18)
- [ ] no real financial data in the fixtures (`CLAUDE.md` Safety)

## Notes

- A direct query asserting `SELECT count(*) FROM category_groups WHERE name = 'Default'` is zero
  is worth writing. [sad.md §6](../sad.md) warns that AC-29 is exactly where an implementer is
  tempted to create a real `Default` row.
- These three are free by design: ADR-0002 resolves the category-to-group mapping at query time, so
  nothing stored has to be migrated when the owner reshapes their categories.
