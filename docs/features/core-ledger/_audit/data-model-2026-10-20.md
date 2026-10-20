# Data-model audit — core-ledger · 2026-10-20

Revision run, not a first pass. Triggered by the PRD amendment of 2026-10-20 (US-09, US-10,
AC-26 to AC-36) and the matching `sad.md` §5 revision (commit `b7c9525`).

Supersedes `data-model-2026-10-18.md` for the `category_groups` and `categories` tables. Everything
it says about `accounts`, `transactions` and `transfers` still stands — those three are untouched.

## Mode

**Greenfield revision, specs amended in place.** No ALTER migrations were emitted, and that is a
decision worth stating plainly:

- `src/FinNavis.Infrastructure/Persistence/Migrations/` is **empty**. Nothing has ever been applied,
  to any database.
- `.claude/rules/migrations.md` defines the staged SQL as *"a reference spec, not a migration… It
  states the exact DDL the feature must end up with. It is never applied, and no tool reads it."*
- The zero-downtime rules (3-step expand → backfill → contract, `CREATE INDEX CONCURRENTLY`) govern
  **existing** tables. These tables exist nowhere, so there is no running system to keep up and no
  data to backfill.

Emitting an `ALTER TABLE category_groups ADD COLUMN kind` against a table that has never been
created would describe a history that did not happen, and EF Core would fold it into one initial
migration anyway at promote time.

## Generated / changed files

All paths are **staged**, under `docs/features/core-ledger/migrations/`. **Nothing was written into
the live `src/FinNavis.Infrastructure/Persistence/Migrations/` tree.** In this repo the promotion
step is not a file copy: `implement-tasks` writes the entity configurations, runs
`./build.sh migrate <Name>`, and diffs `dotnet ef migrations script` output against these specs. A
difference means either the configuration or the spec is wrong.

| File | Change |
|---|---|
| `20261018120001_create_category_groups.up.sql` | `kind VARCHAR(16) NOT NULL` added; `ux_category_groups_name` → `ux_category_groups_name_kind UNIQUE (name, kind)`; comments on AC-26, AC-29, AC-30, AC-31 |
| `20261018120001_create_category_groups.down.sql` | unchanged — `DROP TABLE` already reverses the whole table |
| `20261018120002_create_categories.up.sql` | no DDL change; comments added for AC-29 (why no `ON DELETE SET NULL`), AC-33, and the widened index justification |
| `20261018120002_create_categories.down.sql` | unchanged |
| `data-model.md` | ER diagram, decisions 4/7/8, both entity sections, index table, test fixtures |

Untouched: `create_accounts`, `create_transactions`, `create_transfers` (both halves each).

## Schema changes in this revision

Three, exactly as the PRD amendment predicted:

1. **`category_groups.kind`** — a group belongs to one side of the ledger (AC-26, AC-30, and
   `CONTEXT.md`, which is canonical and was updated in commit `ff1397a`).
2. **`ux_category_groups_name_kind`** — a group name is unique per side, not globally (AC-31). This
   is the one the PRD flagged as *"a second schema change, not documentation"*. Correct: the old
   spec had `UNIQUE (name)`.
3. **AC-29's link-clearing written down** — no DDL change, but the previously undescribed behaviour
   now has a home. See decision 8.

## Decisions taken, with the alternative that was rejected

Both were put to the owner rather than assumed. Both landed on "keep it in code", consistent with
decision 6 and with the `sad.md` §5 text already committed.

| Decision | Chosen | Rejected alternative | Why |
|---|---|---|---|
| 7 — AC-30, a group and its categories share a side | Domain invariant in the entity | Composite FK: `UNIQUE (id, kind)` on `category_groups` + two-column FK from `categories` | The FK would make the rule structural, but costs a unique index no query reads and freezes a group's `kind`. The entity also produces the refusal wording AC-30 asks for; an FK violation does not |
| 8 — AC-29, clearing the group link | `UPDATE … SET NULL` then `DELETE`, one transaction, in the use case | `ON DELETE SET NULL` on the FK | `SET NULL` moves AC-29's behaviour into DDL, where no use-case test covers it and no reviewer of the delete endpoint sees it. With `NO ACTION`, a forgotten `UPDATE` fails loudly on the FK |

## Convention divergences from the skill's defaults

Three, all pre-existing and all governed by `.claude/rules/migrations.md`, which records the repo's
deviation from the course baseline:

1. **Not SQL-first.** The course default is that the `.up.sql` / `.down.sql` pair *is* the migration.
   FinNavis uses EF Core migrations (`CLAUDE.md`, `sad.md` §2). The two cannot be mixed — EF Core
   keeps a `ModelSnapshot.cs`, and a table created by hand-written SQL would be invisible to it.
   The staged SQL is therefore a reference spec.
2. **Promotion is a regeneration, not a copy.** `implement-tasks` generates the real migration from
   entity configurations and diffs it against these files.
3. **Enum stored as a string.** `kind VARCHAR(16)`, never an int (`CLAUDE.md`). Requires
   `.HasConversion<string>()` in the `IEntityTypeConfiguration<CategoryGroup>`.

No divergence introduced by this revision.

## Self-checks — 4 / 4 pass

Run over all ten staged files, not just the changed ones.

| Check | Result |
|---|---|
| Naming — `plural snake_case` tables, `<timestamp>_<verb>_<entity>` filenames | PASS — all ten match |
| `down.sql` reversibility | PASS — every `CREATE TABLE` has a `DROP TABLE`, every index creation a `DROP INDEX`. Note for a future run: a naive `grep "CREATE INDEX"` under-counts, because `CREATE UNIQUE INDEX` does not match it — `transactions` creates 4 indexes and drops 4 |
| FK indexes | PASS — 5 `REFERENCES` clauses, 5 covering indexes: `idx_transactions_account_id`, `idx_transactions_category_id`, `idx_transfers_source_account_id`, `idx_transfers_destination_account_id`, `idx_categories_category_group_id` |
| Forbidden features — `CHECK (`, `CREATE TRIGGER`, business `DEFAULT '…'` | PASS — none. The only `DEFAULT` is `now()` on `created_at` |

`erDiagram` in `data-model.md`: structural lint passes — `erDiagram` directive present, 5
relationship lines with valid cardinality glyphs, every attribute line well formed. `mmdc` is not
installed on this machine, so no render-parse was run.

## Drift

**Schema-vs-source:** not applicable. The Domain entities do not exist yet — the repository is a
skeleton (`sad.md` §3). No `_drift/` proposals generated.

**Model-vs-spec:** clean after this revision. Every PRD entity has a table; every table has a PRD
origin. The three gaps the previous audit and the api-forge gate found are now closed:

| Gap | Status |
|---|---|
| `category_groups` existed with no operation behind it (blocked `/sdlc-api-forge`) | Closed by PRD US-09 + `ICategoryGroupRepository` in `sad.md` §5 |
| `category_groups` carried no side | Closed — `kind` column |
| Group name unique globally rather than per side | Closed — `ux_category_groups_name_kind` |
| Category-to-group link had no described behaviour on group delete | Closed — decision 8 |
| Decision 4 was *"an added rule"* no AC covered | Closed — AC-31, AC-35, AC-36 |

## Seeds and fixtures

Unchanged. Still **no seeds** — no user table, no lookup tables (enums are strings), and the owner
creates their own accounts, categories and groups.

Test fixtures: `CategoryGroupBuilder` now takes a side; `CategoryBuilder`'s group must be on that
same side. PII guard holds — no real-looking personal data, no real bank statement or export ever
enters the repository (`CLAUDE.md` Safety).

## Breaking changes

None decomposed. Nothing is deployed, so nothing can break. See **Mode** above.

## Open items

Carried, unchanged — both are query concerns, not columns, and both belong to `/sdlc-api-forge`:

- `data-model.md` "Open items", TBD 1 — history page size is unspecified.
- `data-model.md` "Open items", TBD 2 — whether a category filter on the history page excludes
  transfers entirely.

New from the PRD amendment, both also owned by `/sdlc-api-forge` and both carried as rows in
`sad.md` §11 with owner and due:

- Should a group holding no categories appear in the breakdown with a total of zero? Default: left out.
- When a group is removed, should the owner be told how many categories were affected? Default: no.

## What this unblocks, and what it does not

**Unblocks:** `/sdlc-complete-sequence-diagrams core-ledger` — `sad.md` §6 lists AC-26 to AC-36 as
the gap, and the participants and tables they need now all exist.

**Does not unblock `/sdlc-api-forge core-ledger --reconcile` on its own.** A second blocker is still
open and is unrelated to the schema: `.claude/rules/openapi.md` §5 records that its error envelope
`{code, message, details?}` contradicts `sad.md` §8 and `CLAUDE.md`, which both name RFC 9457
Problem Details. That needs an ADR or a rules change before the contract can be re-run.
