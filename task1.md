# Task 1 — Accounts: create, list, get by id

First feature of FinNavis. Delivers one vertical slice through all four layers, plus the
plumbing every later feature depends on.

## Goal

A user can create an account, see all accounts, and open one account by id.
The API runs against PostgreSQL and is covered by tests.

## Scope of work

### In scope

**Domain**
- `AccountType` enum: `Cash`, `Card`, `Savings`. Stored as string.
- `Account` entity. Properties: `Id`, `Name`, `Type`, `InitialBalance`, `CreatedAt`.
  Private setters. Created through a static `Create` factory.
- Account rules, owned by Domain:
  - Name is required, trimmed, max 100 characters.
  - Initial balance may be negative. A credit card is overdrawn by design.
  - Initial balance has at most 2 decimal places.
  - Initial balance magnitude is at most 999,999,999,999.99.
- `Account.CalculateBalance(decimal transactionsTotal)` — the single place the balance
  formula lives. Today the total is always zero, because transactions do not exist yet.
- `IAccountRepository` port in `Domain/Abstractions`.

**Application**
- `IUnitOfWork` port in `Application/Abstractions`.
- Small `Result<T>` type in `Application/Common`. Carries success, validation errors, or
  not-found. No exceptions across layers.
- Three use cases: `CreateAccount`, `ListAccounts`, `GetAccountById`.
- `AccountDto`.
- `AddApplication()` DI registration.

**Infrastructure**
- `FinNavisDbContext`.
- `AccountConfiguration` mapping to table `accounts` with snake_case columns.
- `EfAccountRepository` and `EfUnitOfWork`.
- `AddInfrastructure(configuration)` DI registration.
- First EF Core migration, `AddAccounts`.

**Presentation**
- `POST /accounts`
- `GET /accounts`
- `GET /accounts/{id:guid}`
- Request and response records in `Presentation/Contracts`.
- `Program.cs` wired as the composition root. ProblemDetails enabled.

**Supporting**
- `docker-compose.yml` with one PostgreSQL service for local development.
- `.editorconfig` exemption for generated migration files.
- `build.ps1` / `build.sh`: `migrate` no longer requires `launchSettings.json`;
  `db-update` reads the connection string from the environment.
- Docs updated: `SPEC.md`, `ARCHITECTURE.md`, `CLAUDE.md`, new `docs/adr/0002-accounts-slice.md`.

### Out of scope

- Update or delete an account. Delete has no agreed meaning before transactions exist.
- Categories, transactions, transfers.
- Unique account names. Not an agreed business rule.
- Multi-currency. A SPEC non-goal.
- Authentication. A SPEC non-goal.
- React PWA, Playwright tests, CI pipeline.
- `IClock`. No domain rule depends on the current time yet.
- Value objects for money or account name. Revisit when multi-currency arrives.

## Acceptance criteria

### Build and tests
- [ ] `./build.ps1 build` succeeds from a clean clone with zero warnings.
- [ ] `./build.ps1 test` succeeds. Docker must be running.
- [ ] Architecture tests pass unchanged. No edit to `AllowedProjectReferences`.
- [ ] `FinNavis.Domain` still has zero project references and zero packages.
- [ ] `SmokeTests.cs` deleted from Domain.UnitTests, Application.UnitTests and
      Infrastructure.IntegrationTests.

### API behaviour
- [ ] `POST /accounts` with a valid body returns `201`, a `Location` header pointing at the
      new account, and the created account in the body.
- [ ] The response `balance` equals `initialBalance`, because no transactions exist yet.
- [ ] The returned id is a version 7 GUID.
- [ ] `"type"` is accepted case-insensitively. `"cash"` works.
- [ ] A negative `initialBalance` is accepted and returns `201`.
- [ ] A blank name returns `400` with an error under `errors.name`.
- [ ] A name longer than 100 characters returns `400` with an error under `errors.name`.
- [ ] An unknown type returns `400` with an error under `errors.type`.
- [ ] `"type": "7"` returns `400`, not `201`. A numeric string must not become a valid type.
- [ ] An `initialBalance` with 3 decimal places returns `400` under `errors.initialBalance`.
- [ ] Several invalid fields produce one entry per field in one response.
- [ ] Error responses use content type `application/problem+json`.
- [ ] `GET /accounts` returns `200` and an empty array when no accounts exist.
- [ ] `GET /accounts` returns all accounts ordered by name.
- [ ] `GET /accounts/{id}` returns `200` and the account for a known id.
- [ ] `GET /accounts/{id}` returns `404` for an unknown id.
- [ ] `GET /accounts/not-a-guid` returns `404`.

### Database
- [ ] Table is `accounts`. All columns are snake_case.
- [ ] `id` is `uuid`. `name` is `varchar(100)`. `type` is `varchar(20)`.
      `initial_balance` is `numeric(14,2)`. `created_at` is `timestamp with time zone`.
- [ ] `type` is stored as text. `SELECT type FROM accounts` reads `Cash`, never `1`.
- [ ] The only index is the primary key.
- [ ] `./build.ps1 migrate AddAccounts` works on a clean clone, with no database running
      and no `launchSettings.json`.

### Test coverage
- [ ] Every account rule has a Domain unit test.
- [ ] Every use case has Application unit tests, including the failure paths.
- [ ] Each failure path asserts nothing was saved and nothing was committed.
- [ ] Every endpoint has an integration test against PostgreSQL in Testcontainers.
- [ ] Integration tests run the real migration, not `EnsureCreated`.
- [ ] No mocking library is added.

### Documentation
- [ ] `SPEC.md` "Current status" no longer says "skeleton only".
- [ ] `ARCHITECTURE.md` sentence about Infrastructure carrying no reference to Application
      is corrected. `EfUnitOfWork` makes that reference real.
- [ ] `CLAUDE.md` says `dotnet ef` ignores `launchSettings.json`, and says which package
      kinds Application may take.
- [ ] `docs/adr/0002-accounts-slice.md` records the contested decisions.

## Definition of done

`./build.ps1 clean && ./build.ps1 build && ./build.ps1 test` is green with zero warnings,
and the manual walkthrough in the plan passes against a real PostgreSQL started by
`docker compose up -d`.
