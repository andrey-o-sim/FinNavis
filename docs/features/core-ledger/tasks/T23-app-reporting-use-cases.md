---
id: T23
title: "Write the history and breakdown use cases"
layer: "app"
deps: ["T15", "T16"]
acs: ["AC-16", "AC-17", "AC-18", "AC-19", "AC-20"]
files_hint: ["src/FinNavis.Application/Reporting/", "tests/FinNavis.Application.UnitTests/Reporting/"]
owner: "Andrey Simonenko"
estimate: "M"
status: "done"
attempts: 1
---

# T23 — Write the history and breakdown use cases

## Why

Derives from [PRD US-06, US-07, §AC-16 to §AC-20](../PRD.md),
[sad.md §6 flows 3 and 8](../sad.md), ADR-0002 and ADR-0004. US-07 is the answer the owner's
current tool can never give (PRD §1) — *"one request, group totals and category totals"*.

## What

`Reporting/GetHistory`, `Reporting/GetBreakdown`.

- **GetHistory** — carries the filters and the page asked for to `ILedgerQueries`. Every filter is
  optional; a request with none asks for the whole history. An empty period returns an empty page
  and is **not** reported as a failure (AC-17).
- **GetBreakdown** — asks `ILedgerQueries` for the period totals, then **puts every category that
  belongs to no group into the Default group bucket** (sad.md §6 flow 3). This assembly is the one
  piece of breakdown logic that lives in Application rather than in SQL.
- No repository is used in either, so no entity is loaded into memory (ADR-0004).

## Definition of Done

- [x] `GetHistory_FilteredByPeriodAccountAndCategory_ReturnsMarkedRows` passes (AC-16)
- [x] `GetHistory_EmptyPeriod_ReturnsEmptyPageNotFailure` passes (AC-17)
- [x] `GetBreakdown_Period_ReturnsGroupAndCategoryTotalsSidesApart` passes (AC-18)
- [x] `GetBreakdown_GroupTotalsPlusDefaultGroup_EqualPeriodTotal` passes on the spending side and
      the earning side alike (AC-19)
- [x] `GetBreakdown_PeriodWithTransfers_CountsNoTransfer` passes (AC-20)
- [x] the Default group bucket is **assembled, never stored** — no repository write happens in
      either use case
- [x] unit tests touch no database and no network

## Notes

- The Default group *"cannot be renamed, deleted or picked when filing a transaction, and no
  category is ever assigned to it"* (sad.md §12). It is a bucket this use case produces.
- A group holding no categories is **left out** of the breakdown, not reported as zero — settled at
  the api-forge gate from PRD §8's own default.
