---
id: T23
title: "Add the ImportRuns read and undo endpoints: Get and Undo"
layer: "ports"
deps: ["T13", "T18", "T19", "T20"]
acs: ["AC-13", "AC-16", "AC-18"]
files_hint: ["src/FinNavis.Presentation/Endpoints/ImportRuns/Get/", "src/FinNavis.Presentation/Endpoints/ImportRuns/Undo/", "src/FinNavis.Presentation/Program.cs"]
owner: "Andrey Simonenko"
estimate: "M"
status: "todo"
---

# T23 — Add the ImportRuns read and undo endpoints: Get and Undo

## Why

The two operations of US-04 and US-06 at the wire level, specified in
[openapi.yaml](../contracts/openapi.yaml) as `getImportRun` and `undoImportRun`.

Satisfies [AC-13](../PRD.md), [AC-16](../PRD.md) and [AC-18](../PRD.md).

## What

Two operation folders under `src/FinNavis.Presentation/Endpoints/ImportRuns/`, each with its own
records:

- `Get/` — `GET /api/v1/import-runs/{import_run_id}`. The counts, whether the run was undone, and
  its set-aside rows **paged by opaque cursor**, ordered by `row_number`, `limit` min 1, max 200,
  default 50. The page wrapper carries `{items, has_next, has_prev, next_cursor, prev_cursor}`.
- `Undo/` — `POST /api/v1/import-runs/{import_run_id}/undo`. Answers the run as `Undone` (AC-16).

The refusals: `Import.NotFound`, `Import.InvalidPaging`, `Import.MovementsChangedByHand` with
the changed rows in `details` (AC-18), and `Import.AlreadyUndone` on a second undo.

## Definition of Done

- [ ] Both operations match `contracts/openapi.yaml` on every request and response field, status
      code and error code
- [ ] Each operation owns its own records in its own folder, shared with nothing
- [ ] The set-aside list pages by **opaque cursor**, ordered by `row_number`, with `limit` min 1,
      max 200, default 50 — a client never builds a cursor, it passes back the one the server minted
      (AC-13)
- [ ] A malformed or unknown cursor returns `Import.InvalidPaging`
- [ ] An undo answers the run with status `Undone` (AC-16)
- [ ] A run whose movements were changed by hand returns `Import.MovementsChangedByHand` with the
      PRD §10 wording and the changed rows in `details`, and nothing is deleted (AC-18)
- [ ] A second undo returns `Import.AlreadyUndone`
- [ ] Every JSON key is snake_case, and no response is `application/problem+json`
- [ ] `./build.sh test` is green

## Notes

- **The read pages and the load does not, and both are right.**
  [api-sync-report.md](../contracts/api-sync-report.md) decision 2: the load answers in full because
  `CLAUDE.md` §API fixes that shape for a finished batch; the read pages because a history migration
  is ~1500 rows and each set-aside row carries up to 2000 characters of raw bank text, so an
  unpaged report would carry that weight on every visit.
- **The page size is a size budget, not a latency one.** PRD §6 carries no per-page number — only
  ≤ 60 s for a whole load and ≤ 30 s for a whole undo. `raw_line` is bounded at 2000 characters, so
  a default page is at most ~100 KB. The numbers match `core-ledger`'s history paging so a client
  uses one rule for both.
- **A set-aside row carries no id**, although `import_set_aside_rows.id` exists. Nothing addresses
  one — `row_number` is what locates the line in the file
  ([api-sync-report.md](../contracts/api-sync-report.md)).
- **The four link tables and the fingerprint stay off the API.** They are undo bookkeeping
  (ADR-0007, ADR-0008), and AC-13 asks for counts and set-aside rows, never for the list of
  movements a run wrote.
- **Shares a lane with T13, T21 and T22 on `src/FinNavis.Presentation/Program.cs`.**
