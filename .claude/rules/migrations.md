# Migration rules

<!-- Bootstrapped by sdlc:generate-data-model on 2026-10-18. Edit freely. -->
<!-- The baseline is an opinionated default. The FinNavis section below records where this repo -->
<!-- deliberately differs, and why. -->

## How FinNavis differs from the baseline — read this first

The baseline below is SQL-first: the hand-written `.up.sql` / `.down.sql` pair *is* the migration.
**FinNavis does not work that way.** `CLAUDE.md` and `docs/features/core-ledger/sad.md` §2 fix
migrations as EF Core, produced by `./build.sh migrate <Name>` into
`src/FinNavis.Infrastructure/Persistence/Migrations`.

The two cannot be mixed. EF Core keeps a `ModelSnapshot.cs` of the schema it believes exists. A
table created by hand-written SQL is invisible to that snapshot, so the next
`dotnet ef migrations add` would try to create it a second time.

So in this repo:

- **The staged `.up.sql` / `.down.sql` under `docs/features/<slug>/migrations/` is a reference
  spec, not a migration.** It states the exact DDL the feature must end up with. It is never
  applied, and no tool reads it.
- **EF Core owns the real migration.** At `implement-tasks` the entities and their
  `IEntityTypeConfiguration<T>` files are written, `./build.sh migrate <Name>` generates the
  migration, and `dotnet ef migrations script` output is compared against the staged spec. A
  difference means either the configuration or the spec is wrong — resolve it before applying.
- Everything else in the baseline — the type rules, the constraint rules, the "DB as dumb storage"
  rules — applies unchanged. They are expressed through the entity configurations instead of
  through raw SQL.

## Filenames

- Staged spec: `<YYYYMMDDhhmmss>_<verb>_<entity>.up.sql` + matching `.down.sql`.
- Reason: parallel feature branches do not collide on the next sequential number.
- The real EF Core migration gets its own timestamp from `dotnet ef` at promote time, so the two
  timestamps will not match. That is expected.

## Hard rules (DB as dumb storage)

- No `CHECK` constraints on business invariants.
- No `CREATE TRIGGER`.
- No `DEFAULT '<business literal>'` (only `DEFAULT now()` for timestamps).
- No stored procedures.
- Business logic lives in app code — in FinNavis, inside the Domain entities.

## Required constraints

- Every `REFERENCES other_table(id)` is followed by an index on the FK column (same migration).
- Every `.up.sql` has a matching `.down.sql` that fully reverses it.
- `CREATE TABLE` / `CREATE INDEX` use `IF NOT EXISTS` in the staged spec.
- Seed `INSERT` uses `ON CONFLICT DO NOTHING`.

## Defaults

- PK: `UUID v7`, generated app-side with `Guid.CreateVersion7()` in the Application layer
  (`docs/features/core-ledger/adr/0005-*`). Column type `UUID`.
- Timestamps: `TIMESTAMPTZ NOT NULL DEFAULT now()`.
- Strings: `VARCHAR(N)` bounded; `TEXT` only for URLs / long descriptions.
- Money: `NUMERIC(19,4)`, mapping to C# `decimal`. Never `float` or `double`.
- Enums: stored as strings, never as int (`CLAUDE.md`). Use `.HasConversion<string>()`.
- Soft delete: NOT used. Hard delete + audit table if business requires history.
- Audit columns: `created_at` only. `updated_at` is opt-in per entity and must be justified by a
  requirement that actually reads it.
- Naming: `plural snake_case` tables, `snake_case` columns.
- JSONB: only for semantically opaque payload. Structured fields → first-class columns.

## Zero-downtime patterns (mandatory for existing tables)

- New NOT NULL column → 3-step (add nullable → backfill → SET NOT NULL).
- New index on an existing table → `CREATE INDEX CONCURRENTLY`.
  EF Core does not generate `CONCURRENTLY`; write it as a raw `migrationBuilder.Sql(...)` and mark
  the migration so it is not wrapped in a transaction.
- Rename / drop column → 3-step (add new + dual-write in app code → backfill → drop old). Each
  phase is a separate PR and a separate deploy.
- Never edit an applied migration. Add a new one (`CLAUDE.md`).

## Seeds

- **Bootstrap** (admin user, default org): hardcoded deterministic UUID v7 in a migration file.
- **Lookup** (statuses, currencies): separate migration, `INSERT ... ON CONFLICT DO NOTHING`.
  FinNavis stores enums as strings rather than as lookup tables, so this bucket is usually empty.
- **Test fixtures**: NOT in migrations. Builder classes in the test projects.
- **PII guard**: no real-looking emails / names. Use `admin@example.test`, `Test User`.
- **No real financial data** in any seed or fixture (`CLAUDE.md` Safety).

## Out of scope

- Multi-DB (read replicas, sharding).
- Partitioning.
- Materialized views.

These are perf / scale topics, not contract topics. Decide per-project with a separate ADR.
