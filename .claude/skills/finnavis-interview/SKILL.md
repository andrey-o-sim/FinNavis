---
name: finnavis-interview
description: >
  Ideation interview for a solo, self-hosted pet project (FinNavis and projects
  shaped like it). Socratic deep dive, competitive research over tools the author
  actually uses, 3 strategic approaches via parallel sub-agents, three solo-shaped
  review personas (Builder / Future-me / User-me), devil's advocate, Claude-proposed
  RICE with Reach pinned to 1, feasibility read off the repo itself. Writes
  docs/features/<slug>/idea-brief.md (17 sections, <=5 pages). Triggers on
  "raw idea", "capture an idea", "interview a feature X", "brief for X",
  "new feature X", "idea brief", "ideation for {slug}", "/finnavis-interview {slug}".
  Use in a single-owner repo with no team, no customers and no release windows.
argument-hint: "[slug] - kebab-case feature name. Omit it and the skill proposes one"
---

# Ideation interview — solo project

One entry point for the ideation stage of a pet project. It turns a raw idea into
`docs/features/<slug>/idea-brief.md`: a product document the next stage can build from.

The skill is tuned for a very specific situation, and that tuning is the whole point:

- **One owner, who is also the only user.** No segments, no customers, no churn.
- **No team.** Nobody to ask about capacity, nobody who "already shipped something similar".
- **Evenings, not sprints.** The budget is irregular hours, not person-weeks on a plan.
- **Learning is part of the goal.** "Harder than it needs to be" is sometimes the reason to
  build it, not a risk to flag.

A generic ideation protocol misfires on all four. This one does not.

## When to use

- "capture an idea <slug>", "brief for <feature>", "interview a feature <slug>".
- "ideation for <slug>", "raw idea for <feature>", "turn this into a brief".
- `/finnavis-interview <slug>` as an explicit call.
- The user drops an idea in prose and asks to turn it into a brief.

## When not to use

- **The brief already exists** with `status: Confirmed` and is fresh (under two weeks).
  Update it instead of rewriting it.
- **The decision is already made** and you just want the work done — go straight to
  `my-toolkit:task-develop`. This skill exists to compare options, not to record one.

## Language

Project rule, loaded from [`.claude/rules/language.md`](../../rules/language.md): ask in the user's
language, write the artifact in English, quote the user verbatim (§1 Raw idea **is** a quote), name
every section instead of numbering it.

Two things specific to this stage:

- The question bank in `references/socratic-bank.md` is English. Translate each question into the
  user's language before asking it; keep the meaning, not the words.
- At checkpoint time the brief does not exist yet, so a bare `§11` cannot even be looked up. The
  numbering to name from:
  §1 Raw idea · §2 Problem · §3 Who it serves · §4 Why now · §5 Out of scope ·
  §6 Competitive analysis · §7 Strategic approaches · §8 Three-lens review ·
  §9 Trade-offs and edge cases · §10 Risks · §11 RICE · §12 Feasibility · §13 Recommendation ·
  §14 Parked and rejected · §15 Open questions · §16 What this unlocks · §17 Learning goal.

## Inputs

- `<slug>` — kebab-case, short (`core-ledger`, `csv-import`). No slug given? Propose two or
  three from the idea and let the user pick.
- Optional: notes, links, an earlier brief the idea grew out of.

## Depth regulator (first checkpoint)

The first `AskUserQuestion` of every run. Recorded in the brief frontmatter as `depth:`.

- **easy** — 3-4 checkpoints. Phase 2 is one batch of 2-3 questions. Phases 9-11 merge into a
  single confirm. Phases 4-8 run without sub-agents: one or two searches for §6, a paragraph
  per approach, a self-written list of 3-5 risks. All 17 sections still get filled, just tighter.
- **medium** (default) — the full protocol below.
- **hard** — the full protocol, plus a second Socratic batch in Phase 2 and a wider §6
  (five or more products).

`easy` shortens the interview. It does not make the checkpoints optional. Its 3-4 questions
are real questions, and inventing the answers voids the artifact exactly as it would at
`medium`.

## Mode

**Planning-mode native.** Phases 0-11 are read-only: Read, Glob, Grep, WebSearch, Agent,
AskUserQuestion. Everything lives in session memory. The first write happens in Phase 12,
after `ExitPlanMode`.

The `AskUserQuestion` checkpoints in Phases 1, 2, 9, 10 and 11 are not clarifying questions —
they are the data input. Without them the output is the model's reconstruction of a plausible
idea, not the user's idea. If an `Auto Mode` system-reminder is active it does **not** cancel
them: Auto Mode is about not pausing to ask permission between steps, not about skipping the
interview. If a checkpoint is denied by permissions, stop and say so. Do not work around it.

If `ExitPlanMode` is unavailable (the session did not start in plan mode), skip Phase 11.5 and
run Phase 12 directly. Nothing else changes.

## Protocol

### 0. Setup (read-only)

- **Read** `./templates/idea-brief.md` into session memory. Do not copy it yet.
- **Read** `docs/GLOSSARY.md` — load `## Glossary` into session state.
- **Read** `SPEC.md` if present — it holds scope and the draft requirement list. Later features
  are usually already named there; §16 needs them.
- **Check** `docs/features/<slug>/idea-brief.md` does not already exist as `Confirmed`.
- **No Write, Edit or mkdir.** Creating the directory is a step of the plan, not a step of setup.

### 0.5. Depth checkpoint (AskUserQuestion — mandatory)

One question: interview depth. See "Depth regulator".

### 1. Idea capture (AskUserQuestion — mandatory)

One question: "describe the idea in 1-3 sentences, in your own words". Keep the answer
**verbatim** in session memory as the draft of §1. Do not clean it up, do not translate it. It
is the baseline the rest of the interview is measured against, and the user's own wording is
often sharper than any rewrite — in `core-ledger` the whole category model is a direct quote.

### 2. Socratic deep dive (AskUserQuestion — mandatory)

Pick 3-5 questions from → [references/socratic-bank.md](references/socratic-bank.md), matched
to the shape of the idea. Deliver in batches of 2-3, never all at once.

The bank has six categories; the two that matter most here and are missing from generic
protocols are **"why is the tool you use today not enough"** and **"what does this unlock"**.
On a one-user project those two carry the priority signal that Reach normally carries.

### 3. Glossary capture (confirm now, write in Phase 12)

Every new domain word in the user's answers is handed to **`finnavis-fix-term`**, which owns the
glossary — the filter for what counts as a domain word, the line format, the mandatory
NOT-boundary and the alphabetical position all live there, so this skill no longer carries its own
copy of them.

Run its steps 1-5 **now**, while the answer that produced the word is still on screen: that is
where the user can say what they meant. Those steps only ask, and asking is allowed in plan mode.

Do **not** let it write `docs/GLOSSARY.md` here — Phases 0-11 are read-only because
`ExitPlanMode` has not run yet, and a write would fail. The confirmed lines come back as session
state in `pending_glossary_terms`, and Phase 12 writes them.

If the interview did not settle a word, do not invent a definition to fill the glossary. It goes
to §15 Open questions with an owner and a due stage — an unresolved entry is worse than no entry,
because later stages trust it.

### 4. Competitive research (Claude-driven, read-only)

WebSearch for 3-5 products. On a personal project the right set is not "the market" — it is:

1. **The tool the user is leaving.** Named in Phase 2. Its gaps are the actual requirements.
2. **Two or three tools that expose the same thing through an interface the user wants.**
3. **One self-hosted or open-source option**, because that is what the user is building.

Build the §6 table in session memory: **Product · URL · Relevant features · Value 1-5 · Gap**.
One footnote per row with the date and the query used. Finish with a **findings paragraph**
naming the one combination nobody offers — that is the wedge, and §13 will cite it.

If the idea genuinely has no market (pure plumbing), write `N/A — internal plumbing` with the
reason. An invented competitor is worse than an honest N/A.

No user input in this phase.

### 5. Strategic approaches (3 parallel Agent calls, read-only)

Three sub-agents in one message, one shared prompt template, three stances. The stances and the
alternatives that typically show up on this kind of project are in
→ [references/approaches.md](references/approaches.md).

- **A — Simplicity**: the shortest path to the stated success metric. Fewest moving parts.
- **B — Differentiation**: the version that makes the thing worth owning rather than buying.
- **C — Balanced**: the trade-off between the two.

Each returns one paragraph with: **Name** (3-5 words) · **Thesis** (one sentence, product
language) · **For whom** (the owner, in which situation) · **Outcome metric** (one KPI,
baseline → target) · **Key trade-off** (one line) · **Effort signal** (S/M/L).

Tell the sub-agents in the prompt: no library, framework or datastore names. The output is a
product document.

### 6. Three-lens review (3 parallel Agent calls, read-only)

Three sub-agents in one message, each sees all three approaches. The personas are **not**
Engineer / Executive / UX — on a one-person project two of those three are the same person
wearing a hat they do not own. The replacements and their prompts are in
→ [references/personas.md](references/personas.md):

- **Builder** — what it costs to build and to keep correct, in the abstract.
- **Future-me** — the owner six months out: what breaks, what has to be re-opened, what was
  spent on the wrong thing.
- **User-me** — the owner using it on a Tuesday evening with a receipt in hand.

Each returns 3-5 bullets per approach. Build the §8 synthesis matrix (3 lenses × 3 approaches),
`+` / `0` / `−` with a six-word justification in every cell.

### 7. Trade-offs and edge cases (synthesis, read-only)

Claude synthesises, no user input: a pros/cons row per approach, and 5-8 edge cases any
approach has to survive. Bias the edge cases toward the three things that actually hurt a
ledger-shaped personal project: history that changes after it has been read, entities renamed
or deleted under existing records, and local-time versus stored-time boundaries.

### 8. Devil's advocate (1 Agent call, clean context, read-only)

Spawn one sub-agent with **no upstream context** — it must not inherit the optimism of Phases
1-7. Prompt it for 5-10 ways this fails.

Failure signals on a solo project are not incidents and churn. Tell the sub-agent to use
these instead: **the project gets abandoned half-entered**; **the data stops being trusted**;
**the owner keeps using the old tool in parallel**; **a later feature needs a backfill over
every row already entered**. Each attack vector names what breaks and how the owner would
notice.

The most critical vector is reserved for §10 Risks. The rest feed §9.

### 9. RICE (AskUserQuestion — mandatory)

Claude computes, the user confirms. Never ask the user for raw R/I/C/E numbers — they have no
grounding to answer and it turns the interview into a calculator game.

- **Reach — always 1.** One owner, one user. Do not ask for it, do not vary it, state it.
- **Impact** — from §2 problem severity and the Future-me lens in §8. How many of the pains
  named in Phase 2 does this close directly?
- **Confidence** — from the count of open questions in §15. Many unresolved → 0.5. All facts
  settled → 1.0.
- **Effort** — from the effort signal of the approach being priced in §7. S = 1-2 person-weeks,
  M = 3-5, L = 6-12. Also convert to evenings, because that is the unit the user actually
  spends: one person-week ≈ 10-12 evenings.

Compute `R × I × C / E` and **write the caveat in the brief, every time**: at Reach 1 the
absolute number means nothing. It is only good for ranking this feature against the other
features of the same project, which all share Reach 1. `core-ledger` scored 0.27 — that is the
normal scale here, not a red flag.

Ask per number, or one batched question, with options meaning: `Confirm N` / `Adjust higher` /
`Adjust lower` / `Mark TBD`. Every rationale cites the section it came from.

### 10. Feasibility (read-only repo scan + AskUserQuestion — mandatory)

Scan the repo read-only using the module map in
→ [references/repo-map.md](references/repo-map.md). Look for a feature already in the repo that
touches the same layers, so the proposal can cite evidence instead of a guess.

Three boxes, and they are not the generic Tech / Skills / Time:

- **Tech** ☑/☐ — is the path already proven in this repo? Cite the adjacent module, or admit
  there is none. Scaffolding is not proof.
- **Evenings** ☑/☐ — does the effort estimate fit the time the owner actually has? Cite how
  long something comparable took in the git history, or admit there is no baseline.
- **Interest** ☑/☐ — is this worth the owner's evenings? A pet project dies of boredom more
  often than of complexity, and this is the box a work process would never have.

**Skills is deliberately not a box.** On a learning project, not knowing how is the reason to
build it, not a blocker. If a skill gap is real and large, it belongs in §10 Risks with an
honest size.

Boxes may close as ☐ and still be confirmed — `core-ledger` shipped 1 of 3 ☑ because two
honest ☐ were the first concrete tasks. `AskUserQuestion` per box, options meaning:
`Confirm ☑` / `Flip to ☐ — <reason>` / `Mark TBD`.

### 11. Recommendation (AskUserQuestion — mandatory)

Claude picks one of the three approaches and writes 3-5 sentences of rationale. The rationale
**must** cite all four:

- the RICE score (§11),
- the feasibility state (§12),
- at least one cell of the synthesis matrix (§8),
- at least one competitive gap (§6).

Without those four it is "I feel like C". Then add the **locked-in pointer**: one sentence
saying what the next stage inherits as settled.

`AskUserQuestion`, options meaning: `Accept` / `Pick a different approach` / `Mark TBD`.

### 11.5. ExitPlanMode

Everything so far is session memory. Call `ExitPlanMode` with a plan containing:

1. Create `docs/features/<slug>/`.
2. Copy the template into it.
3. Append the pending glossary terms to `docs/GLOSSARY.md` (lines already confirmed in Phase 3).
4. Fill sections 1-17 plus Related and the DoD self-check.
5. Set the frontmatter.
6. Run the Phase 13 self-check.
7. Propose a commit and the next step.

### 12. Write

- `mkdir docs/features/<slug>/`, copy `./templates/idea-brief.md` into it.
- Append `pending_glossary_terms` to `docs/GLOSSARY.md` — the lines `finnavis-fix-term` composed
  and the user confirmed in Phase 3. Insert each at its alphabetical position, change nothing
  else, and bump that file's `updated_at`. Format and position rules:
  → [`finnavis-fix-term` references/entry-format.md](../finnavis-fix-term/references/entry-format.md).
  Commit the glossary change together with the brief — the same interview produced both, and
  splitting them loses the reason the term was added.
- Fill every section from session memory. Frontmatter:
  `status: Confirmed`, `value_score.rice`, `value_score.state: confirmed`,
  `value_score.confirmed_at: <today>`, `feasibility_state: confirmed`, `updated_at: <today>`,
  `depth:` from Phase 0.5.
- The two approaches that were not picked go into §14 with a reason and a revisit trigger.

### 13. Self-check

Read the file back and check:

- **17 sections present**, plus Related and the DoD self-check. No empty H2.
  `<!-- TBD -->` is allowed where something is honestly unknown.
- **No banned terms in the body.** Run the regex from
  → [references/anti-patterns.md](references/anti-patterns.md). Word boundaries on — without
  `\b`, `API` matches inside `rapid`. The DoD self-check line that names the rule is exempt.
- **Length ≤ 5 pages** (~2200 words, ±10%). Over? Compress §6 and the §7 paragraphs first —
  they pad most easily.
- **§13 cites all four sections** (§6, §8, §11, §12).
- **§16 is not empty.** If this feature unlocks nothing, say so explicitly and explain why it
  is worth building anyway — that is a real answer, a blank is not.

A failed check means re-editing that section and checking again, not noting it and moving on.

### 14. Commit and handover

Propose, do not run:

```
01: idea-brief for <slug>
```

That matches the existing history (`fd94fe4 01: idea-brief for core-ledger`).

Next step: `my-toolkit:task-develop`, with the brief as the input to the discussion. The brief
settles **what and why**; `task-develop` writes `docs/tasks/<name>.md` and settles **how**.

No ADR here. If the recommendation locks in something hard to reverse, note it in §15 (Open
questions) as an
open question and leave the decision to the stage that owns it.

## References

| File | Used in | Holds |
|---|---|---|
| [references/socratic-bank.md](references/socratic-bank.md) | Phase 2 | Question bank for a single-owner project |
| [references/approaches.md](references/approaches.md) | Phase 5 | Sub-agent prompts, and the alternatives that keep showing up in personal finance |
| [references/personas.md](references/personas.md) | Phase 6 | Builder / Future-me / User-me prompts |
| [references/repo-map.md](references/repo-map.md) | Phase 10 | FinNavis module map and what it means for feasibility |
| [references/anti-patterns.md](references/anti-patterns.md) | Phase 13 | Banned-term regex and why each term is banned |
| [`finnavis-fix-term`](../finnavis-fix-term/SKILL.md) | Phases 3, 12 | Owns the glossary: the domain-vs-technical filter, the line format, the mandatory NOT-boundary, alphabetical position. This skill confirms terms through it in Phase 3 and writes them in Phase 12 |
| [templates/idea-brief.md](templates/idea-brief.md) | Phase 12 | The artifact |

## Definition of Done

- `docs/features/<slug>/idea-brief.md` exists, 17 sections filled.
- No banned terms in the body (regex verified, word boundaries on).
- Length ≤ 5 pages.
- Frontmatter: `status: Confirmed`, `value_score.state: confirmed`, `feasibility_state: confirmed`.
- §11 states Reach = 1 **and** carries the honest caveat about the absolute score.
- §13 rationale cites §6, §8, §11 and §12.
- §16 lists what this unlocks, or says plainly that it unlocks nothing.
- New domain terms are in `docs/GLOSSARY.md`, each with its NOT-boundary.
- The `AskUserQuestion` checkpoints in Phases 1, 2, 9, 10, 11 actually fired. Check the message
  trail. One fabricated answer and the artifact is not valid.

## Anti-patterns

- **Inventing competitors to fill the table.** `N/A — internal plumbing` with a reason beats
  five fake rows. Phase 4 produces real URLs, real features, real gaps, or nothing.
- **Asking the user for RICE numbers.** They have no basis to answer. Claude proposes from the
  sections above, the user confirms or adjusts.
- **Varying Reach.** It is 1. Varying it to make a feature look better is lying to yourself in
  a document only you will read.
- **Dropping the Reach caveat.** Without it, §11 reads as a score. With it, it reads as a
  ranking key. Same number, different meaning.
- **Generic personas.** Engineer / Executive / UX on a one-person project produces a sub-agent
  improvising a business it cannot see. Use Builder / Future-me / User-me.
- **Business failure signals in Phase 8.** Churn, incidents, SLA breaches — none exist here.
  Abandonment, distrusted data, and the backfill-later trap do.
- **Technical terms in the body.** This is a product document. The stack lives in
  `ARCHITECTURE.md`, `docs/adr/` and the task file. Phase 13 enforces this.
- **One approach in §7.** One approach means the decision is already made and there is nothing
  to interview about. Three or stop.
- **Devil's advocate inheriting the session.** Phase 8 spawns a clean-context sub-agent, or it
  just restates the optimism of Phases 1-7 in a worried tone.
- **Feasibility without a citation.** "Tech ☑ — we know how" is a guess. Name the module.
- **A chat transcript in §14.** Parked approaches are a table: status, reason, revisit trigger.
- **Solutions in §2.** §2 is the problem. Solutions start at §7.
- **Writing during Phases 0-11.** In plan mode that is a permission denial. Hold everything in
  session memory until Phase 12.
- **Switching language on the user.** Questions follow the language they wrote in; the artifact
  is English; their own words stay verbatim.

## Example

> **User:** "interview a feature: I want CSV statement import, entering a month of rows by hand
> is hell"
>
> **— Plan mode, read-only —**
>
> 1. **Phase 0** — proposes `csv-import`, user confirms. Reads the template, `docs/GLOSSARY.md`,
>    `SPEC.md`. Finds CSV import already listed as a draft requirement and as gap #7 of the
>    eight named in `core-ledger` §2.
> 2. **Phase 0.5** — depth: medium.
> 3. **Phase 1** — captures the raw idea verbatim, in the user's own words and language.
> 4. **Phase 2** — batch 1: "which bank, which export format?", "how many rows a month, and how
>    long does it take you today?". Batch 2: "what does 'it worked' mean — zero manual entry or
>    less of it?", "what should happen to a row that is already in the ledger?".
> 5. **Phase 3** — "statement" and "duplicate" go through `finnavis-fix-term` steps 1-5: both pass
>     the domain filter, the user confirms a definition and a NOT-boundary for each, and the two
>     composed lines wait in `pending_glossary_terms`. Nothing is written — plan mode.
> 6. **Phase 4** — searches MoneyOk import, YNAB file-based import, Firefly III CSV importer,
>    Actual Budget. Builds §6. Finding: everyone imports, nobody reconciles against what is
>    already there without a UI.
> 7. **Phase 5** — three sub-agents: A "Trusted File, Manual Mapping" (S), B "Learning Import
>    Rules" (L), C "Mapped Import With Duplicate Guard" (M).
> 8. **Phase 6** — Builder / Future-me / User-me review all three. Future-me flags that B needs
>    a rule store before a single row is imported.
> 9. **Phase 7** — trade-offs plus seven edge cases, including re-importing the same file.
> 10. **Phase 8** — clean-context sub-agent: eight attack vectors. Top one — a silent double
>     import makes every balance wrong and the owner stops trusting the app.
> 11. **Phase 9** — R=1 (fixed), I=2, C=0.8, E=3 → 0.53, with the caveat. Converts to
>     "≈30 evenings". User adjusts E to 4 → 0.4.
> 12. **Phase 10** — scans the repo: `core-ledger` is shipped, so Tech ☑ citing the transaction
>     write path. Evenings ☐ — no comparable import in the history. Interest ☑.
> 13. **Phase 11** — picks C. Rationale cites the §6 reconciliation gap, the Future-me cell,
>     RICE 0.4, feasibility 2 of 3. User accepts.
>
> **— ExitPlanMode —**
>
> 14. **Phase 12** — creates the directory, writes the brief, appends "Statement" and
>     "Duplicate" to `docs/GLOSSARY.md` with their NOT-boundaries.
> 15. **Phase 13** — 17 sections ✓, no banned terms ✓, 4.1 pages ✓, citations ✓, §16 names
>     three later features that need it.
> 16. **Phase 14** — proposes `01: idea-brief for csv-import`. Next: `my-toolkit:task-develop`.
