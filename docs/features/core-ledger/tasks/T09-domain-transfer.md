---
id: T09
title: "Add the Transfer entity and ITransferRepository"
layer: "domain"
deps: ["T05"]
acs: ["AC-05", "AC-06", "AC-23"]
files_hint: ["src/FinNavis.Domain/Entities/Transfer.cs", "src/FinNavis.Domain/Abstractions/ITransferRepository.cs", "tests/FinNavis.Domain.UnitTests/Entities/TransferTests.cs"]
owner: "Andrey Simonenko"
estimate: "S"
status: "todo"
---

# T09 — Add the `Transfer` entity and `ITransferRepository`

## Why

Derives from [PRD §AC-05, §AC-06, §AC-23](../PRD.md), [sad.md §6 flow 2](../sad.md),
[`data-model.md` → `transfers`](../data-model.md) and ADR-0001.

## What

- `Entities/Transfer` — identity, source account, destination account, amount, `occurred_on`.
- **Source and destination must differ** (AC-05). A domain invariant, refused before the row is
  ever built — not a `CHECK` constraint (`data-model.md` decision 6).
- Amount above zero, same rule as a transaction.
- **One amount for both sides, one entity.** AC-04 and AC-15 then hold by construction: the total
  across accounts cannot change. AC-06 is stronger than prevention — a half-applied transfer is not
  representable (data-model.md → `transfers`, sad.md §6 flow 2 note).
- **No category and no note.** A transfer carries no category (sad.md §12), which is what makes
  AC-23 a refusal rather than a validation rule: the field it would need does not exist.

## Definition of Done

- [ ] `Record_SameSourceAndDestination_IsRefused` passes with the AC-05 wording (AC-05)
- [ ] `Record_AmountZeroOrBelow_IsRefused` passes
- [ ] the entity exposes no way to set a category, so AC-23 is unrepresentable rather than
      validated (AC-23)
- [ ] one entity carries both sides, so no test can construct a half-applied transfer (AC-06)
- [ ] `./build.sh test` is green

## Notes

- Accepted debt from [sad.md §11](../sad.md): transfers may be withdrawn from the product later,
  and ADR-0001's separate-table shape keeps that removal down to one migration. It changes nothing
  here — US-02 commits to transfers with five acceptance criteria.
