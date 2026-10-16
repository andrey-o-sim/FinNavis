---
status: Accepted
owner: "Andrey Simonenko"
reviewers: ["Andrey Simonenko"]
updated_at: "2026-10-16"
feature_size: M
stage: "04-05"
ticket: "none"
---

# 0004 — Separate the read path behind a query port instead of reading through repositories

- **Status:** Accepted
- **Date:** 2026-10-16
- **Deciders:** Andrey Simonenko

## Context

Three of the eight user stories are reads, and two of them aggregate: the period breakdown totals by
group and by category at once (US-07), and the balance read sums every movement on an account
(US-05). ADR-0002 decided those figures are computed on demand, so the aggregation has to happen
somewhere.

A repository port returns domain entities, because that is what a collection-like view of an
aggregate means (`ARCHITECTURE.md`). If the breakdown were served that way, the use case would load
roughly 20 000 `Transaction` entities into memory and sum them in C#, and that cost grows every
year. The read path needs its own abstraction, or the repository abstraction has to change meaning.

## Decision drivers

- PRD §6 — period breakdown p95 ≤ 500 ms over ~20 000 movements; history page p95 ≤ 400 ms per page.
- Quality goal 2 — interactive latency on three years of history.
- `ARCHITECTURE.md` — "an interface is a port when the other side of it is outside the application";
  repository ports belong to Domain because a repository is a collection view of an aggregate;
  Application owns the ports describing what a use case needs from the outside world.
- The dependency rule, enforced by `tests/FinNavis.ArchitectureTests` — Application must not
  reference EF Core, so the projection has to live behind an interface.
- ADR-0002 — every read is computed on demand, so read performance is not protected by any cache.

## Considered options

1. **A separate query port.** Writes go through the four repository ports and return entities. Reads
   go through `ILedgerQueries`, declared in `Application/Abstractions` and implemented in
   `Infrastructure/Queries` with EF Core projections, so `SUM` and `GROUP BY` run in PostgreSQL.
2. **Everything through repository ports.** One kind of abstraction, declared in
   `Domain/Abstractions`, returning domain entities for both reads and writes.
3. **Report methods on the repositories.** Keep one port set, but let `ITransactionRepository`
   expose methods such as `GetBreakdown(period)` that return report DTOs computed in SQL.

## Decision outcome

**Chosen: option 1, a separate query port.**

It is the only option that keeps both halves of `ARCHITECTURE.md`'s own definition intact. A
repository stays a collection view of an aggregate and keeps returning entities; a report query is a
use-case need from the outside world, which is exactly what `Application/Abstractions` is for. The
aggregation runs in PostgreSQL, so the 500 ms budget is met by the database rather than by hoping
the row count stays small.

Option 2 was rejected because it forces a choice between two bad ends: either load 20 000 entities
to sum them in memory, which misses the latency target by a wide margin and degrades yearly, or
start returning report DTOs from a repository — at which point it is no longer a collection view of
an aggregate and option 2 has quietly become option 3.

Option 3 was rejected because it puts reporting shapes into `Domain/Abstractions`. The breakdown's
result — group totals and category totals with spending and earning kept apart — is a shape of the
report, not of the aggregate, and Domain is the one ring this repo guards most carefully.

## Consequences

**Positive**
- Aggregation happens in PostgreSQL; only result rows cross the boundary. The 500 ms and 400 ms
  targets are met by the query plan, not by row count luck.
- Domain stays clean: four repository ports, all returning entities, none knowing about reports.
- Read models can change shape as the API evolves without touching any aggregate.
- The read path is easy to test against a real database through Testcontainers, independently of
  the write path.

**Negative**
- Two kinds of port to learn and two places to look — a reader has to know whether what they want is
  a write or a read before they know which abstraction serves it.
- Read DTOs are a second set of types alongside domain entities and Presentation contracts, so a
  new field can need adding in more than one place.
- The query adapter holds SQL-shaped logic that unit tests cannot cover; correctness there depends
  on integration tests.

**Neutral**
- This is CQRS only in the narrow sense of separating read and write models. There is no separate
  read store, no eventual consistency and no event bus — both paths hit the same tables in the same
  database.
- `ILedgerQueries` may later split into one port per report if it grows unwieldy. That is a
  refactor inside one ring and needs no data change.

## Links

- PRD: [[../PRD.md]] — US-05, US-06, US-07, AC-16, AC-18, AC-19, §6 NFR
- SAD: [[../sad.md]] §5, §10
- Related ADR: [[0002-compute-every-read-on-demand]] · [[0001-keep-transactions-and-transfers-in-separate-tables]]
