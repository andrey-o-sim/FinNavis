# Architecture

FinNavis uses **Clean Architecture**. The code is split into four rings. The inner rings hold
business rules. The outer rings hold technical details. Only the outer rings are allowed to
change when we swap a database, a web framework, or a UI.

## The rings

```
   ┌─────────────────────────┬─────────────────────────┐
   │      Presentation       │     Infrastructure      │   outer ring
   │  HTTP, minimal APIs,    │  EF Core, PostgreSQL,   │
   │  composition root (DI)  │  adapters for all ports │
   ├─────────────────────────┴─────────────────────────┤
   │                   Application                     │   inner rings
   │        use cases + outside-world ports            │
   │   ┌───────────────────────────────────────────┐   │
   │   │                 Domain                    │   │
   │   │     entities, rules, repository ports     │   │
   │   └───────────────────────────────────────────┘   │
   └───────────────────────────────────────────────────┘
```

**Presentation and Infrastructure are peers.** They both live in the outer ring, side by side.
Neither one wraps the other, and neither one sits between Application and the outside. They are
two different edges of the same application: one faces the user, the other faces the database.

Ports sit in both inner rings. Adapters sit in the outer ring. See
[Ports and adapters](#ports-and-adapters) for what counts as a port.

### Domain — `src/FinNavis.Domain`

The core. Entities, value objects, enums, and the rules that are always true regardless of how
the app is delivered. For example: a transfer must have two different accounts, and an amount
must be positive.

Domain also owns the **repository ports** — `IAccountRepository`, `ITransactionRepository`.
A repository is a collection-like view of an aggregate, so it is part of the domain language
rather than a technical detail. Domain declares them; it never calls them.

Domain references **nothing**. No NuGet packages, no other projects. If Domain needs a date or a
random number, it takes it as a parameter or behind an interface it defines itself.

### Application — `src/FinNavis.Application`

The use cases. One class per thing a user can do: create an account, record an expense, move
money between accounts. Each use case orchestrates domain objects and calls **ports**.

The ports Application owns live in `Application/Abstractions`, for example `IUnitOfWork` or
`IStatementImporter`. Application says *what* it needs. It never says *how* it is done.

Application does not own every port it uses. Repository ports belong to Domain, and Application
consumes them. So a use case typically takes one or more Domain repositories plus an Application
`IUnitOfWork`.

Application references Domain only. It contains no EF Core, no HTTP, no SQL.

### Infrastructure — `src/FinNavis.Infrastructure`

The adapters. This is where the ports get real implementations: EF Core `DbContext`, entity
configurations, migrations, repositories, and anything else that talks to the outside world.

Infrastructure uses both inner rings: it implements ports from Domain and from Application. In
the project file it references Application only, and Domain arrives transitively. That is on
purpose — see [Why Infrastructure has no reference to Domain](#why-infrastructure-has-no-reference-to-domain).
Nothing references Infrastructure except the composition root.

### Presentation — `src/FinNavis.Presentation`

The delivery layer and the **composition root**. It maps HTTP requests to use cases, translates
results into responses, serves the OpenAPI document, and is the single place where the DI
container is built.

Presentation references Application (to call use cases) and Infrastructure (to register its
services in DI). That reference to Infrastructure is the one intentional exception to "outer
rings stay out of the way" — a composition root has to see the concrete types to register them.
No other file in Presentation may use an Infrastructure type.

## Ports and adapters

"Port" and "adapter" come from Hexagonal Architecture, also called Ports and Adapters. We use
the words in their standard meaning.

- A **port** is a socket on the edge of the application. It declares what can plug in.
- An **adapter** is the plug: the real implementation on the other side.

In C# a port is always an interface. The reverse is not true:

> **An interface is a port when the other side of it is outside the application.**

Judge by that role, not by which project the file sits in. Ports in FinNavis live in two places.

| Interface | Port? | Lives in | Why |
| --------- | ----- | -------- | --- |
| `IAccountRepository` | yes | Domain | Other side is the data store |
| `IClock` | yes | Domain | Other side is the machine clock |
| `IUnitOfWork` | yes | Application | Other side is a database transaction |
| `IStatementImporter` | yes | Application | Other side is a file or bank format |
| a domain service interface | no | Domain | Both sides are Domain. Plain polymorphism |

An interface written only to allow two implementations inside the same layer is an interface,
not a port.

**Direction.** Hexagonal Architecture splits ports in two. *Driven* (outbound) ports are the
ones the application calls out through — all of ours. *Driving* (inbound) ports are the ones the
outside calls in through. We do not use driving ports: Presentation calls use-case classes
directly instead of going through an interface.

**Who implements them.** Infrastructure implements every port, wherever it was declared.
Presentation registers each port against its adapter in DI at startup.

## The dependency rule

**Source code dependencies point inward only. An inner ring never knows about an outer ring.**

```
     ┌───────────────────────────────────┐
     │                                   ▼
Presentation ──> Infrastructure ──> Application ──> Domain
                       │                                ▲
                       └────────────────────────────────┘
```

Read it left to right: outer ring first, innermost ring last. Every arrow points right, which is
the same as saying every dependency points inward. There are five of them — the chain, plus
`Presentation -> Application` over the top and `Infrastructure -> Domain` underneath.

| Project        | May reference                |
| -------------- | ---------------------------- |
| Domain         | nothing                      |
| Application    | Domain                       |
| Infrastructure | Application, Domain          |
| Presentation   | Application, Infrastructure  |

The table says what a project **may** reference. It is a permission list, not a description of
what is in the `.csproj` files today.

### Why Infrastructure has no reference to Domain

Look at `FinNavis.Infrastructure.csproj` and you find one `ProjectReference`, to Application.
The diagram above still draws an arrow from Infrastructure to Domain. Both are correct. This
trips people up, so here is the full story.

**`ProjectReference` is transitive.** Infrastructure references Application, Application
references Domain, so Domain types are already visible inside Infrastructure. Writing
`EfAccountRepository : IAccountRepository` compiles with no change to the project file.

We checked this rather than assumed it. Adding `IAccountRepository` to Domain and its EF Core
implementation to Infrastructure builds clean, and the compiled `FinNavis.Infrastructure.dll`
then carries an assembly reference to `FinNavis.Domain`.

**So the dependency is real, it is just not declared.** We keep it that way on purpose. A second
`ProjectReference` would add a line that changes nothing about what compiles. Do not "fix" the
csproj by adding one.

There is a second surprise in the same place. The compiler writes an assembly reference only for
assemblies whose types are actually used. So while Infrastructure implemented no Application
port, `FinNavis.Infrastructure.dll` carried **no** reference to `FinNavis.Application`, even
though the csproj declared one. `EfUnitOfWork : IUnitOfWork` arrived with the accounts feature,
and the reference is now real in the built assembly too.

Two graphs, and they disagree in both directions:

| Edge | Declared in csproj | Present in the built assembly |
| ---- | ------------------ | ----------------------------- |
| Infrastructure -> Application | yes | only once an Application port is implemented |
| Infrastructure -> Domain      | no  | as soon as a Domain port is implemented       |

Neither graph tells the whole truth alone. That is why the next section has two test classes and
not one.

### What enforces this

Two things, and it takes both.

**The compiler** catches a wrong *project* reference, because most of them would create a
reference cycle. `Application -> Infrastructure` does not build.

**`tests/FinNavis.ArchitectureTests`** catches the rest. The compiler is happy to let you add
`Microsoft.EntityFrameworkCore` to Domain or Application — no cycle, no error, and the rule is
broken anyway. The tests close that hole, and they run as part of `dotnet test`:

- `ProjectReferenceTests` reads the `.csproj` files and checks the declared references against
  the table above. It fails the moment a wrong reference is added, before any code uses it.
- `LayerDependencyTests` reads the compiled assemblies and checks what the code actually touches.
  It catches a type reached through a transitive reference.

The table above is duplicated in `ProjectReferenceTests.AllowedProjectReferences`. If you change
one, change the other.

## How control flows the other way

At runtime the request goes outside-in, but the *dependency* still points inward. That works
through **dependency inversion**:

1. Domain defines the interface `IAccountRepository`.
2. Infrastructure writes `EfAccountRepository : IAccountRepository`.
3. Presentation registers `IAccountRepository -> EfAccountRepository` in DI at startup.
4. The Application use case receives `IAccountRepository` in its constructor and never learns
   which implementation it got.

Both inner layers depend only on abstractions they own. Infrastructure depends on them. The
arrow points inward even though the call goes outward.

The same pattern applies to Application's own ports, such as `IUnitOfWork`: Application defines
it, Infrastructure implements it over `DbContext`, Presentation registers it.

## What this buys us

- **Testable core.** Domain and Application tests run with no database and no web server. They
  are fast enough to run on every save.
- **Replaceable edges.** Swapping PostgreSQL for another store, or adding a gRPC or CLI front
  end, touches only the outer rings.
- **Clear place for new code.** A business rule goes in Domain. An orchestration step goes in
  Application. A library detail goes in Infrastructure. An HTTP concern goes in Presentation.

## The cost

This adds layers and indirection that a small CRUD app does not need on day one. We accept that
cost because the domain here has real rules — balances, double-entry transfers, category
totals — and those rules should not end up scattered inside controllers and EF Core queries.
See `docs/adr/0001-initial-setup.md` for the alternatives considered.
