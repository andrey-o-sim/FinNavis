---
id: T30
title: "Add the history and breakdown endpoints"
layer: "ports"
deps: ["T23", "T24", "T17"]
acs: ["AC-16", "AC-17", "AC-18", "AC-19", "AC-20"]
files_hint: ["src/FinNavis.Presentation/Endpoints/Reporting/"]
owner: "Andrey Simonenko"
estimate: "M"
status: "todo"
---

# T30 — Add the history and breakdown endpoints

## Why

Derives from [`contracts/openapi.yaml`](../contracts/openapi.yaml) operations `getHistory` and
`getBreakdown`, and [PRD US-06, US-07](../PRD.md). `getBreakdown` is the single request that
answers *"where did my money go"* — the gap PRD §1 opens with.

## What

- `Endpoints/Reporting/GetHistory/` — `GET /api/v1/history`
- `Endpoints/Reporting/GetBreakdown/` — `GET /api/v1/reports/breakdown`

Contract details that are already settled — do not re-decide them:

- **Cursor paging only**, never offset. The page wrapper carries `items`, `has_next`, `has_prev`,
  `next_cursor` **and** `prev_cursor` (deviation 3). The cursor is an opaque string a client passes
  back unread and never builds.
- `limit`: min 1, max 200, default 50.
- A `category_id` filter on history excludes transfers entirely.
- Money as a **string** (deviation 2), JSON keys snake_case.

## Definition of Done

- [ ] every shape matches the contract field for field, including the page wrapper and the
      breakdown's side / group / category nesting
- [ ] `GET /api/v1/history` returns matching movements, each marked as transaction or transfer,
      each transaction showing its note (AC-16)
- [ ] an empty period returns `200` with an empty list, **not** an error (AC-17)
- [ ] `GET /api/v1/reports/breakdown` returns group totals and category totals in one response,
      spending and earning kept apart (AC-18)
- [ ] group totals plus the Default group total equal the period total on both sides (AC-19)
- [ ] a period containing transfers counts none of them as spending or earning (AC-20)
- [ ] an invalid period returns `400` with `Breakdown.InvalidPeriod`; an invalid filter returns
      `History.InvalidFilter`
- [ ] every `4xx` carries the flat envelope
- [ ] no response record is shared with another operation folder

## Notes

- The Default group appears in the response as a bucket the breakdown produced. It has no id to
  filter by and cannot be named on a write (sad.md §12).
- A group holding no categories produces **no row** — settled at the api-forge gate.
