---
id: T15
title: "Implement the history page: UNION ALL with a keyset cursor"
layer: "infra"
deps: ["T12", "T10"]
acs: ["AC-16", "AC-17"]
files_hint: ["src/FinNavis.Infrastructure/Queries/LedgerHistoryQueries.cs", "src/FinNavis.Infrastructure/Queries/HistoryCursor.cs", "tests/FinNavis.Infrastructure.IntegrationTests/Queries/HistoryQueryTests.cs"]
owner: "Andrey Simonenko"
estimate: "L"
status: "done"
attempts: 1
---

# T15 — Implement the history page: `UNION ALL` with a keyset cursor

## Why

Derives from [PRD §AC-16, §AC-17](../PRD.md), [sad.md §6 flow 8](../sad.md), ADR-0001 and ADR-0005.
[sad.md §11](../sad.md) flags it as a **Medium risk**: *"Paging history across a `UNION ALL` of two
tables is awkward, and the ≤ 400 ms per-page target depends on getting it right."*

The paging decisions were settled at the api-forge gate and are recorded in
[`contracts/api-sync-report.md`](../contracts/api-sync-report.md) — do not re-decide them.

## What

- One query: a `UNION ALL` over `transactions` and `transfers`, filtered by period and account.
  Each row is marked as a transaction or a transfer; a transaction carries its note (AC-16).
- **Keyset paging over `(occurred_on, id)`.** `id` is a time-ordered UUID v7 (ADR-0005), which
  `data-model.md` already names as the tiebreaker.
- **The cursor is an opaque string the server mints**, not a bare UUID — one id cannot locate a
  page boundary across two tables (`.claude/rules/openapi.md` deviation 3). The page carries both
  `next_cursor` and `prev_cursor`, because the `before` parameter is unusable without the second.
- `limit`: minimum 1, maximum 200, **default 50** (settled at the api-forge gate).
- **A category filter excludes transfers entirely** (settled at the same gate). A transfer carries
  no `category_id`, so returning it anyway would mean the result did not all match what was asked.
- An empty period returns an empty page, not a failure (AC-17).

## Definition of Done

- [ ] `History_FilteredByPeriodAccountAndCategory_ReturnsMatchingRows` passes, each row marked as
      transaction or transfer, each transaction showing its note (AC-16)
- [ ] `History_CategoryFilter_ReturnsNoTransfer` passes
- [ ] `History_EmptyPeriod_ReturnsEmptyPageNotFailure` passes (AC-17)
- [ ] paging forward then back over a seeded set returns every row exactly once, with no gap and no
      repeat across the page boundary
- [ ] a cursor is opaque — the test asserts a client cannot build one from a row id
- [ ] `limit` below 1 or above 200 is rejected; omitting it gives 50
- [ ] the query uses `idx_transactions_occurred_on_account_id` and `idx_transfers_occurred_on`
- [ ] `./build.sh test` is green

## Notes

- The ≤ 400 ms per-page target over ~20 000 movements is proved in
  [T32](./T32-tests-qg2-latency-and-explain.md), not here.
- Largest single task in the epic. If the cursor encoding grows past a few hundred lines, split it
  out rather than letting this task cross a day.
