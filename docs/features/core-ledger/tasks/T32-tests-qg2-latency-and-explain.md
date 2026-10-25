---
id: T32
title: "Prove QG-2: seed 20 000 movements, time the three budgets, assert the index"
layer: "tests"
deps: ["T30", "T25"]
acs: []
files_hint: ["tests/FinNavis.Infrastructure.IntegrationTests/Quality/LatencyTests.cs", "tests/FinNavis.Infrastructure.IntegrationTests/Seeding/", ".github/workflows/ci.yml"]
owner: "Andrey Simonenko"
estimate: "L"
status: "todo"
---

# T32 — Prove QG-2: seed 20 000 movements, time the three budgets, assert the index

## Why

[sad.md §10 QG-2](../sad.md) and [PRD §6](../PRD.md). Every number below is copied from PRD §6
verbatim — do not round them.

## What

- A **seeder** building about three years of history, ~20 000 movements, across several accounts,
  categories and groups, with transfers mixed in.
- The three timed tests, named as [sad.md §10](../sad.md) names them:
  - `RecordTransaction_P95Under300ms` — **p95 ≤ 300 ms**
  - `Breakdown_20kSeededMovements_P95Under500ms` — **p95 ≤ 500 ms**
  - `HistoryPage_P95Under400ms` — **p95 ≤ 400 ms per page**
- A **throughput smoke test in CI** — **≥ 10 requests/s on one instance**. This edits
  `.github/workflows/ci.yml` from [T03](./T03-setup-ci-build-and-test.md).
- An **`EXPLAIN` assertion** that the breakdown query uses `idx_transactions_occurred_on_account_id`
  rather than a sequential scan.

## Definition of Done

- [ ] the seeder produces ~20 000 movements and the run is repeatable from a fixed seed
- [ ] all three latency tests pass against the seeded data in Testcontainers at the exact PRD §6
      figures
- [ ] the CI smoke test measures ≥ 10 requests/s and fails the job below it
- [ ] the `EXPLAIN` assertion fails if the index is dropped — verified by dropping it once in a
      scratch run
- [ ] no real financial data anywhere in the seeder: invented merchants, invented amounts
      (`CLAUDE.md` Safety, `data-model.md` PII guard)

## Notes

- The `EXPLAIN` assertion is the whole mitigation for a Medium risk in
  [sad.md §11](../sad.md): ADR-0002 removed every cache and rollup, so that one index is
  load-bearing, and losing it would show as a slow test rather than as a wrong answer.
- Shares a lane with [T03](./T03-setup-ci-build-and-test.md) on `.github/workflows/ci.yml`.
- `acs` is empty on purpose: these are PRD §6 NFR rows, not §5 acceptance criteria.
