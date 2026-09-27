# FinNavis

Personal finance REST API and PWA. Tracks accounts, categories, income, expenses, and transfers.
Single-user, self-hosted. No authentication yet.

Read `ARCHITECTURE.md` before changing project structure.
Read `SPEC.md` for scope. Read `docs/adr/` for past decisions.

## Layout

```
FinNavis.slnx                        Solution file (.slnx format)
Directory.Build.props                Shared MSBuild settings for all projects
build.sh / build.ps1                 Task runner: build, test
src/FinNavis.Domain/                 Entities, value objects, domain rules, repository ports
src/FinNavis.Application/            Use cases, outside-world ports, DTOs
src/FinNavis.Infrastructure/         EF Core, PostgreSQL, adapters
src/FinNavis.Presentation/           ASP.NET Core Web API, composition root
src/web/                             React + TypeScript PWA (not started)
tests/FinNavis.Domain.UnitTests/     Domain unit tests
tests/FinNavis.Application.UnitTests/  Application unit tests
tests/FinNavis.ArchitectureTests/    Guards the dependency rule. Fails the build if a layer is broken
tests/FinNavis.Infrastructure.IntegrationTests/  Testcontainers + PostgreSQL
tests/e2e/                           Playwright tests (not started)
docs/adr/                            Architecture decision records
```

## Dependency rules

Dependencies point inward only. Never the other way.

```
     ┌───────────────────────────────────┐
     │                                   ▼
Presentation ──> Infrastructure ──> Application ──> Domain
                       │                                ▲
                       └────────────────────────────────┘
```

Left to right is outer ring to inner ring, so every arrow pointing right means every dependency points inward.

- **Domain** references nothing. No NuGet packages, no other projects. Keep it that way. It owns the repository ports.
- **Application** references Domain only. It owns the outside-world ports. It does not know about EF Core, HTTP, or PostgreSQL.
- **Infrastructure** uses Application and Domain. It is the adapter layer: it implements every port, wherever it was declared. Its csproj lists Application only — Domain comes transitively, and that is deliberate. Do not add a second `ProjectReference`. See "Why Infrastructure has no reference to Domain" in `ARCHITECTURE.md`.
- **Presentation** references Application and Infrastructure. It is the only composition root. It wires DI and maps HTTP.
- Application must never reference Infrastructure. If something from the outside is needed, add an interface in the right inner layer (see below) and implement it in Infrastructure.

Before adding a `ProjectReference` or `PackageReference`, check the arrows above.

`tests/FinNavis.ArchitectureTests` enforces this. The compiler only catches wrong *project*
references, because they create a cycle. Adding an EF Core `PackageReference` to Domain or
Application compiles fine and still breaks the rule — that is what the tests are for. If you
change the allowed references, update `ProjectReferenceTests.AllowedProjectReferences` and the
table in `ARCHITECTURE.md` together.

## Conventions

### Ports vs plain interfaces

Not every interface is a port. We use the Hexagonal Architecture meaning:

> **An interface is a port when the other side of it is outside the application.**

`IAccountRepository` is a port — the other side is the data store. A domain service interface is
not a port — both sides are Domain, it is just polymorphism. Infrastructure supplies the
**adapter**: the real implementation on the other side.

Both inner layers own ports. Full explanation in `ARCHITECTURE.md`.

### Where interfaces go

Pick by asking **who needs the abstraction**, not by what implements it.

**`Domain/Abstractions`** — abstractions that are part of the domain model itself:

- **Repository ports**: `IAccountRepository`, `ITransactionRepository`. A repository is a
  collection-like view of an aggregate, so it belongs to the ubiquitous language.
- Domain services: a rule that spans several entities and has no natural home on one of them.
  These are usually not ports.
- Anything a domain rule needs to be expressible, for example `IClock`.

**`Application/Abstractions`** — ports for what a use case needs from the outside world:

- `IUnitOfWork` for committing a set of changes.
- Integrations such as `IStatementImporter` or `IFileStorage`.

Rules:

- Infrastructure implements both. Presentation registers both in DI.
- Domain declares repository ports but never calls them. Entities do not fetch data. Use cases
  in Application do the fetching.
- Do not add a repository just because a table exists. Add one per aggregate, when a use case
  needs it.
- Keep `IUnitOfWork` in Application, not Domain. Transactions are an orchestration concern, not
  a domain rule.
- We have no driving (inbound) ports. Presentation calls use-case classes directly. Do not add
  an interface per use case.

### C#

- .NET 10, C# 14, nullable enabled, implicit usings enabled.
- Warnings are errors (`TreatWarningsAsErrors`). Fix the warning, do not suppress it.
- One public type per file. File name matches the type name.
- `sealed` by default on classes that are not designed for inheritance.
- Use `async`/`await` end to end. Pass `CancellationToken` through every async call.
- Money is `decimal`, never `double` or `float`.
- Use `DateTimeOffset` for timestamps. Store UTC.

### Database

- PostgreSQL via EF Core and Npgsql.
- Table and column names are `snake_case`.
- **Enums are stored as strings, never as int.** Use `.HasConversion<string>()` in the entity configuration.
- Entity configurations go in `Infrastructure/Persistence/Configurations`, one file per entity, using `IEntityTypeConfiguration<T>`.
- After adding a new query, check whether an index is needed. If yes, add it in the same change and write a one-line comment explaining why.
- Migrations live in `Infrastructure/Persistence/Migrations`. Never edit an applied migration. Add a new one.

### Dependency injection

- Every new service must be registered. Infrastructure registers its own services in `Infrastructure/DependencyInjection`. Application does the same for its own.
- Presentation calls those registration methods. It does not register Infrastructure types one by one.
- After adding a class with dependencies, check it is in the container.

### API

- Minimal APIs. Endpoint groups live in `Presentation/Endpoints`, one file per resource.
- Request and response records live in `Presentation/Contracts`. Never return Domain entities from an endpoint.
- OpenAPI document is generated at `/openapi/v1.json` in Development.

### Tests

- xUnit plus AwesomeAssertions (`using AwesomeAssertions;` is a global using in test projects).
- Assert with `.Should()`, not `Assert.*`.
- Test names: `Method_Scenario_ExpectedResult`.
- Unit tests do not touch the database or the network.
- Integration tests use Testcontainers for PostgreSQL. They need Docker running.
- `SmokeTests.cs` in each test project is a placeholder. Delete it once the project has real tests.

## Commands

Use the task runner. It is the same set of tasks on both platforms:

```bash
./build.sh build            # build the whole solution
./build.sh test             # run all .NET tests, architecture tests included
./build.sh run              # start the API
./build.sh migrate <Name>   # add an EF Core migration
./build.sh db-update        # apply pending migrations
./build.sh clean            # delete build output
./build.sh help             # list the tasks
```

On Windows PowerShell use `./build.ps1` with the same task names, for example
`./build.ps1 migrate AddAccounts`.

Before the first `run`, `migrate`, or `db-update`, create the local settings file:

```bash
cp src/FinNavis.Presentation/Properties/launchSettings.example.json \
   src/FinNavis.Presentation/Properties/launchSettings.json
# then fill in the values
```

The runner stops with a clear message if that file is missing.

The plain commands still work if you prefer them:

```bash
dotnet build FinNavis.slnx
dotnet test FinNavis.slnx
dotnet run --project src/FinNavis.Presentation

dotnet ef migrations add <Name> \
  --project src/FinNavis.Infrastructure \
  --startup-project src/FinNavis.Presentation \
  --output-dir Persistence/Migrations

dotnet ef database update \
  --project src/FinNavis.Infrastructure \
  --startup-project src/FinNavis.Presentation
```

## Safety

- `launchSettings.json`, `.env`, `*.pem`, `*.key` are git-ignored. Never commit them.
- Never put a real connection string, password, or key into a committed file or a shell command.
- `launchSettings.example.json` holds variable names and value shapes only. Keep it empty of real values.

## Not set up yet

These are planned but do not exist. Do not assume they work.

- `docker-compose.yml` for PostgreSQL and the app
- React PWA in `src/web`
- Playwright suite in `tests/e2e`
- CI pipeline
