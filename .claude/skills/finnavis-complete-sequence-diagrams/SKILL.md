---
name: finnavis-complete-sequence-diagrams
description: >
  Audits and completes the runtime view of a feature — §6 of docs/features/<slug>/sad.md — for a
  solo, self-hosted .NET pet project (FinNavis and projects shaped like it). Builds a coverage
  matrix of every PRD §4 user story and every PRD §5 acceptance criterion against the Mermaid
  sequenceDiagram blocks already in §6, verifies that a cited AC is actually shown by a step or an
  alt branch rather than merely claimed in a heading, and then draws only the flows that are
  genuinely missing — append-only, never rewriting a block that is already committed. Participants
  come from §5 of that same SAD, so a diagram can never invent a component the architecture never
  declared. Use this whenever the user wants sequence diagrams, a runtime flow, or a coverage check
  over an existing SAD — including phrasings like "дорисуй потоки csv-import", "проверь покрытие §6",
  "sequence для {slug}", "нарисуй поток отмены загрузки", "runtime view для фичи",
  "/finnavis-complete-sequence-diagrams {slug}" — even when they do not say the words sequence or
  §6. Hard-refuses if docs/features/<slug>/sad.md, PRD.md or the repo-root CONTEXT.md is missing.
argument-hint: "[slug] [--flow=<name>] [--depth=easy|medium|hard]"
---

# Sequences — audit and top-up of §6

`finnavis-architecture-design` writes `sad.md` and closes §6 in the same pass. That is still true:
this skill does **not** take §6 away from it, and a feature that came out of stage 03 with a
complete runtime view needs nothing from here.

What this skill owns is the pass **after** that one:

- **Audit.** Every PRD §4 user story and every PRD §5 acceptance criterion, matched against what §6
  actually draws — not against what the flow headings claim.
- **Top-up.** The flows and branches the audit found missing, drawn one at a time and appended.

The most valuable run of this skill produces **no new diagram at all** — just a verified matrix and
a sentence saying §6 holds. A skill that has to justify itself by writing something will write
noise, and noise in a runtime view is worse than a gap, because a gap is visible.

## When to use

- "дорисуй потоки `<slug>`", "проверь §6", "покрыты ли все AC диаграммами", "нарисуй поток
  `<название>`", and the same asks in any language the user writes in.
- `/finnavis-complete-sequence-diagrams <slug>` as an explicit call.
- The PRD changed after `sad.md` was written — new user story, new AC, a reworded refusal — and §6
  has not caught up.
- A reviewer asked "where is this shown?" about an acceptance criterion and nobody could point at a
  block.

## When not to use

- **`sad.md` does not exist** — run `finnavis-architecture-design <slug>` first. §6 is a section of
  a document, not a document.
- **To redraw a flow you dislike.** This skill appends. Rewriting a committed block is a deliberate
  manual edit, with one exception that is always confirmed out loud (Phase 3, *targeted edit*).
- **To decide something.** Sync versus async, whether an import run is undoable, what an idempotency
  strategy should be — those are `finnavis-architecture-design` decisions and may deserve an ADR.
  This skill draws what was decided and flags what was not.

## Hard gates (stop, do not improvise)

| Missing | Message |
|---|---|
| `<slug>` | Ask for it, or list the folders under `docs/features/` that have a `sad.md`. |
| `docs/features/<slug>/sad.md` | «Run `finnavis-architecture-design <slug>` first — §6 is a section of the SAD, and there is nothing here to audit or append to.» |
| `docs/features/<slug>/PRD.md` | «Without §4 and §5 there is no coverage floor, so an audit would only be me agreeing with myself.» |
| `CONTEXT.md` at the repo root | «The glossary is missing. Without it the actor and the domain nouns in a flow are invented names.» |
| `sad.md` has no `## 6. Runtime view` heading | Not a refusal — report it and offer to run the full top-up as if §6 were empty. Say plainly that stage 03 normally fills it and this is unusual. |

`CLAUDE.md` and `ARCHITECTURE.md` are not gates, but a run that did not read them draws arrows that
fight the dependency rule.

## Language

Project rule, loaded from [`.claude/rules/language.md`](../../rules/language.md): ask in the user's
language, write the artifact in English, keep identifiers as names, explain the trade-off rather
than the vocabulary. Question shape for this stage →
[references/ask-style.md](references/ask-style.md).

## Mode

**Audit first, then append.** The matrix is built and shown before a line of Mermaid is written,
because the matrix is what decides whether anything gets written at all.

Writes are **additive**: a new flow goes after the last existing one, and numbers already committed
never move. The two header lines in §6 — coverage and reading order — are rewritten each run,
because they summarise the whole section rather than one flow.

Depth comes from the `depth:` field in the SAD frontmatter, overridden by `--depth=`. This skill
does not open its own depth question; stage 03 already recorded the answer.

## Inputs

- `<slug>` · `docs/features/<slug>/sad.md` (§5 names the participants, §6 holds the flows) ·
  `docs/features/<slug>/PRD.md` (§4, §5, §10) · `CONTEXT.md` — **required**, all four.
- `CLAUDE.md`, `ARCHITECTURE.md`, `docs/features/<slug>/adr/` — read every run. An Accepted ADR
  constrains what a flow may show: ADR-0004 (`separate the read path behind a query port`) is why a
  read flow does not look like a write one.
- `--flow=<name>` (single-flow mode) · `--depth=easy|medium|hard` — optional.

## Protocol

### 0. Setup (read-only)

Read in full: `PRD.md` (§4, §5 and §10 are session state), `sad.md` (hold §5's container list and
the whole of §6), `CONTEXT.md`, `CLAUDE.md`, `ARCHITECTURE.md`, the `adr/` titles, and both
templates. Build the participant list from §5 →
[references/participants.md](references/participants.md). Note the depth from the frontmatter.

### 0.5. One question (AskUserQuestion — mandatory)

**Commits** — *commit it yourself* / *just print the command* (recommended). Nothing else is asked
here: depth is in the frontmatter, and the flow list is unknown until Phase 1 has run.

### 1. The audit (read-only, no user input)

Two passes — user stories, then acceptance criteria — and four verdicts per AC. Method, verdicts and
matrix format → [references/coverage.md](references/coverage.md).

The thing that makes this pass worth running: **a heading is a claim, not proof.** A flow heading
citing five ACs asserts five criteria. Open the block and find the line for each one. An AC named in
a heading with nothing in the block that shows it is `claimed, not shown` — worse than an uncovered
AC, because it reads as done.

### 2. Present the matrix and agree the work (AskUserQuestion — mandatory)

Render the matrix, then one question: *draw them all* / *draw these, N/A the rest* / *nothing is
missing, close the pass*.

**If the audit found no gaps, say so plainly before offering anything**, then offer only the header
lines and the Notes block. Do not propose a flow nobody needs.

### 3. For each gap — draft, write, lint, describe, confirm

One gap at a time; never batch two flows into one question.

1. **Classify** — write, read or refusal-only → [references/flow-rules.md](references/flow-rules.md).
   Read [references/async-flows.md](references/async-flows.md) **only** if §5 declares an async
   participant.
2. **Draft** from [`templates/seq-flow.md`](templates/seq-flow.md) with §5's participants and PRD
   §10's refusal wording verbatim.
3. **Write** it into §6 after the last existing flow, under the next free heading.
4. **Lint** → [references/mermaid-lint.md](references/mermaid-lint.md), before anything is shown.
5. **Describe it in prose** and confirm — four states, shape and wording →
   [references/ask-style.md](references/ask-style.md). **Never paste `sequenceDiagram` source as the
   body of a question**: diagram syntax in a terminal gets approved without being read, and an
   approval nobody read is what this pass exists to prevent.

At `easy`, write and summarise in one line instead of asking, and batch the summaries for a single
veto before Phase 4.

**The targeted edit — the one exception to append-only.** When the audit found `claimed but not
shown`, the honest fix is usually a branch inside the existing block, not another diagram repeating
eight of its ten lines. Name the block and the lines that change, ask, and only then edit. A silent
edit to a reviewed diagram is the worst thing this skill could do.

### 4. The two header lines in §6

Rewritten every run, under the §6 intro paragraph: the **coverage** sentence and the **reading
order**. Wording → [references/coverage.md](references/coverage.md#the-two-header-lines).

### 5. Notes flagged while completing §6

Four fixed buckets, from [`templates/notes-flagged.md`](templates/notes-flagged.md): index hints,
sync-or-async, left for the API-contract stage, deliberate deviations. Merge into the section if it
already exists. Nothing here is decided, and no ADR is written from it.

The sync-or-async line is written even when the answer is boring, so the absence of retry notes
reads as a decision rather than an omission.

### 6. Backstop scans (read-only, run every time)

- The nine lint checks over the whole of §6, plus the file's fence count.
- Every alias in every block is a container §5 declared — no generic placeholder survived.
- Every refusal branch matches PRD §10 for its AC; no new promise invented.
- No HTTP verb, path, status code or SQL in any message. No `<placeholder>`, `TODO` or `NNNN`.
- Matrix closed both ways: every AC has a verdict, every heading's cited ACs are shown.
- §11 has a row, with owner and due, for every *save as open question*.

A failed check means fixing it and checking again, not noting it and moving on.

### 7. Handoff

```md
## ✅ finnavis-complete-sequence-diagrams — <slug>

**What I did**
- audited §6 against PRD §4 (<M> user stories) and §5 (<K> acceptance criteria)
- <N> flows added / 0 added — <one line each>
- <E> targeted edits to existing blocks, each confirmed
- <Q> acceptance criteria recorded as non-runtime N/A, with reasons
- Notes flagged: <n> index hints, sync/async line, <n> items left for the API-contract stage

**Review before continuing**
- `sad.md` §6 — the new blocks, and whether the refusals read like PRD §10
- the reading-order line — it is what a future reader navigates by
- the index hints — they are the first input to the data-model stage

**Run next**
1. `/clear` — the next stage re-reads everything from disk
2. then: the data-model stage, which reads the index hints from §6
```

## Single-flow mode

`/finnavis-complete-sequence-diagrams <slug> --flow=<name>` — draw one flow on demand, for example
after a conversation settles how something should work.

Skips Phases 1, 2 and 4: no matrix, no coverage sentence, no reading-order rewrite. Goes straight to
Phase 3 for that one flow, then Phase 6's lint. Still uses §5's participants, still quotes PRD §10,
still appends. Say in the handoff that coverage was not checked this run, so nobody reads a
single-flow run as an audit.

## Definition of Done

Each line is also the anti-pattern it rules out — the failure mode is named after the dash.

- Every §4 user story maps to ≥1 flow, or is a recorded N/A with a reason — *no silent skip*.
- Every §5 AC is **shown** by a step, a branch or an explicit N/A — *nothing left as `claimed but
  not shown`, and no flow heading trusted without opening the block*.
- Every participant is a container §5 declared — *no `<service>`, no `<data-store>`, and nothing
  §5 never declared quietly drawn into existence; that case is ⚠️*.
- Every refusal branch carries PRD §10's wording — *paraphrasing drifts the two documents apart*.
- Every write flow commits the unit of work and every refusal says what was not written —
  *otherwise the reader cannot tell whether half of it landed*.
- Every read flow through a query port says so in a note — *the port is the point of ADR-0004 and is
  invisible in the arrows*.
- Each flow shows the refusals its ACs demand — *a happy path alone is the only part a reader could
  have guessed*.
- One flow per critical path, grouped only across the same noun — *no mega-diagram, no four
  near-identical blocks*.
- Blocks committed before this run are unchanged, except a targeted edit confirmed out loud —
  *numbers stay put; commits and ADRs point at "flow 3"*.
- Coverage line and reading-order line present and true; Notes has all four buckets, sync/async line
  included — *a missing bucket reads as forgotten*.
- No retry note, dead-letter branch or idempotency step unless §5 declares a participant that needs
  one — *no machinery for a world this repo does not have*.
- Every block passes the nine lint checks; the file's fence count is even; no HTTP verb, path,
  status code or SQL in any message.
- Nothing was written just because the skill ran — *a verified "nothing is missing" is a successful
  run*. No ADR written here; this pass only flags.

## References

| File | Used in | Holds |
|---|---|---|
| [references/participants.md](references/participants.md) | 0, 3, 6 | The repo vocabulary, §5 as the source of truth, what to do when a flow needs more, the ⚠️ rule |
| [references/coverage.md](references/coverage.md) | 1, 2, 4 | The two passes, the three verdicts, the matrix format, the grouping rule, the two header lines |
| [references/flow-rules.md](references/flow-rules.md) | 3 | What makes a FinNavis flow: refusals from PRD §10, the read path, the unit of work, semantic messages, notes that earn their place |
| [references/async-flows.md](references/async-flows.md) | 3, conditional | Read **only** when §5 declares an async participant: idempotency, retry, dead-letter, and the gate that decides |
| [references/mermaid-lint.md](references/mermaid-lint.md) | 3, 6 | The nine checks, each one from a real break |
| [references/ask-style.md](references/ask-style.md) | every question | Question shape, the four confirm states, how a flow is described in prose |
| [templates/seq-flow.md](templates/seq-flow.md) · [templates/notes-flagged.md](templates/notes-flagged.md) | 0, 3, 5 | The scaffolds |

