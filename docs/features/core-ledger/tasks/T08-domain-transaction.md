---
id: T08
title: "Add the Transaction entity and ITransactionRepository"
layer: "domain"
deps: ["T05"]
acs: ["AC-02", "AC-21", "AC-24"]
files_hint: ["src/FinNavis.Domain/Entities/Transaction.cs", "src/FinNavis.Domain/Abstractions/ITransactionRepository.cs", "tests/FinNavis.Domain.UnitTests/Entities/TransactionTests.cs"]
owner: "Andrey Simonenko"
estimate: "M"
status: "done"
attempts: 1
---

# T08 — Add the `Transaction` entity and `ITransactionRepository`

## Why

Derives from [PRD §AC-01, §AC-02, §AC-21, §AC-24](../PRD.md),
[sad.md §6 flows 1, 4 and 9](../sad.md),
[`data-model.md` → `transactions`](../data-model.md), ADR-0001, ADR-0003 and ADR-0006.

## What

- `Entities/Transaction` — identity, one account, one category, amount, `occurred_on`, optional
  note, nullable `external_ref`.
- **Amount must be above zero** (AC-02). `data-model.md` decision 3: the amount is always positive
  and direction comes from the category's kind, so a stored negative would contradict the
  acceptance criterion it is meant to satisfy. No `CHECK` constraint — this is a domain invariant
  (`.claude/rules/migrations.md`).
- Note is optional and capped at **500 characters** (`data-model.md` decision 2, which also closes
  the storage-bloat abuse case in PRD §6.1). Stored and returned as plain text, never interpreted.
- `occurred_on` is a calendar day with no time and no timezone (`data-model.md` decision 1).
- Edit the amount in place (AC-21) — ADR-0003: the ledger is mutable, there is no reversing entry.
- **Refuse a second account** (AC-24): a transaction that touched two accounts would be a transfer,
  and the two are separate entities (ADR-0001). The refusal tells the owner the entry must be
  removed and recorded again as the other kind.
- `external_ref` is nullable and **always null in this feature** (ADR-0006). It ships unused so
  csv-import needs no migration against a table that by then holds real history.

## Definition of Done

- [ ] `Record_AmountZeroOrBelow_IsRefused` passes with the AC-02 wording (AC-02)
- [ ] `Record_NoteOverFiveHundredCharacters_IsRefused` passes
- [ ] `Edit_NewAmount_AppliedInPlace` passes, and no reversing entry is created (AC-21)
- [ ] `Edit_SecondAccount_IsRefused` passes with the AC-24 wording (AC-24)
- [ ] the entity has no `category_group` field — a transaction names its category and never its
      group (data-model.md)
- [ ] `./build.sh test` is green

## Notes

- AC-03 (unknown category) is **not** here. The entity cannot look up a category; flow 1 puts that
  in Application. [T21](./T21-app-transactions-use-cases.md) owns it.
- No `deleted_at` and no audit column beyond `created_at` (ADR-0003, data-model.md decision 5).
