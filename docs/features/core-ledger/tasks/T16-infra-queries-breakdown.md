---
id: T16
title: "Implement the period breakdown aggregate query"
layer: "infra"
deps: ["T12", "T10"]
acs: ["AC-18", "AC-19", "AC-20"]
files_hint: ["src/FinNavis.Infrastructure/Queries/LedgerBreakdownQueries.cs", "tests/FinNavis.Infrastructure.IntegrationTests/Queries/BreakdownQueryTests.cs"]
owner: "Andrey Simonenko"
estimate: "M"
status: "done"
attempts: 1
---

# T16 — Implement the period breakdown aggregate query

## Why

Derives from [PRD §AC-18, §AC-19, §AC-20](../PRD.md), [sad.md §6 flow 3](../sad.md), ADR-0002 and
ADR-0004. This is the answer the owner's current tool cannot give (PRD §1), and
[sad.md §11](../sad.md) flags its index as **load-bearing**: with no cache and no rollup, losing
`idx_transactions_occurred_on_account_id` turns the query into a sequential scan.

## What

- **One** aggregate query over `transactions`, joined to the **current** category-to-group mapping,
  returning totals per category and per group with spending and earning kept apart (AC-18).
- Because the mapping is resolved at query time (ADR-0002), moving a category between groups
  changes group totals at once and cannot change that category's own figure (AC-13, quality goal 3).
- **The `transfers` table is never read.** AC-20 is satisfied structurally by ADR-0001 — a transfer
  cannot leak into a category total because the query never looks at it.
- Rows come back raw. Assembling the Default group bucket is the Application layer's job
  ([T23](./T23-app-reporting-use-cases.md)).

## Definition of Done

- [ ] `Breakdown_PeriodWithSpendingAndEarning_KeepsSidesApart` passes (AC-18)
- [ ] `Breakdown_PeriodWithTransfers_CountsNoTransfer` passes, and the generated SQL names no
      `transfers` table (AC-20)
- [ ] the group totals plus the ungrouped total equal the period total on each side (AC-19, the
      query half)
- [ ] the query uses `idx_transactions_occurred_on_account_id` on its leading column and
      `idx_categories_category_group_id` for the join
- [ ] `./build.sh test` is green

## Notes

- **No `Default` group row is read or written.** The Default group is produced by the breakdown and
  is never stored (sad.md §12, `data-model.md` decision 8). A category reaches it by having no
  group at all.
- **Settled at the api-forge gate:** a group holding no categories is **left out** of the
  breakdown, not reported with a zero total. It was PRD §8's own stated default. Flip it only by
  changing the contract first.
- The `EXPLAIN` assertion that guards the index is in
  [T32](./T32-tests-qg2-latency-and-explain.md).
