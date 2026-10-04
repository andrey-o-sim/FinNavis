# Add accounts: create, list, get by id

Status: draft
Created: 2026-10-04
Repository: FinNavis

## Why

FinNavis records money movement, and every movement belongs to an account. Nothing can be
recorded until accounts exist. The repository is a skeleton today: projects, architecture tests,
and docs are in place, but there are no entities, no database schema, and no endpoints. Accounts
are the first thing a user creates, so they are the first thing to build.

## What should change

A person can create an account, see the list of their accounts, and open one by its id.

An account has a name, a type (cash, card, or savings), an opening balance, and an optional note.
The opening balance is the money in the account at the moment it is added to the app. It may be
zero or negative, because a credit card usually starts in debt.

Account names must not repeat. Trying to add a second account with the same name is an error, not
a silent duplicate.

This is the first feature in an empty repository, so it also brings the database into existence:
the schema, the first migration, and the wiring that lets the API talk to PostgreSQL.

It also adds the way to run that database locally. Today there is no PostgreSQL to point the app
at, so a Docker Compose file brings one up with a single command.

## Acceptance criteria

| ID | Criterion |
|----|-----------|
| AC-1 | `POST /accounts` with a valid name, type, and opening balance creates an account and returns 201 with a `Location` header pointing at the new account. |
| AC-2 | The 201 response body holds the generated id, the name, the type, the opening balance, and the note. |
| AC-3 | `POST /accounts` returns 400 with a ProblemDetails body when the name is missing, empty, whitespace only, or longer than 100 characters. |
| AC-4 | `POST /accounts` returns 400 with a ProblemDetails body when the note is longer than 500 characters. |
| AC-5 | `POST /accounts` returns 400 with a ProblemDetails body when the type is not one of Cash, Card, Savings. |
| AC-6 | `POST /accounts` accepts a zero opening balance and a negative opening balance. |
| AC-7 | `POST /accounts` returns 409 with a ProblemDetails body when an account with the same name already exists, comparing names without letter case and without surrounding spaces. |
| AC-8 | A stored account name has no leading or trailing spaces. |
| AC-9 | A note that is missing, empty, or whitespace only is stored as null and returned as null. |
| AC-10 | `GET /accounts` returns 200 and every account, sorted by name. |
| AC-11 | `GET /accounts` returns 200 and an empty array when no account exists. |
| AC-12 | `GET /accounts/{id}` returns 200 and the matching account when the id exists. |
| AC-13 | `GET /accounts/{id}` returns 404 with a ProblemDetails body when no account has that id. |
| AC-14 | No accounts endpoint returns a creation timestamp in its response body. |
| AC-15 | The account type is stored in PostgreSQL as text, not as a number. |
| AC-16 | The accounts table has a unique index on the name, so the database rejects a duplicate even when two requests arrive at the same time. |
| AC-17 | Creating an account stores a UTC creation timestamp, taken from a clock abstraction that a test can control. |
| AC-18 | Each of the three endpoints has an integration test that runs against PostgreSQL in Testcontainers. |
| AC-19 | The account's rules are covered by unit tests that use no database and no network. |
| AC-20 | `dotnet build FinNavis.slnx` and `dotnet test FinNavis.slnx` both succeed with zero warnings. |
| AC-21 | `docker compose up -d` run from the repository root starts a PostgreSQL container, and the container reaches a healthy state. |
| AC-22 | The database files live in a named Docker volume, so the data is still there after `docker compose down` followed by `docker compose up -d`. |
| AC-23 | The PostgreSQL port is published on `127.0.0.1` only, so the container is not reachable from the local network. |
| AC-24 | The PostgreSQL image in `docker-compose.yml` is pinned to a specific major version, not `latest`. |
| AC-25 | `CLAUDE.md` describes the local workflow: start the container with Compose, then apply the schema with `./build.sh db-update`. |
| AC-26 | `CLAUDE.md` no longer lists `docker-compose.yml` under "Not set up yet". |
| AC-27 | The Safety section of `CLAUDE.md` records the password in `docker-compose.yml` as a named exception, so a reader does not take a committed password for a mistake. |

## Scope of work

- The account entity and its rules: name, type, opening balance, note, creation timestamp.
- A repository port for accounts, and its EF Core adapter.
- The database: `DbContext`, entity configuration, and the first migration.
- Three use cases: create an account, list accounts, get one account by id.
- Three HTTP endpoints: `POST /accounts`, `GET /accounts`, `GET /accounts/{id}`.
- Request and response contracts for those endpoints.
- Dependency injection wiring, so the API can run against a real PostgreSQL database.
- ProblemDetails error handling for 400, 404, and 409.
- Unit tests for the domain rules and the use cases. Integration tests for the three endpoints.
- `docker-compose.yml` with a single PostgreSQL service: pinned image, named volume, healthcheck,
  port published to localhost only.
- A `CLAUDE.md` update: the Compose workflow, `docker-compose.yml` removed from the
  "Not set up yet" list, and a named exception in the Safety section for the password that
  `docker-compose.yml` carries.

### Out of scope

- Update and delete endpoints. No `PUT`, `PATCH`, or `DELETE` for accounts.
- Archiving or soft delete. There is no way to hide an account that is no longer used.
- Categories, transactions, and transfers. This task adds the account aggregate only.
- A derived balance. The API returns the opening balance only. A balance computed from
  transactions arrives with transactions.
- Currency. FinNavis is single currency, so an account has no currency field.
- Paging, filtering, or search on the list endpoint.
- A Dockerfile for the API, and running the API itself in a container. Compose brings up the
  database only. The app still runs from the IDE or `./build.sh run`.
- Applying migrations automatically when the app starts. The schema is applied by hand with
  `./build.sh db-update`.
- Pointing the integration tests at the Compose database. They keep using Testcontainers, which
  starts and throws away its own container.
- The React PWA in `src/web`, the Playwright suite, and the CI pipeline. These stay not set up.
- Authentication. FinNavis is single user with no auth, and that does not change here.

## Decisions taken

- **Id is a `Guid` created in the Domain layer, using `Guid.CreateVersion7()`** - it is time
  ordered, so it does not fragment the PostgreSQL index the way a random Guid does, and the
  entity knows its own id without a database round trip.
- **Account types are exactly Cash, Card, Savings** - the three named in `SPEC.md`. Stored as
  text, per the rule in `CLAUDE.md`.
- **Opening balance may be negative** - a credit card is normally added while in debt.
- **Name uniqueness ignores letter case and surrounding spaces** - "Cash" and " cash " are the
  same account to a person, so they should be the same to the app.
- **A duplicate name is 409, not 400** - the request is well formed, it conflicts with data that
  already exists.
- **The response exposes `openingBalance`, not `balance`** - a field named `balance` would be
  correct today and wrong the moment transactions exist. The real balance field arrives with
  transactions.
- **The creation timestamp is stored but not returned** - adding a column later means a migration
  against live data, which is more expensive than adding it now. Nothing needs to read it yet.
- **The clock is an abstraction in Domain, not `DateTimeOffset.UtcNow`** - otherwise AC-17 cannot
  be tested.
- **Errors use ProblemDetails, RFC 7807** - built into ASP.NET Core, and it gives the future PWA
  one error shape to parse instead of one per endpoint.
- **No paging on the list** - one person has a handful of accounts. Paging would be contract and
  test cost with no benefit.
- **The unique name rule is enforced twice: by a check in the use case and by a unique index** -
  the check gives a clean 409, the index makes the rule true even under a race.
- **Compose brings up PostgreSQL only, not the API** - the app already runs well from the IDE and
  from `./build.sh run`. A Dockerfile would be more to build and more to keep working, and it
  solves a problem nobody has yet.
- **Migrations are applied by hand, not on startup** - schema changes in the startup path are a
  habit that gets expensive later. `./build.sh db-update` already exists.
- **The PostgreSQL credentials are local-only defaults written in `docker-compose.yml`** - chosen
  for a zero-step first run. The container is a throwaway, bound to `127.0.0.1`, and holds no
  real data. This is a deliberate exception to the "no password in a committed file" rule in
  `CLAUDE.md`, and that exception has to be written into `CLAUDE.md` so the next reader does not
  treat it as a mistake.

## Open questions

None. Everything above was settled in the discussion on 2026-10-04.
