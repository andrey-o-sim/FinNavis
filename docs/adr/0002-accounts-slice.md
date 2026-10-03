# ADR 0002: Decisions behind the accounts slice

- **Status**: Accepted
- **Date**: 2026-10-03
- **Deciders**: Andrey Simonenko

## Context

ADR 0001 picked the structure. This is the first feature built inside it: create an account,
list accounts, get one by id. See `task1.md` for the task statement.

The slice is small, but it sets the shape every later feature copies. A few choices here were
contested, or reverse what an earlier project of mine did. Those are recorded below. The rest
were obvious and are not.

## Decision

### Ids are `Guid.CreateVersion7()`, stored as `uuid`

A previous version of this project used `bigint identity`. Anyone comparing the two deserves
the reason for the change.

- The client gets the id with no round trip, so a use case can build an object graph before
  anything is saved. That matters for transfers: two accounts and two transactions in one
  commit.
- An id is not a count. A sequential `bigint` leaks how many accounts exist and invites people
  to type ids by hand.
- Version 7, not version 4. A v7 GUID starts with a timestamp, so values arrive in roughly
  increasing order and the primary key index does not fragment the way random v4 values do.
  That is the only reason a random v4 would have been worse.

The database never generates the id: `ValueGeneratedNever()`, and `Account.Create` is the only
place `Guid.CreateVersion7()` is called.

**Cost:** 16 bytes per row instead of 8, and uglier URLs. Both are acceptable for a
single-user app.

### Balance is derived, through one Domain method

ADR 0001 said the balance rule must live in one place. It does:

```csharp
public decimal CalculateBalance(decimal transactionsTotal) => InitialBalance + transactionsTotal;
```

The table stores `initial_balance`, not `balance`. A stored balance is a cached sum, and a
cached sum goes wrong the first time a transaction is edited or deleted.

The method takes an **already-summed total** rather than a list of transactions. That is
deliberate. When transactions arrive, Infrastructure can produce the sum as a SQL aggregate
instead of loading rows into memory, and the formula still goes through this one method.

Today the total is always zero, written once as `AccountDto.NoTransactionsYet`.

**Cost:** reading a balance will cost a join once transactions exist. If that ever hurts, the
fix is a materialised view or a cached column that is rebuilt from the transactions — not a
second copy of the formula.

### A negative initial balance is allowed

`card` is an account type in SPEC. A credit card is overdrawn by design, and so is a wallet
someone owes money from. Rejecting negative amounts would be a rule nobody asked for.

Two constraints replace it, because "any decimal" is not safe either:

- At most 2 decimal places. The column is `numeric(14,2)`, so a third decimal would be rounded
  away silently after the API had accepted it.
- Magnitude at most 999,999,999,999.99, matching the column's precision. Without this an absurd
  amount becomes a `DbUpdateException` and a 500 instead of a 400.

The validation and the column are written from the same numbers, so they cannot drift.

### `Result<T>` instead of exceptions across layers

A broken rule from user input is a normal outcome, not an exceptional one. Throwing from
Application and catching in Presentation would use the stack as a control-flow channel and make
"which field was wrong" something to reconstruct from a message.

So a use case returns `Result<T>`: success, a field-to-messages dictionary, or not-found.
Presentation maps it to `ValidationProblem` or `NotFound`.

The part worth recording is **where the rule lives**. Domain exposes `Account.ValidateName` and
`Account.ValidateInitialBalance`, which return `null` or the message:

- `Account.Create` calls them and throws. That is a guard against a programmer error, not a path
  user input ever takes.
- The use case calls them first and turns a non-null return into a field error.

Both paths read the same rule and the same wording. The use case adds only the field name, which
is a wire concern. Without this, the rule would exist twice — once in Domain and once as
validation attributes somewhere outside — and the two copies would drift.

`Result<T>` deliberately has no `Map`, `Bind` or `Match`. Three endpoints do not need a
mini-monad.

### The account type is matched by name, never parsed as a number

`Enum.TryParse<AccountType>("7", ...)` **succeeds** and hands back the undefined value
`(AccountType)7`, which `.HasConversion<string>()` would then store as the text `"7"`. Adding
`Enum.IsDefined` fixes that one case and leaves a worse one: `"0"` parses to `Cash`, so a client
sending a number gets a real account of a type it never named.

So the string is compared against `Enum.GetNames<AccountType>()`, case-insensitively, and
nothing else is accepted. No compiler warning catches either hole; the tests do.

For the same reason `CreateAccountRequest.Type` is a `string`, not the enum. With the enum in
the contract, a bad value fails in the JSON binder and returns a 400 whose message names a
Domain type — a leak, and not an error under `errors.type`.

### No value objects yet

`Money` and `AccountName` were considered and rejected for now.

`Money` earns its place when there is a currency to carry. Multi-currency is a SPEC non-goal, so
today a `Money` would be a `decimal` in a wrapper: more code, no rule it can enforce that
`ValidateInitialBalance` does not. `AccountName` is a trimmed string with a length limit, which
is one line in the entity.

Revisit `Money` when multi-currency arrives. That is the change that makes a bare `decimal`
actually wrong.

### No unique index on account name

This reverses the earlier project, which had `ux_accounts_active_name`.

Nothing in SPEC or ADR 0001 says account names must be unique, and "two Cash accounts, one per
wallet" is a reasonable thing to want. A unique index would be a business rule invented by the
schema.

The primary key is the only index on `accounts`, and that was checked rather than skipped, as
CLAUDE.md requires: get-by-id is a primary-key lookup, and the list is an unfiltered scan over a
single-user table holding tens of rows. An index on `name` would be write cost for no read
benefit. Add one when there is a query that needs it.

### No design-time `DbContext` factory

`dotnet ef` runs `Program` up to `builder.Build()`, `AddInfrastructure` only registers the
`DbContext`, and `migrations add` never opens a connection. So `migrate` works on a clean clone
with no database and no `launchSettings.json`, and a factory would add nothing.

It would also cost something: the EF Design package inside Infrastructure with compile assets,
and a second composition root, which ARCHITECTURE.md reserves to Presentation.

This places two requirements on `AddInfrastructure`, both commented in the code: it must not
validate the connection string, and it must read it inside the `AddDbContext` lambda.

### No mocking library

The Application tests use two hand-written fakes, about 30 lines together. The two ports have
three members in total, and the assertions we want are about state — `repository.Items.Single()`,
`unitOfWork.SaveCount` — not about which calls happened.

A mocking library would be allowed by the architecture tests, which only read `src` projects.
It is not needed yet. Add one when a port appears that is genuinely awkward to fake.

## Consequences

- Every later feature has a shape to copy: entity with `Validate*` methods, use case returning
  `Result<T>`, EF configuration, endpoint group, Testcontainers test.
- `Microsoft.Extensions.DependencyInjection.Abstractions` is Application's first package. The
  boundary that makes it acceptable is written down in CLAUDE.md, because the next package may
  not be.
- Generated migration files are exempt from four style rules in `.editorconfig`. EF Core writes
  block-scoped namespaces and a BOM, and the snapshot is rewritten on every `migrate`, so
  hand-fixing is not an option.
- Update and delete are still missing. Delete has no agreed meaning until transactions exist —
  it is either a cascade or a refusal, and that is a decision for the transactions slice.
