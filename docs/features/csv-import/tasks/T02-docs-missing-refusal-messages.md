---
id: T02
title: "Add the three missing refusal messages to PRD §10"
layer: "docs"
deps: []
acs: []
files_hint: ["docs/features/csv-import/PRD.md", "docs/features/csv-import/contracts/openapi.yaml", "docs/features/csv-import/contracts/api-sync-report.md"]
owner: "Andrey Simonenko"
estimate: "S"
status: "todo"
---

# T02 — Add the three missing refusal messages to PRD §10

## Why

[api-sync-report.md](../contracts/api-sync-report.md) Section D parks three refusal messages that
were **drafted at the API-contract gate** rather than taken from the PRD: `Import.MappingProfileNotConfirmed`,
`Import.AlreadyUndone` and `Import.NotRecorded`. Their codes are solid — each traces to a
[sad.md §6](../sad.md) branch — but [PRD §10](../PRD.md) has no row for them, and
`CLAUDE.md` §API says the `message` is *"the refusal wording from the PRD acceptance criteria, not
invented"*.

`acs` is empty: a wording table satisfies no acceptance criterion. It blocks T20, which cannot
transcribe a message that does not exist yet.

## What

Decide the owner-facing sentence for each of the three, add a row per code to
[PRD §10](../PRD.md), and make the contract repeat it verbatim.

The three, with the branch each comes from:

| Code | Branch | What the owner is being told |
|---|---|---|
| `Import.MappingProfileNotConfirmed` | [sad.md §6](../sad.md) flow 2, first `alt` | No rules exist for this file's shape yet, so there is nothing to file its rows under |
| `Import.AlreadyUndone` | [sad.md §6](../sad.md) flow 6, first `alt` | This load was already undone, so there is nothing left to remove |
| `Import.NotRecorded` | [sad.md §6](../sad.md) flow 2, failing-commit branch | The load did not finish, so nothing from it was recorded (AC-22) |

Then update [openapi.yaml](../contracts/openapi.yaml) so each `example.message` carries the same
sentence, and strike the deferral row from
[api-sync-report.md](../contracts/api-sync-report.md) Section D.

## Definition of Done

- [ ] `PRD.md` §10 carries one row per code, written in the same plain voice as the seven rows
      already there
- [ ] `contracts/openapi.yaml` repeats each sentence word for word — one `code` in the contract,
      one message, no second wording
- [ ] The deferral row for the three drafted messages is struck from `api-sync-report.md` Section D
- [ ] No other PRD §10 row is reworded — AC-02's "with that name" stays verbatim, for the reason
      `api-sync-report.md` records under *Accepted as is*

## Notes

- **Write the sentence the owner reads, not the one a developer would.** PRD §10's existing rows set
  the voice: *"You do not have an account with that name, so there was nothing to load this file
  onto."* Short, says what happened and what it means.
- `.claude/rules/language.md`: the artifact is English, and identifiers stay as identifiers. The
  code is `Import.AlreadyUndone` in every document.
- This blocks T20 only. Nothing in the Domain or Infrastructure rings waits on it, so it can run any
  evening before the ports work starts.
