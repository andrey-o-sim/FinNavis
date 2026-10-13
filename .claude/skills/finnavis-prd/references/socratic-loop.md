# Socratic walk — depth dial, 5-state machine, edits-log, floors

Used in Phase 0.5 (the dial) and Phase 6 (the walk). Self-contained: this skill depends on no shared
file outside its own folder.

## Depth dial

One question at the top of every run, recorded in the PRD frontmatter as `depth:`. It tunes **how much
I decide alone**, never **what gets covered**.

| | **easy** | **medium** (default) | **hard** |
|---|---|---|---|
| Phase 2 deep dive | carried §15 questions + `SPEC.md` contradictions only | 3-5 questions, batches of 2-3 | the whole rule bank, every trade-off foregrounded |
| §4-§7 walk | I resolve the reversible items with stated defaults; ask only the irreversible ones | one question per real decision | every decision, each with its trade-off spelled out |
| §9-§11 | one bundled confirm for all three | one confirm each | one confirm each, plus per-row questions where a choice exists |
| Assumptions ledger | yes — batch veto before the write | no | no |

**The floors do not move with the dial.** The §5 coverage model, the use-case floor, the §8 owner+due
rule, the §9 read-path and history answers, the §10 and §11 coverage rules hold at `easy` exactly as
at `hard`.
`easy` reaches them by deciding and stating; `hard` reaches them by asking. Dropping a coverage type
because the run is `easy` is a correctness bug, not a depth choice.

### The assumptions ledger (easy only)

Every decision I took instead of asking is one line:

```
- Assumed: <decision> = <value> — because <reason>. [veto?]
```

All of them are surfaced together in one `AskUserQuestion` before `ExitPlanMode`: accept all, or name
the ones to turn into real questions. Autonomy without silent commitment.

## The walk

Per section, in order: **§4 user stories → §5 acceptance criteria → §6 NFR rows → §7 KPIs**.

1. **Render the whole section first** — the full text plus a numbered list of the decisions inside it.
   The user sees the shape before committing to any single item, so they catch duplicates and gaps early.
2. **Walk item by item** — one `AskUserQuestion` per item, phrased per
   [ask-style.md](ask-style.md).
3. **Apply each transition in memory** as the answer arrives. Nothing is written to disk until Phase 10.
4. **Run the §5 floors** after the §5 section is resolved (and again after any later change to it).
5. **Move on.** I never return to a finished section — cross-section drift is the critic's job in Phase 7,
   not a second walk.

§1-§3 are rendered but not walked item by item: they carry no decision set, and the user edits them
inline if the wording is off. §9, §10 and §11 are rendered whole and confirmed with one question each —
they are derived from §5. If a confirm comes back `Edit`, walk the changed rows item by item.

## The state machine

Four states for every item, five for an acceptance criterion:

- **Approve** — keep it verbatim. No edits-log entry; Approved is the baseline. Move on.
- **Edit** — the user gives the new wording or value; I regenerate under that constraint and ask once
  more. One round only — the second answer is final. Log `action: "edit"`.
- **Save as open question** — the item leaves its section and becomes a §8 row. Owner and due are
  **mandatory**: ask for both immediately. Missing either downgrades it to Drop, with a warning. Log
  `action: "save_as_oq"`.
- **Drop** — the item is removed. If it was mandatory (a coverage type, or the last AC of a retained user
  story), re-ask once with a reframed option set; a second drop escalates to an open question with a
  suggested owner and due. Log `action: "drop"`, `after: null`.
- **Add another AC** (§5 only) — the user dictates one more criterion; I write it in business form, tag
  its coverage type, and run a one-question mini-batch on it. Log `action: "add"`.

### Per-section notes

- **§4 user story** — dropping a story is a legitimate de-scope. It does not fire the use-case floor; it
  takes its ACs with it.
- **§6 NFR row** — a bare adjective ("fast", "quick", "reliable") is never approvable. Force a number or
  an open question with owner + due. A number with no way to measure it today is approvable only if §9 or
  §11 names standing up the measurement as a prerequisite.
- **§7 KPI** — a baseline of 0 is fine for a new feature. A baseline of TBD needs an inline measurement
  plan or an open question.

## Edits-log

After every Edit / Drop / Save-as-OQ / Add — never after Approve — append:

```
{item_id:     "US-06" | "AC-04" | "NFR-row-2" | "KPI-01",
 action:      "edit" | "drop" | "add" | "save_as_oq",
 before:      "<verbatim text before the action, or null for add>",
 after:       "<verbatim text after — for save_as_oq the §8 row incl. owner+due; null for drop>",
 user_reason: "<the reason the user gave, verbatim>"}
```

This log is the **only** signal the clean-context critic has for detecting drift caused by edits. If the
user gives no reason on a Drop or a Save-as-OQ, ask once more — the verbatim wording matters.

## Cadence

- A one-line recap of the decisions so far every ~5 questions, so the dependency chain is visible without
  scrolling.
- Never more than 4 options in a question, and never more than 4 questions in one `AskUserQuestion` call.
- Batch the trivial items; spend the questions on the ones that are expensive to reverse.

## Exit condition

The walk is done when every item in §4-§7 has exactly one resolution applied, §9-§11 are confirmed, both
§5 floors hold, and the edits-log has no pending entry. Then Phase 7 (the critic).
