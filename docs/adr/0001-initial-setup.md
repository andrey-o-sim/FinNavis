# ADR 0001: Clean Architecture for the initial setup

- **Status**: Accepted
- **Date**: 2026-09-26
- **Deciders**: Andrey Simonenko

## Context

FinNavis is a personal finance tracker: accounts, categories, income, expenses, and transfers
between accounts. It is a REST API in .NET 10 with a React PWA client and PostgreSQL behind
EF Core.

The app looks like CRUD from the outside, but it is not only CRUD:

- A transfer is two linked movements. It must keep both sides consistent.
- Account balance is derived from transactions. The rule for deriving it must live in one place.
- Amounts, currencies, and categories have validity rules that apply no matter which endpoint
  is used.

We also expect the project to grow past the first version: import from files, budgets, maybe a
second client. So we needed to pick a structure before writing any feature code.

## Decision

Use **Clean Architecture** with four projects:

| Project                   | Holds                                             |
| ------------------------- | ------------------------------------------------- |
| `FinNavis.Domain`         | Entities, value objects, business rules, repository ports |
| `FinNavis.Application`    | Use cases and outside-world ports                 |
| `FinNavis.Infrastructure` | EF Core, PostgreSQL, adapters for every port      |
| `FinNavis.Presentation`   | ASP.NET Core Web API, composition root            |

Source dependencies point inward only. Domain references nothing. Application references Domain.
Infrastructure references Application. Presentation references Application and Infrastructure.

**Repository ports live in Domain, not Application.** A repository is a collection-like view
of an aggregate, so it belongs to the ubiquitous language. Application keeps the remaining ports
— the ones describing what a use case needs from the outside world, such as `IUnitOfWork` and
import or storage integrations.

We use "port" in its Hexagonal Architecture meaning: an interface whose other side is outside
the application. That makes ports a subset of interfaces, and it means ports are spread across
both inner layers rather than marking one folder.

The alternative was to put the repository ports in `Application/Abstractions`, which is what most
.NET Clean Architecture templates do. We chose the DDD placement because the aggregates here
(account, transaction, transfer) carry the rules we care about, and we want the way they are
loaded and saved described in the same layer as the rules themselves. The cost is that Domain
declares an abstraction it never calls itself — only Application does.

Details are in `ARCHITECTURE.md`. The day-to-day rule for choosing a folder is in `CLAUDE.md`.

## Alternatives considered

### 1. Single project, layered folders

One ASP.NET Core project with `Models`, `Services`, `Data` folders.

- **For**: fastest to start, least ceremony, fine for a small app.
- **Against**: nothing stops a domain rule from calling `DbContext` directly. The compiler
  cannot enforce the boundary, only discipline can, and discipline fades. Unit tests end up
  needing a database.
- **Rejected because**: the boundary we care about most is exactly the one this option cannot
  enforce.

### 2. Classic three-layer (API / BLL / DAL)

Three projects where the business layer depends on the data layer.

- **For**: familiar, common in .NET, fewer projects than Clean Architecture.
- **Against**: the dependency arrow points the wrong way. The business layer takes a hard
  dependency on EF Core, so testing business rules means mocking `DbContext` or running a real
  database. Replacing the data layer means touching the business layer.
- **Rejected because**: it fails the one property we want most — a core that can be tested and
  changed without the database.

### 3. Vertical slice architecture

Group code by feature, each slice handling its own request end to end.

- **For**: very low coupling between features, easy to delete a feature, less indirection.
- **Against**: shared invariants that span features (balance, transfer consistency) have no
  obvious home. They get duplicated across slices or drift apart.
- **Rejected because**: our rules are cross-cutting by nature. We may still use feature folders
  *inside* the Application layer, which gives some of this benefit without giving up the core.

### 4. Modular monolith / microservices

Separate deployable services per bounded context.

- **Rejected because**: this is a single-user, self-hosted app. Network boundaries and eventual
  consistency would add large cost and buy nothing.

## Consequences

### Good

- Domain and Application tests run with no database and no web server, so they stay fast.
- The compiler enforces the dependency rule through project references. A wrong reference does
  not build.
- New code has an obvious home, which keeps the layout predictable as the project grows.
- The edges are replaceable: swapping the data store or adding a second client touches only the
  outer rings.

### Bad

- More projects, more files, and more indirection than a CRUD app needs on day one. Simple
  features cost more keystrokes.
- Mapping between Domain entities, Application DTOs, and API contracts is extra work that a
  single-project app avoids.
- The team must keep the dependency rule in mind. `CLAUDE.md` and `ARCHITECTURE.md` state it so
  it is not tribal knowledge.

### Accepted trade-offs

- Presentation references Infrastructure. This is deliberate and limited to the composition
  root, so DI can register concrete types. No other file in Presentation may use an
  Infrastructure type.
- Repository interfaces are declared in Domain, but we still add them one per aggregate and only
  when a use case needs one — not one per table, and not by reflex.
- `IUnitOfWork` stays in Application, not Domain. Committing a set of changes is an
  orchestration concern, not a domain rule. Infrastructure implements it over EF Core's
  `DbContext`, which is already a Unit of Work.

## Related decisions made at the same time

These were settled together with the structure. They are recorded here rather than in separate
ADRs because they were not contested.

- **Single user, no authentication.** Self-hosted for one person. Auth is a non-goal for now and
  would be its own ADR later.
- **`.slnx` solution format.** Supported by .NET 10 and much easier to read in a diff than the
  old `.sln` format.
- **`Directory.Build.props` for shared MSBuild settings.** Target framework, language version,
  and nullable settings live in one file instead of being repeated per project.
- **Warnings are errors.** Caught a vulnerable transitive package on the very first build, which
  is the point.
- **Testcontainers over an in-memory provider** for integration tests. The in-memory provider
  does not behave like PostgreSQL, so tests passing against it prove little.
