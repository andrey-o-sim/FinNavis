---
id: T20
title: "Add the Import and MappingProfile error codes to the registry"
layer: "ports"
deps: ["T02", "T04", "T05"]
acs: ["AC-02", "AC-03", "AC-09", "AC-18"]
files_hint: ["src/FinNavis.Application/Import/ImportErrors.cs", "tests/FinNavis.Infrastructure.IntegrationTests/Errors/"]
owner: "Andrey Simonenko"
estimate: "M"
status: "todo"
---

# T20 — Add the Import and MappingProfile error codes to the registry

## Why

`CLAUDE.md` §API: *"One `code` in a contract equals one constant in the error registry, returning
the same string."* [api-sync-report.md](../contracts/api-sync-report.md) Section D parks exactly
that as a deferral — the codes have no definition in `src/` because `src/` is a skeleton.
This task closes it, and every endpoint task in the epic depends on it.

Satisfies [AC-02](../PRD.md), [AC-03](../PRD.md), [AC-09](../PRD.md) and [AC-18](../PRD.md) — the
four refusals whose wording PRD §10 fixes.

## What

There is no central registry class, and this task does not add one. `core-ledger` keeps each code
where it is raised, and this feature already follows it:

- **A code a Domain rule raises lives in that entity.** `MappingProfile.InvalidRule`,
  `DuplicateSourceCategory` and `ShapeKeyInvalid` are in `MappingProfile.cs` and `MappingRule.cs`.
  `Import.AlreadyUndone` and `Import.MovementsChangedByHand` are in `ImportRun.cs`.
- **A code a use case raises lives in `src/FinNavis.Application/Import/ImportErrors.cs`**, the
  same shape as `core-ledger`'s `CategoryErrors`. T15–T19 already created it with `Import.NotFound`,
  `Import.MissingRequiredColumn`, `Import.MappingProfileNotConfirmed`, `Import.NotRecorded`,
  `Account.NotFound` and `Category.NotFound`.

So eleven of the thirteen codes exist before this task starts. The work is:

1. Add the two that are missing, **`Import.FileNotReadable`** and **`Import.InvalidPaging`**, to
   `ImportErrors`, and raise them where the refusal happens. Do not create a second `ImportErrors`
   or a `MappingProfileErrors` class, and do not move a code out of a Domain entity — a code
   defined in two places can drift.
2. Add the test that compares every code the feature raises with the list in the contract.
3. Add the test that the `problem+json` switch-off still holds for this feature's routes.

The thirteen codes, read from [openapi.yaml](../contracts/openapi.yaml):

| `MappingProfile` | `Import` | `Account` | `Category` |
|---|---|---|---|
| `InvalidRule` | `NotFound` · `MappingProfileNotConfirmed` | `NotFound` | `NotFound` |
| `DuplicateSourceCategory` | `MovementsChangedByHand` · `AlreadyUndone` | | |
| `ShapeKeyInvalid` | `MissingRequiredColumn` · `FileNotReadable` | | |
| | `InvalidPaging` · `NotRecorded` | | |

`Account.NotFound` and `Category.NotFound` name the aggregate that was not found, not the one the
request worked on, so they are the same codes `core-ledger` already returns
([docs/adr/0004](../../../adr/0004-name-the-not-found-aggregate-in-the-module.md)). They are not
new constants of this feature. `Import.NotFound` is the import run itself, because the run is the
`Import` aggregate.

Each message is the PRD §10 wording — the seven rows already there, plus the three T02 adds. None is
invented here.

## Definition of Done

- [ ] All thirteen codes return exactly the strings `contracts/openapi.yaml` advertises — a test
      compares the two lists rather than a reading
- [ ] Every code matches `^[A-Z][A-Za-z0-9]*\.[A-Z][A-Za-z0-9]*$`
- [ ] Every message is the PRD §10 sentence word for word (AC-02, AC-03, AC-09, AC-18)
- [ ] A refusal answers the flat `{code, message, details?}` envelope, and **no response from this
      feature is `application/problem+json`** — including one that never reaches a handler, such as
      a routing 404
- [ ] Every JSON key, including every key inside `details`, is snake_case — the code is the one
      PascalCase value in the body
- [ ] `./build.sh test` is green

## Notes

- **`core-ledger` T24 owns the envelope and the `problem+json` switch-off.** This task adds two codes
  to `ImportErrors`, which already exists, and **asserts the switch-off held**. Do not turn it off a second
  time — if it is still on, that is a `core-ledger` defect, not work for this epic.
- **The `problem+json` test is the one worth writing carefully.**
  [`.claude/rules/openapi.md`](../../../../.claude/rules/openapi.md) §5: a client that switches on
  `code` breaks on exactly the requests that never reached a handler. Test a routing 404 and a
  model-binding failure, not only a handler refusal.
- **`Import.MissingRequiredColumn` is AC-03 and `Import.FileNotReadable` is the PRD §6.1 abuse
  case** — a file that is not a statement. They are two codes because they are two different things
  going wrong.
- **Three of the thirteen had no PRD §10 row until T02 landed.** That is the dependency.
