---
status: Accepted
owner: "Andrey Simonenko"
reviewers: ["Andrey Simonenko"]
updated_at: "2026-10-16"
feature_size: M
stage: "04-05"
ticket: "none"
---

# 0002 — Compute every read on demand, with no rollup table and no cache

- **Status:** Accepted
- **Date:** 2026-10-16
- **Deciders:** Andrey Simonenko

## Context

The ledger answers three reads: the balance of an account and the total across all accounts
(US-05), a history filtered by period, account and category (US-06), and one period breakdown that
reports group totals and category totals together (US-07). All three are derived figures — nothing
the owner types in is a total.

Derived figures can either be computed when asked, or maintained as stored totals updated on every
write. The choice sets the write path, the read path, and whether the ledger can drift.

## Decision drivers

- Quality goal 1 — ledger correctness: a balance always equals the sum of the movements recorded on
  the account (AC-14), and 0 discrepancies across at least 1 000 randomised sequences of
  recordings, edits and deletions (PRD §6).
- Quality goal 3 / AC-13 — moving a category to another group, or out of every group, must leave
  that category's figure for a period unchanged while group figures follow the new placement.
- PRD §6 — period breakdown p95 ≤ 500 ms over 3 years of history (~20 000 movements); history page
  p95 ≤ 400 ms per page; record a movement p95 ≤ 300 ms.
- PRD §7 KPI "Ledger trust" — target 0 occasions where a FinNavis balance disagrees with the real
  account.
- Data volume is small and bounded: one person, manual entry, roughly 20 000 rows over three years.

## Considered options

1. **Compute on demand** — balance, history and the breakdown are each one query over the base
   tables, with indexes sized for them. No stored totals.
2. **Pre-aggregated rollup table** — maintain per-category and per-group period totals, and an
   account balance column, updated inside the same transaction as every write.
3. **Compute on demand plus a cache** — query the base tables, then cache the answer per period on
   the grounds that closed periods never change.

## Decision outcome

**Chosen: option 1, compute on demand.**

At roughly 20 000 rows an indexed aggregate query is single-digit milliseconds in PostgreSQL, so the
500 ms budget is not under pressure and buys nothing to defend. The deciding argument is not speed
but correctness: a stored total is a second copy of the truth, and every copy is a way for the
ledger to drift. PRD §7 names "balance disagreed with the real account" as a KPI with a target of
zero, and option 2 introduces exactly that failure mode in exchange for latency headroom that is
not needed.

Option 1 also makes quality goal 3 free. Because the category-to-group mapping is resolved at query
time from the current mapping, re-grouping a category changes group totals on the next read and
cannot touch that category's own figure — AC-13 without any recompute step.

Option 2 was rejected because it breaks AC-13 directly: with group totals materialised, moving a
category between groups forces a recompute of every affected period. Option 3 was rejected as
premature — it adds an invalidation rule that must be correct when an old entry is edited (AC-21) or
deleted (AC-22), to solve a latency problem the measurements do not show.

## Consequences

**Positive**
- There is one copy of the truth. A balance cannot disagree with the movements it is derived from,
  because it is not stored anywhere.
- AC-13 holds with no recompute: grouping is resolved per query from the current mapping.
- The write path stays short — an insert, and nothing else to keep in step. That helps the 300 ms
  record budget.
- Editing and deleting old entries (AC-21, AC-22) need no invalidation logic at all.

**Negative**
- Read cost grows with history. At 20 000 rows this is invisible; the figures in §7 say where to
  look again.
- Correct indexes become load-bearing rather than optional. A missing index on the breakdown's
  period and category columns turns a fast query into a sequential scan, and nothing else protects
  the 500 ms target.
- The total-across-all-accounts read touches both tables every time it is asked.

**Neutral**
- Adding a rollup table or a cache later is additive and does not require a data migration — the
  base tables stay the source of truth either way. This is the reason the decision is reversible
  despite being strategic.

## Links

- PRD: [[../PRD.md]] — US-05, US-06, US-07, AC-13, AC-14, AC-18, AC-19, §6 NFR, §7 KPI
- SAD: [[../sad.md]] §4, §10
- Related ADR: [[0001-keep-transactions-and-transfers-in-separate-tables]]
