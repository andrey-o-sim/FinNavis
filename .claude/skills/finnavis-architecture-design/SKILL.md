---
name: finnavis-architecture-design
description: >
  Turns an approved PRD into docs/features/<slug>/sad.md — an Arc42 software architecture
  document with C4 Context and Container diagrams inline, sequence diagrams for every
  critical flow, and MADR architecture decision records spawned on a blast-radius gate —
  for a solo, self-hosted .NET pet project (FinNavis and projects shaped like it).
  Drafts all 12 sections in memory, walks them Socratically one section at a time, writes
  and commits each resolved section with its ADRs, then runs a clean-context critic.
  The whole thing is tuned to a repo whose stack, rings, ports, folder layout and database
  rules are already fixed by CLAUDE.md and ARCHITECTURE.md: §2 and §5 are assembled from
  those files instead of being asked, and anything they already decided can never become
  an ADR. Use this whenever the user wants architecture, a SAD, Arc42, C4 diagrams or ADRs
  for a feature in this repo — including phrasings like "спроектируй архитектуру {slug}",
  "SAD для {slug}", "архитектура фичи", "нарисуй C4", "нужен ADR на {решение}",
  "/finnavis-architecture-design {slug}" — even when they do not say the words SAD or Arc42.
  Hard-refuses if docs/features/<slug>/PRD.md or the repo-root CONTEXT.md is missing.
argument-hint: "[slug] [--depth=easy|medium|hard]"
---

# SAD — solo project

Stage 03 of the pet-project pipeline. `finnavis-interview` settles **what and why**;
`finnavis-prd` settles **what counts as done**; this skill settles **how it is built, and which of
those choices a reader six months from now will need explained**.

Two outputs:

- `docs/features/<slug>/sad.md` — Arc42, 12 sections, C4 Context in §3, C4 Container in §5, a
  sequence diagram per critical flow in §6.
- `docs/features/<slug>/adr/NNNN-*.md` — one MADR record per decision that crosses the blast-radius
  gate. Not one per decision. That distinction is the whole genre.

## Why this is not the generic skill

A generic architecture skill assumes the architecture is open. In this repo it mostly is not, and
pretending otherwise produces three failures that cost real time:

- **It asks what is already decided.** "Layered or hexagonal?" has an answer enforced by
  `tests/FinNavis.ArchitectureTests`. Every such question burns a slot that an actually open
  decision needed.
- **It invents structure that contradicts `CLAUDE.md`.** The `core-ledger` pass drew
  `Application/Features/` and a central `Presentation/Contracts/`, both of which the repo
  explicitly rejects, and both had to be undone by hand in `f8c558b`. §5 here is rendered from the
  conventions, not improvised.
- **It describes a world that does not exist.** A security lead to review the document, an
  availability SLO nobody is owed, a metrics stack nobody built, an on-call nobody is on. One
  person owns, uses and reviews this project.

Everything frozen, and everything genuinely open, is listed in
[references/repo-conventions.md](references/repo-conventions.md). That file is the skill.

## When to use

- "architecture for <slug>", "design the architecture of <slug>", "SAD for <slug>", "draw the C4",
  and the same asks in the user's own language — "спроектируй архитектуру <slug>", "SAD для <slug>",
  "архітектура фічі". These are trigger phrases, kept in the languages they actually arrive in; the
  run that follows is held in whichever one was used.
- `/finnavis-architecture-design <slug>` as an explicit call.
- The PRD is `Approved` and you want the shape fixed before any code exists.

## When not to use

- **No PRD yet** — run `finnavis-prd <slug>` first. This skill hard-refuses.
- **`sad.md` already has all 12 sections filled and `adr/` is non-empty.** Re-running overwrites a
  reviewed document. Edit the section you need, or record a late decision as a single new ADR.
- **The change is a one-evening fix** with no new aggregate, no new read path and no schema change.
  A SAD for that is ceremony. Write the task and do it.

## Hard gates (stop, do not improvise)

| Missing | Message |
|---|---|
| `<slug>` | Ask for it, or list the folders under `docs/features/` that have a PRD and no `sad.md`. |
| `docs/features/<slug>/PRD.md` | «Run `finnavis-prd <slug>` first — an architecture without acceptance criteria is me designing against my own guesses.» |
| `CONTEXT.md` at the repo root | «The glossary is missing. Without it §1 stakeholders, §3 actors and §12 are invented names.» |
| `PRD.md` has `status: Draft` | Not a refusal — a warning. Say it once: the PRD is still a draft, so anything built on it may move. Ask whether to continue. |

`CLAUDE.md`, `ARCHITECTURE.md` and `SPEC.md` are not gates — but a run that did not read them
produces a fictional §2 and a §5 that fights the repo. Phase 0 reads all three.

## Language

Project rule, loaded from [`.claude/rules/language.md`](../../rules/language.md): ask in the user's
language, write the artifact in English, keep identifiers as names, name every section instead of
numbering it, explain the trade-off rather than the vocabulary.

`sad.md`, the ADRs and the commit messages are all English. Question shape for this stage →
[references/ask-style.md](references/ask-style.md).

## Mode

**Per-section write, not plan mode** — deliberately different from `finnavis-interview` and
`finnavis-prd`.

Those two produce one document from one walk, and holding the draft in memory until the end is
cheap. This one produces a 700-line document plus five to twelve separate files, over twenty-odd
questions. Holding all of that in memory to the end means a lost session loses everything and there
is nothing to resume from.

So: **the document is the state.** Each section is drafted in memory, walked, written, and
committed. An interrupted run resumes by reading `sad.md` and continuing at the first section whose
body is still a placeholder.

Commits are proposed, not run, unless the user says otherwise in Phase 0.5. Asking for approval
fifteen separate times is worse than asking once.

## Inputs

**Required:**

- `<slug>` — kebab-case, matches the folder the earlier stages created.
- `docs/features/<slug>/PRD.md` — §2 Goals, §3 Non-goals, §4 user stories, §5 acceptance criteria,
  §6 NFR with its numbers, §6.1 Security/privacy, §8 Open questions, §9 Data impact, §10 Refusal
  messages.
- `CONTEXT.md` (repo root) — the canonical glossary.

**Read always, not optional in practice:**

- `CLAUDE.md`, `ARCHITECTURE.md`, `SPEC.md`, `Directory.Build.props`, `docs/adr/`.
- Every earlier `docs/features/*/sad.md` and its `adr/` — their Accepted decisions are constraints
  on this feature, not open questions.

**Optional:**

- `docs/features/<slug>/.size` — read if present, established in Phase 1 if absent.
- `--depth=easy|medium|hard` — skips the Phase 0.5 depth question.

## Protocol

### 0. Setup (read-only)

- **Read** `./templates/sad-template.md` and `./templates/adr-template.md` into session memory.
- **Read** `CONTEXT.md` — `## Glossary` is session state. These names are canonical for §1, §3, §12.
- **Read** `docs/features/<slug>/PRD.md` in full.
- **Read** `CLAUDE.md`, `ARCHITECTURE.md`, `SPEC.md`, `docs/adr/`.
- **Read** every earlier feature's `sad.md` §2 and §9 plus its `adr/` titles. A pin or a decision
  made there is inherited here.
- **Scan the repo** with the bounded commands in
  [references/repo-conventions.md](references/repo-conventions.md) section E. The scan confirms the
  conventions; it does not replace them.
- **Check** `docs/features/<slug>/sad.md` — if it exists with every section filled, stop and say so.
  If it exists with placeholders left, offer to resume at the first unfilled section.

### 0.5. Two opening questions (AskUserQuestion — mandatory)

One call, two questions.

**Depth** — `easy` / `medium` (recommended) / `hard`. The dial tunes how much I decide alone, never
what gets covered → [references/socratic-loop.md](references/socratic-loop.md#depth-dial).

**Commits** — this run will produce roughly fifteen commits.

- *Commit them yourself* — I stage and commit each section as it resolves.
- *Just print the command* (recommended default) — I print the `git commit` line and you run it, or
  batch them at the end.

The floors hold at every depth: the blast-radius gate, `target_surfaces` being asked, owner+due on
every open question, PRD §6 numbers verbatim, and §6 covering every critical flow.

### 1. Size (read `.size`, or establish it)

If `docs/features/<slug>/.size` exists, read it. If not, classify now and write it in Phase 2.

**One signal: evenings of work.**

| | XS | S | M | L |
|---|---|---|---|---|
| **Evenings** | 1-2 | 3-10 | 10-30 | 30+ |

The size sets the expected ADR count — 2-4 for XS/S, 5-12 for M, 10-15 for L — and nothing else. It
is a sanity check on the pass, never a quota to fill.

### 2. Bootstrap (first write)

Copy the template to `docs/features/<slug>/sad.md`. Fill the frontmatter: `slug`, `stage: "03"`,
`feature_size`, `depth`, `updated_at`, `source_prd`. Leave `target_surfaces: []` — it is filled when
§4's first decision resolves. Create `docs/features/<slug>/adr/` if it does not exist.

Commit: `03: bootstrap sad for <slug>`.

### 3. Draft §1-§12 in memory (no user input)

Per-section sources, the item banks, and the pre-walk hygiene checks →
[references/draft-generation.md](references/draft-generation.md).

Two sections are **assembled, not asked**:

- **§2 Constraints** — from `CLAUDE.md`, `ARCHITECTURE.md`, `SPEC.md`, `Directory.Build.props`,
  `docs/adr/` and the earlier features' pins. Only what the repo leaves open becomes a question.
- **§5's folder layout** — rendered from the tree in `ARCHITECTURE.md` § Folder layout, with this
  feature's areas and operations substituted for the placeholders, and the branches it does not
  touch dropped. Rules for rendering it →
  [references/repo-conventions.md](references/repo-conventions.md) section C. The aggregates, the
  ports and the read path are still walked; the tree is not.

Run the hygiene checks before the walk starts. A failure regenerates that section in memory.

### 4. The walk — §1 to §12, write and commit each (AskUserQuestion — mandatory)

Machine, order, logs and cadence → [references/socratic-loop.md](references/socratic-loop.md).

For each section:

1. **Render it whole** — the full text plus a numbered list of the decisions inside it, so the shape
   is visible before any single item is committed to. Name the section every time you refer to one,
   here and in every question: `§4 (Solution strategy)`, not a bare `§4`. The user is reading a
   question, not the document — a number alone makes them reconstruct the numbering from memory.
2. **Walk the decisions**, one question each, phrased per
   [references/ask-style.md](references/ask-style.md).
3. **Run the blast-radius gate on every Approved decision** →
   [references/blast-radius.md](references/blast-radius.md). Two of three criteria → an ADR is
   spawned with that section, from `./templates/adr-template.md`.
4. **Write** the resolved section into `sad.md`, plus its ADRs into `adr/`.
5. **Lint every Mermaid block just written** →
   [references/c4-and-sequences.md](references/c4-and-sequences.md). A broken block is fixed before
   the commit, never after.
6. **Commit** `03: sad §<N> — <what the section settled>`.
7. **Move on and never come back.** Cross-section drift is Phase 5's job.

Three things this phase must not get wrong:

- **The first decision in §4 (Solution strategy) is the target surface.** `backend-service` or
  `web-frontend` — the only two this product has. It is always asked, can never be dropped or
  deferred, and its result is written to the frontmatter `target_surfaces`, which gates §5
  (Building block view) and everything downstream.
- **The C4 diagrams in §3 (Context and scope) and §5 (Building block view) are confirmed in
  prose.** Write the block, lint it, then describe what it says — who talks to what, which
  dependency points where — and ask about *that*. Raw Mermaid as the body of a question gets
  approved without being read.
- **§6 (Runtime view) closes here.** There is no later stage in this project that finishes the
  runtime view, so a flow left undrawn stays undrawn. Cover every critical path in the PRD,
  grouping the ones that differ only in which noun they touch.

If a decision turns out to be a repo-wide convention rather than a choice about this feature, stop
and raise it with ⚠️ before writing the section →
[references/repo-conventions.md](references/repo-conventions.md) section H. `CLAUDE.md` is edited
only after an explicit yes.

### 5. A second reader (one Agent call, clean context)

Dispatch `plugin:critic` (fallback `general-purpose`) with the self-contained prompt in
[references/critic-prompt.md](references/critic-prompt.md). Send the finished `sad.md`, the
edits-log and the ADR-spawns log. The reader opens `PRD.md`, `CONTEXT.md`, `CLAUDE.md`,
`ARCHITECTURE.md`, `SPEC.md` and the `adr/` folders itself — never paste their bodies.

It runs eight checks: was the document changed outside the review; does it still follow its own §4
strategy; does it contradict the repo's own rules; are the ADRs a real genre or a formality; are the
numbers honest; is anything required missing; does it describe a world this project does not have;
are the diagrams sound.

Resolve each finding: `Accept as suggested` / `Accept, my wording` / `Leave it, here is why`. The
last one puts a line into §1 ¶4. A timeout, `CANNOT READ`, or an unreadable answer → stop and
report. Never fall back to a silent finalize.

Commit: `03: sad finalization (critic pass)`.

### 6. Backstop scans (read-only, run whatever the critic said)

- **Mermaid lint** over the whole file — all nine checks in
  [references/c4-and-sequences.md](references/c4-and-sequences.md). Count the code fences: an odd
  number means a stray fence swallowed the rest of the document.
- **§9 closed both ways** — a row for every file in `adr/`, a file for every row.
- **Every ADR**: title in decision form, `Status: Accepted`, options match the ADR-spawns log, no
  strawman, Negative and Neutral consequences both filled, Links section present.
- **ADR count** inside the band for the size class, or an explicit sentence saying why not.
- **§10 numbers** appear in PRD §6 character for character.
- **§11** has a row for every `save_as_oq`, each with owner and due.
- **Frontmatter** `target_surfaces` is non-empty and §5 draws one container per surface.
- **No `<placeholder>`, `TODO` or `NNNN` survives** anywhere in the file or the ADRs.
- **No invented role** — every name in §1 and §3 is in `CONTEXT.md`.

A failed check means fixing that section and checking again, not noting it and moving on.

### 7. Conventions that outgrew the feature (⚠️)

Sweep the pass for decisions that are really repo-wide rules — an error-handling shape, a paging
convention, a naming rule. For each one, ask once whether it goes into `CLAUDE.md`. Never edit
`CLAUDE.md` without that yes. A no means the convention stays feature-local and gets a §11 row, so
the next feature inherits the question instead of the silence.

Same sweep for `CONTEXT.md`: a genuine **domain** word that surfaced during the walk and is not in
the glossary is named in the handoff, with its NOT-boundary, for the user to add.

### 8. Handoff

The last output of the run, every time:

```md
## ✅ finnavis-architecture-design — <slug>

**What I did**
- `sad.md`, 12 sections, size <X>, depth <Y>, target_surfaces <[...]>
- <N> ADRs in `adr/` — <one line each, decision form>
- <M> commits proposed / made
- <K> open questions carried into §11 (Risks and technical debt), each with owner and due

**Review before continuing**
- `sad.md` §4 (Solution strategy) — the strategy everything else hangs off
- `sad.md` §3 (Context and scope) and §5 (Building block view) — the two C4 diagrams, and whether
  the arrows match how you think about it
- `adr/` — these are the files you will reread in a year. If one does not earn its place, say so
- flip `status: Draft` → `Approved` yourself once you agree with it

**Run next**
1. `/clear` — the next stage re-reads everything from disk
2. then: the data-model stage, then implementation. §8's index row and §11's open questions are
   its first inputs
```

## Definition of Done

- `docs/features/<slug>/sad.md` exists, all 12 sections filled or explicitly `<!-- N/A: reason -->`.
- `status: Draft` in the frontmatter — the skill never writes `Approved`. That flip is the human gate.
- `target_surfaces` is non-empty, chosen from `backend-service` / `web-frontend`, and §5 draws one
  container per declared surface.
- §2 contradicts nothing in `CLAUDE.md`, `ARCHITECTURE.md` or `SPEC.md` without an override recorded
  in §1 ¶4 and §11.
- §5's tree matches the repo's conventions: business areas at the Application root, no `Features/`,
  contracts co-located per operation, no central `Contracts/`.
- §3 has a real `C4Context` block, §5 a real `C4Container` block, and §6 a `sequenceDiagram` for
  every critical path in the PRD. Every block passes the lint.
- §9 is closed against `adr/` in both directions; the ADR count fits the size class or says why not.
- Every ADR: decision-form title, `Status: Accepted`, honest consequences, no strawman option, Links
  filled.
- Nothing already decided by `CLAUDE.md`, `ARCHITECTURE.md` or an earlier Accepted ADR became a new ADR.
- §10 has a scenario per §1 quality goal, with PRD §6 numbers verbatim and a named way to verify.
- §11 carries a row for every `save_as_oq`, each with owner and due, plus the unproven-ground rows.
- §1 and §3 use `CONTEXT.md` names verbatim. No invented reviewer, PM, security lead or operator.
- The second reader ran on the finished document; every finding is resolved or left with a §1 ¶4 line.

## Anti-patterns

- **Asking what the repo already decided.** The four rings, where ports live, enums as strings,
  money as `decimal`, one folder per operation. Stating them in §2 is right; asking is noise.
- **Drawing `Application/Features/` or a central `Presentation/Contracts/`.** Both contradict
  `CLAUDE.md`, both were drawn by the generic skill, both had to be undone by hand. §5 renders from
  the conventions.
- **An ADR for every decision.** Five to twelve for an M-sized feature, not twenty-five. Conversely,
  missing an irreversibility under-records the feature — the gate cuts both ways.
- **An ADR for something `CLAUDE.md` already fixed.** The alternative is excluded by a constraint, so
  criterion 3 can never fire. It only dilutes the folder.
- **An ADR title in problem form** (`0003-storage-strategy.md`) or with a strawman option. Both kill
  the genre, and the critic catches both.
- **`Status: Proposed`.** This skill decides with the user, synchronously. There is no second
  reviewer to move it to Accepted later, so it would be a status that never changes.
- **Inventing a number in §10** that the PRD never agreed to, or rounding `≤ 300 ms` to `≤ 500 ms`.
  Copy them.
- **Inventing a world.** A security lead, an SLO, an on-call rotation, a metrics stack, a sprint.
  Naming something as a task still to be built is honest; asserting it exists is not.
- **Leaving §6 half-drawn.** There is no later stage here to finish it. An undrawn flow is a gap
  that ships.
- **Returning to a written section.** Each section commits atomically. Re-opening §4 after §10 means
  the per-section batch was never trusted.
- **Save-as-OQ without owner and due.** Capture both immediately; missing either downgrades to Drop
  with a warning, never a half-filled §11 row.
- **Resolving critic findings unilaterally**, or finalizing after a `CANNOT READ`. Both defeat the
  point of a second reader.
- **Editing `CLAUDE.md` without asking.** A skill that quietly rewrites the conventions file is the
  thing the ⚠️ rule exists to prevent.
- **Committing a broken Mermaid block.** It renders as an error box, and nobody fixes an error box
  in a file they are only reading.

## References

| File | Used in | Holds |
|---|---|---|
| [references/repo-conventions.md](references/repo-conventions.md) | 0, 3, 4, 6 | **The core.** What is frozen, what is open, the §5 skeleton and its three traps, the scan, what is unproven, how §10 verifies, the ⚠️ rule |
| [references/blast-radius.md](references/blast-radius.md) | 4 | The three criteria in project units, the never-an-ADR list, counts, the ADR-spawns log, how the file is written |
| [references/draft-generation.md](references/draft-generation.md) | 3 | Per-section sources §1-§12, the §7 and §8 default banks, pre-walk hygiene |
| [references/socratic-loop.md](references/socratic-loop.md) | 0.5, 4 | Depth dial, walk order, state machine, per-section write, edits-log, cadence |
| [references/ask-style.md](references/ask-style.md) | every question | Question shape for a senior solo reader, the stage's specific question forms, worked rewrite |
| [references/c4-and-sequences.md](references/c4-and-sequences.md) | 4, 6 | C4 and sequence syntax, this repo's participant vocabulary, the nine-check lint |
| [references/critic-prompt.md](references/critic-prompt.md) | 5 | The second reader: full prompt, eight checks, what to do with the answer |
| [templates/sad-template.md](templates/sad-template.md) · [templates/adr-template.md](templates/adr-template.md) | 2, 4 | The artifacts |

## Example

> **User:** "спроектируй архитектуру csv-import"
>
> The whole walk below is therefore held in Russian — every question, every option label. The
> `sad.md` it writes is in English, like the rest of the repo.
>
> 1. **Phase 0** — reads both templates, `CONTEXT.md` (Statement, Duplicate, Mapping profile, Import
>    run, Set-aside row), `csv-import/PRD.md`, `CLAUDE.md`, `ARCHITECTURE.md`, `SPEC.md`,
>    `docs/adr/0001`, and `core-ledger/sad.md` §2 + its six ADRs. Notes two inherited constraints:
>    PostgreSQL pinned to 18 by `core-ledger`, and `external_ref` already exists on transactions
>    (ADR-0006) — which is exactly what this feature was meant to need.
> 2. **Phase 0.5** — depth: medium. Commits: print the command, do not run.
> 3. **Phase 1** — `.size` says L. Expected band: 10-15 ADRs.
> 4. **Phase 2** — bootstraps `sad.md`, commits.
> 5. **Phase 3** — drafts all 12 in memory. §2 assembles itself and asks nothing: everything is
>    pinned, including the PostgreSQL version. §5 renders the tree with `Import/` as a new
>    Application area and `Endpoints/Imports/{Upload,Confirm,Undo}/`.
> 6. **Phase 4** — §4 surface: `[backend-service]`, one question, no debate — PRD §3 keeps the
>    client out. Then four strategic choices: where the uploaded file lives while it is being
>    mapped, how a duplicate is recognised, whether an import run is undoable as a unit, and whether
>    a mapping profile is domain data or configuration. All four clear the gate; the duplicate rule
>    scores 3 of 3 because getting it wrong corrupts recorded history. §5 adds `IFileStorage` and
>    `IStatementReader` to `Application/Abstractions`. §6 draws five flows, including the one that
>    matters most — the same file loaded twice. §8's index row names the lookup the duplicate check
>    needs. Eleven ADRs, eleven commits.
> 7. **Phase 5** — the critic finds three: a §10 number rounded from the PRD, an ADR whose third
>    option was a strawman the single-currency constraint already excluded, and a §7 line claiming
>    the CI pipeline runs the import suite. All three accepted.
> 8. **Phase 6** — lint clean, §9 closed, no placeholders left.
> 9. **Phase 7** — ⚠️ one convention outgrew the feature: how a partially-failed batch reports which
>    rows were set aside. The user says yes, and it goes into `CLAUDE.md`.
> 10. **Phase 8** — handoff, with the reminder that the `Approved` flip is theirs.
