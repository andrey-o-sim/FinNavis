# Repo map — feasibility scan

Used in Phase 10. Read-only. The goal is to replace "Tech ☑ — we know how" with "Tech ☑ — this
is the same shape as `<module>`, which already works".

## A note on the course brief

The course task asks for "sentinel errors and module patterns of your repo as reference".
Sentinel errors are a Go idiom (`var ErrNotFound = errors.New(...)`) and this repo is .NET —
there is no literal equivalent, and as of now `Application/Common` is empty, so there is no
error convention to point at yet. The part of that instruction that does transfer is the
**module pattern**: the agent must know how this repo is laid out, so feasibility is read off
the code instead of guessed. That is what this file is.

When an error-handling convention does land in `Application/Common`, add it to the "What a
feature touches" table below — the feasibility scan should be able to say whether a feature
needs a new failure mode or reuses an existing one.

## Layers

Dependencies point inward only. Full rules in `CLAUDE.md` and `ARCHITECTURE.md`.

The folder tree itself lives in `ARCHITECTURE.md` § Folder layout — do not rebuild it from the
table below. This table adds the one thing that file has no reason to carry: what each location
means for **feasibility**, which is the only reason this skill reads the repo at all.

```
Presentation ──> Infrastructure ──> Application ──> Domain
                       │                                ▲
                       └────────────────────────────────┘
```

| Path | Holds | Feasibility meaning |
|---|---|---|
| `src/FinNavis.Domain/Entities` | Entities and aggregates | A feature needing a **new** aggregate is a bigger step than one extending an existing entity |
| `src/FinNavis.Domain/ValueObjects` | Value objects | A new value type (Money, DateRange) is cheap; changing an existing one touches everything that uses it |
| `src/FinNavis.Domain/Enums` | Domain enums | Stored as strings, never ints — adding a member is cheap, renaming is a data change |
| `src/FinNavis.Domain/Abstractions` | Repository ports, domain services, `IClock` | One repository per aggregate, added when a use case needs it — not per table |
| `src/FinNavis.Application/<Area>/` | Use cases, one folder per business area at the project root | **Start the scan here.** A use case doing something structurally similar is the strongest Tech ☑ evidence available. There is no `Features/` wrapper — everything in this layer is a feature |
| `src/FinNavis.Application/Abstractions` | Outside-world ports: `IUnitOfWork`, importers, file storage | A feature needing a **new** outside-world port (file, network, clock) is a new adapter in Infrastructure too |
| `src/FinNavis.Application/Common` | Shared application plumbing | Empty today. When it holds a result/error convention, a new failure mode becomes a cost to name |
| `src/FinNavis.Infrastructure/Persistence/Configurations` | One entity configuration per entity | New entity ⇒ new configuration file ⇒ new migration |
| `src/FinNavis.Infrastructure/Persistence/Migrations` | Schema history | **Has any migration ever run?** If not, "we know the data path works" is unproven |
| `src/FinNavis.Infrastructure/Repositories` | Repository adapters | An adapter implementing a port that already exists is routine work |
| `src/FinNavis.Infrastructure/DependencyInjection` | Registration for Infrastructure's own services | A new adapter is also a new registration — cheap, but never free to forget |
| `src/FinNavis.Presentation/Endpoints/<Resource>/<Operation>/` | One folder per operation: the endpoint plus the request and response records only it uses | A feature adding a resource adds a folder group; one extending a resource adds one folder. There is no central `Contracts/` — contracts live with their operation, and no two operations share a response record |
| `src/web/` | React PWA | **Not started.** Any feature needing a screen is unproven ground, whatever its effort signal says |
| `tests/FinNavis.ArchitectureTests` | Dependency-rule guard | Already passing: the layering is enforced, not aspirational. Counts as evidence |
| `tests/FinNavis.Infrastructure.IntegrationTests` | Testcontainers | Needs Docker. If the local setup has never run, say so — it is a real first-time cost |
| `docs/features/*/idea-brief.md` | Earlier briefs | Read them. Their §16 may already name the feature being interviewed |
| `docs/features/*/sad.md` + `adr/` | Per-feature architecture, once a feature has been designed | A decision recorded in an Accepted ADR is a constraint on the new feature. It also shows what that feature's own first tasks turned out to be |
| `docs/adr/` | Project-wide decisions | A decision already recorded is a constraint, not an open question |

## How to run the scan

1. **`ls docs/features/`** — what has been interviewed before. Read the §13 locked-in pointer
   and the §16 unlock list of each. This is the cheapest and most useful step.
2. **`git log --oneline`** — has anything shipped, or is the history all scaffolding? Count
   commits between the brief of a past feature and its last code commit; that is the only
   evening baseline the repo offers.
3. **`ls src/FinNavis.Application`** — is there a business area with a use case of the same
   shape? Areas sit at the root of the project, one folder each.
4. **`ls src/FinNavis.Infrastructure/Persistence/Migrations`** — has the data path ever run
   for real?
5. **`ls src/web`** — if the feature needs a screen, is there a client at all?

Keep any wider scan bounded — `bin/` and `obj/` are enormous and carry nothing:

```bash
find src tests -type d -not -path "*/bin/*" -not -path "*/obj/*" | sort
```

## Writing the three boxes

- **Tech** — cite a module by path, or write "no adjacent module — all scaffolding" and set ☐.
  Scaffolding is not proof. `core-ledger` honestly carried ☐ here and that was correct.
- **Evenings** — cite a comparable feature's elapsed time from the git history, or write "no
  baseline in the history" and set ☐. Guessing a week because it feels like a week is the
  thing this box exists to prevent.
- **Interest** — this one comes from the user, not the repo. The answer to "what do you want to
  learn on this feature" from Phase 2 is the evidence.

A box closing as ☐ does not block the brief. Two honest ☐ and one ☑ were the right answer for
`core-ledger`, and the two empty boxes became its first two tasks.

## What this file is not for

The module map informs **feasibility**. None of it goes into the brief body — §12 cites module
names as evidence, and that is the only place a path may appear. Everything else in the
document stays product language; Phase 13 enforces it.
