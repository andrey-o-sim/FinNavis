---
status: Accepted
slug: "csv-import"
stage: "03"
updated_at: "2026-10-17"
---

# 0007 — Keep import bookkeeping in this feature's own tables instead of adding columns to the ledger

- **Status:** Accepted
- **Date:** 2026-10-17
- **Deciders:** Andrey Simonenko

## Context

PRD §9 says every movement a load records has to remember that load, and that a movement typed by
hand answers "none". Two criteria depend on it: AC-13, reading back what a load recorded, found
already there and set aside; and AC-16, removing everything a load recorded so every balance returns
to what it would have been.

The link is not only to transactions. AC-11 says a history migration can record a Transfer, and
ADR-0001 of `core-ledger` keeps transfers in their own table. AC-17 goes further: undo also removes
the accounts and categories the run created, except any that a movement from outside the run now
names. So four of `core-ledger`'s tables are involved — accounts, categories, transactions,
transfers — and for each the run has to be able to say "this one is mine".

ADR-0008 adds one more thing that needs a home: a content fingerprint per recorded movement, so
AC-18 can tell that a row was changed by hand after the import.

## Decision drivers

- PRD §5 AC-13, AC-16, AC-17, AC-18 — read back, undo, undo what was created, block undo after a
  hand edit.
- PRD §9 — "Changed data: nothing already recorded loses anything."
- `SPEC.md` "Safe schema change" — a change reaching a database that already holds rows must lose or
  alter no row.
- SAD §2 Constraints — `core-ledger` ADR-0001 puts transactions and transfers in separate tables, so
  anything covering both covers two.
- `ARCHITECTURE.md` — aggregates own their own data; a cross-aggregate reference is by id.

## Considered options

1. **The feature's own tables.** Rows owned by `ImportRun` point at `core-ledger`'s rows by id,
   with real foreign keys: movement links for the transactions and transfers a run recorded, and
   creation links for the accounts and categories it created. The fingerprint of ADR-0008 lives on
   the movement link.
2. **Columns on the ledger tables.** A nullable `import_run_id` is added to `transactions`,
   `transfers`, `accounts` and `categories`, with a foreign key back to the run.
3. **A polymorphic link table.** One table with a kind column and a bare id, with no foreign key,
   covering all four kinds in one shape.

## Decision outcome

**Chosen: option 1, the feature's own tables.**

It puts this feature's bookkeeping where this feature can change it. `core-ledger` owns the ledger's
four aggregates; a column on them that only `csv-import` ever reads makes every future change to
either feature a change to both. With option 1 the migration this feature ships **adds tables and
alters none**, which is the strongest possible form of `SPEC.md`'s safe-schema-change rule — there
is no backfill to design and no existing row to touch, so PRD §9's "nothing already recorded loses
anything" is true by construction rather than by care.

It also gives ADR-0008's fingerprint a natural home. The fingerprint is import bookkeeping, not
ledger data: it describes what *this load wrote*, not what the movement *is*. On a movement link it
sits next to the thing it describes. Under option 2 it would be a second import-shaped column on
`core-ledger`'s tables, and the awkwardness of the first one would simply double.

Option 2 is genuinely easier to read — "which load brought me" is a field, not a join — and that is
the whole of what it buys. Against it: four tables of another feature carrying a column for this
one, a second column for the fingerprint, and a coupling that goes the wrong way for a ring that is
supposed to be independent of whatever loads into it.

Option 3 was rejected because it drops referential integrity exactly where this feature needs it
most. Undo deletes by following these links; without a foreign key, a link pointing at a movement
that no longer exists is representable, and the failure shows up as a silently incomplete undo
rather than as an error. PostgreSQL can enforce option 1's links and cannot enforce option 3's.

## Consequences

**Positive**
- `core-ledger`'s tables are untouched. This feature's migration only adds, so PRD §9's promise
  holds with no backfill and no risk to recorded history.
- Real foreign keys, so a link can never point at a movement that is gone, and undo cannot be
  silently partial.
- The fingerprint of ADR-0008 has an obvious home, and nothing import-shaped leaks into the ledger.
- Undo is one walk of one run's links — the read PRD §9 calls "everything one load brought" — rather
  than four scans looking for a column value.

**Negative**
- "Which load brought this transaction?" is a join rather than a field. Nothing in the PRD asks that
  question from the transaction's side today, but a later history view that wants to mark imported
  rows will pay for it.
- More tables: separate shapes for movement links and creation links, each needing an entity
  configuration and an index on the run id.
- Two sources of truth about the same relationship could drift if anything ever wrote a movement
  without its link. Keeping both inside one transaction (ADR-0004) is what prevents that, so this
  record depends on that one holding.

**Neutral**
- The exact split — one link table with nullable foreign keys and a check constraint, or one table
  per kind — is a data-model question for the next stage. This record fixes only that the rows
  belong to this feature.
- `transactions.external_ref` stays what `core-ledger` ADR-0006 made it: a column on the ledger
  table, used by ADR-0002's exact tier. That is not bookkeeping about a run; it says where a row
  came from in the outside world, which is a property of the movement itself.

## Links

- PRD: [[../PRD.md]] — US-04, US-06, AC-13, AC-16, AC-17, AC-18, §9
- SAD: [[../sad.md]] §5, §6, §8, §10
- Related ADR: [[0004-commit-an-entire-import-run-in-one-database-transaction]] · [[0008-detect-post-import-hand-edits-with-a-stored-content-fingerprint]]
