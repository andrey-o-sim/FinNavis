---
id: T06
title: "Add the Account entity and IAccountRepository"
layer: "domain"
deps: ["T05"]
acs: ["AC-08"]
files_hint: ["src/FinNavis.Domain/Entities/Account.cs", "src/FinNavis.Domain/Abstractions/IAccountRepository.cs", "tests/FinNavis.Domain.UnitTests/Entities/AccountTests.cs"]
owner: "Andrey Simonenko"
estimate: "S"
status: "done"
attempts: 1
---

# T06 — Add the Account entity and `IAccountRepository`

## Why

Derives from [PRD §AC-08](../PRD.md), [sad.md §5](../sad.md) (five aggregates, five repository
ports) and [sad.md §6 flow 5](../sad.md). The entity is where the name rule lives;
[`data-model.md`](../data-model.md) bounds it at `VARCHAR(100)`.

## What

- `Entities/Account` — identity plus a name. Create and rename. **Rename keeps the same identity**,
  which is what makes AC-08 free: every movement points at the account by id, not by name.
- A blank name, or one over 100 characters, is refused with the domain error that maps to
  `Account.NameInvalid`.
- `Abstractions/IAccountRepository` — the repository port. It belongs in Domain because a
  repository is a collection-like view of an aggregate (`CLAUDE.md`, `ARCHITECTURE.md`).

## Definition of Done

- [ ] `Rename_ValidName_KeepsSameIdentity` passes (AC-08)
- [ ] `Create_NameOverHundredCharacters_IsRefused` and the blank-name case pass with
      `Account.NameInvalid`
- [ ] **No balance field on the entity.** AC-14 is computed at read time (ADR-0002), so a stored
      balance would have nothing to keep it honest
- [ ] the port is declared in `Domain/Abstractions` and Domain still references nothing
- [ ] `./build.sh test` is green

## Notes

- **Name uniqueness (AC-35) is not here.** [sad.md §6 flow 13](../sad.md) puts the existence check
  in the Application layer, backed by the unique index as the last guard. The entity cannot see
  other accounts. [T18](./T18-app-accounts-use-cases.md) owns AC-35.
- No `owner_id` column anywhere — one owner, no sign-in, records carry no owner field
  (PRD §3, sad.md §12).
