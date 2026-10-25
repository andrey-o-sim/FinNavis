# Definition of Done, estimates, coverage

## What makes a DoD testable here

A DoD line passes if someone other than its author can run it and get a yes or a no. In this repo
that means it names one of:

- a command — `./build.sh build`, `./build.sh test`, `./build.sh migrate <Name>`,
  `dotnet ef migrations script`, `dotnet ef database update 0`;
- a test that exists after the task — named, in the test project the layer maps to;
- a PRD §5 criterion by id, with the outcome the PRD words say;
- a figure from SAD §10, copied, not rounded.

"Works", "is clean", "is covered", "is reviewed" are not DoD lines.

### The lint line that does not exist

`Directory.Build.props` sets `TreatWarningsAsErrors` **and** `EnforceCodeStyleInBuild`, so style
violations are compile errors too. There is no lint task, no vet task, no formatter task, and
`build.sh` has no `check`. **`./build.sh build` is the lint gate.** A DoD that names
`dotnet format`, `lint` or `vet` names a command the repo does not have.

`.claude/sdlc.local.md` sets `gate_lint: true` and `gate_vet: true`. Those are flags for the
implement stage, which skips a gate gracefully when no command is detected. They are not a reason
to invent one here.

## Patterns per layer

Each task ends with `./build.sh test` is green. The lines above it are what makes the task itself
provable.

### `domain`

- [ ] `<Invariant>` is refused — unit test in `tests/FinNavis.Domain.UnitTests` (AC-NN)
- [ ] the entity exposes no `<field the design deliberately omits>`
- [ ] `src/FinNavis.Domain` still references no project and no package

The third line matters more than it looks. Domain references nothing, and the architecture test
only catches a project reference — a `PackageReference` compiles fine and still breaks the rule.

### `app`

- [ ] AC-NN … AC-NN are covered by unit tests in `tests/FinNavis.Application.UnitTests`
- [ ] the use case fetches through its port; `src/FinNavis.Application` references no EF Core
- [ ] `<the multi-step change>` happens in one `IUnitOfWork` commit

### `infra`

- [ ] the port round-trips against PostgreSQL in Testcontainers
- [ ] `<the query>` returns `<the PRD outcome>` (AC-NN)
- [ ] the index the query needs exists and is used — assert on the plan, and let the test fail when
      the index is dropped
- [ ] a failed commit rolls back every write in the unit of work

The index line is the one people skip. `CLAUDE.md` says to check for a needed index after adding a
query; a DoD that only checks the result passes on a sequential scan.

### `migration`

- [ ] `./build.sh migrate <Name>` generates one migration for the feature's tables
- [ ] `dotnet ef migrations script` output matches the staged pairs under
      `docs/features/<slug>/migrations/` on every table, column, type, bound, index and FK; a
      difference is resolved in the configuration or in the spec before this task closes
- [ ] it applies against PostgreSQL in Testcontainers, and `dotnet ef database update 0` reverts it
- [ ] the EF timestamp and the staged timestamp differ — **that is expected**

The last line is there because it looks like a bug and is not. `.claude/rules/migrations.md` says
the staged spec gets its timestamp at design time and EF gets its own at generate time.

### `ports`

- [ ] every operation's request and response shape matches `contracts/openapi.yaml`
- [ ] each operation has its own records under
      `Presentation/Endpoints/<Resource>/<Operation>/`, shared with no other operation
- [ ] `<refusal>` returns the flat envelope with `code: <Module.ErrorName>` and the PRD's wording
      (AC-NN)
- [ ] money crosses the wire as a string
- [ ] no response is `application/problem+json`

### `wiring`

- [ ] every port resolves from the container — one test proves it, rather than a reading of
      `Program.cs`
- [ ] `tests/FinNavis.ArchitectureTests` is green
- [ ] registration happens through the layer's own `DependencyInjection` entry point, not type by
      type in `Program.cs`

### `tests` (quality gate)

- [ ] `<the SAD §10 figure, verbatim>` — for example: 1 000 randomised sequences, **0**
      discrepancies; p95 ≤ 300 ms
- [ ] the assertion fails when `<the thing it guards>` is removed

The second line is what separates a gate from a test that always passes. A latency assertion with
no index to drop proves nothing.

### `docs`

- [ ] the ADR is written and both contracts agree with it
- [ ] `/openapi/v1.json` and `contracts/openapi.yaml` agree on every operation and every error code
- [ ] the deferral it closes is struck from `api-sync-report.md`

## Estimates

S = 2 hours · M = half a day · L = a full day · more than L splits.

A task over ~500 lines of diff is a signal, not a verdict — a migration touching five tables is
large and still one task, because it cannot be half-applied. Say so in Notes when a task is
deliberately over the line.

Put the person-day total in `tracker.md`. It is the number that tells the user whether `.size` is
still right: `core-ledger` came out at 34 tasks and ~17 person-days, which is an L dressed as an M.
That mismatch belongs in the parked items, not in a silent shrug.

## Coverage rules

- Every PRD §5 criterion appears in at least one task's `acs`. Unclaimed is a finding that stops
  the run.
- A criterion may be claimed by several tasks when it is proved at several levels. Write the reason
  in the coverage line under the task table.
- A task with an empty `acs` states why in its Why section. Migrations, wiring and harness tasks
  satisfy no criterion by themselves, and that is normal.
- `acs` ids are copied from the PRD, never renumbered. If an id looks wrong, the PRD is wrong — say
  so rather than fixing it here.
