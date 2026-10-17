# Repo conventions — what is already decided, and what is still open

Used in Phases 0, 3, 4 (§2 Constraints and §5 Building block view) and 6. Read-only.

This file exists because a generic architecture skill asks the user to choose things this repo chose
long ago. Asking "layered or hexagonal?" in a repo whose dependency rule is enforced by a test suite
is not a question — it is noise that costs trust and burns the question budget needed for the two or
three decisions that are genuinely open.

The split below is the whole point:

- **Frozen** — stated in §2 (Constraints) or rendered in §5 (Building block view), never asked,
  never contradicted without an explicit override that lands in §1 (Introduction and goals) ¶4
  **and** a row in §11 (Risks and technical debt).
- **Open** — the real decision surface of a feature. Spend every question here.

## A. What is frozen, and where it lives

**This table carries no rules. It carries only the verdict "closed" and a pointer.** Phase 0 already
read `CLAUDE.md`, `ARCHITECTURE.md`, `SPEC.md`, `Directory.Build.props` and `docs/adr/` in full, so
the rules themselves are in context from the source. Restating them here would create a second copy
that goes stale the first time you edit `CLAUDE.md` — and a stale copy is worse than none, because
the skill would have no reason to doubt its own reference file.

| Topic | Where it is fixed |
|---|---|
| Language, SDK, compiler settings, warnings-as-errors | `Directory.Build.props`; `CLAUDE.md` § C# |
| Architecture style, the four projects, the direction of dependencies | `ARCHITECTURE.md`; `docs/adr/0001-initial-setup.md` |
| What enforces the dependency rule, and why two test classes are needed | `ARCHITECTURE.md` § What enforces this; `tests/FinNavis.ArchitectureTests` |
| What counts as a port, where each kind lives, who implements and who registers | `ARCHITECTURE.md` § Ports and adapters; `CLAUDE.md` § Where interfaces go |
| Folder layout inside each project | `ARCHITECTURE.md` § Folder layout — the tree; `CLAUDE.md` § Folders inside a project — the rules. Rendering it into §5 → section C below |
| HTTP surface: Minimal APIs, one folder per operation, contract co-location, OpenAPI | `CLAUDE.md` § API |
| Datastore and the technology that reaches it | `SPEC.md` § Technical decisions; `CLAUDE.md` § Database |
| Database conventions: naming, enum storage, entity configuration, migrations, index discipline | `CLAUDE.md` § Database |
| C# rules for money, time, async and cancellation, class design and file layout | `CLAUDE.md` § C# |
| Service registration | `CLAUDE.md` § Dependency injection |
| Test stack, assertion style, test naming, what needs a real database | `CLAUDE.md` § Tests |
| Product boundaries: one owner, no sign-in, one dataset, single currency | `SPEC.md` § Non-goals |
| Project-wide non-functional stance, and the safe-schema-change rule | `SPEC.md` § Non-functional decisions |
| What exists today versus what is only planned | `SPEC.md` § Planned, not built yet, § Current status — consequences in section F below |

**If this table and the file it points at ever disagree, the file wins.** Say so with ⚠️, carry on
from the file, and flag the stale row in the handoff. The table is a classification, not a source.

### Inherited from an earlier feature, not from a root file

Read `docs/features/*/sad.md` §2 (Constraints) and §9 (Architecture decisions) before drafting §2.
A pin or a decision another feature already made is an inherited constraint for this one — cite
where it came from and move on, rather than deciding it a second time.

As of `core-ledger`: **PostgreSQL is pinned to 18**. The root `SPEC.md` still says only
"PostgreSQL", so the origin to cite is `core-ledger/sad.md` §2, not a root file.

## B. The rings, for drawing them correctly

The structure, the permission table and the reasoning are in `ARCHITECTURE.md`. Read it there.

Two points are repeated here only because they are what a generic C4 diagram gets wrong, and a wrong
arrow in §5 (Building block view) is a factual error rather than a cosmetic one:

- **Presentation and Infrastructure are peers.** Both sit in the outer ring, side by side. Neither
  wraps the other, and Infrastructure does **not** sit between Presentation and Application.
- **Infrastructure points inward**, at Domain and Application, because it implements their ports.
  Presentation points at Infrastructure only to register adapters in DI, and the arrow label has to
  say so — otherwise the diagram asserts the opposite of `ARCHITECTURE.md`.

Diagram syntax and this repo's participant vocabulary →
[c4-and-sequences.md](c4-and-sequences.md).

## C. The §5 decomposition skeleton

**The tree lives in `ARCHITECTURE.md` § Folder layout.** Read it there and render it into §5
(Building block view) with this feature's own areas and operations substituted for the `<Area>`,
`<Resource>` and `<Operation>` placeholders.

Two things to carry across while rendering:

- **Folders marked `*` in that tree do not exist yet.** If this feature is the first to need one —
  a business area in Application, a `Queries/` adapter, the first endpoint folder — say so in §5
  instead of drawing it as though it were already on disk. The same honesty §2 (Constraints) owes
  about the missing harness, §5 owes about the missing folders.
- **Drop the branches this feature does not touch.** §5 is a view of the feature, not a copy of the
  repo. A tree with four empty rings in it tells the reader nothing.

Do not keep a second copy of the tree in this file, in the SAD template, or in another skill. One
source, rendered per feature.

### Three traps a generic skill falls into every time

`ARCHITECTURE.md` now states the three structural rules positively — areas at the root, contracts
with their operation, a port per aggregate. This list is the negative form: what a generic agent
writes **instead**, which is the thing that actually has to be prevented.

1. **`Application/Features/`.** Business areas sit at the **root** of the Application project.
   Everything in that layer is a feature, so the wrapper adds a level of nesting and no information.
   The empty `Features/` directory was scaffolding and is gone.
2. **A central `Presentation/Contracts/`.** Request and response records live **inside the
   operation's own folder**, next to the endpoint. There is no shared contracts folder, and two
   operations never share a response record even when the shapes are identical today.
3. **A second `ProjectReference` from Infrastructure to Domain.** Domain arrives transitively and
   that is deliberate. `ARCHITECTURE.md` has a whole section on why. Do not "fix" it.

Both of the first two were written by the generic skill into `core-ledger/sad.md` and had to be
undone by hand (`f8c558b`). That is the single most expensive mistake this file prevents.

### What §5 still has to decide

The skeleton is free. These are not:

- **Which aggregates the feature introduces**, and which of them get a repository port. One port per
  aggregate, added when a use case needs it — never because a table exists. `core-ledger` gave
  `CategoryGroup` no port of its own and said why, which is the standard to match.
- **Whether the read path is separate from the write path.** A repository returns entities; a read
  that aggregates in SQL needs its own port in `Application/Abstractions`. This crosses the gate
  almost always (see [blast-radius.md](blast-radius.md)).
- **Which outside-world ports the feature adds** — a file reader, a clock use beyond `IClock`, a
  notifier. Each one is a new adapter in Infrastructure and a new DI registration.
- **Whether anything belongs in `Application/Common`.** It is empty today. The first thing put there
  becomes a convention — see section H.

## D. The real decision surface of a feature

When drafting, assume the answer to everything in section A and spend the walk here:

| Question | Lands in |
|---|---|
| What are the aggregates, and what are the invariants that must live inside them? | §4, §5 |
| Which reads does the feature add, and does any of them aggregate? | §4, §5, §8 |
| Does the feature reshape data that is already recorded? | §4, §11, and the ADR gate |
| Is the write mutable in place, or append-only with corrections? | §4 → almost always an ADR |
| One table or two for things the glossary treats as different? | §4 → almost always an ADR |
| What is the identity strategy if the feature's rows can arrive from outside? | §8 |
| Which new read path needs an index, and what breaks if it is missing? | §8, §10, §11 |
| What does the feature add to the deployment unit — a container, a job, a volume? | §7 |
| What cannot be measured today because the harness does not exist? | §2, §7, §11 |

## E. How to scan the repo

The scan **confirms** the pointers in section A; it never replaces reading the files. Keep it
bounded — `bin/` and `obj/` are enormous and carry nothing.

```bash
git log --oneline -15
ls docs/features/                                     # what has been designed before
ls docs/adr/                                          # project-wide decisions — constraints, not options
find src tests -type d -not -path "*/bin/*" -not -path "*/obj/*" | sort
find src -name "*.cs" -not -path "*/bin/*" -not -path "*/obj/*" | sort
ls src/FinNavis.Infrastructure/Persistence/Migrations # has the data path ever run?
ls src/web                                            # is there a client at all?
```

Read §2 (Constraints) and §9 (Architecture decisions) of every earlier feature's `sad.md`. Their
ADRs are constraints on this one, and re-deciding something an Accepted ADR settled is a §11 row,
not a fresh choice.

## F. What is proven and what is not

`SPEC.md` lists what exists and what is planned. This table adds the part `SPEC.md` has no reason to
carry — what each gap **means for the document being written**. Getting this wrong produces a SAD
that quietly assumes a harness nobody has built.

| Thing | State | Consequence for the SAD |
|---|---|---|
| Four projects, references, architecture tests | **Works** — passing today | Counts as evidence; §5 may rely on it |
| Any EF Core migration | **Never run** | "The data path works" is unproven until the first one lands |
| Testcontainers integration suite | **Not built** | Every latency and correctness number in §10 needs it first — a prerequisite task, named in §7 and carried into §11 |
| CI pipeline | **Not built** | A target measured "in CI" is a wish until this exists |
| `docker-compose.yml` | **Not built** | The feature that needs it creates it, and says so in §7 |
| React PWA in `src/web` | **Not started** — empty folder | Any `web-frontend` surface is unproven ground. Say that in §4 instead of debating SSR vs SPA as if a client existed |
| Playwright suite in `tests/e2e` | **Not built** | No end-to-end verification is available for §10 |

Re-read `SPEC.md` § Planned, not built yet at Phase 0 — the moment one of these lands, the row above
is wrong, and the file is the source.

A §10 scenario whose **How verify** names a harness from this list must say, in the same breath,
that standing it up is a prerequisite task. `core-ledger` did exactly that and it was right.

## G. How §10 states verification

The test conventions are in `CLAUDE.md` § Tests. What that file does not say is how a *quality
scenario* uses them, which is this:

- **Name the test**, in the repo's own naming form:
  `Breakdown_20kSeededMovements_P95Under500ms`.
- **Say where it runs.** Domain and Application rules → unit tests. Anything whose correctness lives
  in SQL → integration test against real PostgreSQL. An in-memory double proves nothing about a
  query, and a scenario that leans on one is not verified.
- **Say what makes it fail.** A latency budget that depends on one index deserves an `EXPLAIN`
  assertion, so losing the index shows up as a failing test rather than as a slow afternoon.
- **Copy the number from PRD §6 verbatim.** No rounding, no "about".

## H. When a decision outgrows the feature

Some answers are not about this feature at all — they are a rule the whole repo will follow: an
error-handling convention in `Application/Common`, a paging convention, a naming rule for query
ports, a standard shape for refusal responses.

When one surfaces, the skill **stops and says so with ⚠️** before writing the section:

> ⚠️ This is a repo-wide convention, not a decision about `<slug>`. I can record it in the SAD for
> this feature now, and `CLAUDE.md` is where it belongs permanently. Add it there?

`CLAUDE.md` is edited **only after an explicit yes** — that rule comes from the project's own working
agreement, and a skill that quietly rewrites the conventions file is exactly what it exists to
prevent. If the answer is no, the convention stays feature-local and gets a §11 row so the next
feature inherits the question rather than the silence.

A convention that does land in `CLAUDE.md` needs no new row in section A — the table points at
`CLAUDE.md` § by section, and a new rule inside an existing section is covered already.
