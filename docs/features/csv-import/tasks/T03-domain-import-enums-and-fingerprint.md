---
id: T03
title: "Add the four import enums and the MovementFingerprint value object"
layer: "domain"
deps: []
acs: []
files_hint: ["src/FinNavis.Domain/Enums/", "src/FinNavis.Domain/ValueObjects/MovementFingerprint.cs", "tests/FinNavis.Domain.UnitTests/ValueObjects/MovementFingerprintTests.cs"]
owner: "Andrey Simonenko"
estimate: "M"
status: "todo"
---

# T03 — Add the four import enums and the MovementFingerprint value object

## Why

The root of this feature's Domain ring. [sad.md §5](../sad.md) names four enums that both aggregates
need, and [ADR-0008](../adr/0008-detect-post-import-hand-edits-with-a-stored-content-fingerprint.md)
needs one hashing rule that the import and the undo can never disagree about.
[data-model.md](../data-model.md) § The content fingerprint states that rule field by field.

`acs` is empty: four enums and a hash function satisfy no acceptance criterion on their own. AC-18 —
the criterion the fingerprint exists for — is claimed by T05, which compares it, by T19, which
refuses on it, and by T25, which proves it still covers every editable field.

## What

**Four enums** in `src/FinNavis.Domain/Enums/`, values taken from
[data-model.md](../data-model.md):

- `ImportRunKind` — `Statement`, `HistoryMigration`. The two entry points of ADR-0005.
- `ImportRunStatus` — `Completed`, `Undone`. **No `Failed`** — a load that fails leaves no run row
  at all, which is what AC-22 rests on.
- `SetAsideReason` — `NoRuleCovers`, `TransferBetweenOwnAccounts`, `ForeignCurrency`. **Three
  values, not four**: an unknown account is not one, because AC-09 refuses the whole load.
- `MappingRuleTarget` — `Category`, `TransferBetweenOwnAccounts`. **Two, not three** — the
  "recorded with no category" rule was dropped at the data-model stage.

**`MovementFingerprint`** in `src/FinNavis.Domain/ValueObjects/`. SHA-256 over the UTF-8 bytes of
the joined field string, rendered as 64 lowercase hex characters. Two shapes, exactly as
[data-model.md](../data-model.md) § The content fingerprint lists them:

- transaction — `account_id` · `category_id` · `amount` · `occurred_on` · `note`
- transfer — `source_account_id` · `destination_account_id` · `amount` · `occurred_on`

Fields joined with `|` (U+007C). Formatting is part of the contract, not an implementation detail:
a `UUID` is the lowercase canonical `8-4-4-4-12` form, `amount` is
`decimal.ToString("F4", CultureInfo.InvariantCulture)`, `occurred_on` is `yyyy-MM-dd`, and `note` is
the stored text or the empty string when null.

## Definition of Done

- [ ] `MovementFingerprint` gives `12.5` and `12.5000` the same 64 hex characters — a unit test in
      `tests/FinNavis.Domain.UnitTests`
- [ ] A null note and an empty note hash the same, and a one-character change to any covered field
      changes the hash
- [ ] The hash is the same under a non-invariant current culture — set `CultureInfo.CurrentCulture`
      to one using a comma decimal separator and assert it does not move
- [ ] The four enums carry exactly the values above, and no fifth
- [ ] `src/FinNavis.Domain` still references no project and no package — check the `.csproj`, not
      just `tests/FinNavis.ArchitectureTests`
- [ ] `./build.sh test` is green

## Notes

- **The culture test is the one that matters.** `decimal.ToString("F4")` without
  `CultureInfo.InvariantCulture` renders `12,5000` under a Russian or German culture, and the undo
  would then disagree with the import on a machine configured differently from the one that loaded
  the file. [data-model.md](../data-model.md) names the formatting *"so that the import and the undo
  can never disagree"* — this test is what holds it.
- **The field list has a revisit rule.** [sad.md §11](../sad.md): *"the day a movement gains a field
  the owner can edit, this list has to grow with it"*. T07 adds the comment in the entity
  configuration that points the next reader at ADR-0008.
- `id` and `created_at` are deliberately not covered — they are immutable, so hashing them would add
  nothing.
- One root of three. T01, T02 and this task all start at once; T04, T05 and T06 fan out from here.
