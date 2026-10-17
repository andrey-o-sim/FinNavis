---
status: Accepted
slug: "csv-import"
stage: "03"
updated_at: "2026-10-17"
---

# 0004 — Commit an entire import run in one database transaction

- **Status:** Accepted
- **Date:** 2026-10-17
- **Deciders:** Andrey Simonenko

## Context

AC-22 states the requirement in product words: a load that fails part-way through recording its rows
leaves the ledger looking exactly as it did before the load started — nothing recorded, every
balance unchanged, and the Owner told the load did not happen.

The largest load this feature is designed for is the one-time move of the old history: about 1500
rows across several accounts, which may create accounts and categories as well (AC-10) and may
record Transfers as well as Transactions (AC-11). All of it has to arrive together or not at all,
including the Import run record itself.

What has to be decided is the unit of commit, because the alternative — committing in chunks and
cleaning up after a failure — is the standard shape for bulk import, and choosing against it is the
kind of thing a reader a year from now would ask about.

## Decision drivers

- PRD §5 AC-22 — a part-way failure leaves the ledger as it was.
- PRD §5 AC-16, AC-17 — undo removes everything the run recorded, including what it created, so the
  run's own record of what it did has to be exactly as trustworthy as the movements themselves.
- PRD §6 — load of ~1500 rows p95 ≤ 60 s.
- SAD §1 quality goal 2 — a load is all-or-nothing, and reversible.
- SAD §2 Constraints — `core-ledger` ADR-0005 generates UUID v7 keys in the Application layer, so
  every row and every link between rows exists before any insert.
- `ARCHITECTURE.md` and `CLAUDE.md` — `IUnitOfWork` is an Application port that Infrastructure
  implements over EF Core's `DbContext`. Committing a set of changes is already its job.

## Considered options

1. **One transaction for the whole run.** Everything the run produces — movements, created accounts
   and categories, the Import run record, the set-aside rows — is built in memory and committed
   through a single `IUnitOfWork.CommitAsync`.
2. **Chunked commits with compensation.** Commit every N rows. A failure part-way leaves committed
   chunks behind, so a compensating pass deletes them to restore AC-22.
3. **Chunked commits with resume.** Commit every N rows and record progress, so a failed run is
   continued rather than undone.

## Decision outcome

**Chosen: option 1, one transaction for the whole run.**

AC-22 is a database transaction, stated in product words. Option 1 satisfies it with the mechanism
PostgreSQL already provides and the port `core-ledger` already declares, so this feature adds
nothing to get it — no new port, no new adapter, no progress table, no recovery path.

It is affordable because of `core-ledger` ADR-0005: UUID v7 keys are generated in the Application
layer, so the whole run — every Transaction, every Transfer, every created Account and Category,
the Import run and the links between all of them — is assembled in memory first and written once.
That record named this feature as the caller the choice was made for, and this is where it pays.

Option 2 was rejected because it reaches AC-22 by writing a second mechanism that has to be as
reliable as the first. If the compensating pass fails — which is likely, since it runs right after
something else already failed — the ledger is left in exactly the state AC-22 forbids, and the Owner
has a half-loaded month with no way to tell which half.

Option 3 was rejected because it does not satisfy AC-22 at all. A resumable run is a run that leaves
rows behind, which is the opposite of what the criterion asks for. It is the right design for a file
far larger than this product will ever see.

**The ceiling.** One transaction holding a whole file stops being the right shape when the file
stops being small. The number to watch is about **50 000 rows in one file**: above that, the EF Core
change tracker holding every entity of the run, and a single transaction holding them for its whole
duration, both stop being comfortable. For scale, `core-ledger` sizes the entire ledger at about
20 000 movements over three years, so a file above this ceiling would be larger than all the history
the product is designed to hold. If one ever appears, option 3 is the thing to revisit, and the
Import run record is already the progress table it would need. §7 (Deployment view) carries this
number as a scaling threshold.

## Consequences

**Positive**
- AC-22 holds with no code: the rollback is PostgreSQL's, not the feature's.
- There is no half-loaded state to design for, to report, or to recover from, so the Owner never has
  to find out which rows arrived.
- The Import run record commits with the movements it describes, so "what this load did" cannot
  disagree with what is in the ledger.
- Undo of a whole run is the same shape in reverse, inside one transaction, and comfortably inside
  PRD §6's ≤ 30 s.

**Negative**
- A ceiling exists. Above roughly 50 000 rows in one file this choice has to be revisited, and the
  revision is not small — it is option 3, with progress, resume and a different AC-22.
- Everything the run produces is held in memory until the commit. Together with the candidate
  snapshot of ADR-0003 this is the feature's peak memory, and it arrives all at once.
- One long transaction keeps a connection and its locks for the whole load. With one owner and one
  client nothing contends for them, so this costs nothing today and would need rethinking the moment
  a second writer exists.
- A single failure anywhere — one unparseable row late in the file — discards the work of the whole
  load. That is what AC-22 asks for, but it means the refusal message has to be good enough that the
  Owner can fix the file in one go rather than discovering problems one load at a time.

**Neutral**
- No new port. `IUnitOfWork` is declared in `Application/Abstractions` by `core-ledger` and
  implemented in Infrastructure over `DbContext`; this feature is a caller.
- The decision says nothing about *how* the rows are inserted — `AddRange` with one `SaveChanges`,
  or a bulk path. That is an Infrastructure detail, reversible in an afternoon, settled at
  implementation against the PRD §6 numbers.

## Links

- PRD: [[../PRD.md]] — US-02, US-03, US-06, AC-16, AC-17, AC-22, §6 NFR
- SAD: [[../sad.md]] §4, §5, §6, §7, §8, §10, §11
- Related ADR: [[0003-take-one-snapshot-of-candidate-movements-before-the-run-writes]] · `core-ledger/adr/0005-use-uuid-v7-primary-keys-generated-in-the-application-layer`
