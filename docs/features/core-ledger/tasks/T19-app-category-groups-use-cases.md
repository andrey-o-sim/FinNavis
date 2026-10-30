---
id: T19
title: "Write the four category-group use cases"
layer: "app"
deps: ["T13"]
acs: ["AC-26", "AC-27", "AC-28", "AC-29", "AC-31"]
files_hint: ["src/FinNavis.Application/CategoryGroups/", "tests/FinNavis.Application.UnitTests/CategoryGroups/"]
owner: "Andrey Simonenko"
estimate: "M"
status: "done"
attempts: 1
---

# T19 — Write the four category-group use cases

## Why

Derives from [PRD US-09, §AC-26 to §AC-29, §AC-31](../PRD.md) and
[sad.md §6 flows 10 and 13](../sad.md). These are the operations the PRD amendment of 2026-10-20
added — the gap that stopped the first api-forge run, because nothing could produce a group to
name.

## What

`CategoryGroups/Create`, `CategoryGroups/List`, `CategoryGroups/Rename`, `CategoryGroups/Delete`.

- **Create** — a group on one named side of the ledger, holding no categories yet (AC-26). Refuses
  a name already taken **on that same side**, while the name stays free on the other side (AC-31).
- **List** — every group with its name and its side, groups holding no categories included
  (AC-27).
- **Rename** — refuses a taken name on that side (AC-31); otherwise applies it. The breakdown then
  reports the group under the new name and no category figure changes (AC-28).
- **Delete** — the one that needs care (AC-29). In **one** unit of work: load the categories
  pointing at the group, clear each category's group link, then remove the group row.

## Definition of Done

- [x] `CreateCategoryGroup_NamedSide_CreatedEmpty` passes (AC-26)
- [x] `ListCategoryGroups_ReturnsNameAndSide_IncludingEmptyGroups` passes (AC-27)
- [x] `RenameCategoryGroup_GroupWithCategories_KeepsEveryCategoryFigure` passes (AC-28)
- [x] `DeleteCategoryGroup_StillHoldingCategories_ClearsLinksAndRemovesGroup` passes — **no
      category is deleted** and every one keeps all of its recorded history (AC-29)
- [x] the clear-and-delete runs in one unit of work; a failure part way leaves the group and every
      link as they were
- [x] `CreateCategoryGroup_NameTakenOnSameSide_IsRefused` returns `CategoryGroup.DuplicateName`,
      and the same name on the other side is accepted (AC-31)
- [x] unit tests touch no database and no network

## Notes

- **Nothing is assigned to the Default group.** It is produced by the breakdown and never stored
  (sad.md §12, `data-model.md` decision 8). A category reaches it by having no group at all.
  [sad.md §6](../sad.md) is explicit: *"An implementer who creates a real `Default` row has misread
  it."*
- **Removing a group is allowed where removing an account (AC-09) or a category (AC-12) is
  refused.** Those two are named directly by recorded movements; a group is named by no movement
  (sad.md §5).
- **Settled at the api-forge gate:** the removal does **not** report how many categories were
  affected. `deleteCategoryGroup` returns `204` with no body. It was PRD §8's own stated default.
