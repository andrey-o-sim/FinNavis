---
name: finnavis-prd
description: >
  Turns a confirmed idea-brief into docs/features/<slug>/PRD.md for a solo, self-hosted
  pet project (FinNavis and projects shaped like it). Socratic walk over user stories,
  acceptance criteria, NFRs and KPIs, a clean-context critic, then one write. The
  acceptance-criteria coverage model is solo-shaped: 4 always-mandatory types plus
  history-integrity / duplicate-safety / authorization, each resolved explicitly instead
  of invented. Writes a 10-section PRD that stays technology-free end to end — data impact
  and refusal messages included, failure codes and test plans excluded. Also writes the
  feature's own docs/features/<slug>/CONTEXT.md — terms it uses pointing at the canonical
  docs/GLOSSARY.md, plus its invariants and out-of-scope list.
  Triggers on "/finnavis-prd {slug}", "write PRD for {slug}", "draft PRD for {slug}",
  "PRD для {slug}", "напиши PRD {slug}", "опиши требования", "product requirements for {slug}".
  Hard-refuses if docs/features/<slug>/idea-brief.md or docs/GLOSSARY.md is missing.
argument-hint: "[slug] [--depth=easy|medium|hard] [--reference <path>]"
---

# PRD — solo project

Stage 02 of the pet-project pipeline. `finnavis-interview` settles **what and why** in
`idea-brief.md`; this skill settles **what exactly counts as done** in `PRD.md`.

It is tuned for the same situation as `finnavis-interview`, and that tuning is the whole point:

- **One owner, who is also the only user.** No reviewers, no tickets, no sign-off chain.
- **No permission boundary yet.** Nobody to deny, nothing to hide — until the sign-in feature lands.
- **No CI, no metrics stack, no on-call.** A target nobody can measure today is a prerequisite task,
  not a number to write down and forget.
- **Evenings, not sprints.** Size is counted in evenings, because that is what gets spent.

A generic PRD protocol misfires on all four: it invents a Security Lead to review the document,
demands an authorization criterion where no second actor exists, and asks for an availability SLO
nobody will ever be shown. This one does not.

## When to use

- "write PRD for <slug>", "PRD для <slug>", "опиши требования к <slug>".
- `/finnavis-prd <slug>` as an explicit call.
- The idea-brief is `Confirmed` and you want the acceptance criteria before you touch code.

## When not to use

- **No idea-brief yet** — run `finnavis-interview <slug>` first. This skill hard-refuses.
- **The PRD already exists with `status: Approved`** and nothing upstream changed. Edit the
  section you need; do not re-run the whole walk.
- **The change is a one-evening fix** with no new entity and no new rule. A PRD for a typo is
  ceremony. Write the task and do it.

## Hard gates (stop, do not improvise)

| Missing | Message |
|---|---|
| `<slug>` | Ask for it, or propose two or three from the idea. |
| `docs/features/<slug>/idea-brief.md` | «Run `finnavis-interview <slug>` first — a PRD without a confirmed brief is me guessing your idea back at you.» |
| `docs/GLOSSARY.md` | «The glossary is missing. Run `finnavis-interview` or add the terms first — without it §4 roles and §5 domain terms are invented.» |

### Two files, two jobs — do not mix them

- **`docs/GLOSSARY.md` is the canonical glossary, shared by every feature.** A term is defined
  there once, with its NOT-boundary. This skill reads it, and appends to it when the walk surfaces
  a new domain word. It never copies a definition out of it.
- **`docs/features/<slug>/CONTEXT.md` is this feature's own slice.** This skill **writes** it in
  Phase 10, from [templates/CONTEXT.md](templates/CONTEXT.md). Its `## Glossary` section carries no
  definitions — only a link to `docs/GLOSSARY.md` and the list of terms this feature uses. The
  other four sections are per-feature: `Invariants`, `Sentinel errors`, `Org-filter invariant`,
  `Out of scope`.

A definition lives in exactly one place. If `CONTEXT.md` and `docs/GLOSSARY.md` ever disagree, the
glossary wins.

## Language

Project rule, loaded from [`.claude/rules/language.md`](../../rules/language.md): ask in the user's
language, write the artifact in English, quote the user verbatim, name every section instead of
numbering it. Question shape for this stage →
[references/ask-style.md](references/ask-style.md).

## Mode

**Planning-mode native,** same discipline as `finnavis-interview`. Phases 0-8 are read-only: Read,
Glob, Grep, WebSearch, Agent, AskUserQuestion. The draft lives in session memory. The first write
happens in Phase 10, after `ExitPlanMode`.

The `AskUserQuestion` checkpoints in Phases 0.5, 1, 2, 4 and 6 are not clarifying questions — they
are the data input. An `Auto Mode` system-reminder does **not** cancel them. If `ExitPlanMode` is
unavailable (the session did not start in plan mode), skip Phase 9 and run Phase 10 directly.

## Inputs

**Required:**

- `<slug>` — kebab-case, matches the folder `finnavis-interview` created.
- `docs/features/<slug>/idea-brief.md` — §2 Problem, §5/§6 Out of scope, §11 RICE, §13 Recommendation,
  §15 Open questions.
- `docs/GLOSSARY.md` — the canonical glossary, shared by every feature.

**Optional:**

- `docs/features/<slug>/.size` — read if present, established here if absent (Phase 1).
- `SPEC.md` — repo scope. Read it always: it is the most common source of a scope contradiction with
  the brief, and catching that is this stage's job.
- A neighbouring feature's `idea-brief.md` / `PRD.md`, or a path under `src/` — offered as channels in
  Phase 4. `ARCHITECTURE.md` and `docs/adr/` are not: they describe HOW.
- `--depth=easy|medium|hard` — skips the Phase 0.5 question.
- `--reference <path>` — pre-selects the "existing code" channel in Phase 4.

## Protocol

### 0. Setup (read-only)

- **Read** `./templates/PRD-template.md` and `./templates/CONTEXT.md` into session memory. Do not
  copy either yet.
- **Read** `docs/GLOSSARY.md` — load `## Glossary` as session state. These role names and domain
  terms are canonical for §4 and §5.
- **Read** a neighbouring feature's `CONTEXT.md` if one exists — it shows the house wording for the
  five sections.
- **Read** `docs/features/<slug>/idea-brief.md` in full.
- **Read** `SPEC.md` if present.
- **Check** `docs/features/<slug>/PRD.md` does not already exist as `Approved`.
- **No Write, Edit or mkdir.**

### 0.5. Depth checkpoint (AskUserQuestion — mandatory)

One question: `easy` / `medium` (recommended) / `hard`. The dial tunes **how much I decide alone**,
never **what gets covered** → [references/socratic-loop.md](references/socratic-loop.md#depth-dial).

- **easy** — I take the reversible calls myself with stated defaults, ask only the irreversible ones,
  and surface an assumptions ledger before the write for a batch veto.
- **medium** — one question per real decision in §4-§7.
- **hard** — every decision walked, each question foregrounds the trade-off, edge cases probed harder.

The §5 coverage floors and the §9-§10 completeness rules hold at **every** depth. `easy` reaches them
by deciding, `hard` by asking. Dropping a coverage type because the depth is `easy` is a bug.

### 1. Size (read `.size`, or establish it)

If `docs/features/<slug>/.size` exists, read it. If not, classify now and write it in Phase 10 —
later stages must never silently default to M.

**One signal: evenings of work.** Not PR count, not sprints, not team-weeks — the only unit this
project actually spends.

| | XS | S | M | L |
|---|---|---|---|---|
| **Evenings** | 1-2 | 3-10 | 10-30 | 30+ |

One `AskUserQuestion` with my estimate and its reasoning ("S — about 6 evenings: one read path, no
reshaping of recorded data"), options to confirm or move it a class. At `easy` take the estimate and
put it in the assumptions ledger.

Two rules around it:

- **Do not invent extra signals.** Whether the feature needs a schema change, or touches data already
  recorded, is a question of risk, not of size — it has its own home in §9 and the critic reads it
  there. Size answers one thing: how many evenings.
- **Creep is said out loud.** If §5 grows the feature past the class, say so in Phase 11. Never
  silently re-write `.size` to make the estimate look right afterwards.

### 2. Deep dive on what the brief left open (AskUserQuestion — mandatory)

The brief settled the direction. The PRD needs the rules. Ask only what the brief does **not** already
answer — re-asking a settled question is how an interview loses trust.

Work three sources, in this order:

1. **`idea-brief.md` §15 Open questions** — each one is either answered now, or carried into PRD §8
   with owner + due. None may silently disappear.
2. **Contradictions between the brief and `SPEC.md`.** Name the contradiction, propose the resolution,
   get a decision, and record it in §1 ¶4 as a decision taken here. This is the single highest-value
   question this stage asks — the `core-ledger` run found two.
3. **The rules the brief never covers.** Derive each question from the feature's own nouns and verbs,
   then ask it with those names substituted. **Ask the derived question, never the rule itself** —
   "what happens to recorded transactions if a category is deleted?", not "what happens to records
   that reference a domain object".

   - **For every domain object the feature creates or changes** — what happens to data already
     recorded against it when it is renamed or deleted?
   - **For every action the feature adds** — can it be repeated, undone or corrected, and what has to
     stay unchanged when it is?
   - **For every figure the feature computes or shows** — what must it be made of for the owner to
     trust it?
   - **For every date or period the feature touches** — whose date counts, the one the owner sees or
     the one that was stored, and what happens at the boundary?

   A rule that yields no question for this feature is skipped silently — never asked as "not
   applicable". Each answer usually becomes a §5 criterion, so keep the owner's own wording.

Deliver in batches of 2-3. Volume scales with depth: `easy` asks only 1 and 2 plus any rule whose
answer cannot be inferred; `hard` works every rule that applies.

### 3. Glossary capture (deferred)

Every new domain word goes into a session-state list, `pending_glossary_terms`. Skip generic technical
words. Do **not** write anything now — Phases 0-8 are read-only. Terms are appended to
`docs/GLOSSARY.md` in Phase 10, in the house format: one line, canonical definition, then the
boundary — what it is **NOT**. A term without a NOT-boundary is a label, not a definition.

Keep a second session-state list alongside it, `feature_terms`: every glossary term this feature
actually works with, new ones included. It becomes the `## Glossary` line of the feature's
`CONTEXT.md` in Phase 10. A term the feature only mentions in passing does not belong on it.

By the time §4 and §5 are drafted, every role and domain term used in them is either already in
`docs/GLOSSARY.md` or pending in that list. The PRD never invents a role.

### 4. Extra channels (AskUserQuestion, multi-select)

Which sources may I read beyond the brief and the glossary? The real ones for this repo — no Jira,
no Confluence, no ticket system:

- **`SPEC.md`** — product scope and the draft requirement list (usually already read in Phase 0).
- **Existing code** under `src/` — name the path. Read it for what the product already does, not for
  how it does it. Pre-selected if `--reference` was passed.
- **A neighbouring feature's `idea-brief.md` or `PRD.md`** — the closest thing this repo has to a
  reference module.
- **Web search** — only for a specific question ("how do other ledgers treat a split transaction").
- **None.**

For each picked channel ask the **specific** path or question. No silent broad scans.

`ARCHITECTURE.md` and `docs/adr/` are deliberately **not** on the list. They record HOW the repo is
built, and a PRD that reads them starts writing HOW. A decision that closes a *product* option — "one
currency", "no sign-in" — belongs in `SPEC.md` or in the brief, and that is where this skill reads it.

### 5. Draft §1-§10 in memory (no user input)

Read the per-section contract in the template's `<!-- Skill instruction: ... -->` comments, then draft
every section. Sources and rules:

| Section | Comes from |
|---|---|
| §1 Context | brief §2 Problem, "why now", §13 Recommendation; ¶4 holds decisions taken in Phase 2 and critic overrides |
| §2 Goals | brief §13 — strategic outcomes, no numbers (numbers live in §7) |
| §3 Non-goals | brief §5/§6 Out of scope — each with its reason |
| §4 User stories | §2 goals × glossary roles; `As a <role> / I want / So that` |
| §5 Acceptance criteria | Phase 2 answers + the coverage model → [references/ac-coverage.md](references/ac-coverage.md) |
| §6 NFR | the default row bank → [references/nfr-kpi-bank.md](references/nfr-kpi-bank.md) |
| §6.1 Security / privacy | solo-shaped: one dataset, no auth yet, what an exposed installation means |
| §7 KPIs | the solo KPI bank → [references/nfr-kpi-bank.md](references/nfr-kpi-bank.md) |
| §8 Open questions | carried brief §15 items + `save_as_oq` resolutions; owner + due mandatory |
| §9 Data impact | §5 AC + §6 NFR → what the feature does to the data, and what it leaves open |
| §10 Refusal messages | every §5 AC of type error / domain invariant / cross-context → [references/refusal-messages.md](references/refusal-messages.md) |

**The whole document is WHAT and WHY.** No datastore, no framework, no entity-class names, no
protocol detail, **no failure codes** — `finnavis.transfers.same_account` is a name for a machine —
and **no test plan**. Which test proves a criterion is a decision of the stage that writes the code,
not of a document that describes the product. The forbidden-token scan covers every section
(→ [references/ac-coverage.md](references/ac-coverage.md#forbidden-tokens)).

**§9 is impact, not design.** It does not pick the storage shape, the lookups, or how the change
reaches an existing database — at PRD time there is nothing to decide that against, and a guess
written down now gets read later as a decision. It states what the feature does to the data, what
has to exist first, and which questions it hands to the stage that builds the thing. The expensive
answer it must never skip is whether already-recorded history has to be reshaped.

Before the walk begins, check the draft-time floors: every §4 user story already has at least one §5
AC, and the four mandatory coverage types are present.

### 6. Socratic walk (AskUserQuestion — mandatory)

Machine, edits-log and cadence → [references/socratic-loop.md](references/socratic-loop.md).

Walked **per item**: §4 user stories → §5 acceptance criteria → §6 NFR rows → §7 KPIs.
Rendered whole and confirmed with **one question each**: §9 and §10 — they are derived from §5, not
independent decisions. If a confirm comes back `Edit`, walk the changed rows per item.

After every §5 resolution re-check both floors (coverage types, and every retained user story keeps at
least one AC). Both hold at every depth.

### 7. A second reader (one Agent call, clean context)

Dispatch `plugin:critic` (fallback `general-purpose`) with the self-contained prompt in
[references/critic-prompt.md](references/critic-prompt.md). Inline the draft and the list of changes;
the reader opens `docs/GLOSSARY.md`, `idea-brief.md` and `SPEC.md` itself — never paste their bodies.

It runs seven checks: does the document still follow the approach it claims; has the work grown past
the estimate; was anything the brief called important dropped; was the document changed outside the
review; is anything required missing; is there technical detail where there should be none; and does
it describe a world this project does not have — invented reviewers, customers, uptime promises or
measures that need a second user. That last one is the check a generic reader has no reason to run.

Resolve each finding: `Accept as suggested` / `Accept, my wording` / `Leave it, here is why`. The last
one puts a line into §1 ¶4 so the deliberate choice is readable later. A timeout, `CANNOT READ`, or an
unreadable answer → stop and report. Never fall back to a silent write.

### 8. Self-check (read-only)

Run against the in-memory draft, before `ExitPlanMode`:

- 10 sections present (plus §6.1). Empty is not allowed; `<!-- N/A: reason -->` is.
- Forbidden-token regex over §1-§10 — zero hits that were not explicitly overridden. §9's Prerequisites
  bullet is exempt: naming the real harness is its job.
- §5: four mandatory types present; at least one conditional type present; every conditional type
  either present or carrying an explicit N/A reason; every retained §4 user story has ≥1 AC.
- §6: every row has a numeric target and a measurement that exists today — or is marked as a
  prerequisite task.
- §7: no adoption-rate, retention-cohort, DAU/MAU, NPS or churn metric. One user.
- §8: every row has owner + due. No lone "TBD".
- §9: every new read path implied by §5/§6 is named; the already-recorded-history answer is stated
  explicitly even when it is "no"; the open points are handed over, not decided.
- §10: every error-type, invariant-type and cross-context AC has a row, each a sentence for the owner.
  No code, no status number, no field name anywhere in the table.

A failed check means fixing that section and checking again, not noting it and moving on.

### 9. ExitPlanMode

Plan contents: create the folder if needed, write `PRD.md`, write `CONTEXT.md`, write `.size` if it
was established here, append pending glossary terms to `docs/GLOSSARY.md`, re-run the Phase 8
self-check against the files on disk, propose the commit, print the handoff.

### 10. Write

- `docs/features/<slug>/PRD.md` from the template, every section filled from session memory.
- `docs/features/<slug>/.size` if Phase 1 established it.
- Append `pending_glossary_terms` to `docs/GLOSSARY.md`, each with its NOT-boundary, and bump its
  `updated_at`. Terms stay alphabetical.
- `docs/features/<slug>/CONTEXT.md` from [templates/CONTEXT.md](templates/CONTEXT.md). Five sections:
  - **Glossary** — the link to `docs/GLOSSARY.md` plus `feature_terms` from Phase 3, alphabetical,
    each name verbatim. **No definitions.** Copying one here creates a second source of truth.
  - **Invariants** — the rules that hold across the whole feature, phrased «X always» / «X never»,
    each citing the §5 AC it protects. Drawn from the AC of type *domain invariant* and
    *history integrity*, lifted one level: state the rule, not the scenario. Do not restate an AC.
  - **Sentinel errors** — the pointer block from the template. **Never a list of codes.** Codes are
    derived later by `finnavis-api-forge` into `contracts/openapi.yaml`, and a copy here would be a
    third place to keep in sync. Fill only the module list — the §5 aggregates that refuse, singular
    and PascalCase.
  - **Org-filter invariant** — the template's N/A block, unchanged. It stays in the file rather than
    being pruned, so the gap is visible until the sign-in feature lands.
  - **Out of scope** — §3 Non-goals, one line each, reason kept.
- Frontmatter: `status: Draft` — **always Draft**. Flipping to `Approved` is the human gate; the skill
  never does it, not even when the critic returns clean. Also `slug`, `stage: "02"`, `feature_size`,
  `depth`, `updated_at`, `source_brief`.

### 11. Commit and handoff

Propose, do not run:

```
02: PRD for <slug>
```

That matches the existing history (`dd09606 02: PRD for … via write-prd`). Then print the handoff
block — the last output of the run, every time:

```md
## ✅ finnavis-prd — <slug>

**What I did**
- <artifact(s) written> + <the commit proposed> + size <X> (from `.size` / established here)

**Review before continuing**
- `docs/features/<slug>/PRD.md` — §5 acceptance criteria and §8 open questions first
- `docs/features/<slug>/CONTEXT.md` — the invariants, and the term list under `## Glossary`
- `docs/GLOSSARY.md` — <N> new terms appended
- flip `status: Draft` → `Approved` yourself once you agree with it

**Run next**
1. `/clear` — the next stage re-reads everything from disk
2. then: the implementation stage, with this PRD as its input
```

## Definition of Done

- `docs/features/<slug>/PRD.md` exists, 10 sections + §6.1 filled or explicitly `N/A: reason`.
- `status: Draft` in frontmatter — the skill never writes `Approved`.
- §4 roles come from `docs/GLOSSARY.md` verbatim. No invented `user`, `admin` or `reviewer`.
- §5 holds the four mandatory coverage types, at least one conditional type, an explicit resolution for
  every conditional type, and ≥1 AC per retained §4 user story.
- Zero forbidden tokens in §1-§7 (regex-verified, word boundaries on).
- §6 rows carry numeric targets and a measurement that exists today or is named a prerequisite.
- §7 KPIs are solo-shaped — nothing that needs a second user to compute.
- §8 rows all carry owner + due; every `idea-brief.md` §15 question is answered or carried here.
- §9 names every new read path and answers the already-recorded-history question; §10 covers every
  failure AC.
- New domain terms are in `docs/GLOSSARY.md`, each with its NOT-boundary.
- `docs/features/<slug>/CONTEXT.md` exists with all five sections. Its `## Glossary` holds a link and
  a term list, **no definitions**; its `## Sentinel errors` holds a pointer and a module list, **no
  codes**; its `## Org-filter invariant` carries the N/A block rather than being pruned.
- The second reader ran on the reviewed draft; every finding is resolved or left with a §1 ¶4 line.

## Anti-patterns

- **Inventing a review board.** `reviewers: ["Tech Lead", "Security Lead"]` on a one-person project is
  cosplay. There is one reviewer and they are reading this.
- **Authorization theatre.** Writing an authorization criterion where there is no second actor, or
  skipping the type silently. Resolve it explicitly: N/A with a reason plus an §8 row until sign-in exists.
- **Business-SaaS KPIs.** Adoption rate, retention cohorts, DAU/MAU, NPS, churn — all need users you do
  not have. Use the solo bank.
- **An availability SLO.** Nobody is owed one. If downtime costs only your own evening, say that.
- **A target nobody can measure.** "p95 ≤ 300 ms" with no harness is a wish. Name the measurement, or
  name standing it up as a prerequisite task in §9.
- **Technology in §1-§10.** The datastore, the ORM, the framework and the entity-class names belong to
  `ARCHITECTURE.md` and to the stage that writes code. The forbidden-token scan is the backstop, not
  the rule.
- **A failure code anywhere in the PRD.** `finnavis.transfers.same_account` is a name for a machine.
  The PRD states the refusal in the owner's words — in §5 and again, collected, in §10 — and hands the
  naming to the stage that writes the code.
- **Copying a definition into the feature's `CONTEXT.md`.** A term is defined once, in
  `docs/GLOSSARY.md`. A copy in a feature folder is a second source of truth that nobody will keep in
  sync. The feature's file lists the names it uses and links to the glossary.
- **Listing error codes under `## Sentinel errors`.** Codes are derived later, by
  `finnavis-api-forge`, into `contracts/openapi.yaml` and the error registry. Writing them here makes a
  third copy out of a document that cannot be checked against the code.
- **Pruning `## Org-filter invariant` because it is empty.** It is empty for a reason that will change.
  The N/A block keeps the gap readable until the sign-in feature lands.
- **Designing the schema in §9.** "Add an index on `date`", "a new migration adds table `movements`" —
  at PRD time both are guesses, and a guess in a document is read later as a decision. §9 names the
  read path and hands the lookup decision over.
- **Skipping the history answer in §9.** Whether already-recorded data has to be reshaped is the most
  expensive thing this feature can hide. It is answered even when the answer is "no".
- **Re-asking what the brief already settled.** Read §13 before Phase 2 and ask only about the gaps.
- **Dropping an idea-brief §15 question.** It is answered here or carried into §8 with owner + due.
- **Flipping `status` to `Approved`.** Human gate. The skill writes `Draft` and says so in the handoff.
- **Writing during Phases 0-8.** In plan mode that is a permission denial. Hold it in session memory.
- **Switching language on the user.** Questions follow the language they wrote in; the artifact is
  English; their own words stay verbatim.

## References

| File | Used in | Holds |
|---|---|---|
| [references/ask-style.md](references/ask-style.md) | every AskUserQuestion | Question shape for a senior solo reader, worked rewrites |
| [references/socratic-loop.md](references/socratic-loop.md) | Phases 0.5, 6 | Depth dial, 5-state machine, edits-log, coverage floors |
| [references/ac-coverage.md](references/ac-coverage.md) | Phases 5, 6, 8 | 4 + 3 coverage model, forbidden tokens, worked ACs |
| [references/nfr-kpi-bank.md](references/nfr-kpi-bank.md) | Phase 5 | Default NFR rows for this stack, solo KPI bank, banned metrics |
| [references/refusal-messages.md](references/refusal-messages.md) | Phases 5, 8 | The §10 table, its coverage rule, and why no code goes in it |
| [references/critic-prompt.md](references/critic-prompt.md) | Phase 7 | The second reader: full prompt, its seven checks, what to do with the answer |
| [templates/PRD-template.md](templates/PRD-template.md) | Phases 0, 10 | The artifact |
| [templates/CONTEXT.md](templates/CONTEXT.md) | Phases 0, 10 | The feature's own context file — five sections, two of them pointers |

## Example

> **User:** "напиши PRD для csv-import"
>
> **— Plan mode, read-only —**
>
> 1. **Phase 0** — reads both templates, `docs/GLOSSARY.md` (Statement, Duplicate already there),
>    `idea-brief.md` (approach C, "Mapped Import With Duplicate Guard"), `SPEC.md`.
> 2. **Phase 0.5** — depth: medium.
> 3. **Phase 1** — no `.size`. Classifies S: ~8 evenings, no new project, one additive migration.
> 4. **Phase 2** — carries three §15 questions; finds one contradiction with `SPEC.md`; asks what
>    happens to an import when the file is loaded a second time, and what an imported row may be
>    edited into afterwards.
> 5. **Phase 3** — "mapping profile" goes to `pending_glossary_terms`. `feature_terms` collects the
>    seventeen glossary words this feature works with, the seven of its own included.
> 6. **Phase 4** — channels: `core-ledger/PRD.md` as the reference, plus `ARCHITECTURE.md`.
> 7. **Phase 5** — drafts §1-§10. §5 gets the four mandatory types; **duplicate safety** is mandatory
>    here (data comes from outside), **history integrity** too (an import touches past periods),
>    **authorization** resolves to N/A — the glossary still has one actor.
> 8. **Phase 6** — walks 6 user stories, 19 ACs, 2 NFR rows, 2 KPIs; confirms §9-§10 one question each.
>    §9 names the duplicate lookup as a new read path, answers "no reshaping of recorded history", and
>    hands the lookup behaviour to the implementation stage.
> 9. **Phase 7** — critic finds two: an AC that leaked a column name, and a §7 metric that needed a
>    second user. Both accepted.
> 10. **Phase 8** — self-check: regex clean, floors closed, §10 covers every refusal.
>
> **— ExitPlanMode —**
>
> 11. **Phase 10** — writes `PRD.md` (`status: Draft`), `.size` = S, appends one term to
>     `docs/GLOSSARY.md`, writes `CONTEXT.md`: the term list, nine invariants lifted from the
>     invariant-type and history-integrity ACs, `Import` and `MappingProfile` as the refusing modules,
>     the N/A org-filter block, and §3's five non-goals.
> 12. **Phase 11** — proposes `02: PRD for csv-import`, prints the handoff, reminds the user that the
>     `Approved` flip is theirs.
