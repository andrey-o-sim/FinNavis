---
status: Accepted
owner: "Andrey Simonenko"
reviewers: ["Andrey Simonenko"]
updated_at: "2026-10-16"
feature_size: M
stage: "04-05"
ticket: "none"
---

# 0006 — Add a nullable external reference to transactions now, before any feature uses it

- **Status:** Accepted
- **Date:** 2026-10-16
- **Deciders:** Andrey Simonenko

## Context

PRD §8 carries an open question with a deadline at this stage: *should an entry carry a
client-supplied identifier from day one, so later de-duplication, import and message capture need no
backfill?* The PRD's standing default was no.

The question is live because of what comes next. CONTEXT.md already defines *Duplicate* — a row in a
loaded file naming a movement already in the ledger — and *Import run*, which must be readable back
and undoable as a whole. csv-import is the next feature in the queue, and the planned message
capture creates an expense from an incoming Telegram or email message. Both need a way to say "this
entry came from that outside thing" so the same thing loaded twice does not become two entries.

De-duplication itself is explicitly out of scope for core-ledger (PRD §6.1), so the question is not
whether to build it, but whether to leave room for it.

## Decision drivers

- PRD §8 — the open question is due at this stage and must be closed, not carried further.
- PRD §6.1 — repeated-request de-duplication is out of scope for this feature.
- `SPEC.md` "Safe schema change" — a change reaching a database that already holds rows must lose or
  alter no row.
- CONTEXT.md *Duplicate* and *Import run* — both already defined, so the need is known rather than
  speculative.
- The ledger is expected to hold at least three months of real history within 90 days (PRD §7
  "Migration depth"), so the next migration will land on a populated table.

## Considered options

1. **Add a nullable `external_ref` now**, with a unique index, unused by this feature and left null
   by every entry the owner types in.
2. **No — keep the PRD default.** Add nothing; let csv-import add the column itself when it knows
   what shape it needs.
3. **Let callers supply the primary key.** A repeated identical request would then collide on the
   primary key and be naturally idempotent, with no extra column.

## Decision outcome

**Chosen: option 1, add a nullable `external_ref` now.**

The need is not speculative — it is already written into the glossary, and the feature that consumes
it is next. By the time csv-import ships, the transactions table will hold real history (PRD §7
targets three months within 90 days), so option 2 moves a migration from an empty table to a
populated one for no gain. Adding one nullable column with an index to a table nobody has filled yet
is the cheapest moment this decision will ever have.

The column stays genuinely unused here: core-ledger writes null into it for every entry, enforces
nothing beyond uniqueness, and exposes it in no API contract. That keeps this feature's scope
unchanged while removing the backfill the PRD question was worried about.

Option 3 was rejected because it is new scope rather than a schema detail. Accepting a caller-chosen
primary key means the ledger trusts an outside party for identity, and PRD §6.1 put repeated-request
de-duplication out of scope for this feature. It also conflates two different things: who an entry
is, and where it came from.

## Consequences

**Positive**
- csv-import and message capture add no migration to a table that by then holds real history.
- The unique index makes "this outside thing is already recorded" a database-level guarantee when a
  later feature starts using it, rather than a check that can race.
- Closes a PRD open question at its deadline, with the reasoning on record.

**Negative**
- One column and one index that carry no value today, on a table where every row leaves them null. A
  reader of the schema will ask what it is for, and only this ADR answers.
- Unique indexes on a nullable column treat nulls as distinct in PostgreSQL, which is the behaviour
  wanted here but is a subtlety the next feature must not misread.
- If csv-import eventually needs a composite reference — source plus row identity — rather than a
  single value, this column is the wrong shape and gets replaced. Dropping an unused column is
  cheap, so the bet is small.

**Neutral**
- Nothing in core-ledger reads or writes the column, so it needs no acceptance criterion, no
  endpoint field and no test here beyond the migration itself.
- The decision says nothing about *how* duplicates are detected. That stays csv-import's to design.

## Links

- PRD: [[../PRD.md]] — §6.1, §8 open questions
- SAD: [[../sad.md]] §8, §11
- Related ADR: [[0005-use-uuid-v7-primary-keys-generated-in-the-application-layer]] · [[0001-keep-transactions-and-transfers-in-separate-tables]]
