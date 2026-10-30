---
id: T22
title: "Add the ImportRuns load endpoints: LoadStatement and MigrateHistory"
layer: "ports"
deps: ["T13", "T16", "T17", "T20"]
acs: ["AC-05", "AC-09", "AC-13", "AC-22"]
files_hint: ["src/FinNavis.Presentation/Endpoints/ImportRuns/LoadStatement/", "src/FinNavis.Presentation/Endpoints/ImportRuns/MigrateHistory/", "src/FinNavis.Presentation/Program.cs"]
owner: "Andrey Simonenko"
estimate: "L"
status: "todo"
---

# T22 — Add the ImportRuns load endpoints: LoadStatement and MigrateHistory

## Why

The two write operations of the feature, specified in [openapi.yaml](../contracts/openapi.yaml) as
`loadStatement` and `migrateHistory`. This is where
[`CLAUDE.md`](../../../../CLAUDE.md) §API's batch rule becomes concrete: a load that finished is a
`200` with counts and per-item reasons, never an error.

Satisfies [AC-05](../PRD.md), [AC-09](../PRD.md), [AC-13](../PRD.md) and [AC-22](../PRD.md) at the
wire level.

## What

Two operation folders under `src/FinNavis.Presentation/Endpoints/ImportRuns/`, each with its own
request and response records:

- `LoadStatement/` — `POST /api/v1/import-runs/statement`, multipart: the file plus the account the
  owner names.
- `MigrateHistory/` — `POST /api/v1/import-runs/history-migration`, multipart: the file alone.

Both answer **`200`** with `recorded_row_count`, `already_present_row_count` and the **full**
`set_aside` list, each entry `{row_number, reason, source_category, raw_line}`
([api-sync-report.md](../contracts/api-sync-report.md), decision 3). The load answers in full
because `CLAUDE.md` §API fixes that shape for a batch that finished; only the *read* in T23 pages
it.

The refusals that stop a whole load: `Account.NotFound` (AC-09),
**`409` `Import.MappingProfileNotConfirmed`**, `Import.MissingRequiredColumn`,
`Import.FileNotReadable`, and `Import.NotRecorded` when the commit failed (AC-22).

## Definition of Done

- [ ] Both operations match `contracts/openapi.yaml` on every request and response field, status
      code and error code
- [ ] Each operation owns its own records in its own folder — **not shared**, even though the two
      responses serialise to the same shape (`CLAUDE.md` §API)
- [ ] A finished load answers `200` with the counts and every set-aside row with its reason, in
      full (AC-05, AC-13) — a load that set rows aside is **not** an error and not a `207`
- [ ] An unknown named account returns `Account.NotFound` with the PRD §10 wording and nothing
      created (AC-09)
- [ ] A file whose shape has no confirmed rules returns **`409`**, not `404`
      ([api-sync-report.md](../contracts/api-sync-report.md), decision 4)
- [ ] A load that failed part-way returns `Import.NotRecorded` and nothing is in the ledger (AC-22)
- [ ] `ImportRunId` is in the body; there is **no `Location` header** and no `201`
- [ ] Every JSON key is snake_case, and no response is `application/problem+json`
- [ ] `./build.sh test` is green

## Notes

- **`200` where `core-ledger` would answer `201`.**
  [api-sync-report.md](../contracts/api-sync-report.md) flags this as the one place the contract
  looks unusual next to `core-ledger`, which answers `201` for every create. It is correct:
  `.claude/rules/openapi.md` §6 fixes a finished batch as a `200` with counts and per-item reasons.
- **`409`, not `404`, for an unconfirmed shape.** The request is well-formed and the client named no
  profile — the server derived the shape from the file. The client's next move is
  inspect-then-confirm, not "fix the id you sent".
- **Shares a lane with T13, T21 and T23 on `src/FinNavis.Presentation/Program.cs`.**
- **Three quality gates depend on this task** — T24, T25 and T26 all drive the loads through it.
  It is the widest `ports` task in the epic and deliberately over the usual line, because multipart
  intake plus the full batch report is one coherent change.
- **No money field anywhere.** A load reports counts and raw lines; balances follow from the
  movements (`core-ledger` ADR-0002).
