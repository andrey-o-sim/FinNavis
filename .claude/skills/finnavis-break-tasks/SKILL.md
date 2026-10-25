---
name: finnavis-break-tasks
description: >
  Breaks a designed feature into atomic, ≤1-day tasks for a solo, self-hosted .NET pet project
  (FinNavis and projects shaped like it): writes docs/features/<slug>/tasks/ — _epic.md with the
  dependency DAG, tracker.md, one T<NN>-<slug>.md per task, and tasks.json, the machine contract
  the implement stage reads. The cut follows the four rings, never a vertical slice: a task is one
  ring of one business area, so the dependency rule the ArchitectureTests enforce is also the shape
  of the graph. Tuned to a repo where the layout, the commands and the test tiers are already fixed
  by CLAUDE.md and ARCHITECTURE.md — four fixed test projects, EF Core migrations diffed against a
  staged SQL spec, TreatWarningsAsErrors instead of a lint task, one owner, no UI surface — so none
  of them is ever re-asked. Use this whenever the user wants a task breakdown, a work plan, a
  dependency graph or tasks.json for a feature in this repo — including phrasings like "разбей на
  задачи {slug}", "декомпозиция {slug}", "план работ", "список задач", "что делать по порядку",
  "/finnavis-break-tasks {slug}" — even when they do not say the word task. Hard-refuses if
  docs/features/<slug>/PRD.md, sad.md, an Accepted ADR or the repo-root CONTEXT.md is missing, and
  refuses to overwrite an existing tasks/ without --reconcile.
argument-hint: "[slug] [--reconcile]"
---

# Task breakdown — solo project

Stage 06, the last design stage before code. `finnavis-architecture-design` settled the rings and
the ports, `finnavis-complete-sequence-diagrams` the flows, `finnavis-generate-data-model` the
columns, `finnavis-api-forge` the HTTP surface. This skill turns all four into an ordered list of
changes, each one small enough to finish and review in a sitting.

Four outputs, all under `docs/features/<slug>/tasks/`:

- `_epic.md` — goal, scope, the DAG as a Mermaid `flowchart`, the task table, the hard rules.
- `tracker.md` — one status row per task. The implement stage flips `todo` → `done` as it commits.
- `T<NN>-<short-slug>.md` — one per task: frontmatter, Why, What, Definition of Done, Notes.
- `tasks.json` — the machine contract. Same model as the markdown, same field names.

## The one thing to understand before anything else

**A task is one ring of one business area. Never a vertical slice.**

The obvious way to cut a feature is by user story: "accounts end to end", then "transactions end to
end". That is wrong here, and not as a matter of taste.

`CLAUDE.md` and `ARCHITECTURE.md` fix one direction of dependency — Presentation → Infrastructure →
Application → Domain — and `tests/FinNavis.ArchitectureTests` fails the build when it is broken. A
vertical-slice task touches all four rings at once, so:

- it cannot be reviewed against the dependency rule, because every ring moved in the same diff;
- it has no single test tier — the repo has four test projects, one per ring, and a slice spans
  all of them;
- it collides with the next slice on `FinNavisDbContext`, `Program.cs` and the DI registration,
  turning two "parallel" tasks into a merge conflict.

So the cut is **ring first, area second**: `Transaction` the entity, then the transaction use
cases, then the transaction endpoints. Each is one project, one test project, one reviewable diff.

The useful consequence: **the dependency graph is not invented, it is read off the rings.** Domain
has no inward dependency, so every domain task starts at once. Application needs Domain.
Infrastructure needs both. Presentation needs everything plus the error registry. That is why this
skill derives `deps` instead of asking for them.

## When to use

- "разбей на задачи `<slug>`", "декомпозиция `<slug>`", "план работ", "список задач", "с чего
  начинать", and the same asks in any language the user writes in.
- `/finnavis-break-tasks <slug>` as an explicit call.
- After `contracts/openapi.yaml` exists. Without it the `ports` tasks are guesses, and the error
  registry has no code list.
- `--reconcile` — `tasks/` exists and an upstream document moved. Re-derives, and changes only
  what actually differs → [references/reconcile.md](references/reconcile.md).

## When not to use

- **To write code.** That is the implement stage. This one writes markdown and JSON, nothing under
  `src/` or `tests/`.
- **To decide an aggregate, a flow, a column or an endpoint.** SAD §5, SAD §6, `data-model.md` and
  `contracts/openapi.yaml` settled those. A breakdown built on a wrong flow gets rewritten, not
  patched — fix the source and come back.
- **To re-open a repo convention.** See the table below. Those are closed.
- **When `tasks/` exists and nothing upstream moved.** Re-running overwrites a reviewed plan and
  erases the statuses in `tracker.md`. Use `--reconcile`, or edit the one task that changed.

## Hard gates (stop, do not improvise)

| Missing | Message |
|---|---|
| `<slug>` | Ask for it, or list the folders under `docs/features/` that have a `sad.md` and no `tasks/`. |
| `docs/features/<slug>/PRD.md` | «Run `finnavis-prd <slug>` first — without §5 acceptance criteria no task has a Definition of Done.» |
| `docs/features/<slug>/sad.md` | «Run `finnavis-architecture-design <slug>` first — §5 is where the task boundaries come from.» |
| no Accepted ADR in `docs/features/<slug>/adr/` | «The design has no recorded decisions. Run `finnavis-architecture-design <slug>`, or record the decision that was made.» |
| `CONTEXT.md` at the repo root | «The glossary is missing. Without it the task titles invent names for things that already have one.» |
| `docs/features/<slug>/tasks/` already exists, no `--reconcile` | Refuse. «A breakdown exists. Re-running erases the statuses in `tracker.md`. Use `/finnavis-break-tasks <slug> --reconcile`, or delete the folder on purpose.» |
| `docs/features/<slug>/contracts/openapi.yaml` | Not a refusal — a warning. The `ports` tasks lose their operation list and the registry task its code list. Offer `finnavis-api-forge <slug>` first, and ask whether to continue. |
| `docs/features/<slug>/data-model.md` | Not a refusal — a warning. There is no `migration` task to anchor and the `infra` tasks lose their table names. Offer `finnavis-generate-data-model <slug>` first. |

## Language

Project rule, loaded from [`.claude/rules/language.md`](../../rules/language.md): ask in the user's
language, write the artifact in English, keep identifiers as names, explain the trade-off rather
than the vocabulary. Task ids, layer names, file paths, `AC-NN` and `§6` never translate.

## Inputs

- `<slug>` · `PRD.md` (§2 goals, §3 non-goals, §4 user stories, §5 acceptance criteria, §6 NFR,
  §8 open questions) · `sad.md` (frontmatter `target_surfaces`, §5 containers and ports, §6 flows,
  §7 deployment, §10 quality gates, §11 risks) · every Accepted ADR in `adr/` · `CONTEXT.md` —
  **required**.
- `data-model.md` and the staged pairs under `migrations/` — the `migration` and `infra` tasks.
- `contracts/openapi.yaml` and `contracts/api-sync-report.md` — the `ports` tasks, the error
  registry, and the Deferrals that become parked items in `_epic.md`.
- `CLAUDE.md`, `ARCHITECTURE.md`, `.claude/rules/{migrations,openapi,language}.md`,
  `docs/adr/` — read every run. Read, never written.
- `.size` — how many tasks the feature should produce, as a sanity check, not a cap.
- **Every earlier feature's `tasks/`** — a task that already exists upstream is depended on, never
  written a second time. `core-ledger` creates the compose stack, the Testcontainers fixture and
  the error envelope; `csv-import` must not recreate any of them.
- `src/` and `tests/` — read to see what already exists. A task that creates a file that is already
  there is a reconcile finding, not a new task.

## What is already decided — never ask it again

The repo fixed these. Re-asking them wastes the user's time and invites an answer that contradicts
a committed document.

| Topic | Decided | Source |
|---|---|---|
| Task cut | Ring first, area second. Never a vertical slice | `ARCHITECTURE.md`, `tests/FinNavis.ArchitectureTests` |
| Dependency direction | Domain → Application → Infrastructure → Presentation. `deps` are derived from it, never asked | `CLAUDE.md` Dependency rules |
| `ui` layer | **Never emitted.** `src/web` is not started and both SADs declare `target_surfaces: [backend-service]` | `sad.md` frontmatter, `CLAUDE.md` "Not set up yet" |
| Owner | `Andrey Simonenko` on every task. One owner, so the field is filled, never asked | single-owner repo |
| Atom size | ≤1 working day. S = 2h, M = half-day, L = day; more than L splits | generic baseline, confirmed for this repo |
| Migrations | EF Core via `./build.sh migrate <Name>`. The staged `.up.sql`/`.down.sql` is a **spec that is diffed, never applied** | `.claude/rules/migrations.md` |
| Test framework | xUnit + AwesomeAssertions, `.Should()`, `Method_Scenario_ExpectedResult` | `CLAUDE.md` §Tests |
| Where a test lives | Four fixed projects, one per ring. A task's tests go in the one that matches its layer | `CLAUDE.md` Layout |
| Per-task tests | Written **first**, inside the task, by the implement stage (`tdd: true`). A separate `tests` task exists only for a SAD §10 quality gate | `.claude/sdlc.local.md` |
| Lint gate | `TreatWarningsAsErrors` — `./build.sh build` **is** the lint gate. There is no lint or vet command to put in a DoD | `Directory.Build.props`, `CLAUDE.md` |
| Commands | `./build.sh build \| test \| run \| migrate \| db-update \| clean`. No `check` target, no CI | `build.sh` |
| Error envelope | One `ports` task owns the registry and switches off `application/problem+json`, and every endpoint task depends on it | `docs/adr/0002`, `docs/adr/0003`, `CLAUDE.md` §API |
| Response records | Never shared between operations, so two endpoint tasks for two resources never share a lane | `CLAUDE.md` §API |
| Contract direction | Handlers follow `contracts/openapi.yaml`; `/openapi/v1.json` is diffed **against** it | `.claude/rules/openapi.md` |
| Safety | No real statement, export, connection string or key in any fixture a task creates | `CLAUDE.md` Safety |

If the user asks to change one of these, that is an ADR, not an answer inside this run. Say so and
offer `finnavis-architecture-design` or a standalone ADR.

## Protocol

### 0. Setup (read-only)

Read the gates and the inputs. Read the three templates. Read every earlier feature's `tasks/` so
ids, titles and file names stay in one style across features.

Then say, in one line each:

- whether `contracts/openapi.yaml` and `data-model.md` are present, and what is lost if not;
- the `target_surfaces` read from `sad.md` frontmatter, and therefore which layers can appear;
- `--reconcile` or a first run.

With `--reconcile`, jump to [references/reconcile.md](references/reconcile.md) and follow it
instead of steps 1–5.

### 1. Work items, layer by layer

Nine layers exist; this repo emits eight of them and never `ui`. The layer → ring → folder map, the
cut rule for each layer, and what belongs in `files_hint` →
[references/layers-and-lanes.md](references/layers-and-lanes.md).

Where each task comes from — which upstream section produces it, how a §10 quality gate becomes a
`tests` task, what gets no task at all → [references/deriving-tasks.md](references/deriving-tasks.md).

Every task is ≤1 working day. A change over roughly 500 lines is a signal the task is too wide, not
a reason to shrug.

### 2. Checkpoint — the task list (AskUserQuestion, mandatory)

Before writing thirty files, show the list: id, title, layer, estimate, grouped by layer, with the
total count and the person-day sum. Ask **Proceed** / **Too coarse, split further** / **Too fine,
merge**.

A bad cut found here costs one message. Found after the files are written it costs a rewrite of all
of them.

### 3. Dependencies and lanes

`deps` come from the rings, not from a question. Then mark the lanes that serialize work regardless
of the graph — `layer: migration`, and any two tasks whose `files_hint` overlap. The files that
actually create lanes in this repo are listed in
[references/layers-and-lanes.md](references/layers-and-lanes.md).

State the parallel branches in words under the diagram. A DAG with no parallel branch means the cut
collapsed into a chain — go back to step 1.

### 4. Definition of Done, `acs`, `files_hint`

One testable DoD per task, written from the repo's real commands and tiers. Patterns per layer, and
the AC-coverage rule → [references/dod-catalog.md](references/dod-catalog.md).

`acs` lists the PRD §5 ids the task satisfies. An id may appear in several tasks when it is proved
at several levels — a balance rule in the query, in the use case and in the quality gate. An id in
**no** task is a finding that stops the run.

### 5. Write the four artifacts

From [`templates/_epic.md`](templates/_epic.md), [`templates/tracker.md`](templates/tracker.md) and
[`templates/task.md`](templates/task.md), plus `tasks.json`.

Task files **link** upstream, never paste it. `[PRD §AC-07](../PRD.md)`, `[sad.md §6](../sad.md)`,
`[ADR-0005](../adr/0005-....md)`, `[openapi.yaml](../contracts/openapi.yaml)`. A task body that
repeats an acceptance criterion verbatim has created a second copy that will go stale.

Then run the `flowchart` checks in [references/mermaid-lint.md](references/mermaid-lint.md). Nothing
else in the repo will catch a broken diagram.

### 6. Self-check

Run the Definition of Done below as a list, and report each line. A failure is fixed before the
commit question, not noted as a caveat.

### 7. Checkpoint — commit (AskUserQuestion) and handoff

**Commits** — *commit it yourself* / *just print the command* (recommended). Message:
`06: task-breakdown for <slug> (breakdown + tasks.json)`.

```md
## ✅ finnavis-break-tasks — <slug>

**What I did**
- `tasks/` — <N> tasks across <L> layers, ~<P> person-days, <B> parallel branches at the start
- `tasks.json` — <N> entries, DAG acyclic, every PRD §5 criterion claimed
- <K> items parked in `_epic.md` — each with where it came from and why

**Review before continuing**
- the task table in `_epic.md` — the order you will actually work in
- the parked items — each is something this epic deliberately does not close
- the coverage line — which criteria are claimed by more than one task, and why

**Run next**
1. `/clear`
2. the test plan, then the implement stage with `tasks/tasks.json`
```

## Definition of Done

Each line is also the anti-pattern it rules out — the failure mode is named after the dash.

- Nothing was written under `src/` or `tests/` — *this stage plans the work; a half-written entity
  must not reach `./build.sh build`*.
- Every task is one ring of one area — *a vertical slice cannot be reviewed against the dependency
  rule and collides with the next slice on `Program.cs`*.
- No `ui` task exists — *`src/web` is not started and `target_surfaces` says `backend-service`, so
  a UI task is work nobody can start*.
- Every task is ≤1 working day with an estimate — *a five-day task produces a diff nobody can
  review, including its author three evenings later*.
- `deps` form an acyclic graph with at least one parallel branch — *a chain means the cut collapsed
  and the rings were ignored*.
- `layer: migration` tasks are serialized, and overlapping `files_hint` share a lane — *two agents
  editing `Program.cs` at once is the one failure the graph cannot express*.
- Every migration task diffs the generated script against the staged spec — *EF Core owns the
  snapshot, so hand-written SQL and generated SQL cannot both be applied*
  (`.claude/rules/migrations.md`).
- Every PRD §5 criterion appears in at least one task's `acs` — *an unclaimed criterion is a feature
  nobody builds and nobody misses until the review*.
- Every task has a testable DoD naming a real command — *"done when it works" is a status, not a
  gate*.
- A DoD never names a lint or vet command — *this repo has none; `TreatWarningsAsErrors` makes
  `./build.sh build` the gate*.
- One `ports` task owns the error registry, and every endpoint task depends on it — *two endpoints
  inventing their own error shape is how a client that switches on `code` breaks*.
- Task bodies link upstream instead of pasting it — *a pasted acceptance criterion is a second copy
  that goes stale silently*.
- A task that an earlier feature already created is depended on, not rewritten — *`csv-import` must
  not create the compose stack `core-ledger` already created*.
- `tasks.json` and the markdown carry the same ids, deps and acs — *they are written from one model;
  two models drift*.
- The `flowchart` passes the lint checks — *a broken diagram renders as an error box, and nobody
  fixes an error box in a file they are only reading*.
- Repo conventions were read, not re-decided — *a breakdown that quietly disagrees with `CLAUDE.md`
  makes both of them untrustworthy*.

## References

| File | Used in | Holds |
|---|---|---|
| [references/layers-and-lanes.md](references/layers-and-lanes.md) | 1, 3 | The layer → ring → folder map, the cut rule per layer, `files_hint` rules, the files that create lanes in this repo |
| [references/deriving-tasks.md](references/deriving-tasks.md) | 1, 4 | Which upstream section produces which task, quality gates, ADR and docs tasks, what gets no task |
| [references/dod-catalog.md](references/dod-catalog.md) | 4, 6 | DoD patterns per layer with the repo's real commands, the AC-coverage rule, estimate calibration |
| [references/reconcile.md](references/reconcile.md) | 0 | `--reconcile` semantics: what is preserved, what is re-derived, how a finding is reported |
| [references/mermaid-lint.md](references/mermaid-lint.md) | 5 | The `flowchart` checks that stand in for a renderer |
| [templates/_epic.md](templates/_epic.md) · [templates/tracker.md](templates/tracker.md) · [templates/task.md](templates/task.md) | 5 | The artifacts |
