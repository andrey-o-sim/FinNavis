# Tracker — core-ledger

> Status of every task in the epic. `implement-tasks` updates `done` as it commits each task.
> States: `todo` · `in_progress` · `blocked` · `review` · `done`.
>
> Sizing calibration: **S = 2h · M = half-day · L = day.** Nothing larger — a task above L is split.

| # | Task | Layer | Owner | Estimate | Blocked by | Status |
|---|---|---|---|---|---|---|
| T01 | Compose stack with PostgreSQL 18 | wiring | Andrey Simonenko | S | — | done |
| T02 | Testcontainers PostgreSQL 18 fixture | tests | Andrey Simonenko | M | — | done |
| T03 | CI job: build + test | wiring | Andrey Simonenko | S | T02 | done |
| T04 | ADR for the `*.NotFound` module | docs | Andrey Simonenko | S | — | done |
| T05 | Domain primitives | domain | Andrey Simonenko | S | — | done |
| T06 | `Account` + `IAccountRepository` | domain | Andrey Simonenko | S | T05 | done |
| T07 | `Category` + `CategoryGroup` + ports | domain | Andrey Simonenko | M | T05 | done |
| T08 | `Transaction` + port | domain | Andrey Simonenko | M | T05 | done |
| T09 | `Transfer` + port | domain | Andrey Simonenko | S | T05 | done |
| T10 | `IUnitOfWork` + `ILedgerQueries` | app | Andrey Simonenko | S | T05 | done |
| T11 | DbContext + 5 configurations | infra | Andrey Simonenko | M | T06, T07, T08, T09 | done |
| T12 | EF migration, diffed against the spec | migration | Andrey Simonenko | M | T11, T02 | done |
| T13 | 5 repository adapters + unit of work | infra | Andrey Simonenko | M | T12, T10 | done |
| T14 | Balance query + 2 existence checks | infra | Andrey Simonenko | M | T12, T10 | done |
| T15 | History page: `UNION ALL` + keyset cursor | infra | Andrey Simonenko | L | T12, T10 | done |
| T16 | Breakdown aggregate query | infra | Andrey Simonenko | M | T12, T10 | done |
| T17 | DI registration, composition root | wiring | Andrey Simonenko | S | T01, T13, T14, T15, T16 | done |
| T18 | 4 account use cases | app | Andrey Simonenko | M | T13, T14 | done |
| T19 | 4 category-group use cases | app | Andrey Simonenko | M | T13 | done |
| T20 | 5 category use cases | app | Andrey Simonenko | L | T13, T14 | todo |
| T21 | 3 transaction use cases | app | Andrey Simonenko | M | T13 | todo |
| T22 | 3 transfer use cases | app | Andrey Simonenko | M | T13 | todo |
| T23 | History + breakdown use cases | app | Andrey Simonenko | M | T15, T16 | todo |
| T24 | Error registry + flat envelope | ports | Andrey Simonenko | M | T05, T04 | todo |
| T25 | 4 account endpoints | ports | Andrey Simonenko | M | T18, T24, T17 | todo |
| T26 | 4 category-group endpoints | ports | Andrey Simonenko | M | T19, T24, T17 | todo |
| T27 | 5 category endpoints | ports | Andrey Simonenko | M | T20, T24, T17 | todo |
| T28 | 3 transaction endpoints | ports | Andrey Simonenko | M | T21, T24, T17 | todo |
| T29 | 3 transfer endpoints | ports | Andrey Simonenko | M | T22, T24, T17 | todo |
| T30 | History + breakdown endpoints | ports | Andrey Simonenko | M | T23, T24, T17 | todo |
| T31 | QG-1 balance property test | tests | Andrey Simonenko | L | T25, T28, T29 | todo |
| T32 | QG-2 latency + `EXPLAIN` | tests | Andrey Simonenko | L | T30, T25 | todo |
| T33 | QG-3 report stability | tests | Andrey Simonenko | M | T30, T27, T26 | todo |
| T34 | Served document vs the contract | docs | Andrey Simonenko | S | T25–T30 | todo |

**Total:** 34 tasks, ~17 person-days (9 × S + 21 × M + 4 × L = 134 h).

> Solo project — one owner on every row. The column is kept because `implement-tasks` reads the
> tracker, and an epic with no owner column drifts into "nobody starts".
