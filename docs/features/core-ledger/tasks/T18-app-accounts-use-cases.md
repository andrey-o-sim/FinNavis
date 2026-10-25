---
id: T18
title: "Write the four account use cases"
layer: "app"
deps: ["T13", "T14"]
acs: ["AC-07", "AC-08", "AC-09", "AC-10", "AC-25", "AC-35"]
files_hint: ["src/FinNavis.Application/Accounts/", "tests/FinNavis.Application.UnitTests/Accounts/"]
owner: "Andrey Simonenko"
estimate: "M"
status: "todo"
---

# T18 — Write the four account use cases

## Why

Derives from [PRD §AC-07 to §AC-10, §AC-25, §AC-35](../PRD.md) and
[sad.md §6 flows 5, 7 and 13](../sad.md). Business areas sit at the **root** of the Application
project — `Accounts/`, with no `Features/` wrapper (`CLAUDE.md`, sad.md §5).

## What

`Accounts/Create`, `Accounts/Rename`, `Accounts/Delete`, `Accounts/GetBalances`.

- **Create** — mints the id with `Guid.CreateVersion7()` in the Application layer
  ([ADR-0005](../adr/0005-use-uuid-v7-primary-keys-generated-in-the-application-layer.md)). Refuses
  a name already taken (AC-35), using the existence check from
  [T13](./T13-infra-repositories-and-unit-of-work.md), scoped exactly as the unique index is.
- **Rename** — refuses a taken name (AC-35); otherwise applies it. Identity is unchanged, so every
  movement, balance and breakdown follows the new name with nothing rewritten (AC-08).
- **Delete** — asks `ILedgerQueries` whether any movement names the account. If yes, **refuse**
  with `Account.HasRecordedHistory` (AC-09). If no, remove it (AC-10).
- **GetBalances** — a balance per account and one total across all of them. Refuses an unknown
  account id as `Account.NotFound` and invents no balance (AC-25, read side).

## Definition of Done

- [ ] `CreateAccount_NewName_CreatedWithZeroBalance` passes — a new account reads back with a
      balance of zero (AC-07)
- [ ] `RenameAccount_AccountWithHistory_KeepsEveryMovement` passes (AC-08)
- [ ] `DeleteAccount_WithRecordedHistory_IsRefused` returns `Account.HasRecordedHistory` with the
      PRD's refusal wording (AC-09)
- [ ] `DeleteAccount_WithNoMovement_IsRemoved` passes (AC-10)
- [ ] `CreateAccount_NameAlreadyTaken_IsRefused` and the rename case both return
      `Account.DuplicateName` (AC-35)
- [ ] an unknown account id returns `Account.NotFound` and creates nothing (AC-25)
- [ ] unit tests touch no database and no network (`CLAUDE.md` Tests)
- [ ] `CancellationToken` is passed through every async call (`CLAUDE.md`)

## Notes

- **Refusal wording comes from the PRD acceptance criteria, not invented** (sad.md §8,
  `CLAUDE.md` §API).
- There is no plain "list accounts" operation — `getAccountBalances` covers AC-07's *"appears in
  the account list"*. Recorded in `api-sync-report.md` Section D.
- The unique index is the **last** guard, not the first: a race fails at the database instead of
  creating a duplicate (sad.md §6 flow 13).
