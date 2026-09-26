# Architecture

FinNavis uses **Clean Architecture**. The code is split into four rings. The inner rings hold
business rules. The outer rings hold technical details. Only the outer rings are allowed to
change when we swap a database, a web framework, or a UI.

## The rings

```
    ┌──────────────────────────────────────────────────────────┐
    │  Presentation (ASP.NET Core Web API)                     │
    │  HTTP, minimal API endpoints, DI wiring                  │
    │   ┌──────────────────────────────────────────────────┐   │
    │   │  Infrastructure (EF Core, PostgreSQL)            │   │
    │   │  adapters: implements every port                 │   │
    │   │   ┌──────────────────────────────────────────┐   │   │
    │   │   │  Application                             │   │   │
    │   │   │  use cases + outside-world ports         │   │   │
    │   │   │   ┌──────────────────────────────────┐   │   │   │
    │   │   │   │  Domain                          │   │   │   │
    │   │   │   │  entities, rules                 │   │   │   │
    │   │   │   │  + repository ports              │   │   │   │
    │   │   │   └──────────────────────────────────┘   │   │   │
    │   │   └──────────────────────────────────────────┘   │   │
    │   └──────────────────────────────────────────────────┘   │
    └──────────────────────────────────────────────────────────┘
```

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

Infrastructure references Application and Domain. Nothing references Infrastructure except the
composition root.

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
Presentation ──> Application ──> Domain
       │                            ▲
       └──> Infrastructure ─────────┘
```

The compiler enforces most of this through project references:

| Project        | May reference                |
| -------------- | ---------------------------- |
| Domain         | nothing                      |
| Application    | Domain                       |
| Infrastructure | Application, Domain          |
| Presentation   | Application, Infrastructure  |

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
