---
id: T20
title: "Write the five category use cases"
layer: "app"
deps: ["T13", "T14"]
acs: ["AC-11", "AC-12", "AC-13", "AC-30", "AC-32", "AC-33", "AC-34", "AC-36"]
files_hint: ["src/FinNavis.Application/Categories/", "tests/FinNavis.Application.UnitTests/Categories/"]
owner: "Andrey Simonenko"
estimate: "L"
status: "done"
attempts: 1
---

# T20 — Write the five category use cases

## Why

Derives from [PRD US-04, US-10, §AC-11 to §AC-13, §AC-30, §AC-32 to §AC-34, §AC-36](../PRD.md) and
[sad.md §6 flows 6, 11, 12 and 13](../sad.md).

## What

`Categories/Create`, `Categories/List`, `Categories/Rename`, `Categories/MoveToGroup`,
`Categories/Delete`.

- **Create** — with or without a group (AC-11). If a group is named and it is not in the ledger,
  refuse with `CategoryGroup.NotFound`, name it back to the owner, and **do not create it
  implicitly** (AC-32). If the group is on the other side, the entity refuses (AC-30). Refuses a
  name already taken on that same side (AC-36).
- **List** — all categories, or only those in one named group. Each returned with its name, its
  side, and the group it currently sits in, if any (AC-33).
- **Rename** — refuses a taken name on that side (AC-36); otherwise applies it. Every recorded
  movement, balance and breakdown follows the new name, and the figure for a closed period is
  unchanged (AC-34).
- **MoveToGroup** — to another group, or out of every group (AC-13). Same AC-32 and AC-30 checks as
  create.
- **Delete** — asks `ILedgerQueries` whether any transaction names the category. If yes, **refuse**
  with `Category.HasRecordedHistory` (AC-12).

## Definition of Done

- [x] `CreateCategory_WithAndWithoutGroup_BecomesAvailableToName` passes (AC-11)
- [x] `DeleteCategory_NamedByATransaction_IsRefused` returns `Category.HasRecordedHistory` (AC-12)
- [x] `MoveCategoryToGroup_CategoryWithHistory_FigureUnchanged` passes, and moving out of every
      group works too (AC-13)
- [x] `PlaceCategory_GroupOnOtherSide_IsRefused` returns `Category.GroupKindMismatch` (AC-30)
- [x] `CreateCategory_UnknownGroup_IsRefusedAndCreatesNothing` returns `CategoryGroup.NotFound`
      (AC-32)
- [x] `ListCategories_AllAndNarrowedToOneGroup_ReturnsNameSideAndGroup` passes (AC-33)
- [x] `RenameCategory_CategoryWithHistory_KeepsClosedPeriodFigure` passes (AC-34)
- [x] `CreateCategory_NameTakenOnSameSide_IsRefused` returns `Category.DuplicateName`, and the same
      name on the other side is accepted (AC-36)
- [x] unit tests touch no database and no network

## Notes

- `Category.GroupNotFound` **does not exist.** It was merged into `CategoryGroup.NotFound` at
  api-forge run 3 — one refusal, one code. See `api-sync-report.md` Section B.2.
- The `*.NotFound` module convention is being settled by [T04](./T04-adr-notfound-module-convention.md).
  If it lands first, follow it.
- Largest use-case task — five operations. If it crosses a day, split the list/rename pair (US-10)
  from the create/move/delete trio (US-04); they have no shared state.
