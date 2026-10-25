---
id: T04
title: "Add the MappingProfile aggregate, its rules and IMappingProfileRepository"
layer: "domain"
deps: ["T03"]
acs: ["AC-01", "AC-04"]
files_hint: ["src/FinNavis.Domain/Entities/MappingProfile.cs", "src/FinNavis.Domain/Entities/MappingRule.cs", "src/FinNavis.Domain/Abstractions/IMappingProfileRepository.cs", "tests/FinNavis.Domain.UnitTests/Entities/MappingProfileTests.cs"]
owner: "Andrey Simonenko"
estimate: "M"
status: "todo"
---

# T04 — Add the MappingProfile aggregate, its rules and IMappingProfileRepository

## Why

One of the two aggregates [sad.md §5](../sad.md) declares: *"the rules remembered for one shape of
file, keyed by the shape itself"*. It is a separate aggregate **because of AC-04** — changing a rule
must leave recorded rows exactly as they were, which is only true if no recorded row points at a
rule. Columns and bounds come from [data-model.md](../data-model.md) `mapping_profiles` and
`mapping_profile_rules`; the shape key is
[ADR-0009](../adr/0009-recognise-a-statement-shape-by-its-header-row.md).

Satisfies [AC-01](../PRD.md) (the rules are remembered for a shape) and [AC-04](../PRD.md) (changing
a rule changes nothing recorded).

## What

**`MappingProfile`** — aggregate root. Carries `Id`, `ShapeKey` (≤ 1000 characters, the normalised
header row) and its rules. **No `name` column** — ADR-0009 rejected it: a name that takes no part in
the lookup can only go stale.

**`MappingRule`** — child, no life outside its profile and **no port of its own**
(`CLAUDE.md`: one port per aggregate, added when a use case needs it). Carries `SourceCategory`
(≤ 200 characters, the file's own category text as the file writes it), `Target`
(`MappingRuleTarget`) and `TargetCategoryId`.

The invariants, all refused inside the entity rather than by a `CHECK` constraint:

- `TargetCategoryId` is set **exactly when** `Target = Category`, and null for
  `TransferBetweenOwnAccounts`, which records nothing.
- Two rules cannot name the same `SourceCategory` inside one profile.
- Re-confirming a shape **replaces its rules wholesale** — the profile keeps its identity and its
  id, and the old rules go.

**`IMappingProfileRepository`** in `Domain/Abstractions`: find one profile by shape key, add one,
replace its rules. The entity never fetches anything itself.

## Definition of Done

- [ ] A rule with `Target = Category` and no target category is refused, and one with
      `Target = TransferBetweenOwnAccounts` and a target category is refused too — unit tests in
      `tests/FinNavis.Domain.UnitTests`
- [ ] Adding a second rule for a source category the profile already covers is refused
- [ ] Replacing a profile's rules keeps the profile's `Id` and `ShapeKey` and drops every old rule
      (AC-01)
- [ ] No type in this task gives a `Transaction` or a `Transfer` any way to point back at a rule —
      the structural half of AC-04
- [ ] A blank shape key and one over 1000 characters are both refused
- [ ] `src/FinNavis.Domain` still references no project and no package
- [ ] `./build.sh test` is green

## Notes

- **AC-04 is proved by an absence, which is easy to lose.** Nothing recorded knows a rule exists —
  [sad.md §5](../sad.md). The test for it is a structural one: assert that no movement type reaches a
  rule. The day someone adds a `MappingRuleId` to a link table for convenience, AC-04 breaks and no
  behavioural test notices.
- **`source_category` is the file's own text, not the owner's.** `MappingProfile` maps *from* what
  the bank writes *to* what the owner keeps. Do not normalise it beyond what the shape key already
  does.
- **Nothing deletes a profile.** ADR-0009 records that profiles accumulate silently and accepts it,
  so the repository port needs no delete.
- The `target_category_id` foreign key reaches into `core-ledger`'s `categories`. That is T07's
  concern, not this one, but it is the reason `data-model.md` carries a finding: deleting a category
  a rule targets is now blocked by PostgreSQL and has no refusal message.
