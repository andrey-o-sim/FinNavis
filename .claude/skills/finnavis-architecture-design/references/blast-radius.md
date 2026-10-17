# Blast radius — when a decision earns an ADR

Used in Phase 4, on every **Approved** decision. Self-contained.

A pass over one feature settles 15 to 30 things. Write an ADR for each and the folder becomes
unreadable, so nobody reads any of them. Write none and the expensive choices lose their *why* —
which is the only thing an ADR is for. The gate below picks the right handful.

The question an ADR answers is always the same: **six months from now, reading the code alone, would
I wonder why it was done this way?**

## The three criteria

Score each Approved decision. **Two of three → ADR.**

### 1. Expensive to reverse — would redoing it cost ≥3 evenings?

Evenings, not person-days: that is the unit this project actually spends, and the one `.size` and the
PRD already use.

Fires: the storage shape (one table or two), mutable rows versus append-only history, the identity
strategy, whether a read aggregates in SQL or in memory, whether a column other features will depend
on exists from day one.

Does not fire: a package choice inside the same ecosystem, a timeout value, a folder or type name, a
log message. An IDE rename is not a reversal cost.

### 2. Crosses a boundary — does it touch ≥2 of the four projects, or reshape data already recorded?

Two ways to fire, and the second one is specific to this repo.

**Crosses a ring.** The dependency rule means a decision that changes what Domain declares changes
what Infrastructure implements and what Presentation registers. A new port, a changed port, a new
kind of port — all of them are seen by at least two projects.

**Reshapes recorded history.** `SPEC.md` holds the whole product to one rule: a change reaching a
database that already holds rows must lose or alter no row. Any decision that would break that rule,
or that forces a future feature to break it, fires this criterion on its own — the cost is not in the
code, it is in the data that already exists. Add the column now and the cost is one migration against
an empty table; add it in a year and it is a migration plus a backfill against real history.

Does not fire: a private method signature, the internal layout of one use case, a response field
name inside one operation's own folder.

### 3. A real alternative exists — would a competent reader ask "why not X?"

X has to be real. Two things disqualify it:

- **A constraint already excludes it.** "Why not MongoDB" is not a question in a repo whose `SPEC.md`
  names PostgreSQL. An ADR whose rejected option was never available is a strawman, and the critic
  flags it.
- **It is obviously worse.** If writing the "Considered options" section feels like building a
  scarecrow to knock down, there was only ever one option. Record it inline.

Fires when two reasonable engineers would genuinely split — the kind of choice where the rationale is
invisible in the resulting code.

## Never an ADR in this repo

Everything in [repo-conventions.md](repo-conventions.md) section A is already decided. An ADR for any
of it fails criterion 3 by construction, because the alternative is excluded by a constraint. The
list below is not exhaustive; the rule is.

- Enums stored as strings. Money as `decimal`. Timestamps as `DateTimeOffset` in UTC.
- `snake_case` tables and columns. One `IEntityTypeConfiguration<T>` per entity.
- Clean Architecture, four projects, dependencies inward, ports in the two inner rings.
- Minimal APIs, one folder per operation, contracts co-located, no shared response records.
- xUnit + AwesomeAssertions, Testcontainers for anything touching SQL.
- No authentication in the current product. No availability target.

Writing one of these into §2 or §8 as an inherited constraint is correct. Writing it into `adr/` is
not — it dilutes the folder and the critic will say so.

Equally: **a decision an earlier feature's Accepted ADR already made is not re-decided here.** Cite it
and move on. If this feature genuinely needs the opposite, that is a new ADR that supersedes the old
one, and it starts by saying which one it supersedes.

## Scoring, in practice

| Fires | What to do |
|---|---|
| 0 | Inline in the section. No ADR. |
| 1 | Borderline — ask. Default to inline, except in §4, where the bar is lower because every §4 choice is strategic by definition. |
| 2 or 3 | ADR, spawned with its section. |

The borderline question is one `AskUserQuestion`, phrased plainly:

> This is on the edge. It is hard to undo (criterion 1), but it lives inside one project, so a reader
> may never need the backstory. Lock it as an ADR, or keep it as a paragraph in §5?

Options: `Lock as ADR (Recommended if it is hard to undo)` / `Inline in §5 only`.

## How many to expect

| `.size` | ADRs | Note |
|---|---|---|
| XS / S | 2–4 | Below 2, check you did not miss an irreversibility |
| M | 5–12 | `core-ledger` landed on 6 and that was right |
| L / XL | 10–15 | More than 15 means tactical detail crept into `adr/` |

Counts are a sanity check, not a quota. Never invent an ADR to reach a number, and never bundle two
real decisions into one file to stay under it.

## While the walk runs

Keep an **ADR-spawns log** next to the edits-log:

```
{adr:      "0003",
 title:    "<decision in imperative form>",
 section:  "§4",
 score:    "irreversible + crosses a ring",
 options:  ["<option 1>", "<option 2>", "<option 3>"],
 chosen:   "<option N>",
 reason:   "<the user's own words for why>"}
```

The critic reads this log. It is the only way a reader who never saw the conversation can tell a real
option set from a reconstructed one.

## Writing the file

Numbering restarts at `0001` inside `docs/features/<slug>/adr/`. The repo-root `docs/adr/` is for
project-wide decisions and is never renumbered from here.

**The title is the decision, not the problem.** This is the difference between a file you can scan
and a file you have to open:

- ✅ `0001-keep-transactions-and-transfers-in-separate-tables.md`
- ✅ `0003-edit-and-delete-ledger-rows-in-place.md`
- ❌ `0001-storage-strategy.md`
- ❌ `0003-mutability.md`

`Status: Accepted`, always. This skill decides synchronously, with the user, right now — there is
nobody to review it afterwards, so `Proposed` would be a status that never changes. An asynchronous
Proposed → Accepted flow belongs to a different skill.

**Considered options lists every option that was actually on the table**, including the ones rejected
in the `AskUserQuestion`. Consequences carry **Negative** and **Neutral** entries, not only Positive —
an ADR with three upsides and no cost is marketing, and it is the clearest sign the alternatives were
never seriously held.

## Closing self-review, before the critic

1. Does §9 have a row for every file in `adr/`, and a file for every row?
2. Does every ADR state an outcome, not only a context?
3. Would the gate still pass each one on a second run? If one now looks like a config value, fold it
   back inline and delete the file.
4. Does any inline decision now look like it should have been an ADR? Promote it — the §11 Open
   questions table is not where a settled decision goes.
