# Indexes

Used in Phase 2. Every index costs write time and disk, so each one has to name the query it serves.
An index list with no "why" column is a list nobody can ever prune.

## Where the queries come from

Two sources, both in the SAD, both already written by the time this stage runs:

1. **The §6 flows.** Each `Inf->>PG:` line is a real query. Read what it filters on and what it
   aggregates. A refusal branch that checks "does any row name this account" is also a query — a
   different one from an aggregate over the same column, which matters below.
2. **§6's "Notes flagged while completing §6" → index hints.** `finnavis-complete-sequence-diagrams`
   writes this bucket for exactly this stage: which table, by which column, and whether the query
   reads rows or only asks whether one exists.

If §6 is incomplete, say so rather than inventing queries. An index guessed from a column name is
the "just in case" index with better camouflage.

## One index per real query

Walk the queries, not the columns. For each, decide what it needs:

- **A filter on one column** → an index on that column.
- **A filter on two, always together** → a composite, leading with the column the other queries also
  filter on alone. A composite on `(occurred_on, account_id)` serves a period-only filter too,
  because `occurred_on` leads.
- **A filter on the second column alone** → the composite cannot serve it. This is the case people
  get wrong: an aggregate over all time filtered by account is *not* served by
  `(occurred_on, account_id)`, so it needs its own index on `account_id`.
- **An existence check** (`does any row name this?`) → may not want the same index as an aggregate
  over the same column. Usually they coincide; when they do not, say which query each one is for.

## The FK rule

`.claude/rules/migrations.md`: every `REFERENCES other_table(id)` is followed by an index on the FK
column, in the same migration. This is not optional and does not need a separate query to justify
it — without it, a delete or an update on the parent scans the child table.

When the FK index is also serving a real query, say both things in the "Query it serves" cell. One
index, two reasons, and the next reader does not remove it thinking it was only bureaucratic.

## What PostgreSQL already gives you

A `UNIQUE` constraint is backed by an index. Adding a second index on the same column is pure cost.
Say this in the negative list rather than leaving the reader to wonder whether it was forgotten.

A unique index treats nulls as distinct, so a nullable unique column allows any number of null rows.
When that is the point — a reference that only a later feature fills — write it down, because it
looks like a bug otherwise.

## The negative list

A section of `data-model.md` that is as valuable as the table above it: **indexes deliberately not
created**, each with a reason.

Without it, the next pass over this schema re-proposes every candidate this one rejected, and the
reasoning is gone. With it, the argument is already made:

- nothing beyond the unique constraints on name columns — PostgreSQL backs those already;
- nothing on a free-text note — nothing searches it, and the PRD closed that question;
- no covering or partial index on the main read — the plain composite is load-bearing on its own,
  and a test asserts the plan.

If a quality goal in SAD §10 depends on an index, say which test guards it. An index whose loss
turns a query into a sequential scan should fail a test, not slowly get slower.

## Changing an index on a table an earlier feature defined

`.claude/rules/migrations.md` requires `CREATE INDEX CONCURRENTLY` on an existing table, and EF Core
does not generate it — it has to be written as a raw `migrationBuilder.Sql(...)` in a migration
marked as not wrapped in a transaction.

Apply the rule. Then add one line to the audit naming what it costs: a separate migration, outside a
transaction, for a database that is not deployed anywhere yet. The rule holds — the user decides
whether to keep paying for it, and they can only decide if the cost is visible.

A table this feature creates is empty in the same migration, so there is no write lock to avoid and
`CONCURRENTLY` does not apply. Say that too; otherwise a reviewer reads its absence as a miss.
