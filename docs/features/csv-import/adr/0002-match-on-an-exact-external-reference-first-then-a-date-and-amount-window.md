---
status: Accepted
slug: "csv-import"
stage: "03"
updated_at: "2026-11-07"
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
They were settled on 2026-11-07 — see the amendment below.

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

## Amendment 2026-11-07 — the parameters of the proximity tier

The open half of this record is closed. The owner opened a real bank statement and a real export of
the old tracker, and answered PRD §8. The shape above is unchanged, and the Status stays Accepted.
What changed is listed here, so the original decision stays readable as it was taken.

**What the real files showed:**

- **Neither file carries a stable operation number.** The statement has no identifier column. The
  export has none either. So the exact tier would be dead code for both the statement load and the
  history migration, and the proximity tier carries everything. The exact tier is therefore **not
  built now**: the matcher is one tier. The two-tier shape stays the decision, so the day a bank or
  an export carries an identifier, the exact tier is added in front of the proximity tier with no
  change to the rest.
- **The statement names a time as well as a date** (`dd.MM.yyyy HH:mm:ss`). The time is in the
  bank's own zone, which is not the zone the owner lives in. A purchase late in the evening can
  carry the next day's date in the statement.
- **The export names a date only** (`yyyy.MM.dd`). The owner typed those dates, so they are the
  dates the owner meant.

**The parameters:**

| Parameter | Statement load | History migration |
|---|---|---|
| Account | the same account | the same account |
| Amount | exactly equal | exactly equal |
| Date window | the file's date ±1 day | the file's date only (window 0) |
| The file's text | takes no part | takes no part |

- **The date window is a parameter of the policy object** (ADR-0005), not one number for both
  jobs. The statement needs ±1 day because of the time-zone shift above. The export needs no window,
  and a narrower window means fewer false matches.
- **Only the calendar date of the statement's timestamp is compared.** The time is read and then
  dropped, because a movement in the ledger carries a date, not a time.
- **The file's text takes no part, so there is no normalisation rule.** A bank description such as
  a shop name and a city almost never equals the note the owner typed by hand. If the text took
  part, AC-21 would almost never fire, and a hand-typed movement would be recorded a second time.
- **One ledger movement covers at most one file row.** When a ledger movement is matched, it is
  removed from the candidates for the rest of the run. So two identical rows in one file against one
  hand-typed movement give one row "already there" and one row recorded. This is also what keeps
  the *Duplicate* NOT-boundary in `GLOSSARY.md`: two real identical spends are both kept.
- **When one file row matches more than one ledger movement**, the movement with the closest date
  is taken. On equal distance, the movement with the smallest `id` is taken, so the result is the
  same on every run. The row is never set aside for this, and no new set-aside reason is added. If
  the wrong movement is taken, the worst outcome is one visible extra row later — the cheaper of the
  two failures in the Context above.

**What this does not change:** the proximity tier is still a heuristic. A false match is less
likely with these parameters, but it is still possible. `sad.md` §11 keeps that risk.

## Links

- PRD: [[../PRD.md]] — US-05, US-07, AC-15, AC-20, AC-21, §8 open questions
- SAD: [[../sad.md]] §4, §6, §8, §10, §11
- Related ADR: [[0003-take-one-snapshot-of-candidate-movements-before-the-run-writes]] · `core-ledger/adr/0006-add-a-nullable-external-reference-to-transactions-now`
