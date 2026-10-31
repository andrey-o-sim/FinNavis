# Tracker — csv-import

> Status of every task in the epic. The implement stage updates `status` as it commits each task.
> States: `todo` · `in_progress` · `blocked` · `review` · `done`.
>
> `--reconcile` preserves this column. Nothing else may rewrite it.

| # | Task | Layer | Estimate | Blocked by | Status |
|---|---|---|---|---|---|
| T01 | Answer PRD §8's four file questions and fix ADR-0002's proximity parameters | docs | M | — | todo |
| T02 | Add the three missing refusal messages to PRD §10 | docs | S | — | todo |
| T03 | Add the four import enums and the `MovementFingerprint` value object | domain | M | — | done |
| T04 | Add the `MappingProfile` aggregate, its rules and `IMappingProfileRepository` | domain | M | T03 | done |
| T05 | Add the `ImportRun` aggregate with its five child types and `IImportRunRepository` | domain | L | T03 | done |
| T06 | Declare `IStatementReader` and `IImportQueries` in `Application/Abstractions` | app | S | T03 | done |
| T07 | Add the `DbContext` sets and the eight entity configurations | infra | L | T04, T05 | done |
| T08 | Generate the import migration and diff it against the staged spec | migration | M | T07 | done |
| T09 | Add `EfImportRunRepository` and `EfMappingProfileRepository` | infra | M | T08 | todo |
| T10 | Implement the candidate snapshot in `EfImportQueries` | infra | L | T06, T08 | todo |
| T11 | Implement the AC-17 still-in-use check in `EfImportQueries` | infra | M | T06, T08 | todo |
| T12 | Add the CSV adapter behind `IStatementReader` and the synthetic sample files | infra | L | T06 | todo |
| T13 | Register the four import adapters in the Infrastructure container | wiring | S | T09, T10, T11, T12 | todo |
| T14 | Build the shared import pipeline, the policy object and the two-tier duplicate matcher | app | L | T01, T04, T05, T06, T10 | todo |
| T15 | Add the `InspectFile` and `ConfirmMappingProfile` use cases | app | M | T04, T06, T09, T12 | todo |
| T16 | Add the `LoadStatement` use case with the statement policy | app | L | T14 | todo |
| T17 | Add the `MigrateHistory` use case with the migration policy | app | M | T14 | todo |
| T18 | Add the `GetImportRun` use case | app | S | T05, T09 | todo |
| T19 | Add the `UndoImportRun` use case | app | L | T05, T09, T11 | todo |
| T20 | Add the `Import` and `MappingProfile` error codes to the registry | ports | M | T02, T04, T05 | todo |
| T21 | Add the `MappingProfiles` endpoints: `Inspect` and `Confirm` | ports | M | T13, T15, T20 | todo |
| T22 | Add the `ImportRuns` load endpoints: `LoadStatement` and `MigrateHistory` | ports | L | T13, T16, T17, T20 | todo |
| T23 | Add the `ImportRuns` read and undo endpoints: `Get` and `Undo` | ports | M | T13, T18, T19, T20 | todo |
| T24 | Quality gate QG-1 — nothing is recorded twice | tests | M | T22 | todo |
| T25 | Quality gate QG-2 — a load is all-or-nothing, and reversible | tests | L | T22, T23 | todo |
| T26 | Quality gate QG-3 — a bulk load fits inside one evening's patience | tests | M | T22 | todo |
| T27 | Diff the served document against the contract and close the deferrals | docs | M | T21, T22, T23 | todo |

**Total:** 27 tasks, ~17 person-days (9 × L, 14 × M, 4 × S).
**Owner:** Andrey Simonenko — one owner, so the column is not repeated per row.

`.size` says **L**, and ~17 person-days — roughly 30 evenings — agrees with it. `PRD.md` §1 already
flags that RICE was scored from effort **M** while the scope written is **L**; this count confirms
the PRD was right to ask for a re-score. The parked item is in [`_epic.md`](./_epic.md).

One number worth carrying: `core-ledger` came out at 34 tasks and ~17 person-days for size **M**.
This epic reaches the same person-day total in 7 fewer tasks, because its tasks are heavier — 9 are
`L`, against `core-ledger`'s lighter mix.
