# Layers, folders and serialization lanes

## The map

Nine layer names exist in the `tasks.json` contract. This repo emits eight of them. Each one is a
ring, a folder and a test project — that triple is what makes the task reviewable.

| `layer` | Lives in | Tests go to |
|---|---|---|
| `domain` | `src/FinNavis.Domain/{Entities,ValueObjects,Enums,Abstractions}/` | `tests/FinNavis.Domain.UnitTests` |
| `app` | `src/FinNavis.Application/<Area>/`, plus `Abstractions/` and `Common/` | `tests/FinNavis.Application.UnitTests` |
| `infra` | `src/FinNavis.Infrastructure/{Persistence,Repositories,Queries,Import}/` | `tests/FinNavis.Infrastructure.IntegrationTests` |
| `migration` | `src/FinNavis.Infrastructure/Persistence/{Configurations,Migrations}/` + the staged pair under `docs/features/<slug>/migrations/` | `tests/FinNavis.Infrastructure.IntegrationTests` |
| `ports` | `src/FinNavis.Presentation/Endpoints/<Resource>/<Operation>/`, `Middleware/` | `tests/FinNavis.Infrastructure.IntegrationTests` |
| `wiring` | `src/FinNavis.Infrastructure/DependencyInjection/`, `src/FinNavis.Application/DependencyInjection/` (does not exist yet — `CLAUDE.md` §DI says Application registers its own, `ARCHITECTURE.md` §Folder layout does not list the folder; create it, do not register Application types from `Program.cs`), `Program.cs`, `docker-compose.yml` | `tests/FinNavis.ArchitectureTests` + one resolve test |
| `tests` | `tests/**` | itself |
| `docs` | `docs/**`, `.claude/rules/**`, an ADR under `docs/adr/` or `docs/features/<slug>/adr/` | none |
| `ui` | **never emitted** — see below | — |

### Why `ui` never appears

`src/web` is listed under "Not set up yet" in `CLAUDE.md`, and both `sad.md` files declare
`target_surfaces: [backend-service]`. A `ui` task would be work with no project to put it in.

This flips the day a SAD declares `web-frontend`. Read `target_surfaces` from the feature's own
`sad.md` frontmatter every run — do not assume it from the last feature.

### Why `Abstractions/` is not its own layer

A repository port is declared in `Domain/Abstractions` and belongs to the aggregate's `domain`
task. An outside-world port is declared in `Application/Abstractions` and belongs to the `app`
task that needs it. Splitting the interface from the thing it describes produces two tasks that
must land together — which is one task.

## Cut rules per layer

How many tasks a layer produces. The rule is the same shape every time: **one task per unit the
repo already treats as a unit.**

| Layer | One task per | Split when |
|---|---|---|
| `domain` | aggregate | the aggregate has more than ~3 invariants with their own tests |
| `domain` | shared value objects, enums and `IClock` — **one task, first** | never; it is the root of the graph |
| `app` | business area at the Application root (`Accounts/`, `Transactions/`, `Reporting/`) | the area has more than ~5 use cases |
| `infra` | repository adapters + `IUnitOfWork` together — **one task** | the feature has more than ~6 aggregates |
| `infra` | each read query behind a query port — **one task each** | never merge them; they are the expensive ones and each has its own index to prove |
| `migration` | feature | only when the feature adds tables in two separate deploys |
| `ports` | **resource**, not operation | the resource has more than ~5 operations, or the task passes ~500 lines |
| `ports` | the error registry and the `problem+json` switch-off — **one task, before every endpoint task** | never |
| `wiring` | feature — one DI and composition-root task | the compose stack or CI does not exist yet; then those are their own tasks |
| `tests` | SAD §10 quality gate | never; a gate is one task |
| `docs` | the contract diff, and each ADR the design stage deferred | — |

### Why `ports` is per resource and not per operation

`CLAUDE.md` says each operation owns its folder and its own request and response records, shared
with nothing. So a resource's operations touch disjoint files, and putting them in one task costs
nothing in collision risk while saving five near-identical task files.

Twenty-one operations across six resources is six tasks, not twenty-one. That is what
`core-ledger` produced.

### Why the error registry comes before the endpoints

`docs/adr/0002` and `docs/adr/0003` fix one envelope and one spelling, and
`.claude/rules/openapi.md` §5 warns that ASP.NET Core answers `application/problem+json` by
default for anything that never reached a handler. That switch-off is global. If each endpoint task
did it, the first one to land would own it and the rest would be no-ops with merge conflicts.

So: one `ports` task creates the registry from the contract's code list and turns the default off.
Every endpoint task lists it in `deps`.

## `files_hint`

The directories and files the task touches. It is not a comment — the implement stage uses it to
decide what may run in parallel, so an inaccurate hint is a real defect.

Rules:

- **List directories, not every file**, except where a single shared file is the point
  (`Program.cs`, `FinNavisDbContext.cs`, `PostgresFixture.cs`).
- **Include the test project.** A task's tests are written inside it, so the test directory is
  touched.
- **A migration task points at the staged spec**, `docs/features/<slug>/migrations/`, as well as
  `src/FinNavis.Infrastructure/Persistence/Migrations/`. The staged pair is read and diffed, which
  still means the task owns it.
- **Never list a path the task only reads** — the contract, the PRD, `data-model.md`. Those go in
  the task's Why section as links.

## Lanes

Two tasks run in parallel when their `deps` are satisfied and they share no lane. Three things
create a lane:

1. **`layer: migration`** — always serialized into one ordered sequence, whatever the graph says.
   EF Core keeps a `ModelSnapshot`, so two migrations generated at once produce a snapshot that
   matches neither.
2. **Overlapping `files_hint`** — regardless of layer.
3. Nothing else. `layer: ui` would not be serialized, but this repo emits none.

### The files that actually create lanes here

Worth checking by name, because they are the ones that bite:

| File | Shared by |
|---|---|
| `src/FinNavis.Presentation/Program.cs` | the `wiring` task and any `ports` task that registers middleware |
| `src/FinNavis.Infrastructure/Persistence/FinNavisDbContext.cs` | the configurations task and any later task adding a `DbSet` |
| `src/FinNavis.Infrastructure/DependencyInjection/*` | `wiring`, and any task registering a new adapter |
| `tests/FinNavis.Infrastructure.IntegrationTests/PostgresFixture.cs` | the harness task and the migration task |
| `.github/workflows/ci.yml` | the CI task and any task adding a gate to it |
| `docker-compose.yml` | the compose task and anything adding a service |

If two tasks must share one of these, say so in both tasks' Notes — "shares a lane with T<NN> on
`Program.cs`" — so the reason survives without reading `tasks.json`.

## Ids and file names

- Ids are `T01`, `T02`, … — **zero-padded to two digits**, so the task files sort in graph order in
  any file listing. The generic contract shows `T1`; this repo pads.
- File names are `T<NN>-<short-kebab-slug>.md`, the slug naming the thing, not the verb:
  `T08-domain-transaction.md`, `T24-ports-error-envelope-and-registry.md`.
- Ids are stable across `--reconcile`. A task that is dropped leaves its number unused rather than
  renumbering everything below it.
