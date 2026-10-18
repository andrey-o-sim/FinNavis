# Audit — data model, core-ledger

**Date:** 2026-10-18 · **Stage:** 07 · **Mode:** greenfield · **Owner:** Andrey Simonenko

## Generated files

**Design doc**

- `docs/features/core-ledger/data-model.md`

**Migrations — STAGED, not live**

| File | Creates |
|---|---|
| `docs/features/core-ledger/migrations/20261018120000_create_accounts.{up,down}.sql` | `accounts` |
| `docs/features/core-ledger/migrations/20261018120001_create_category_groups.{up,down}.sql` | `category_groups` |
| `docs/features/core-ledger/migrations/20261018120002_create_categories.{up,down}.sql` | `categories` + 1 index |
| `docs/features/core-ledger/migrations/20261018120003_create_transactions.{up,down}.sql` | `transactions` + 4 indexes |
| `docs/features/core-ledger/migrations/20261018120004_create_transfers.{up,down}.sql` | `transfers` + 3 indexes |

**These migrations are staged, not live.** Nothing was written into
`src/FinNavis.Infrastructure/Persistence/Migrations`. `implement-tasks` promotes them — and in this
repo "promote" means something specific, see the next section.

**Rules file bootstrapped.** `.claude/rules/migrations.md` did not exist and was created from the
skill baseline, with a FinNavis section at the top recording where this repo differs. Edit it if
any default is wrong.

## Convention divergences

### 1. SQL-first vs EF Core — resolved by decision, not by default

The skill's house style makes the hand-written `.up.sql` / `.down.sql` the migration itself.
`CLAUDE.md` and SAD §2 fix migrations as EF Core (`./build.sh migrate <Name>`). The two cannot be
mixed: EF Core keeps a `ModelSnapshot.cs` of the schema it believes exists, so a table created by
hand-written SQL is invisible to it and the next `dotnet ef migrations add` would create it twice.
Applying the course default as written would have broken the repo.

**Decided with the owner (2026-10-18):** the staged SQL is a **reference spec**, never applied. EF
Core owns the real migration. At `implement-tasks`:

1. Write the entities and their `IEntityTypeConfiguration<T>` files.
2. Run `./build.sh migrate CoreLedger`.
3. Run `dotnet ef migrations script` and diff the output against the staged spec.
4. A difference means the configuration or the spec is wrong. Resolve it **before** applying.

Consequence: the staged timestamps and the EF migration timestamp will not match, and the
constraint names EF generates may differ in case from `pk_accounts` / `ux_accounts_name`. The diff
is about columns, types, nullability, keys and indexes — not about identifier casing.

### 2. `CREATE INDEX CONCURRENTLY` is not used

The house rule requires `CONCURRENTLY` on existing tables. Every table here is new and empty in the
same migration, so there is no write lock to avoid. The rule still applies to the next feature,
and `.claude/rules/migrations.md` records that EF Core does not generate `CONCURRENTLY` — it has to
be written as a raw `migrationBuilder.Sql(...)` outside a transaction.

### 3. Seeds: all three buckets empty

Bootstrap has nothing to bootstrap (no user table, no organisation, no sign-in — PRD §3). Lookup is
empty because `CategoryKind` is stored as a string rather than as a foreign key into a lookup table
(`CLAUDE.md`). Test fixtures are specified in `data-model.md` but not written — the Domain entities
do not exist yet, so the builders belong to `implement-tasks`.

## Drift findings

### Schema-vs-source — skipped

`src/FinNavis.Domain` holds `AssemblyMarker.cs` and nothing else. There is no struct or entity to
map against a column, so this check has no input. SAD §3 already records the repository as a
skeleton. No `_drift/` proposals were generated.

### Model-vs-spec — 5 findings, none blocking

| # | Finding | Severity | Resolution |
|---|---|---|---|
| 1 | Every entity in SAD §5 (`Account`, `Category`, `CategoryGroup`, `Transaction`, `Transfer`) has exactly one table. No orphan table has no PRD/SAD origin | — | Clean |
| 2 | **`UNIQUE (name)` on `accounts` is backed by no acceptance criterion.** AC-07 is phrased *"Given the owner has no account by that name"*, which implies a name identifies an account, but PRD §5 carries no AC refusing a duplicate name | Medium | Added anyway — a ledger with two accounts named "Cash" is unusable. **Needs an AC, or the constraint should be dropped.** Decide at `/sdlc-api-forge`, which owns the refusal contract |
| 3 | **`UNIQUE (name, kind)` on `categories` is backed by no acceptance criterion.** Same gap as #2. Scoped by kind so "Gifts" can exist once as spending and once as earning | Medium | Same as #2 |
| 4 | `transactions.external_ref` has no PRD origin at all — the PRD never mentions it | — | Expected. Its origin is ADR-0006, recorded in SAD §8 and §11 as deliberately unused debt. Not drift |
| 5 | **PRD §8 and SAD §11 still list two open questions this stage closed.** The note-length question and the local-calendar-month question were both due *before* `/sdlc-generate-data-model` and are now answered | Low | Both answered in `data-model.md` §"Decisions taken in this document". **PRD §8 and SAD §11 need updating** so they stop reading as open — see Follow-ups |

## Open questions closed at this stage

Both were due before this skill ran (PRD §8, SAD §11).

| Question | Answer |
|---|---|
| What is the maximum length of a transaction note? | **`VARCHAR(500)`.** Bounded, closing the storage-bloat abuse case in PRD §6.1. The bound becomes the validation rule in Presentation and Application |
| How are stored timestamps reconciled with the owner's local calendar month, so a late-evening purchase near a boundary lands in the right period? | **The entry date is a calendar `DATE`, not a timestamp.** The owner picks the day; a period filter is a plain date range, so the boundary problem does not exist. `created_at` stays `TIMESTAMPTZ` |

A third open question — *"Is an extra filter dimension (payee, merchant, free tag) needed soon?"* —
is listed in SAD §11 as due before this stage, but SAD §8 already closed it as "no". No column was
added for it. Nothing to do.

## Breaking changes decomposed

**None.** Greenfield: five `CREATE TABLE` statements against an empty database. No rename, no drop,
no re-type, so no expand → backfill → contract sequence was needed.

## Self-check — 4/4 pass

| Check | Result |
|---|---|
| **Naming** | All 10 files match `<timestamp>_<verb>_<entity>.(up\|down).sql`. All 5 tables are plural `snake_case` |
| **`down.sql` reversibility** | Every `CREATE TABLE` has a `DROP TABLE`; every `CREATE INDEX` has a `DROP INDEX`. Counts match per file (1/1 tables; 0/0, 0/0, 1/1, 4/4, 3/3 indexes). Drops are ordered indexes-before-table |
| **FK indexes** | All 5 FK columns are the leading column of an index: `transactions.account_id`, `transactions.category_id`, `categories.category_group_id`, `transfers.source_account_id`, `transfers.destination_account_id` |
| **Forbidden features** | No `CHECK (`, no `CREATE TRIGGER`, no stored procedure, no `DEFAULT '<business literal>'`. The only `DEFAULT` is `now()` on `created_at` |

Mermaid `erDiagram` validated by the structural lint (`mmdc` is not installed on this machine):
cardinality glyphs are all valid, every attribute line matches `type name [KEY] ["comment"]`.

## TBDs left in `data-model.md`

| File:line | TBD |
|---|---|
| `data-model.md` §"Open items" | History page size is unspecified. Flow 8 passes a page and SAD §10 sets a per-page latency target, but nothing says what a page holds. Not a schema concern — `/sdlc-api-forge` owns it |
| `data-model.md` §"Open items" | Whether a category filter on the history page excludes transfers entirely. SAD §6 flags AC-16 as pulling two ways. Not a schema concern — a transfer has no `category_id`, so the schema already makes a category filter unable to match one |

## Follow-ups for the owner

1. **Decide findings #2 and #3** — keep the unique name constraints and add the matching acceptance
   criteria, or drop the constraints. They currently enforce a rule the PRD never states.
2. **Update PRD §8 and SAD §11** to mark the note-length and calendar-month questions closed, with
   the answers above. Leaving them open makes the next reader re-litigate a settled decision.
3. **Confirm `NUMERIC(19,4)`** is the money precision you want. `CLAUDE.md` says `decimal` but names
   no precision; 19,4 is the common default and gives four decimal places on amounts up to 15
   digits. Changing it later is a re-type on a populated table.
