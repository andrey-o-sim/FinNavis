---
status: Accepted
slug: "csv-import"
stage: "03"
updated_at: "2026-10-17"
---

# 0003 — Take one snapshot of candidate movements before an import run writes anything

- **Status:** Accepted
- **Date:** 2026-10-17
- **Deciders:** Andrey Simonenko

## Context

ADR-0002 fixed the shape of the duplicate rule. This record fixes how it is executed: the run asks
PostgreSQL once, before writing anything, for every ledger movement that could possibly match a row
in the file, and from then on matches only against that snapshot.

The question looks like a performance question and is not. PRD §6 presented it as one when this
record was written — it explained the ≤ 60 s and ≤ 30 s targets by saying a row-by-row answer
"misses both targets by a wide margin". That premise does not survive arithmetic: roughly 1500
indexed point lookups against a local PostgreSQL take a fraction of a second, well inside a 60 s
budget. Per-row checking would meet the numbers too. The PRD was corrected on 2026-10-17 and no
longer makes that claim; this paragraph keeps it because it is what the alternative had going for
it, and removing it would make the decision below look unopposed.

What actually decides it is correctness, and `CONTEXT.md` states the invariant: two separate spends
that happen to share a date, an amount and a description are **not** duplicates — both are real and
both belong in the ledger. So a file carrying the same purchase twice must produce two entries. A
check that runs row by row, interleaved with the inserts of the same run inside one transaction
(ADR-0004), sees the row it just wrote and reports the second real spend as already recorded —
dropping it silently, which is the exact failure ADR-0002 called the worst one available.

## Decision drivers

- `CONTEXT.md` *Duplicate* — two coincidentally identical spends are both real.
- SAD §1 quality goal 1 — nothing is recorded twice; and its mirror, that nothing real is dropped.
- PRD §6 — load of ~1500 rows p95 ≤ 60 s, undo of 1500 rows p95 ≤ 30 s. Both are met by either
  option, so neither is the deciding driver here.
- ADR-0004 — the whole run commits in one transaction, which is what makes a per-row check able to
  see its own writes.
- `core-ledger` ADR-0004 — reads that aggregate go through a query port, not through repositories.

## Considered options

1. **One snapshot before the first write.** A single query loads every ledger movement that could
   match anything in the file — bounded by the accounts the file names and the date range it
   covers — and the run matches against that in memory.
2. **One check per file row.** Each row asks the database directly, in the order the rows are read,
   while earlier rows of the same run are already inserted.
3. **One snapshot, refreshed as the run writes.** The snapshot is loaded up front and this run's own
   inserts are appended to it as they happen.

## Decision outcome

**Chosen: option 1, one snapshot before the first write.**

It makes the invariant structural instead of guarded. The snapshot is a picture of the ledger as it
was before this load began, so by construction it cannot contain a row this load produced, and two
identical rows in one file both pass the check and both get recorded. Nothing has to remember to
exclude anything.

Option 2 was rejected because it inverts that: the check sees the run's own inserts, so the second
of two identical spends is reported as already recorded and dropped without a message. It can be
made correct — by excluding rows carrying this run's id — but then correctness rests on a condition
somebody has to keep writing, in a code path where getting it wrong produces no error and no log
line, only a missing entry.

Option 3 was rejected because it is option 2 with extra machinery: the refreshed snapshot ends up
holding this run's own rows, which is precisely what must not be matched against.

The secondary benefit is that one query replaces about 1500, which makes the PRD §6 budgets
comfortable rather than tight. That is a benefit, not the reason.

## Consequences

**Positive**
- Two identical rows in one file are both recorded, structurally, with no guard condition to
  maintain and no test that only fails when somebody deletes a line.
- One query instead of ~1500 round trips, so PRD §6's ≤ 60 s budget has a wide margin and does not
  depend on connection latency.
- The snapshot is a plain read with no side effect, so the match can be covered by tests that need
  no write at all.
- It composes with ADR-0002: whatever the open window parameters turn out to be, they change what
  the snapshot query selects, not how the run uses it.

**Negative**
- The snapshot lives in memory for the length of the run, and its size follows the **date range**
  the file covers and the accounts it names — not the number of rows in the file. A three-year
  history migration therefore pulls a candidate set proportional to three years of ledger, which is
  the largest memory this feature holds at once. At the ~20 000 movements `core-ledger` sizes for,
  this is small; it is the number to watch, and §7 (Deployment view) repeats it as a threshold.
- A movement recorded by something else while the run is in flight is invisible to the snapshot.
  With one owner, one client and no concurrent writer this cannot happen today — it is recorded
  here so the assumption is visible when a second writer appears.
- Two rows of the same file can both match the same single ledger movement. The snapshot makes this
  easy to get wrong by matching both against it. Which one wins, and whether the second is a
  duplicate or a set-aside row, is part of the open parameters in §11.

**Neutral**
- The snapshot is a read that selects across accounts and a date range, so it goes behind a query
  port in `Application/Abstractions` rather than through a repository, following `core-ledger`
  ADR-0004. §5 (Building block view) names the port.
- Nothing here constrains the predicate itself. ADR-0002 owns that, and its open parameters land in
  the snapshot's `WHERE` clause.

## Links

- PRD: [[../PRD.md]] — AC-20, AC-21, §6 NFR, §9 new read paths
- SAD: [[../sad.md]] §4, §5, §6, §7, §8, §10
- Related ADR: [[0002-match-on-an-exact-external-reference-first-then-a-date-and-amount-window]] · [[0004-commit-an-entire-import-run-in-one-database-transaction]]
