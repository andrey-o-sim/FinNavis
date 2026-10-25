---
id: T07
title: "Add the DbContext sets and the eight entity configurations"
layer: "infra"
deps: ["T04", "T05"]
acs: []
files_hint: ["src/FinNavis.Infrastructure/Persistence/FinNavisDbContext.cs", "src/FinNavis.Infrastructure/Persistence/Configurations/"]
owner: "Andrey Simonenko"
estimate: "L"
status: "todo"
---

# T07 — Add the DbContext sets and the eight entity configurations

## Why

The persistence cut of the two aggregates. Every table, column, type, bound, index and foreign key
is specified in [data-model.md](../data-model.md) § Entities and § Indexes; this task expresses it
as `IEntityTypeConfiguration<T>` files, which is how `.claude/rules/migrations.md` says a FinNavis
migration is produced.

`acs` is empty: a schema satisfies no acceptance criterion by itself. Every criterion it carries is
claimed by the use case or the query that reads the table.

## What

Eight `DbSet<T>` entries on `FinNavisDbContext` and **one configuration file per entity** in
`Persistence/Configurations/`, for `mapping_profiles`, `mapping_profile_rules`, `import_runs`,
`import_transaction_links`, `import_transfer_links`, `import_created_account_links`,
`import_created_category_links` and `import_set_aside_rows`.

What has to be right, per [data-model.md](../data-model.md):

- **Names** are `snake_case`, tables plural.
- **Every enum uses `.HasConversion<string>()`** — `ImportRunKind`, `ImportRunStatus`,
  `SetAsideReason`, `MappingRuleTarget`. Never an int (`CLAUDE.md`).
- **Bounds**: `shape_key` `VARCHAR(1000)`, `source_category` `VARCHAR(200)`, `file_name`
  `VARCHAR(255)`, `fingerprint` `VARCHAR(64)`, `raw_line` `VARCHAR(2000)`, `target`/`kind`/`status`
  / `reason` bounded as the table says.
- **Timestamps** are `TIMESTAMPTZ NOT NULL DEFAULT now()`.
- **Cascades**, each one deliberate: `mapping_profile_rules` cascades from its profile;
  `import_transaction_links`, `import_transfer_links`, `import_created_account_links` and
  `import_created_category_links` cascade from the **ledger** row they point at;
  `import_runs.account_id` is `ON DELETE SET NULL`; `import_set_aside_rows` **does not cascade** —
  those rows survive an undo.
- **Thirteen indexes**, exactly the list in [data-model.md](../data-model.md) § Indexes, and
  **nothing on `transactions` or `transfers`**.

## Definition of Done

- [ ] Eight `IEntityTypeConfiguration<T>` files exist, one per entity, each named after its type
- [ ] Every enum property uses `.HasConversion<string>()` — grep the folder for `HasConversion` and
      count four
- [ ] The thirteen indexes of `data-model.md` § Indexes are declared, with the four `ux_*` ones
      unique
- [ ] No configuration adds an index or a column to `transactions` or `transfers` (ADR-0007)
- [ ] No `HasCheckConstraint` anywhere — "a rule targets a category exactly when its target is
      `Category`" is refused in the entity (`.claude/rules/migrations.md`)
- [ ] Both link configurations carry a comment naming
      [ADR-0008](../adr/0008-detect-post-import-hand-edits-with-a-stored-content-fingerprint.md) and
      its field list, so the next person adding a movement field sees why it matters
- [ ] `./build.sh build` is green — with `TreatWarningsAsErrors` that is also the lint gate
- [ ] `./build.sh test` is green

## Notes

- **Shares a lane with nothing in this epic, but it is the one file `core-ledger` also owns.**
  `FinNavisDbContext.cs` gains eight `DbSet<T>` entries next to `core-ledger`'s five. That feature
  must have shipped first.
- **The comment in the link configurations is a Definition-of-Done line for a reason.**
  [sad.md §11](../sad.md) names the failure mode: *"the day a movement gains a field, AC-18 silently
  stops noticing edits to it"*. The comment is the only thing that puts ADR-0008 in front of the
  person making that change.
- **`import_set_aside_rows` must not cascade from its run.** It is the one child table that does
  not, and the reason is that a set-aside row was never in the ledger, so an undo has nothing to
  take back from it.
- **One `UNIQUE` per link table on the ledger id.** One transaction belongs to at most one run.
  `data-model.md` carries this as a finding: no acceptance criterion says so, and it is still right,
  because two runs claiming the same row would make undo ambiguous.
- The four `ux_*` unique indexes double as the FK index their column needs — PostgreSQL backs a
  unique constraint with an index already, so do not add a second.
