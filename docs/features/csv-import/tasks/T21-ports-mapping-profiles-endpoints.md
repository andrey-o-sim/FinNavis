---
id: T21
title: "Add the MappingProfiles endpoints: Inspect and Confirm"
layer: "ports"
deps: ["T13", "T15", "T20"]
acs: ["AC-01", "AC-02", "AC-03"]
files_hint: ["src/FinNavis.Presentation/Endpoints/MappingProfiles/", "src/FinNavis.Presentation/Program.cs"]
owner: "Andrey Simonenko"
estimate: "M"
status: "todo"
---

# T21 — Add the MappingProfiles endpoints: Inspect and Confirm

## Why

The HTTP surface of US-01, specified operation by operation in
[openapi.yaml](../contracts/openapi.yaml): `inspectFile` and `confirmMappingProfile`.
[sad.md §5](../sad.md) puts both under `MappingProfiles/` rather than `ImportRuns/`, because
`Inspect/` exists only to feed `Confirm/` and neither records anything.

Satisfies [AC-01](../PRD.md), [AC-02](../PRD.md) and [AC-03](../PRD.md) at the wire level.

## What

Two operation folders under `src/FinNavis.Presentation/Endpoints/MappingProfiles/`, each holding
its endpoint plus the request and response records **only that operation uses**:

- `Inspect/` — `POST /api/v1/mapping-profiles/inspect`, multipart. Answers the file's own
  categories and whether rules already exist for its shape.
- `Confirm/` — `POST /api/v1/mapping-profiles`, carrying the `shape_key` and the rules.
  **`201` on a new shape, `200` on a re-confirm** —
  [api-sync-report.md](../contracts/api-sync-report.md) records that two success codes on one
  operation is the honest shape here.

Handlers follow the contract, never the other way round
([`.claude/rules/openapi.md`](../../../../.claude/rules/openapi.md)).

## Definition of Done

- [ ] Both operations match `contracts/openapi.yaml` on every request and response field, status
      code and error code
- [ ] Each operation owns its own records in its own folder, shared with no other operation
      (`CLAUDE.md` §API) — generated per folder from a single `$ref`'d schema
- [ ] Confirm answers `201` on a new shape and `200` on a re-confirm, and the rules are found by
      shape key afterwards (AC-01)
- [ ] A rule pointing at an unknown category returns the flat envelope with
      `code: Category.NotFound` and the PRD §10 wording, with the id and the file's
      own category text in `details` (AC-02)
- [ ] A file naming no date or amount returns `Import.MissingRequiredColumn` naming which (AC-03)
- [ ] Every JSON key is snake_case, including inside `details`
- [ ] No response is `application/problem+json`
- [ ] `./build.sh test` is green

## Notes

- **Shares a lane with T13, T22 and T23 on `src/FinNavis.Presentation/Program.cs`**, where the route
  registration lands. T13 is upstream; T22 and T23 are siblings on the same lane.
- **`security: []` and no `401` or `403`.** There is no sign-in
  ([`.claude/rules/openapi.md`](../../../../.claude/rules/openapi.md) §1), so a `404` here is a
  plain "not found", not existence-hiding — there is nobody to hide from.
- **Confirm carries the `shape_key`, not the file.**
  [api-sync-report.md](../contracts/api-sync-report.md) decision 1: ADR-0001 keeps nothing between
  the two calls, and the shape key is the readable normalised header row, not an upload id. Two
  uploads per new bank, which is exactly what ADR-0001 priced.
- **No money field appears anywhere in this resource**, so the money-as-a-string rule has nothing to
  apply to here. Recorded so the next reader does not think it was forgotten.
