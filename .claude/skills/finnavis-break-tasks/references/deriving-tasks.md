# Where each task comes from

A task is never invented. Every one traces to a section of an upstream document, and that trace
goes in the task's Why. If a task has no source, it is scope the user did not ask for — drop it or
ask.

## The source map

| Upstream | Produces |
|---|---|
| `sad.md` §5 containers and ports | the `domain`, `app` and `infra` task skeleton — one task per aggregate, per business area, per port |
| `sad.md` §6 flows | the order inside a layer. A flow that reads before it writes tells you the query task comes before the use case that calls it |
| `sad.md` §7 deployment | `wiring` tasks for anything that does not exist yet — compose stack, Testcontainers harness, CI |
| `sad.md` §10 quality gates | one `tests` task per gate |
| `sad.md` §11 risks and open items | parked items in `_epic.md`, and a `docs` task when the open item is a missing ADR |
| `data-model.md` + staged `migrations/` | the one `migration` task, and the table names the `infra` tasks use |
| `contracts/openapi.yaml` | the `ports` tasks — one per resource — and the error-code list the registry task implements |
| `contracts/api-sync-report.md` Deferrals | parked items, and a `docs` task per deferral that this epic closes |
| `PRD.md` §5 acceptance criteria | the `acs` of every task and most of the DoD lines |
| `PRD.md` §3 non-goals | the **Out** half of `_epic.md` §Scope. Nothing here becomes a task |
| Accepted ADRs | constraints quoted in Notes, and the hard rules list in `_epic.md` |

## Reading the rings into an order

`sad.md` §5 names the aggregates and the ports. Turn that into tasks in this order, and the graph
builds itself:

1. **Shared domain primitives** — value objects, enums, `IClock`, the domain error type. One task,
   no dependencies. Everything in Domain depends on it.
2. **One task per aggregate** — the entity, its invariants, and its repository port in
   `Domain/Abstractions`. All of these depend only on step 1, so they are the first parallel fan-out.
3. **Application abstractions** — `IUnitOfWork` and any query port. Depends on step 1.
4. **Persistence configurations** — the `DbContext` and one `IEntityTypeConfiguration<T>` per
   entity. Depends on every aggregate task.
5. **The migration** — depends on step 4.
6. **Repository adapters and the unit of work** — depend on steps 3 and 5.
7. **Read queries** — one task each, depending on steps 3 and 5.
8. **Wiring** — depends on everything registrable existing.
9. **Use cases, one task per business area** — depend on the adapters and queries they call.
10. **The error registry** — depends on the domain error type.
11. **Endpoints, one task per resource** — depend on their use case task, the registry, and wiring.
12. **Quality gates** — depend on the endpoints that make them measurable.
13. **The contract diff** — depends on every endpoint task.

This is a template, not a law. A feature with no new tables skips 4–5. A feature that only adds a
read skips 9–11 for most resources.

## Tasks that exist for a reason other than a ring

### Deployment tasks — `wiring`

`sad.md` §7 is the honest place to find out that nothing can be measured yet. In this repo the
compose stack, the Testcontainers harness and CI are all listed under "Not set up yet" in
`CLAUDE.md`. The feature that needs them first creates them, and later features depend on them.

Check `src/`, `tests/` and the repo root before emitting one of these. If `docker-compose.yml`
already exists, the task is not "create it" but either nothing or "add a service".

### Quality-gate tasks — `tests`

A SAD §10 row names a number the feature must hit: a latency budget, a property that must hold over
randomised input, a report figure that must survive a reshape. Each becomes one `tests` task whose
DoD is that number, copied verbatim — §10 took it from PRD §6 without rounding, and so does this.

These are the only `tests` tasks. Per-task tests are written first, inside the task, by the
implement stage.

### ADR tasks — `docs`

`sad.md` §11 or `api-sync-report.md` Deferrals sometimes name a decision that was made in passing
and never recorded. That is one `docs` task: write the ADR, then make both contracts agree with it.
It has no `acs` and usually blocks the task that depends on the decision.

### The contract-diff task — `docs`

Where `.claude/rules/openapi.md` closes the loop. Start the app, fetch `/openapi/v1.json`, diff it
against `contracts/openapi.yaml`, resolve every difference in the handler or in the contract.
Depends on every `ports` task. One per feature that has a contract.

## What gets no task

- **Anything in PRD §3 non-goals.** It goes in `_epic.md` §Scope under **Out**, by name, so a later
  reader can see it was considered.
- **Anything an earlier feature already built.** Depend on it. `csv-import` does not recreate the
  compose stack, the Testcontainers fixture, the error envelope or the `Account` entity — it reads
  `core-ledger`'s `tasks/` and depends on the outcome.
- **A PRD §8 open question.** It is not resolved work. It becomes a parked item with its owner.
- **"Refactor later", "add logging everywhere", "improve test coverage".** No acceptance criterion,
  no DoD, no end. If the user wants one, ask which criterion it serves.

## Coverage, both directions

Two checks, and both are findings when they fail:

- **Every PRD §5 criterion appears in at least one task's `acs`.** An unclaimed criterion is a
  feature nobody builds.
- **Every task either has a non-empty `acs` or a stated reason for an empty one.** Infrastructure
  tasks legitimately have none — a migration satisfies no acceptance criterion by itself. Say so in
  the task's Why rather than leaving the reader to guess.

Record the coverage line under the task table in `_epic.md`, including which criteria are claimed
more than once and why. A criterion proved at the query level, again at the use-case level and
again in a quality gate is correct, not duplication — but only if the reason is written down.
