---
id: T11
title: "Add FinNavisDbContext and the five entity configurations"
layer: "infra"
deps: ["T06", "T07", "T08", "T09"]
acs: []
files_hint: ["src/FinNavis.Infrastructure/Persistence/FinNavisDbContext.cs", "src/FinNavis.Infrastructure/Persistence/Configurations/"]
owner: "Andrey Simonenko"
estimate: "M"
status: "done"
attempts: 1
---

# T11 — Add `FinNavisDbContext` and the five entity configurations

## Why

Derives from [`data-model.md`](../data-model.md) (5 tables, 8 indexes, 8 decisions) and
[sad.md §5](../sad.md). In this repo the entity configurations are **how the staged SQL spec gets
expressed** — `.claude/rules/migrations.md` says EF Core owns the real migration, so the
configuration is the thing that has to match the spec.

## What

- `FinNavisDbContext` with a `DbSet` per aggregate.
- One `IEntityTypeConfiguration<T>` file per entity in `Persistence/Configurations`
  (`CLAUDE.md`): `AccountConfiguration`, `CategoryGroupConfiguration`, `CategoryConfiguration`,
  `TransactionConfiguration`, `TransferConfiguration`.
- Match [`data-model.md`](../data-model.md) exactly:
  - tables and columns `snake_case`; tables plural
  - `CategoryKind` via **`.HasConversion<string>()`** — enums are stored as strings, never as int
    (`CLAUDE.md`)
  - `amount` → `NUMERIC(19,4)`; `occurred_on` → `DATE`; `created_at` → `TIMESTAMPTZ NOT NULL
    DEFAULT now()`
  - `VARCHAR(100)` names, `VARCHAR(500)` note, `VARCHAR(200)` `external_ref`, `VARCHAR(16)` kind
  - UNIQUE: `accounts(name)`, `categories(name, kind)`, `category_groups(name, kind)`,
    `transactions(external_ref)`
  - all eight indexes from the data-model Indexes table
  - every FK `NO ACTION` — **no `ON DELETE CASCADE` and no `ON DELETE SET NULL`**

## Definition of Done

- [ ] every column type, bound, index and unique constraint matches the
      [`data-model.md`](../data-model.md) tables and Indexes section, row by row
- [ ] `CategoryKind` is configured with `.HasConversion<string>()` on both `categories` and
      `category_groups`
- [ ] **no `CHECK` constraint, no trigger, no business `DEFAULT`** — the database is dumb storage
      (`.claude/rules/migrations.md`)
- [ ] no FK has a cascade; `categories.category_group_id` stays `NO ACTION`
- [ ] `./build.sh build` is clean

## Notes

- `data-model.md` decision 8 explains why the cascade is absent: with `NO ACTION`, forgetting
  AC-29's `UPDATE` **fails loudly on the foreign key** instead of silently rewriting rows. A
  cascade here would move AC-29's behaviour into DDL where no use-case test covers it.
- `data-model.md` decision 5: **no `updated_at` on any table.** Nothing reads a last-changed value.
