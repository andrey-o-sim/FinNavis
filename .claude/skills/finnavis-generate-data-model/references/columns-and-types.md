# Columns and types

Used in Phase 1. The rules come from `CLAUDE.md` and `.claude/rules/migrations.md` — they are read,
not re-decided. What this file adds is where to *source* each choice, because a bound invented
during drafting is the thing that has to be unpicked later.

## The fixed set

| Topic | Value | Source |
|---|---|---|
| PK | `UUID`, app-generated with `Guid.CreateVersion7()` in Application | ADR-0005 |
| Money | `NUMERIC(19,4)` ↔ C# `decimal` | `CLAUDE.md`, rules file |
| Timestamps | `TIMESTAMPTZ NOT NULL DEFAULT now()`, stored UTC | `CLAUDE.md` |
| Enums | stored as **strings**, `.HasConversion<string>()` — never int, never a lookup table | `CLAUDE.md` |
| Bounded strings | `VARCHAR(N)`; `TEXT` only for a URL or a genuinely long description | rules file |
| Audit columns | `created_at` only | rules file |
| Delete | hard delete; no `deleted_at`, no status column for it | rules file |
| Naming | plural `snake_case` tables, `snake_case` columns | `CLAUDE.md` |
| Forbidden | `CHECK`, `TRIGGER`, stored procedure, business `DEFAULT` | rules file |

An enum stored as a string means there is usually **no lookup table to seed**. That is why the
lookup seed bucket in this project is normally empty, and saying so is better than leaving the
bucket out.

## Where a bound comes from

`VARCHAR(N)` needs an `N`, and the PRD often does not give one. Three honest sources, in order:

1. **An acceptance criterion that states it.** `maxLength: 200` in an AC → `VARCHAR(200)`. Copy it.
2. **A PRD §6.1 abuse case.** "a note could be used to store megabytes" is an argument for a bound,
   and the bound then becomes the validation rule in Presentation and Application. Record it as a
   decision, because the number is yours.
3. **A working bound, stated as such.** «100 is a working bound — the PRD states none.» This is
   fine, as long as the sentence is in the document. A bound presented as derived when it was
   chosen is the one nobody revisits.

Never silently widen to `TEXT` to avoid the question. The bound is documentation: it tells the next
reader what the field is for.

## Dates versus timestamps

This distinction has already bitten this project once, so it is worth making deliberately.

- **A business day the owner chooses** — when a purchase happened, which month an entry belongs to
  — is a calendar `DATE`. No time, no timezone. A period filter is then a plain date range and a
  late-evening entry cannot slide into the next month.
- **A record-keeping moment** — when the row was written — is `TIMESTAMPTZ`.

`CLAUDE.md`'s "use `DateTimeOffset`, store UTC" rule governs timestamps. It does not make a calendar
date into a timestamp, and reading it that way reintroduces the boundary bug.

## Nullability

Nullable is a claim that the absence means something. Say what it means in the Notes column:

- `note VARCHAR(500) NULL` — the owner did not write one.
- `category_group_id UUID NULL` — the category belongs to no group, which the read path treats as
  the default group.
- `external_ref VARCHAR(200) NULL` — always null in the feature that added it; a later feature
  fills it (ADR-0006).

A nullable column with no stated meaning is usually a missing decision.

## `updated_at` is opt-in and never silent

The default is `created_at` only. A mutable entity is **not** sufficient reason — the question is
whether any requirement *reads* a last-changed value. If nothing does, the column ships unused.

If one does, add it and say which requirement. Surfacing this is the point: an `updated_at` that
appears without a sentence next to it is one nobody can later remove, because nobody knows whether
something depends on it.

## JSONB

Only for a payload that is genuinely opaque to the schema — something the database stores and never
filters on. A structured field becomes a column. Every `JSONB` column carries a one-line
justification in Notes; without one it is a place where the schema stopped being designed.

## Constraints: what the database may enforce

The database enforces **structure**: `NOT NULL`, `UNIQUE`, `FOREIGN KEY`, `PRIMARY KEY`.

It does not enforce **business rules**. "An amount must be above zero" and "a transfer needs two
different accounts" are domain invariants, refused in the Domain entity before a row is built. A
`CHECK` that duplicates them puts the same rule in two places that can drift, and the database's
version produces an error nobody wrote a message for.

Write the reason next to the table when a reader might expect a `CHECK` and not find one. That
sentence is why the next pass does not add it back.

## A UNIQUE nobody asked for

A `UNIQUE` constraint is a business rule wearing structural clothes, so it needs an acceptance
criterion behind it. "Two accounts named Cash make the ledger unusable" is a good argument — and
if no AC says it, the honest move is to add the constraint **and** record it as a finding, so the
PRD gets the criterion it is missing. See
[drift-and-audit.md](drift-and-audit.md#direction-3--a-constraint-no-acceptance-criterion-asks-for).
