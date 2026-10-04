# ADR 0002: Precedents set by the accounts slice

- **Status**: Accepted
- **Date**: 2026-10-04
- **Deciders**: Andrey Simonenko

## Context

Accounts (`docs/tasks/add-accounts.md`) is the first feature built on top of the structure
`docs/adr/0001-initial-setup.md` chose. That ADR covers the four-project layering, not the
day-to-day shape of a feature. Three choices made while building accounts are not obvious from
the layering alone, and every later feature (categories, transactions, transfers) will copy them.
This ADR records those three choices so they do not have to be re-derived, or re-argued, each
time.

## Decisions

### 1. Case-insensitive uniqueness via a persisted `normalized_name` column

An account name must be unique ignoring letter case and surrounding spaces. EF Core's fluent API
cannot express a function-based index (`lower(name)`), so that approach would need hand-written
`migrationBuilder.Sql`, invisible to the model snapshot and to every future `migrations add`.

Instead, `Account` stores a second column, `NormalizedName` (`name.Trim().ToLowerInvariant()`,
computed once in the `Create` factory), with a unique index on it. The duplicate check, the
unique index, and the list ordering all use the same column. Any later aggregate that needs
case-insensitive uniqueness should do the same: a real, persisted, indexed column — not a
database-side expression.

### 2. The error pipeline: Domain exception -> Application `Result` -> ProblemDetails

A rule lives in exactly one place: the Domain entity's factory method. `Account.Create` collects
every broken rule and throws one `DomainValidationException` carrying every field error at once.
The use case catches that single exception type and turns it into `Result<T>.Failure(Error)`.
The endpoint turns a `Validation` error into a 400 `ValidationProblem` and a `Conflict` error into
a 409 `ProblemDetails`.

Application's `Error` and `Result<T>` carry only strings and Application types — never a Domain
type. That is also why `AccountDto.Type` is a `string`, not the Domain `AccountType` enum:
`ARCHITECTURE.md` permits Presentation to reference Application and Infrastructure only, and
keeping Domain types out of Application's public surface keeps the compiled
`FinNavis.Presentation.dll` free of a `FinNavis.Domain` assembly reference, so the documented
dependency graph stays literally true, not just true in spirit.

The race path (two requests at once) gets the same 409 through a second route: the database's
own unique index rejects the duplicate, `EfUnitOfWork` translates the PostgreSQL error
(`SqlState == "23505"`) into `UniqueConstraintViolationException`, and the use case catches that
too. Application never learns the store is PostgreSQL; Presentation never learns the Domain enum
exists.

### 3. Compose for PostgreSQL only, with committed local-only credentials

`docker-compose.yml` runs a single `postgres:17-alpine` service: a named volume so data survives
`docker compose down`, a healthcheck, and the port published to `127.0.0.1` only. It does not run
the API — the app still runs from the IDE or `./build.sh run`. Automatic migration-on-startup was
also rejected; the schema is applied by hand with `./build.sh db-update`.

The user and password in `docker-compose.yml` are plain text, which is normally forbidden by
`CLAUDE.md`. This is a deliberate, named exception: the container is a throwaway bound to
`127.0.0.1` and holds no real data. The exact wording lives in the Safety section of `CLAUDE.md`
so a later reader does not mistake a committed password for a mistake.

## Related

- `docs/adr/0001-initial-setup.md` — the layering these precedents build on.
- `docs/tasks/add-accounts.md` — the task that produced them.
