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

**AC-22 is a hard constraint on this decision, not a preference.** It says a removed entry
"disappears from history, and every balance it affected returns to what it would have been had the
entry never existed". Any option that keeps the entry visible contradicts it, and could only be
taken by overruling the PRD with a recorded Decision override.

## Decision drivers

- AC-21 — changing the amount of a recorded entry makes every balance affected by that entry follow
  the new amount.
- AC-22 — a removed entry "disappears from history, and every balance it affected returns to what it
  would have been had the entry never existed".
- Quality goal 1 — ledger correctness under any order of recording, editing and deletion.
- PRD §6 — history page p95 ≤ 400 ms per page. Reversed pairs would have to be detected and hidden
  on every history read.
- One owner, self-hosted, no regulatory retention duty and no second party to be accountable to
  (PRD §6.1).

## Considered options

1. **Edit and delete in place** — `UPDATE` the row on a correction, `DELETE` it on a removal. The
   ledger holds the current truth and nothing else.
2. **In place, plus a change log** — update and delete the live row as in option 1, but copy the
   previous version into a separate audit table first.

**Excluded by AC-22, not weighed:** *append-only with reversing entries*, where a correction writes
a compensating row and nothing is ever updated or deleted. It is the classic accounting answer and
is listed here because a reader will ask about it, but it is not a live alternative — AC-22 requires
the removed entry to disappear, and an append-only ledger keeps it. Taking it would have meant
overruling the PRD with a Decision override bullet in SAD §1.

## Decision outcome

**Chosen: option 1, edit and delete in place.**

AC-22 describes this literally — it asks that a removed entry *disappear* and that balances return
to what they would have been had it never existed. An `UPDATE` and a `DELETE` are exactly that, with
no reinterpretation of the acceptance criterion needed.

It also keeps history trivial to read, which matters for the ≤ 400 ms per-page target: there are no
reversed pairs to detect and hide on the way out.

Option 2 was rejected as work without a stated need. The PRD asks for no audit trail, there is no
second party to be accountable to, and an audit table that nothing reads is a table that will rot.
It stays the obvious upgrade path if losing a correction ever becomes a real problem — see
Consequences.

## Consequences

**Positive**
- AC-21 and AC-22 hold exactly as written, with no reinterpretation.
- History contains only entries the owner believes are real — no reversed pairs to filter out on
  every read.
- The schema stays small, and the later csv-import "undo a run" behaviour (GLOSSARY.md, *Import run*)
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
