# SPEC

FinNavis is a personal finance tracker. One person, self-hosted.
A REST API plus a React PWA for recording money movement.

## Goals

- Track **accounts** (cash, card, savings) and their current balance.
- Record **income**, **expenses**, and **transfers between accounts**, each assigned to a **category**.
- Work on a phone as an installable PWA, including a read-only view when offline.

## Non-goals

- No multi-user support and no authentication. One owner, one dataset.
- No bank or card integrations. Entries are added by hand or imported from a file.
- No budgets, forecasts, investments, or multi-currency. Single currency for now.

## Technical decisions

- **Clean Architecture** with four projects: Domain, Application, Infrastructure, Presentation. Dependencies point inward. See `ARCHITECTURE.md` and `docs/adr/0001-initial-setup.md`.
- **.NET 10 / C# 14** with ASP.NET Core Minimal APIs, **PostgreSQL** through **EF Core + Npgsql**, OpenAPI for the contract.
- **Testing**: xUnit + AwesomeAssertions for unit tests, Testcontainers for real PostgreSQL integration tests.
- **Architecture tests** guard the dependency rule, so it cannot rot quietly. See `tests/FinNavis.ArchitectureTests`.

## Acceptance criteria

- `dotnet build FinNavis.slnx` and `dotnet test FinNavis.slnx` both succeed from a clean clone, with zero warnings.
- The dependency rule holds: Domain has no references, and Application does not reference Infrastructure or any web or EF Core package. Checked by `tests/FinNavis.ArchitectureTests`, not by eye.
- Every feature ships with tests: domain rules covered by unit tests, and every endpoint covered by an integration test running against PostgreSQL in Testcontainers.

## Planned, not built yet

Decided, but no code exists. Do not assume any of this works.

- **Docker Compose** to run PostgreSQL and the app locally.
- **Playwright** for end-to-end tests, in `tests/e2e`.
- **React + TypeScript PWA** in `src/web`.
- **CI pipeline** running build and test on every push.

## Current status

Skeleton only. Projects, references, architecture tests, and docs exist. No entities, endpoints, or database schema yet.
