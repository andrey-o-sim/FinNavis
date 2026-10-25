---
id: T20
title: "Add the Import and MappingProfile error codes to the registry"
layer: "ports"
deps: ["T02", "T04", "T05"]
acs: ["AC-02", "AC-03", "AC-09", "AC-18"]
files_hint: ["src/FinNavis.Presentation/Errors/", "tests/FinNavis.Infrastructure.IntegrationTests/Errors/"]
owner: "Andrey Simonenko"
estimate: "M"
status: "todo"
---

# T20 — Add the Import and MappingProfile error codes to the registry

## Why

`CLAUDE.md` §API: *"One `code` in a contract equals one constant in the error registry, returning
the same string."* [api-sync-report.md](../contracts/api-sync-report.md) Section D parks exactly
that as a deferral — the twelve codes have no definition in `src/` because `src/` is a skeleton.
This task closes it, and every endpoint task in the epic depends on it.

Satisfies [AC-02](../PRD.md), [AC-03](../PRD.md), [AC-09](../PRD.md) and [AC-18](../PRD.md) — the
four refusals whose wording PRD §10 fixes.

## What

Two classes in `src/FinNavis.Presentation/Errors/`, next to the registry `core-ledger` created:
**`MappingProfileErrors`** and **`ImportErrors`**. The class is the module, the member is the name,
so `ImportErrors.AccountNotFound` returns `"Import.AccountNotFound"`
([docs/adr/0003](../../../adr/0003-spell-an-error-code-in-pascalcase.md)).

The twelve codes, read from [openapi.yaml](../contracts/openapi.yaml):

| `MappingProfile` | `Import` |
|---|---|
| `InvalidRule` | `AccountNotFound` · `MappingProfileNotConfirmed` |
| `CategoryNotFound` | `MovementsChangedByHand` · `AlreadyUndone` |
| `DuplicateSourceCategory` | `MissingRequiredColumn` · `FileNotReadable` |
| | `RunNotFound` · `InvalidPaging` · `NotRecorded` |

Each message is the PRD §10 wording — the seven rows already there, plus the three T02 adds. None is
invented here.

## Definition of Done

- [ ] Twelve constants return exactly the strings `contracts/openapi.yaml` advertises — a test
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

- **`core-ledger` T24 owns the envelope and the `problem+json` switch-off.** This task adds codes to
  a registry that already exists and **asserts the switch-off held**. Do not turn it off a second
  time — if it is still on, that is a `core-ledger` defect, not work for this epic.
- **The `problem+json` test is the one worth writing carefully.**
  [`.claude/rules/openapi.md`](../../../../.claude/rules/openapi.md) §5: a client that switches on
  `code` breaks on exactly the requests that never reached a handler. Test a routing 404 and a
  model-binding failure, not only a handler refusal.
- **`Import.MissingRequiredColumn` is AC-03 and `Import.FileNotReadable` is the PRD §6.1 abuse
  case** — a file that is not a statement. They are two codes because they are two different things
  going wrong.
- **Three of the twelve have no PRD §10 row until T02 lands.** That is the dependency.
