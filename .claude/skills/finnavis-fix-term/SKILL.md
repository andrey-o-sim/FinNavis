---
name: finnavis-fix-term
description: >
  Owns the domain vocabulary of a solo, self-hosted .NET pet project (FinNavis and projects shaped
  like it): adds, amends and audits entries in the canonical docs/GLOSSARY.md, one line per term —
  a one-sentence definition plus a mandatory NOT-boundary naming the concept it gets confused with.
  Given a feature slug it also adds the term to that feature's docs/features/<slug>/CONTEXT.md
  "Terms this feature uses" line, so a feature can never work with a word it never declared. Runs
  standalone, or inside finnavis-interview and finnavis-prd, which stopped carrying their own copy
  of the format rules and call this skill instead — one definition, one owner, no drift. Modes:
  add (single term or a batch), --amend to change a definition that has moved, --audit to check the
  whole glossary without writing. Use this whenever the user wants to define, capture, rename,
  disambiguate or check a domain word — including phrasings like "добавь термин {X}", "что такое
  {X} в нашем домене", "зафиксируй термин", "обнови глоссарий", "определи {X}", "проверь
  глоссарий", "/finnavis-fix-term {X}" — even when they do not say the word glossary. Refuses
  technical vocabulary: a port, an aggregate, a cursor or a datastore name belongs in
  ARCHITECTURE.md or an ADR, never here. No upstream gate — it runs at any point in the pipeline.
argument-hint: "[term] [slug] [--amend] [--audit]"
---

# Domain glossary — solo project

A capture utility, not a stage. It runs wherever a fuzzy word turns up: an interview answer, a PRD
draft, a design walk, a review where two sentences clearly meant different things by "shape".

Outputs, in order of how often they happen:

- `docs/GLOSSARY.md` — one line longer, or one line changed. The canonical definition.
- `docs/features/<slug>/CONTEXT.md` — the term's name added to the `**Terms this feature uses:**`
  line, when a slug is given and that file already exists.

## The one thing to understand before anything else

**A definition lives in exactly one place, and every other skill in the pipeline reads it.**

`docs/GLOSSARY.md` is canonical. All seven — `finnavis-interview`, `finnavis-prd`,
`finnavis-architecture-design`, `finnavis-complete-sequence-diagrams`,
`finnavis-generate-data-model`, `finnavis-api-forge`, `finnavis-break-tasks` — read it, and
`finnavis-architecture-design` goes further: §1 stakeholders, §3 actors and §12 take their names
from it verbatim, and anything contradicting it loses.

`finnavis-prd` is the one that hard-refuses when the file is missing. The rest gate on the
feature's own `CONTEXT.md`, which links here — so a broken glossary does not stop them, it just
makes everything they write quietly wrong. That is the worse failure of the two.

A feature's `CONTEXT.md` **names** the terms that feature works with and **never defines** them.
That split is the whole design: a term is defined once, so two features cannot drift apart on what
a word means.

Two consequences shape the protocol:

- **The definition is confirmed with the user, never inferred and written silently.** A wrong line
  propagates into a PRD, a SAD, a schema and a contract before anyone reads it.
- **A term is cross-feature even when it surfaces inside one feature.** `Account` has to mean the
  same thing in `core-ledger` and in `csv-import`, so the wording is checked against both, not
  only against the feature the conversation is about.

## Two run shapes

This skill is called in two very different situations, and the difference is **when the file is
written**, never whether the user is asked.

### Standalone

`/finnavis-fix-term <term>` typed by the user, mid-anything. Ask, then write, then stamp and offer
the commit. The full protocol below.

### Inside `finnavis-interview` or `finnavis-prd`

Both callers run under `ExitPlanMode`: their early phases are read-only because plan mode forbids
a write, not because they chose to defer. So the protocol splits:

- **Steps 1–5 run in-flow**, the moment the word surfaces. Asking is not writing, and plan mode
  allows it. By the time §4 and §5 are drafted, every term in them is settled — which is the point
  of reconciling in-flow rather than batching.
- **Step 6, the write, is handed to the caller's write phase.** The composed lines go back as
  session state; the caller writes `docs/GLOSSARY.md` in its own atomic write, together with its
  own artifact, under one commit.

A caller that is **not** in plan mode writes immediately, like the standalone shape.

Never write `docs/GLOSSARY.md` from inside a plan-mode phase. It fails, and the failure looks like
the skill is broken rather than like a mode conflict.

## When to use

- "добавь термин `<X>`", "что такое `<X>` в нашем домене", "зафиксируй термин", "обнови
  глоссарий", "определи `<X>`", "проверь глоссарий", and the same asks in any language.
- `/finnavis-fix-term <term>` as an explicit call.
- From `finnavis-interview` Phase 3 and `finnavis-prd` Phase 3 — every new domain word.
- `--amend` — the term is in the glossary and its meaning has moved.
- `--audit` — check the whole glossary and the documents against it. Writes nothing.

## When not to use

- **For a technical word.** A port, an adapter, an aggregate, a ring, a unit of work, a cursor, an
  error envelope, a migration, PostgreSQL, EF Core — none of these are domain vocabulary. The test
  and the destination for each → [references/domain-vs-technical.md](references/domain-vs-technical.md).
- **To record a rule.** "A transfer never carries a category" is a PRD §5 criterion and an
  invariant in the feature's `CONTEXT.md`. The glossary carries what a word *means*; a boundary
  comes along only as the NOT-reference.
- **To write a definition into a feature's `CONTEXT.md`.** That file names terms and links to the
  glossary. A definition copied there is a second copy that will go stale.
- **To rename a term across the repo.** `--amend` changes the definition and *reports* where the
  old sense is in print. Rewriting those documents is a separate, deliberate change.

## Hard gates

Almost none — a glossary that can only be edited at one stage is a glossary that stays empty.

| Situation | What happens |
|---|---|
| no `<term>` given | Ask for it. If the conversation already surfaced a candidate, offer it rather than asking blank. |
| `docs/GLOSSARY.md` missing | Bootstrap from [`templates/GLOSSARY.md`](templates/GLOSSARY.md) and say so. `finnavis-prd` is hard-refusing to run; this fixes that. |
| `<term>` is technical | Refuse, and name where it belongs → [references/domain-vs-technical.md](references/domain-vs-technical.md). |
| `<term>` already defined, no `--amend` | Stop. Show the existing line. Ask: same concept (nothing to do, but still add it to the feature's term list if a slug was given), a different concept (two names needed), or a changed meaning (re-run with `--amend`). |
| `--amend` and `<term>` is not defined | Stop. Nothing to amend — drop the flag and add it. |
| a slug was given but `docs/features/<slug>/CONTEXT.md` does not exist | Not a refusal. Write the glossary, and say the feature's term list will be created by `finnavis-prd` — which is the normal order. |

## Language

Project rule, loaded from [`.claude/rules/language.md`](../../rules/language.md): ask in the user's
language, write the artifact in English, keep identifiers as names, explain the trade-off rather
than the vocabulary. The glossary is English always — every other skill quotes it verbatim.

## Inputs

- `<term>` — the word. Multi-word is fine (`Mapping profile`, `Set-aside row`). A batch is fine.
- `<slug>` — optional. Present → the feature's `CONTEXT.md` term list is updated too.
- `docs/GLOSSARY.md` — read in full, every run. The committed entries are the format.
- `docs/features/*/CONTEXT.md` — read to see which features already claim the term.
- `CLAUDE.md`, `ARCHITECTURE.md`, `SPEC.md` — read to decide domain versus technical. If
  `ARCHITECTURE.md` already explains the word, it is not a glossary term.

## What is already decided — never ask it again

| Topic | Decided | Source |
|---|---|---|
| Where a definition lives | `docs/GLOSSARY.md`, once, for the whole repo | the file's own header; every other skill reads it |
| What a feature's `CONTEXT.md` holds | Term **names** only, plus invariants, sentinel-error modules and out-of-scope. Never a definition | `finnavis-prd` templates/CONTEXT.md |
| Sections in the glossary | `## Glossary` only | the file as committed |
| Entry shape | `- Term — definition. NOT Other (how it differs).` The NOT-boundary is **mandatory** | every committed entry has one |
| Term case | First word capitalised, the rest lowercase unless a proper noun: `Mapping profile`, `Set-aside row` | every committed entry but `Category Group` |
| Ordering | Alphabetical, case-insensitive | the file as committed |
| Who confirms | The user, every time. Nothing is written from inference alone | this file, §The one thing |

## Protocol

With `--audit`, skip to [references/modes.md](references/modes.md) — nothing below applies, and
nothing is written.

### 0. Setup (read-only)

Read `docs/GLOSSARY.md` in full, not a grep. The committed entries are the only accurate
specification of the house format, and they beat any description of them.

Say in one line: how many terms exist today, whether this is an add or an `--amend`, and which run
shape applies — standalone write, or confirmation now with the write handed to the caller.

### 1. Domain or technical

Apply the test in [references/domain-vs-technical.md](references/domain-vs-technical.md): **could
the owner say this word in a sentence about their own money?** `Statement`, yes. `Cursor`, no.

A technical word is refused with its real destination named, not merely rejected.

### 2. Conflict check

Look for the term and for a near-miss — a singular or plural, a synonym already present, a word an
existing NOT-reference points at. Then follow the gate table.

### 3. Ask the definition (AskUserQuestion)

One sentence, in the owner's language about the owner's money. When the conversation, an
idea-brief or a PRD draft already phrased it, **offer those phrasings as options** instead of
asking blank — the user wrote them, and picking beats composing.

### 4. Ask the NOT-boundary (AskUserQuestion) — mandatory

Which concept this gets confused with, and how it differs. There is always an answer: with no
obvious homonym, the nearest term already in the glossary is the one to draw the line against. The
rule and the worked patterns → [references/entry-format.md](references/entry-format.md).

A term may need two boundaries. `Category Group` and `Default group` both have two.

**In a batch, fold steps 3 and 4 into one question per term.** Compose the whole line from what the
conversation already said, show it, and ask to confirm or adjust — with a named alternative as the
second option so the user can steer without typing. Six terms asked as twelve blank questions is
where a reader stops reading, and an unread question is a worse answer than a confirmed draft.
Each term still gets its own confirmation; nothing is approved as a group.

### 5. Compose

Build the line per [references/entry-format.md](references/entry-format.md) and work out its
alphabetical position. If a slug was given, note the term for that feature's list too.

### 6. Write

**Standalone shape:** insert the line into `docs/GLOSSARY.md` at its alphabetical position and
change nothing else. If a slug was given and `docs/features/<slug>/CONTEXT.md` exists, add the term
name to its `**Terms this feature uses:**` line, keeping that list alphabetical. Bump `updated_at`
in the frontmatter of each file touched.

**Caller shape:** write nothing. Hand the composed lines and their positions back as session state
and say so plainly, so the caller includes them in its own write phase.

With `--amend`, see [references/modes.md](references/modes.md) — the old line disappears, which is
the only irreversible thing this skill does.

### 7. Stamp, commit, handoff

Standalone only — a caller folds this into its own commit.

**Commits** — *commit it yourself* / *just print the command* (recommended). Message:
`glossary: + <term>`, `glossary: amend <term>`, or `glossary: + <term>, <term2>` for a batch.

```md
## ✅ finnavis-fix-term — <term>

**What I did**
- `docs/GLOSSARY.md` — <added | amended> `<term>`, now <N> terms
- `docs/features/<slug>/CONTEXT.md` — term list updated   ← only when a slug was given

**Review before continuing**
- the line itself — it is canonical for every document written after this
- <with --amend: the documents that still carry the old sense>

**Run next**
- resume what you were doing; no `/clear` needed — this is a one-line change
```

## Definition of Done

Each line is also the anti-pattern it rules out — the failure mode is named after the dash.

- The term is in `docs/GLOSSARY.md` in the house format, alphabetically — *every other skill reads this
  file by shape; an entry that breaks the shape is an entry they misread*.
- The NOT-boundary is present — *a definition without one is a label: it says what the word means
  in isolation, not where it stops, and overlapping edges are what causes the confusion*.
- The definition was confirmed by the user — *a silent line propagates into a PRD, a SAD, a schema
  and a contract before anyone reads it*.
- No definition was written into a feature's `CONTEXT.md` — *that file names terms and links out;
  a copied definition is a second source that goes stale*.
- A slug run also updated the feature's term list — *a feature using a word it never declared is
  exactly the gap the two-file split exists to close*.
- Nothing was written during a plan-mode phase — *the write belongs to the caller's write phase,
  and attempting it there fails in a way that looks like a broken skill*.
- A technical word was refused with its destination named — *"not here" sends the user away; "that
  belongs in an ADR" sends them somewhere*.
- Nothing else in either file changed — *a reformat buried in a one-term commit is invisible in
  review*.
- `updated_at` is today on every file touched — *a Living document with a stale date reads as
  abandoned*.
- The entry is true for every feature, not just the current one — *the glossary has no slug, so a
  core-ledger-shaped definition silently constrains csv-import*.

## References

| File | Used in | Holds |
|---|---|---|
| [references/entry-format.md](references/entry-format.md) | 4, 5 | The line format, NOT-boundary patterns, the case rule and its one committed exception, ordering, a worked pass |
| [references/domain-vs-technical.md](references/domain-vs-technical.md) | 1 | The test, the refusal list with destinations, the grey cases this repo has already decided |
| [references/modes.md](references/modes.md) | 6, audit | `--amend` and `--audit`: what each checks, what it preserves, how it reports |
| [templates/GLOSSARY.md](templates/GLOSSARY.md) | gate | Bootstrap scaffold, used only when the file is missing |

## Callers

| Skill | Phase | Shape |
|---|---|---|
| `finnavis-interview` | 3 (collect + confirm), 12 (write) | Caller — plan mode |
| `finnavis-prd` | 3 (collect + confirm), 10 (write) | Caller — plan mode |
| the user, directly | any | Standalone |
