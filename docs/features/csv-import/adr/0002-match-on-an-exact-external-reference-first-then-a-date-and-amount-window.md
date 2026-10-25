---
status: Accepted
slug: "csv-import"
stage: "03"
updated_at: "2026-10-17"
---

# 0002 — Match an imported row on an exact external reference first, then on a date-and-amount window

- **Status:** Accepted
- **Date:** 2026-10-17
- **Deciders:** Andrey Simonenko

## Context

Three acceptance criteria turn on one question. AC-20: the same file handed over twice records
nothing a second time. AC-15: after a rule is added and the file is handed over again, the rows that
were waiting land and the rows the earlier load recorded do not repeat. AC-21: a file row that names
a movement the Owner already typed by hand on the day counts as already there, rather than being
recorded again or set aside.

`GLOSSARY.md` defines *Duplicate* as a row in a loaded file naming a movement already recorded — and
draws the boundary explicitly: two separate spends that happen to share a date, an amount and a
description are **not** duplicates; both are real and both belong in the ledger.

PRD §8 leaves two things open with a deadline before implementation: what exactly makes two rows
name the same movement, and whether a real statement or a real export carries a stable operation
number at all. Neither can be answered without opening a real file. What can be decided now, and
has to be before §5 (Building block view), §8 (Crosscutting concepts) and §10 (Quality requirements)
can be written, is the *shape* the rule takes.

The asymmetry of the two failures decides the tone of the whole record. A false match drops real
spending out of the ledger silently, with nothing shown to the Owner. A missed match leaves one
visible extra row they can delete. The first is far worse.

## Decision drivers

- PRD §5 AC-20, AC-21, AC-15 — the three criteria this rule has to satisfy.
- PRD §8 — the exact predicate and the existence of an operation number are open, due before
  implementation starts.
- `GLOSSARY.md` *Duplicate* — the NOT-boundary: two coincidentally identical spends are both real.
- SAD §1 quality goal 1 — nothing is recorded twice.
- SAD §2 Constraints — `core-ledger` ADR-0006 already put `transactions.external_ref` in place,
  nullable with a unique index, and deliberately left the detection rule to this feature.

## Considered options

1. **Two tiers.** First an exact match on `external_ref`, when the file supplies a stable
   per-row identifier. Then, for every row that has no such identifier or found no exact match, a
   proximity match inside a candidate window — same account, date within a window, same amount, and
   possibly the file's text.
2. **Proximity only.** One code path. `external_ref` takes no part in the check, whether the file
   carries an identifier or not.
3. **Exact only.** Match solely on `external_ref`, and treat a file that carries no identifier as
   unsupported.

## Decision outcome

**Chosen: option 1, two tiers.**

Neither tier covers the other, which is what makes this a shape rather than a preference. The
proximity tier cannot be dropped: AC-21 requires a file row to recognise a movement the Owner typed
by hand, and a hand-typed movement has `external_ref` null by construction (`core-ledger` ADR-0006
writes null into it everywhere). The exact tier cannot be dropped either, where it is available:
`external_ref` carries a unique index, so a second load of a row the bank numbered is refused by
PostgreSQL rather than by a heuristic. Given that a false match silently destroys real spending,
replacing a database guarantee with a guess wherever the guarantee exists is the wrong trade.

Option 2 was rejected for that reason: it makes a file that carries a perfectly good operation
number no safer than one that does not, and it leaves the column ADR-0006 added specifically for
this feature unused — which would turn that record retrospectively into a mistake.

Option 3 was rejected because it fails AC-21 outright and makes the whole feature depend on an
answer PRD §8 does not have yet. If a real statement turns out to carry no operation number, option
3 means nothing works.

**What this record does not decide.** The parameters of the proximity tier — how wide the date
window is, whether the file's description takes part in the comparison and how it is normalised, and
what happens when one file row matches two ledger movements — stay open. They need a real statement
and a real export opened first, which is exactly what PRD §8 says. They are carried into §11 (Risks
and technical debt) with an owner and a due date. The shape above holds whichever way they land.

## Consequences

**Positive**
- Where the bank numbers its rows, "already recorded" is a uniqueness constraint in the database
  rather than a judgement, and it cannot race or drift.
- AC-21 is satisfiable at all, which the exact tier alone would not be.
- `core-ledger` ADR-0006's column gets the use it was added for, with no migration against a table
  that by then holds real history.
- The open parameters are confined to one tier, so the rest of the design can be finished now.

**Negative**
- Two code paths and two sets of tests for one question, and they can disagree: a row can miss on
  the exact tier and then be caught by proximity, or the reverse, and the reported reason has to
  stay truthful about which one fired.
- The proximity tier stays a heuristic however it is tuned, so the risk of a silent false match is
  reduced and not removed. §11 carries it.
- Until PRD §8 is answered, the feature cannot be implemented from this document alone. That is
  honest rather than convenient.

**Neutral**
- Nothing here says where `external_ref` gets its value from, or what a row with no identifier
  writes into it. That is a data-model question and belongs to the next stage.
- Transfers have no `external_ref` — `core-ledger` ADR-0006 put it on transactions only. A Transfer
  recorded by a history migration (AC-11) is therefore reachable by the proximity tier only. In
  practice the old tracker's export is loaded once, so this costs nothing today; it is noted so the
  next reader does not assume symmetry.

## Links

- PRD: [[../PRD.md]] — US-05, US-07, AC-15, AC-20, AC-21, §8 open questions
- SAD: [[../sad.md]] §4, §6, §8, §10, §11
- Related ADR: [[0003-take-one-snapshot-of-candidate-movements-before-the-run-writes]] · `core-ledger/adr/0006-add-a-nullable-external-reference-to-transactions-now`
