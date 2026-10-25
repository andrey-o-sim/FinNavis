---
id: T22
title: "Write the three transfer use cases"
layer: "app"
deps: ["T13"]
acs: ["AC-04", "AC-05", "AC-06", "AC-23", "AC-25"]
files_hint: ["src/FinNavis.Application/Transfers/", "tests/FinNavis.Application.UnitTests/Transfers/"]
owner: "Andrey Simonenko"
estimate: "M"
status: "todo"
---

# T22 — Write the three transfer use cases

## Why

Derives from [PRD US-02, US-08, §AC-04 to §AC-06, §AC-23, §AC-25](../PRD.md) and
[sad.md §6 flows 2 and 4](../sad.md).

## What

`Transfers/Record`, `Transfers/Edit`, `Transfers/Remove`.

- **Record** — loads both accounts. If either is missing, refuse with
  `Transfer.SourceAccountNotFound` or `Transfer.DestinationAccountNotFound` (AC-25). Build the
  `Transfer`; the entity refuses a same-account transfer (AC-05). Add and commit in one unit of
  work: if the commit fails, neither account changes (AC-06).
- The source falls and the destination rises by the same amount, and the total across all accounts
  is unchanged (AC-04) — which holds by construction, because one row carries both sides.
- **Edit** — change the accounts, amount or date in place (ADR-0003). **Refuse any attempt to give
  it a category** with `Transfer.CategoryNotAllowed`, telling the owner transfers carry no category
  (AC-23).
- **Remove** — a hard delete; both balances follow on the next read.

## Definition of Done

- [ ] `RecordTransfer_TwoAccounts_SourceFallsDestinationRises_TotalUnchanged` passes (AC-04)
- [ ] `RecordTransfer_SameAccountBothSides_IsRefused` returns `Transfer.SameAccount` with the AC-05
      wording (AC-05)
- [ ] `RecordTransfer_CommitFails_NeitherAccountChanges` passes — the transfer is never applied to
      one side only (AC-06)
- [ ] `EditTransfer_GivenACategory_IsRefused` returns `Transfer.CategoryNotAllowed` with the AC-23
      wording (AC-23)
- [ ] an unknown transfer or account id returns the matching `*.NotFound` and changes nothing
      (AC-25)
- [ ] unit tests touch no database and no network

## Notes

- `api-sync-report.md` Section D records that [sad.md §6 flow 2](../sad.md) draws **no not-found
  branch**, while the contract already returns both account `*.NotFound` codes. **Follow the
  contract** — it is the source of truth for the HTTP surface
  (`.claude/rules/openapi.md`). Closing the diagram gap was deliberately left out of this epic;
  run `/finnavis-complete-sequence-diagrams core-ledger --flow "record a transfer"` separately.
- A half-applied transfer is **not representable** (one row, data-model.md), so AC-06 is stronger
  than a rollback guarantee.
