---
status: Accepted
owner: "Andrey Simonenko"
reviewers: ["Andrey Simonenko"]
updated_at: "2026-10-16"
feature_size: M
stage: "04-05"
ticket: "none"
---

# 0003 — Edit and delete ledger rows in place instead of keeping an append-only ledger

- **Status:** Accepted
- **Date:** 2026-10-16
- **Deciders:** Andrey Simonenko

## Context

The owner records movements by hand, late in the evening, and will sometimes get one wrong (PRD
US-08). The product has to let them fix it. Two traditions answer this differently: business
software updates the record, while accounting systems never change a posted entry and instead write
a compensating one.

The choice decides whether ledger rows are mutable, which sets the schema, the shape of every
history read, and whether past states can be reconstructed.

## Decision drivers

- AC-21 — changing the amount of a recorded entry makes every balance affected by that entry follow
  the new amount.
- AC-22 — a removed entry "disappears from history, and every balance it affected returns to what it
  would have been had the entry never existed".
- Quality goal 1 — ledger correctness under any order of recording, editing and deletion.
- Repo database convention (CLAUDE.md): created_at-only audit, hard delete, "the database is dumb
  storage".
- One owner, self-hosted, no regulatory retention duty and no second party to be accountable to
  (PRD §6.1).

## Considered options

1. **Edit and delete in place** — `UPDATE` the row on a correction, `DELETE` it on a removal. The
   ledger holds the current truth and nothing else.
2. **Append-only with reversing entries** — a correction writes a new compensating row; nothing is
   ever updated or deleted, and history hides reversed pairs when listing.
3. **In place, plus a change log** — update and delete the live row as in option 1, but copy the
   previous version into a separate audit table first.

## Decision outcome

**Chosen: option 1, edit and delete in place.**

AC-22 describes this literally — it asks that a removed entry *disappear* and that balances return
to what they would have been had it never existed. Option 2 cannot satisfy that wording: under an
append-only ledger the entry is still there, and the balance is corrected by adding a second row
rather than by removing the first. Choosing option 2 would have meant overruling the PRD, so the
decision follows the acceptance criteria rather than the accounting tradition.

Option 1 also matches the repo's existing database convention and keeps history trivial to read,
which matters for the ≤ 400 ms history page target: no reversed pairs to detect and hide.

Option 3 was rejected as work without a stated need. The PRD asks for no audit trail, there is no
second party to be accountable to, and an audit table that nothing reads is a table that will rot.

## Consequences

**Positive**
- AC-21 and AC-22 hold exactly as written, with no reinterpretation.
- History contains only entries the owner believes are real — no reversed pairs to filter out on
  every read.
- The schema stays small, and the later csv-import "undo a run" behaviour (CONTEXT.md, *Import run*)
  is a plain delete of that run's rows.

**Negative**
- There is no audit trail. A wrong edit is unrecoverable, and "what did this period look like last
  month" cannot be answered. For a single-owner ledger with no outside accountability this is
  accepted, and it is carried as debt in SAD §11.
- Nothing protects against an accidental delete beyond the owner's own care and database backups.
  Backups therefore become the only recovery path.

**Neutral**
- Adding a change-log table later is additive and needs no migration of existing rows — it would
  simply start recording from the day it ships, with no history before that date.
- Because a correction is an `UPDATE`, a future concurrent writer would need an optimistic
  concurrency token. There is one owner today, so none is added — see SAD §8 and §11.

## Links

- PRD: [[../PRD.md]] — US-08, AC-21, AC-22, AC-23, AC-24
- SAD: [[../sad.md]] §4, §8, §11
- Related ADR: [[0001-keep-transactions-and-transfers-in-separate-tables]] · [[0002-compute-every-read-on-demand]]
