# Ask style — questions for the person who wrote the repo

Every `AskUserQuestion` in this skill follows these rules. They are not the generic ones: a generic
style glosses each technical term for someone reading the codebase for the first time. Here the
reader wrote `ARCHITECTURE.md`.

> **The examples below are in English because this file is. The questions are not.**
>
> Ask in whatever language the user opened the conversation in, and stay in it for the whole run.
> What follows is the *shape* of a good question — the three blocks, the action-form labels, the
> cost named out loud. Copy the shape, write the words fresh in their language. A question that is
> grammatically perfect in the wrong language has already failed.

## The two rules that matter

1. **Explain the trade-off, not the vocabulary.** Do not gloss `index`, `migration`, `aggregate`,
   `port`, `backfill`, `UNION`. The reader uses these daily, and glossing them reads as
   condescension. Do explain what the choice costs: what has to be rewritten later, which data needs
   a backfill, how many evenings the other option adds, what breaks in six months.
2. **An option label is the next mechanical step, not a name.** "Two tables, plus an ADR" says what
   will happen. "Option A" does not.

## Language

Mirror the language the user wrote in. Every question, label and description follows it. Do not
switch, and do not ask which language they prefer — it is already on screen. The artifact is written
in English regardless, and so are the commit messages.

Keep identifiers and names in their original form, in any language: `CLAUDE.md`, `ILedgerQueries`,
`AC-04`, `§11`, `target_surfaces`, `UUID v7`. They are names, not jargon to translate.

## Shape

- **`question`** — three short blocks, each one or two sentences:
  - **what is being decided**, with a concrete example from this feature;
  - **what it costs if it is wrong**, and whether it is reversible;
  - a nudge to read the options.
- **Name the section, never just number it.** `§4 (Solution strategy)`, `§11 (Risks and technical
  debt)`, `§8 (Crosscutting concepts)`. A bare `§8` forces the reader to remember a numbering they
  have no reason to hold in their head — and in a question they cannot look it up, because the
  document is not on screen yet. Name it on every mention inside a question, not only the first.
  The full list is the walk-order table in [socratic-loop.md](socratic-loop.md#walk-order).
- **`header`** — ≤12 characters, where the number alone is fine because the question body spells it
  out: `§4 strategy`, `Surface`, `ADR?`, `§8 ids`, `Critic`.
- **each option**:
  - `label` — 1-5 words in action form. "(Recommended)" on the first option when you recommend one.
  - `description` — 2-4 sentences carrying three things: what I do next concretely (which section
    changes, whether an ADR is born), what it costs or buys, and the hidden catch if there is one.

## The canonical action set

```
- label: "Accept"
  description: "I keep the wording as drafted, run the ADR gate over it, and move on."
- label: "Change it"
  description: "You give a different decision, I redraw the section under it and ask once more.
    One round — the second answer is final."
- label: "Open question (§11 Risks)"
  description: "The decision is not taken now: a row in §11 (Risks and technical debt) with
    severity 'Open question'. I will ask for the owner and the due date next. Without both it
    becomes a Drop."
- label: "Drop it"
  description: "I remove the item. If the section cannot stand without it, I reframe the options
    and ask once more."
```

## Question shapes specific to this stage

### Target surface — §4 (Solution strategy), always asked

The one question that cannot be skipped, dropped or deferred: every later section and every later
stage reads the answer.

```
Question: "What exactly are we building here. PRD §3 says the client is a separate later feature,
  which leaves the REST API and its database. The answer goes into target_surfaces, decides how
  many containers §5 (Building block view) draws, and is read by every stage after this one.
  Changing it later means rewriting §5 (Building block view) and §6 (Runtime view)."
Options:
- label: "backend-service only (Recommended)"
  description: "target_surfaces: [backend-service]. §5 (Building block view) draws one container —
    the ASP.NET Core process with the database beside it. Matches PRD §3 Non-goals. If a screen
    shows up later it becomes its own feature with its own SAD, and this document never has to
    change."
- label: "backend-service + web-frontend"
  description: "I add a second container and a UI decision in §4 (Solution strategy). Worth
    knowing: src/web is an empty folder and SPEC.md still keeps the PWA under 'Planned, not built
    yet'. So this is not a choice between SSR and SPA — it is the first line of a client that does
    not exist, and it earns a row in §11 (Risks and technical debt) whichever way it goes."
```

### A strategic choice that will spawn an ADR

Say that an ADR is coming. The user should know a file is about to be born, and the options they see
are the ones that will appear in `## Considered options` — so a thin option set here produces a thin
ADR later.

```
Question: "A transaction and a transfer: one table with a discriminator, or two separate tables.
  The glossary already treats them as different things, but two tables cost a UNION in history and
  a three-part sum in the balance. Undoing it later is a migration against recorded history, so
  3+ evenings. The gate scores 3 of 3, which means this becomes an ADR — the options below are
  what will be written into it as the ones considered."
```

### Blast-radius borderline

Only when the gate scores exactly 1. Never dress a 0 or a 2 as a question.

```
Question: "This one is on the edge. It is hard to undo, but it lives inside a single project, so a
  reader a year from now may never need the backstory. Does it earn a file in adr/, or is a
  paragraph in §5 (Building block view) enough?"
Options:
- label: "Lock it as an ADR (Recommended)"
- label: "Keep it inline in §5 (Building blocks)"
```

### A row in §8 (Crosscutting concepts) where the project already has a default

The default goes first, with its reason. The question is an override, not a blank field.

```
Question: "Identifiers. The project default is UUID v7, generated in the Application layer. The
  alternative only earns its place if this feature's rows arrive from outside and the other side
  brings an identifier of its own."
```

### Resolving a critic finding

Three options, always the same three. The third one is what makes a deliberate exception readable
six months later instead of looking like an oversight.

```
- label: "Accept as suggested"
- label: "Accept, my wording"
- label: "Leave it, here is why"
  description: "The document does not change. A line goes into §1 (Introduction and goals), ¶4:
    «Decision override: <the finding> — rationale: <your words>»."
```

## Worked rewrite

Too dry — unanswerable without re-reading the draft:

```
Question: "§5: separate query port?"
Options: "Yes" / "No"
(Bare section number, no cost named, labels that are not actions — three failures in two lines.)
```

Right — the decision, its cost, and what each answer does:

```
Question: "The reads that aggregate — the period breakdown and the balance — go either through the
  repositories or through a port of their own. A repository returns entities by definition, so the
  breakdown would load ~20 000 Transaction objects and sum them in C#. A separate port puts the
  SUM and the GROUP BY in PostgreSQL. PRD §6 budgets p95 ≤ 500 ms, and that budget rests entirely
  on this choice."
Options:
- label: "Separate query port (Recommended)"
  description: "ILedgerQueries in Application/Abstractions, its adapter in Infrastructure/Queries.
    §5 (Building block view) gets a paragraph on splitting the read and write paths, and an ADR is
    born. The cost: two kinds of abstraction and a second set of DTOs — a reader has to know
    whether they want a read or a write before they know where to look."
- label: "Everything through repositories"
  description: "One kind of abstraction, all in Domain/Abstractions. The honest price: either
    20 000 entities in memory and a missed budget that degrades every year, or repositories start
    returning report DTOs — at which point they are no longer a collection view of an aggregate,
    and you have the query port anyway, just hidden."
- label: "Open question (§11 Risks)"
  description: "§5 (Building block view) ships with no decision on the read path, and §11 (Risks
    and technical debt) gets a row with owner and due. Note what that costs: the latency scenario
    in §10 (Quality requirements) has nothing to stand on, so its How-verify is left hanging."
```

The second version costs three more lines and removes a round trip. That is the trade.

## Forbidden

- Terse labels with no action (`Accept`, `Yes`, `Option B`) on a decision that is hard to reverse.
- Hiding the catch in a follow-up ("if you pick this, I'll ask about X later").
- Asking about anything in [repo-conventions.md](repo-conventions.md) section A. It is decided.
- Asking a question whose answer is already in `PRD.md`, `CONTEXT.md` or an Accepted ADR.
- More than 4 options, or more than 4 questions in one call.
- Pasting raw Mermaid as the body of a question — describe the diagram in prose instead.
- A bare `§7` anywhere in a question or an option. Name the section every time.
- Writing the question in English because this file is in English. Mirror the user.
