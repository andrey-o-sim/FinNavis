---
status: Accepted
slug: "csv-import"
stage: "03"
updated_at: "2026-10-17"
---

# 0008 — Detect post-import hand edits with a content fingerprint stored at import time

- **Status:** Accepted
- **Date:** 2026-10-17
- **Deciders:** Andrey Simonenko

## Context

AC-18: when a load's rows have since been changed by hand, undoing that load is blocked and the
changed rows are named back. The reason is plain — undo deletes, and deleting a row the Owner has
since corrected throws away work they did by hand.

The difficulty is that the ledger keeps no trace of an edit. `core-ledger` ADR-0003 decided
corrections edit and delete rows in place, with no audit trail, and recorded that absence as
accepted debt. So at undo time there is nothing in the ledger that says whether a movement still
looks the way the import wrote it.

This has to be solved by this feature, because AC-18 is this feature's criterion and `core-ledger`
has no reason of its own to care.

## Decision drivers

- PRD §5 AC-18 — undo is blocked and the changed rows are named back.
- PRD §10 — the exact wording the Owner is told: "Some rows from this load were changed by hand
  afterwards, so the load cannot be undone as a whole."
- SAD §1 quality goal 2 — a load is all-or-nothing and reversible; AC-18 is the limit of that
  promise.
- SAD §2 Constraints — `core-ledger` ADR-0003: rows are edited in place and no audit trail exists.
- SAD §2 Constraints — one developer. A mechanism that depends on somebody remembering a rule in
  another feature's code is a mechanism that will eventually be wrong.

## Considered options

1. **A content fingerprint stored at import time.** When the run records a movement it also stores
   a hash of the fields it wrote — account, date, amount, category, note. Undo recomputes the hash
   from the current row and compares. A difference means the row was changed.
2. **A `updated_at` column on the ledger tables.** `core-ledger` adds a nullable timestamp and sets
   it on every edit. Undo blocks if any of the run's movements has a non-null value.
3. **Do not detect it.** Undo always proceeds, and AC-18 is dropped or deferred.

## Decision outcome

**Chosen: option 1, a content fingerprint stored at import time.**

The deciding property is that it depends on nothing outside this feature. The fingerprint is written
by the import and read by the undo, both inside the same pipeline. There is no rule another feature
has to keep obeying, so no future edit path can break AC-18 by forgetting something. Option 2's
failure mode is exactly that, and it is the worst kind: a second way to edit a transaction, added a
year from now by the same person, that does not set `updated_at` — after which AC-18 passes its
tests, reports nothing, and quietly deletes corrected rows.

Option 1 also behaves better in one case that matters. If the Owner edits a row and then puts it
back the way it was, the fingerprint matches and undo proceeds — correctly, because the data really
is what the import wrote. Option 2 blocks undo over a change that no longer exists.

Option 2 is cheaper on storage and would give the whole product a useful "last modified" field. If
that field is ever wanted for its own sake, it should be added for that reason, by `core-ledger`,
and AC-18 should still not lean on it.

Option 3 was rejected because AC-18 is an approved acceptance criterion and the behaviour it
protects is real: the Owner fixes a category by hand after an import and later undoes the load for
an unrelated reason.

**What it does not cover.** A movement of the run that the Owner *deleted* by hand is simply absent
at undo time. That is not a conflict — there is nothing left to remove — so undo proceeds for the
rest. It is stated here so nobody later reads the fingerprint as a general integrity check.

## Consequences

**Positive**
- AC-18 cannot be broken by a change in another feature, now or later.
- Edit-and-revert does not block undo, which is the correct behaviour and a free side effect.
- `core-ledger`'s tables stay untouched, which is what ADR-0007 is about; the fingerprint lives on
  the movement link next to the thing it describes.
- The check is a pure comparison over data the run already loads for undo, so it adds no query.

**Negative**
- A fingerprint per recorded movement is stored data that is read exactly once, and only if the
  Owner undoes the load. Most of it is never used.
- The field list the hash covers is a decision that can be wrong in both directions. Too narrow and
  a real edit slips past; too wide and something the import did not write — a field a later feature
  adds — starts blocking undo for no reason. The list has to be explicit, and it has to be revisited
  whenever a movement gains a field.
- The hash is opaque. When undo is blocked, naming the changed rows back (AC-18, PRD §10) means
  reporting *which* rows differ, not *what* differs, unless more than a hash is stored.

**Neutral**
- The hash algorithm and the exact field list are data-model questions for the next stage. This
  record fixes that the check is a stored fingerprint rather than a timestamp.
- Nothing stops `core-ledger` adding `updated_at` later for its own reasons. This record only says
  AC-18 does not depend on it.

## Links

- PRD: [[../PRD.md]] — US-06, AC-16, AC-18, §10 refusal messages
- SAD: [[../sad.md]] §5, §6, §8, §10, §11
- Related ADR: [[0007-keep-import-bookkeeping-in-the-features-own-tables]] · `core-ledger/adr/0003-edit-and-delete-ledger-rows-in-place`
