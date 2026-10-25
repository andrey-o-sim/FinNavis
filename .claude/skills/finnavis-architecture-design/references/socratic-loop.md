# The walk — depth dial, state machine, per-section write, logs

Used in Phases 0.5 and 4. Self-contained: this skill depends on no file outside its own folder.

## Depth dial

One question at the top of the run, recorded in the SAD frontmatter as `depth:`. It tunes **how much
I decide alone**, never **what gets covered**.

| | **easy** | **medium** (default) | **hard** |
|---|---|---|---|
| §2 Constraints, §5 layout | one confirm for both | one confirm each | one confirm each, plus a question on anything the repo does not pin |
| §4 strategy | I take the reversible calls; ask only what the gate would catch | one question per strategic choice | every choice, each with its trade-off spelled out |
| §6 flows | I draft every flow and confirm the set in one question | one question per flow | one question per flow, plus the failure branches |
| §7, §8 | defaults applied, listed in the ledger | one question per real decision, project default offered first | every row walked |
| §10 scenarios | drafted from PRD §6, one confirm | one confirm per scenario | per scenario, plus the verification method |
| §11 risks | drafted, one confirm | one confirm, then refine | walked row by row |
| Assumptions ledger | yes — batch veto before the critic | no | no |

**The floors do not move with the dial:**

- The blast-radius gate runs on every Approved decision at every depth.
- `target_surfaces` is never assumed — §4's first question is asked even at `easy`.
- Every `save_as_oq` row carries owner **and** due.
- §10 numbers come from PRD §6 verbatim.
- §6 covers every critical flow. There is no later stage to finish it.

`easy` reaches the floors by deciding and stating; `hard` reaches them by asking. Dropping a floor
because the run is `easy` is a bug, not a depth choice.

### The assumptions ledger (easy only)

Every decision taken instead of asked is one line:

```
- Assumed: <decision> = <value> — because <reason>. [veto?]
```

They are surfaced together in one `AskUserQuestion` before Phase 5, as a batch veto: accept all, or
name the ones to turn into real questions. Autonomy without silent commitment.

## Walk order

Strictly §1 → §12. Each section is rendered whole, walked, written to `sad.md`, committed, and then
closed forever.

| Section | How it is handled |
|---|---|
| §1 Introduction and goals | Rendered; the top-3 quality goals get one question — they shape §10 |
| §2 Constraints | Auto-assembled from the repo; one confirm. Only unpinned things are asked |
| §3 Context and scope | Rendered; the C4 Context diagram is confirmed in prose |
| §4 Solution strategy | **The dense one.** Target surface first, then each strategic choice. Expect ADRs |
| §5 Building block view | Layout auto-rendered from conventions; aggregates, ports and the read path are walked. C4 Container confirmed in prose |
| §6 Runtime view | One question per critical flow. Covers all of them — nothing is deferred |
| §7 Deployment view | Walked by topic: topology, what the feature adds, backups, monitoring, scaling thresholds |
| §8 Crosscutting concepts | Walked by row, project default offered as the first option |
| §9 Architecture decisions | Not walked — assembled from the ADRs spawned in §4–§8 |
| §10 Quality requirements | One scenario per §1 quality goal. Numbers verbatim from PRD §6 |
| §11 Risks and technical debt | Assembled from the logs, PRD §8 and the unproven-ground list; then confirmed |
| §12 Glossary | Not walked — extracted from `docs/GLOSSARY.md` plus terms this document introduces |

**Never return to a written section.** Cross-section drift is the critic's job in Phase 5. Re-opening
§4 after §10 is written means the per-section batch was not trusted in the first place, and it costs
a commit that says nothing.

The one exception: §9 and §11 are *assembled last by construction*, because they index things earlier
sections produced. That is not a return — they were never walked.

## The state machine

Four states for every decision:

- **Approve** — keep it as drafted. No edits-log entry; Approved is the baseline. Then run the
  blast-radius gate on it ([blast-radius.md](blast-radius.md)).
- **Edit** — the user gives the new shape; I regenerate under that constraint and ask once more. One
  round only — the second answer is final. Log `action: "edit"`.
- **Save as open question** — the decision is not taken now. It leaves the section and becomes a §11
  row with severity literal `Open question`. Owner and due are **mandatory**: ask for both
  immediately. Missing either downgrades it to Drop, with a warning. Log `action: "save_as_oq"`.
- **Drop** — the decision is removed. If the section cannot stand without it (a surface, a storage
  shape, the last flow in §6), re-ask once with a reframed option set; a second drop escalates to an
  open question with a suggested owner and due. Log `action: "drop"`, `after: null`.

### Per-section notes

- **§4 Target surface** cannot be dropped and cannot be saved as an open question. Every later
  section and every later stage reads it. If the user is unsure, the answer is the narrowest surface
  that satisfies PRD §2, and the doubt becomes a §11 row.
- **§6 flow** — dropping a flow is legitimate only if the PRD does not require it. If it maps to an
  acceptance criterion, dropping it means §6 stops covering the PRD, so say that out loud before
  accepting the drop.
- **§7 monitoring** — "none" is a valid answer and a real decision. Record the reason, not the
  absence. A blank row reads later as an oversight.
- **§10 scenario** — a quality goal with no number in the PRD is stated as an invariant, honestly.
  Inventing a figure to fill the slot is worse than having none.

## Per-section write and commit

After the last decision in a section resolves:

1. Write the resolved section into `docs/features/<slug>/sad.md`, replacing the placeholder body and
   keeping the section's `<!-- … -->` contract comment.
2. Write any ADRs the gate spawned in that section into `docs/features/<slug>/adr/`.
3. Validate every Mermaid block just written ([c4-and-sequences.md](c4-and-sequences.md)).
4. Stage and commit — or print the command, depending on what the user chose in Phase 0.5:

```
03: sad §<N> — <what the section settled, in the user's own terms>
```

The document is the state. An interrupted run resumes by reading `sad.md` and continuing at the first
section whose body is still a placeholder — no context to reconstruct, nothing lost.

## Edits-log

After every Edit / Drop / Save-as-OQ — never after Approve — append:

```
{item_id:     "§4-surface" | "§4-strategy-2" | "§6-flow-3" | "§8-row-ids",
 action:      "edit" | "drop" | "save_as_oq",
 before:      "<verbatim text before the action, or null>",
 after:       "<verbatim text after — for save_as_oq the §11 row incl. owner+due; null for drop>",
 user_reason: "<the reason the user gave, verbatim>"}
```

This log and the ADR-spawns log are the **only** signals the clean-context critic has for telling what
the conversation changed. If the user gives no reason on a Drop or a Save-as-OQ, ask once more — the
verbatim wording is what makes the finding readable later.

## Cadence

- A one-line recap of the decisions so far every ~5 questions. The chain §4 → §5 → §6 is a dependency
  chain, and it should be visible without scrolling.
- Never more than 4 options in a question, never more than 4 questions in one `AskUserQuestion` call.
- Bundle the trivial, spend on the expensive. One question per *uncertainty*, not per *parameter*.
- Target for a whole M-sized pass: **10–22 questions**. Past 28 it is fatigue, and fatigue produces
  Approve-everything, which is the same as not asking.

## Exit condition

The walk is done when every section is written and committed, §9 is closed against `adr/`, §11 holds
a row for every `save_as_oq`, and no log entry is pending. Then Phase 5 — the second reader.
