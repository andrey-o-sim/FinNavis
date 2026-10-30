---
id: T21
title: "Write the three transaction use cases"
layer: "app"
deps: ["T13"]
acs: ["AC-01", "AC-03", "AC-21", "AC-22", "AC-24", "AC-25"]
files_hint: ["src/FinNavis.Application/Transactions/", "tests/FinNavis.Application.UnitTests/Transactions/"]
owner: "Andrey Simonenko"
estimate: "M"
status: "todo"
---

# T21 — Write the three transaction use cases

## Why

Derives from [PRD US-01, US-08, §AC-01 to §AC-03, §AC-21, §AC-22, §AC-24, §AC-25](../PRD.md) and
[sad.md §6 flows 1, 4 and 9](../sad.md).

## What

`Transactions/Record`, `Transactions/Edit`, `Transactions/Remove`.

- **Record** — loads the named account and category. If either is not in the ledger, refuse, name
  what was not found, and **create nothing — not even the unknown category** (AC-03). Otherwise
  build the `Transaction`, add it, commit. The note is kept with the entry (AC-01). The id is
  minted with `Guid.CreateVersion7()` in this layer (ADR-0005).
- **Edit** — change the amount, date, category or note in place (AC-21, ADR-0003). No reversing
  entry. Refuse a second account with `Transaction.KindChangeNotAllowed`, telling the owner the
  entry must be removed and recorded again as the other kind (AC-24).
- **Remove** — a hard delete (AC-22, ADR-0003). Every balance it affected returns to what it would
  have been, which is free: nothing is stored, so each balance follows on the next read.
- An entry that is not part of this ledger is refused as unknown, and nothing is created (AC-25).

## Definition of Done

- [ ] `RecordTransaction_ValidInput_RecordedWithNote` passes, and the account balance falls by the
      amount on the next read (AC-01)
- [ ] `RecordTransaction_UnknownCategory_IsRefusedAndCreatesNothing` returns
      `Category.NotFound`, and no category row appears (AC-03)
- [ ] `EditTransaction_NewAmount_AppliedInPlace` passes with no reversing entry (AC-21)
- [ ] `RemoveTransaction_Entry_DisappearsFromHistory` passes (AC-22)
- [ ] `EditTransaction_SecondAccount_IsRefused` returns `Transaction.KindChangeNotAllowed` with the
      AC-24 wording (AC-24)
- [ ] an unknown transaction id returns `Transaction.NotFound` and changes nothing (AC-25)
- [ ] `external_ref` is left **null** on every write (ADR-0006)
- [ ] unit tests touch no database and no network

## Notes

- AC-02 (amount above zero) is enforced inside the entity
  ([T08](./T08-domain-transaction.md)); this layer only surfaces the refusal.
- The owner's way through AC-24 is: remove the entry, then record it as a transfer
  ([sad.md §6 flow 9](../sad.md)).
