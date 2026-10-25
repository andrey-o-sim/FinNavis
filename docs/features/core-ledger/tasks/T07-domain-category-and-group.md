---
id: T07
title: "Add the Category and CategoryGroup entities with the same-side invariant"
layer: "domain"
deps: ["T05"]
acs: ["AC-13", "AC-28", "AC-29", "AC-30", "AC-34"]
files_hint: ["src/FinNavis.Domain/Entities/Category.cs", "src/FinNavis.Domain/Entities/CategoryGroup.cs", "src/FinNavis.Domain/Abstractions/ICategoryRepository.cs", "src/FinNavis.Domain/Abstractions/ICategoryGroupRepository.cs", "tests/FinNavis.Domain.UnitTests/Entities/CategoryTests.cs", "tests/FinNavis.Domain.UnitTests/Entities/CategoryGroupTests.cs"]
owner: "Andrey Simonenko"
estimate: "M"
status: "todo"
---

# T07 — Add the `Category` and `CategoryGroup` entities with the same-side invariant

## Why

Derives from [PRD §AC-13, §AC-28, §AC-29, §AC-30, §AC-34](../PRD.md),
[sad.md §5](../sad.md) (*"A group belongs to one side of the ledger, exactly as a category does"*),
[sad.md §6 flows 6, 10, 11 and 12](../sad.md), and
[`data-model.md` decisions 7 and 8](../data-model.md).

The two entities ship together because AC-30 is a rule **between** them, and a `CategoryGroup`
alone cannot be tested against it.

## What

- `Entities/CategoryGroup` — identity, name (≤ 100 chars), `CategoryKind`. Create and rename;
  rename keeps identity (AC-28).
- `Entities/Category` — identity, name (≤ 100 chars), `CategoryKind`, an **optional** group link.
  Create, rename (AC-34), place in a group, move between groups, and clear the link (AC-13).
- **The same-side invariant (AC-30) is refused by the entity, not by the schema.**
  [`data-model.md` decision 7](../data-model.md) weighed a composite FK and rejected it: it would
  cost a second unique index no query reads, and it would freeze a group's `kind`. The entity
  refuses the placement instead, so the owner gets the acceptance criterion's wording rather than
  an FK violation.
- Clearing the link is a plain operation on `Category` — this is the domain half of AC-29.
- Both repository ports in `Domain/Abstractions`. `ICategoryGroupRepository` exists because US-09
  makes the group lifecycle a job of its own (sad.md §5).

## Definition of Done

- [ ] `PlaceInGroup_GroupOnOtherSide_IsRefused` passes with the AC-30 refusal wording (AC-30)
- [ ] `MoveToGroup_CategoryWithHistory_KeepsSameIdentity` passes — no transaction row is rewritten,
      because a transaction names its category and never its group (AC-13)
- [ ] `Rename_ValidName_KeepsSameIdentity` passes on both entities (AC-28, AC-34)
- [ ] `ClearGroup_CategoryKeepsIdentityAndKind` passes (AC-29, domain half)
- [ ] **nothing creates a `Default` group object.** The Default group is produced by the breakdown
      and is never stored (sad.md §12, data-model.md decision 8)
- [ ] `./build.sh test` is green

## Notes

- Hard rule from [sad.md §6 note](../sad.md): *"An implementer who creates a real `Default` row has
  misread it."* The same applies to a `Default` entity here.
- Name uniqueness per side (AC-31, AC-36) is **not** here — flow 13 puts it in Application, and the
  entity cannot see its siblings. [T19](./T19-app-category-groups-use-cases.md) and
  [T20](./T20-app-categories-use-cases.md) own those.
