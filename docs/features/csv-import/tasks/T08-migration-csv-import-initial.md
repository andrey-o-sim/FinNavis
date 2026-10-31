---
id: T08
title: "Generate the import migration and diff it against the staged spec"
layer: "migration"
deps: ["T07"]
acs: []
files_hint: ["src/FinNavis.Infrastructure/Persistence/Migrations/", "docs/features/csv-import/migrations/", "tests/FinNavis.Infrastructure.IntegrationTests/Persistence/"]
owner: "Andrey Simonenko"
estimate: "M"
status: "done"
---

# T08 — Generate the import migration and diff it against the staged spec

## Why

[data-model.md](../data-model.md) staged eight `.up.sql` / `.down.sql` pairs under
[`migrations/`](../migrations/). They are a **reference spec, never applied** — EF Core owns the
real migration and keeps the `ModelSnapshot` (`.claude/rules/migrations.md`). This task generates
the real one and proves the two agree.

`acs` is empty: a migration satisfies no acceptance criterion by itself.

## What

Run `./build.sh migrate AddCsvImport`, then diff `dotnet ef migrations script` output against the
eight staged `.up.sql` files, table by table. A difference means either the configuration (T07) or
the staged spec is wrong — resolve it in one of them before this task closes. Do not edit the
generated migration to make the diff pass.

The eight tables, in the order the staged files number them: `mapping_profiles`,
`mapping_profile_rules`, `import_runs`, `import_transaction_links`, `import_transfer_links`,
`import_created_account_links`, `import_created_category_links`, `import_set_aside_rows`.

Then apply it against PostgreSQL in Testcontainers and revert it, to prove the down path works
before any data exists to lose.

## Definition of Done

- [ ] `./build.sh migrate AddCsvImport` produces **one** migration covering all eight tables
- [ ] `dotnet ef migrations script` matches the eight staged `.up.sql` files on every table, column,
      type, bound, index and foreign key — including `ON DELETE` behaviour
- [ ] It applies against PostgreSQL 18 in Testcontainers, and `dotnet ef database update` back to
      the previous migration reverts it cleanly
- [ ] The migration carries **no `CHECK` constraint, no trigger, no stored procedure and no business
      `DEFAULT`** — only `DEFAULT now()` on timestamps (`.claude/rules/migrations.md`)
- [ ] No `CREATE INDEX CONCURRENTLY` — every table here is created empty in the same migration, so
      there is no write lock to avoid
- [ ] Nothing in the generated script touches `transactions` or `transfers` (ADR-0007)
- [ ] `./build.sh test` is green

## Notes

- **The two timestamps will not match, and that is expected.** The staged spec carries
  `20261018190000`…`20261018190007`, set at the data-model stage; `dotnet ef` mints its own at
  generate time (`.claude/rules/migrations.md`). Do not rename either to make them line up.
- **The staged files use `IF NOT EXISTS` and EF Core does not.** That is a difference in the spec's
  own convention, not a drift — compare the schema the two produce, not the literal text.
- **This is the only `migration` task in the epic**, so the serialization lane that `layer:
  migration` always creates is trivially held.
- **Never edit this migration once it is applied** (`CLAUDE.md`). If the diff finds something after
  the fact, it is a second migration.
- The FK from `mapping_profile_rules.target_category_id` into `core-ledger`'s `categories` means
  `core-ledger`'s migration must already be applied. [data-model.md](../data-model.md) carries the
  finding that deleting a targeted category is now blocked by PostgreSQL with no refusal message of
  its own.
