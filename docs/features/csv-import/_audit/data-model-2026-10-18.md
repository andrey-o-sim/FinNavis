# Data model audit — csv-import

**Date:** 2026-10-18 · **Stage:** 04 · **Baseline:** earlier features' staged specs.
`src/FinNavis.Infrastructure/Persistence/Migrations/` is an empty directory and
`Persistence/Configurations/` is too, so there is no EF Core migration to compute a delta against.
The baseline is `docs/features/core-ledger/migrations/` — five tables, the design agreed so far.
**The two will diverge the moment `core-ledger` is built**, and this delta is therefore against a
document, not a database.

## 1. Generated files

| File | What it is |
|---|---|
| `docs/features/csv-import/data-model.md` | The design: ER, 13 decisions, 8 entities, 13 indexes, seeds, fixtures |
| `migrations/20261018190000_create_mapping_profiles.{up,down}.sql` | Reference spec |
| `migrations/20261018190001_create_mapping_profile_rules.{up,down}.sql` | Reference spec |
| `migrations/20261018190002_create_import_runs.{up,down}.sql` | Reference spec |
| `migrations/20261018190003_create_import_transaction_links.{up,down}.sql` | Reference spec |
| `migrations/20261018190004_create_import_transfer_links.{up,down}.sql` | Reference spec |
| `migrations/20261018190005_create_import_created_account_links.{up,down}.sql` | Reference spec |
| `migrations/20261018190006_create_import_created_category_links.{up,down}.sql` | Reference spec |
| `migrations/20261018190007_create_import_set_aside_rows.{up,down}.sql` | Reference spec |
| `_audit/data-model-2026-10-18.md` | This report |

16 SQL files, 8 pairs. **Nothing was written into `src/`.**

**These migrations are a specification, not something to run.** This repo's migrations are EF Core.
At `implement-tasks`: write the entities and their `IEntityTypeConfiguration<T>` files, run
`./build.sh migrate <Name>`, then diff `dotnet ef migrations script` output against the spec above.
A difference means the configuration or the spec is wrong — resolve it before applying.

The staged timestamps and EF's migration timestamp will not match, and EF may generate constraint
names in a different case. The diff is about columns, types, nullability, keys and indexes — not
about identifier casing.

**Ordering matters at promote time.** `mapping_profiles` must exist before `mapping_profile_rules`,
and `import_runs` before its five child tables. All eight also depend on `core-ledger`'s
`accounts`, `categories`, `transactions` and `transfers` already existing — SAD §11 already carries
that as a High ordering risk.

## 2. Convention divergences

### 1. `ON DELETE CASCADE` on every foreign key into `core-ledger`'s tables

**What the convention says.** `core-ledger/data-model.md` states "No `ON DELETE CASCADE`… A cascade
would silently do the opposite of what the acceptance criterion asks for", and uses no cascade
anywhere.

**What was done.** Six foreign keys in this feature carry `ON DELETE CASCADE`:
`import_transaction_links.transaction_id`, `import_transfer_links.transfer_id`,
`import_created_account_links.account_id`, `import_created_category_links.category_id`, plus
`mapping_profile_rules.mapping_profile_id` (a cascade inside this feature's own aggregate).
`import_runs.account_id` carries `ON DELETE SET NULL`.

**Why.** ADR-0008 states that a movement the Owner deleted by hand "is simply absent at undo time…
so undo proceeds for the rest". With a plain foreign key that is false — PostgreSQL would refuse
the delete, and `core-ledger` AC-22 ("a removed entry disappears from history") would stop working
for every imported row. The cascade is the only shape under which both acceptance criteria hold at
once, so here it does not do the opposite of an AC; it is what makes two of them true. The same
reasoning covers a created account or category the Owner deletes by hand.

**What it costs.** `core-ledger`'s rule now has an exception, and the next person reading both
documents has to know which side they are on. The boundary is stated once and is easy to check: a
cascade exists only on foreign keys pointing **out of** this feature **into** the ledger. Every
foreign key pointing at `import_runs` has none, because nothing deletes a run.

**Who decides.** Andrey. If the cascade is unwanted, the alternative is that `core-ledger`'s
delete-transaction use case learns to delete the import link first — which is the cross-feature
coupling ADR-0007 exists to avoid.

### 2. No zero-downtime ceremony anywhere in this spec

**What the rule says.** `.claude/rules/migrations.md` requires a three-step sequence for a new
`NOT NULL` column on an existing table, and `CREATE INDEX CONCURRENTLY` for a new index on one.

**What was done.** Neither, and the rule is not being skipped. **Every one of the eight tables is
new and created empty in the same statement**, so there is no existing row to backfill and no write
lock to avoid. ADR-0007 is what buys this: keeping the bookkeeping in this feature's own tables
means the migration adds tables and alters none.

**What it cost.** Nothing — which is the point worth recording, because its absence would otherwise
read as a miss. The one case that *would* pay the ceremony is named in `data-model.md`'s negative
list: if SAD §10's `EXPLAIN` assertion ever shows a sequential scan for a monthly load,
`transactions(account_id, occurred_on)` is a separate `CONCURRENTLY` migration outside a
transaction, against a table that by then holds real history.

### 3. `VARCHAR(2000)` for a raw file line, rather than `TEXT`

**What the rule says.** `VARCHAR(N)` bounded; `TEXT` only for a URL or a genuinely long description.

**What was done.** `import_set_aside_rows.raw_line` is `VARCHAR(2000)`. A raw CSV line could argue
its way into the "long description" bucket, and the bound was chosen anyway.

**Why.** The bound is the documentation, and here it is also half of PRD §6.1's abuse-case defence:
a line longer than 2000 characters is not a statement line, and the reader refuses the file before
anything is recorded. The bound becomes the validation rule in the reader.

**What it costs.** A legitimate statement with very long merchant descriptions would be refused.
Unknown until a real file is opened — PRD §8's "open a real statement" question covers it, and the
number is cheap to raise before anything is applied.

## 3. Drift findings

### Schema versus source

**Skipped — the Domain project is a skeleton.** `src/FinNavis.Domain/` holds `AssemblyMarker.cs`
and four empty folders (`Entities/`, `Enums/`, `Abstractions/`, `ValueObjects/`);
`src/FinNavis.Infrastructure/Persistence/Configurations/` and `Migrations/` are both empty. There is
nothing to map columns against. This is the expected answer until `core-ledger` is built — SAD §11
carries it as the High ordering risk — and no `_drift/*.sql` proposals were produced.

### Model versus spec, and constraints without an acceptance criterion

| # | Finding | Severity | Resolution |
|---|---|---|---|
| 1 | **PRD AC-01 offers a rule target the schema cannot store.** It says the Owner may say a file category is "one of their categories, a move between their own accounts, or **recorded with no category**". `transactions.category_id` is `NOT NULL`, `GLOSSARY.md` says every transaction names exactly one category, and SAD §6 flow 2 draws no branch for the third | **High** | `MappingRuleTarget` has two values. Confirmed with the owner on 2026-10-18: a category is required, the Owner provides it, and there is no default category. **AC-01 must be reworded** — the exact edit is in §4 below. Until it is, PRD §5 describes behaviour this schema refuses |
| 2 | **SAD §8 lists a fourth set-aside reason with no flow and no AC behind it.** "the named account is unknown" — but AC-09 refuses the *whole load* when the named account does not exist, and SAD §6 flows 2 and 4 draw exactly three set-aside branches | Medium | `SetAsideReason` has three values. **SAD §8 needs the fourth removed** — edit in §4. Left in, it would produce an enum value no code path can ever write |
| 3 | **SAD §8 and §11 both say this feature's migration must create the candidate-snapshot index, and it already exists.** Both were written on 2026-10-17; `core-ledger/data-model.md` landed on 2026-10-18 and declares `idx_transactions_occurred_on_account_id`, `idx_transactions_account_id`, `idx_transfers_occurred_on` and both transfer account indexes. SAD §11 names the failure mode as "each feature assumes the other creates it, and neither does" — the opposite happened | Medium | No new index on `transactions` or `transfers`. The query-by-query reasoning and the escape hatch are in `data-model.md` § Indexes. **SAD §8's Indexes row and §11's index row are now stale** — edits in §4 |
| 4 | **`UNIQUE` on each link's ledger-side id, asked for by no acceptance criterion.** `ux_import_transaction_links_transaction_id`, and the same on transfers, accounts and categories | Medium | Added anyway — two runs claiming the same row would make undo ambiguous in a way nothing in the design resolves, and AC-16 would then be unsatisfiable. **Needs an AC, or the constraint should be dropped.** The PRD would have to say: a movement belongs to at most one import run. Closed at the API-contract stage, which owns the refusal wording |
| 5 | **`UNIQUE (mapping_profile_id, source_category)`, asked for by no acceptance criterion.** AC-01 says the Owner confirms "per category the file carries", which *implies* one rule per category without *requiring* it | Medium | Added anyway — two rules disagreeing about the same file category have no tiebreaker, and the row would be filed arbitrarily. **Needs an AC**, phrased as a refusal: confirming two rules for one of the file's categories is blocked. Closed at the API-contract stage |
| 6 | **`mapping_profile_rules.target_category_id` reaches into `core-ledger` and changes its delete behaviour.** Deleting a category a rule targets is now refused by PostgreSQL. `core-ledger` AC-12 refuses deleting a category that *history* names — a rule is not history, and has no refusal message | Medium | The foreign key stays: without it a rule can point at a category that is gone, and AC-02's guarantee ends the moment the rule is stored. **`core-ledger`'s DeleteCategory use case must also refuse when a mapping rule targets the category**, with its own message, so the Owner never meets a raw foreign-key error. Follow-up 4 below. ADR-0007 said this feature adds no *column* to the ledger — true, and an incoming foreign key is a second kind of coupling that record did not weigh |
| 7 | `import_runs.account_id` is written and no SAD §6 flow reads it | Low | Explained, not drift: PRD §9 states the run remembers "the account it was loaded onto when the file names only one". AC-13's report does not read it today. Kept because PRD §9 asks for it; if the API-contract stage finds nothing reads it either, it is one column to drop before anything is applied |
| 8 | `fingerprint` is stored for every imported movement and read only if that run is ever undone | — | Explained: ADR-0008, and SAD §11 already lists it under accepted debt. Not drift |
| 9 | `transactions.external_ref` has no PRD origin | — | Explained: its origin is `core-ledger` ADR-0006, recorded in SAD §8 as deliberately-unused debt added *for this feature*. This stage is where it stops being unused. Not drift |
| 10 | Every SAD §5 aggregate has exactly one root table; no orphan table | — | Clean. `ImportRun` → `import_runs` plus five child tables, `MappingProfile` → `mapping_profiles` plus `mapping_profile_rules`. No child entity was given a repository port, per `CLAUDE.md` and SAD §5 |

## 4. Open questions closed at this stage

| Question | Where it was open | Answer |
|---|---|---|
| The exact split of the link tables — one table with nullable foreign keys and a check constraint, or one table per kind | ADR-0007, "Neutral" | **One table per kind**, every foreign key `NOT NULL`. `CHECK` is forbidden by `.claude/rules/migrations.md`, so the nullable-pair option would leave the invariant invisible to the database. Chosen by the owner on 2026-10-18. `data-model.md` decision 3 |
| How the shape key is computed — the ordered column names joined, or a hash of them | ADR-0009, "Neutral" | **The joined names, stored readable**: trimmed, lowercased, trailing empty columns dropped, joined with `\|`, file's own column order. `VARCHAR(1000)` UNIQUE. A hash is opaque exactly when ADR-0009's two known failure modes need debugging. `data-model.md` decision 2 |
| Where `external_ref` gets its value, and what a row with no identifier writes into it | ADR-0002, "Neutral" | **`<source>:<the file's own row identifier>`**, with `<source>` the applied `mapping_profiles.id` for a statement and the literal `history` for a migration. `NULL` when the file supplies no identifier. The qualifier exists because `ux_transactions_external_ref` is global and two banks numbering a row identically would collide. `data-model.md` decision 5 |
| The field list ADR-0008's fingerprint covers | SAD §11 ("Name the field list explicitly at the data-model stage") | **Named**: SHA-256 over five `\|`-joined fields for a transaction, four for a transfer, with exact formatting rules. `data-model.md` § "The content fingerprint" |
| Should a recorded entry carry its own identifier from day one? | PRD §8 (due: core-ledger data-model stage) | **Yes**, as a nullable `external_ref`. Closed by `core-ledger` ADR-0006. SAD §11 already lists it under "Open questions closed during this pass"; **PRD §8 still lists it as open**, so the two documents disagree today |

**Not closed here, and deliberately so.** PRD §8's remaining questions are due "before
implementation starts" and need a real file opened, not a schema decision: what exactly makes two
rows name the same movement, whether either file carries a stable operation number, whether the
statement names a currency, whether the export names both sides of a transfer, and the file-size
limit. None of them changes a column — they land in the snapshot query's `WHERE` clause (ADR-0003)
or in a configuration value. They are listed in `data-model.md` § Open items so the next stage finds
them.

**Upstream updated:** **yes — applied on 2026-10-18 after the owner approved them.** The five edits
below were proposed first and made second; the diffs are kept verbatim so the change is reviewable
without a `git diff`.

**A sixth edit was needed and is not in the list below.** `sad.md` §5 (Building block view) also
described three rule meanings — the same defect as finding 1, in a second place. Leaving it would
have recreated the contradiction the AC-01 edit removes, so it was corrected in the same pass:

```diff
-  Owner's categories, a move between the Owner's own accounts, or recorded with no category. A
+  Owner's categories, or a move between the Owner's own accounts. **Two meanings, not three** — a
+  rule saying "record this with no category" was dropped on 2026-10-18 at the data-model stage,
+  because a Transaction names exactly one category (`GLOSSARY.md`) and there is no default category.
+  A file category the Owner leaves without a rule has its rows set aside instead (AC-08, AC-14). A
```

**Three consequential edits**, made so no document describes the others as they were before this
pass: a short correction block in `PRD.md` §1 and in `sad.md` §11 recording what changed and why
(both files already use that pattern, dated 2026-10-17); `updated_at` bumped to `2026-10-18` in both
front matters; and the back-references in `data-model.md` decisions 9 and 13 and in two staged SQL
comments moved to the past tense, since they quoted SAD §8 as it read before the edit.

### Proposed edit 1 — `PRD.md` AC-01 (finding 1, High)

```diff
 **Given** a statement of a shape never handed over before
-**When** the owner says, per category the file carries, that it is one of their categories, a move
-between their own accounts, or recorded with no category
+**When** the owner says, per category the file carries, that it is one of their categories or a
+move between their own accounts — and leaves a category they do not want to file without a rule
 **Then** the rules are remembered for that shape, and the next file of the same shape needs no
 confirmation
```

A category the Owner leaves without a rule already has defined behaviour: its rows are set aside
with the reason named back (AC-08), and they are never recorded on their own (AC-14). So the third
target is not lost behaviour — it is the same outcome, reached without a rule.

### Proposed edit 2 — `PRD.md` §8, the identifier question (finding in §4 above)

```diff
-- [ ] Should a recorded entry carry its own identifier from day one? Default now: no. — owner: Andrey,
-      due: core-ledger data-model stage (idea-brief §15; core-ledger §8 carries the same row)
```

Closed by `core-ledger` ADR-0006; SAD §11 already records it as closed.

### Proposed edit 3 — `sad.md` §8, the Set-aside reasons row (finding 2)

```diff
-| Set-aside reasons | A domain enum stored as a string (`CLAUDE.md`): a move between the Owner's own accounts, another currency, no rule covers it, the named account is unknown. The Owner-facing wording for each comes from PRD §10 | PRD §10, §6 |
+| Set-aside reasons | A domain enum stored as a string (`CLAUDE.md`), three values: a move between the Owner's own accounts, another currency, no rule covers it. An unknown account is **not** one of them — AC-09 refuses the whole load. The Owner-facing wording for each comes from PRD §10 | PRD §10, §6 |
```

### Proposed edit 4 — `sad.md` §8, the Indexes row (finding 3)

```diff
-| Indexes | Five, all from PRD §9's new read paths: … **The second and third do not exist in any document yet** — `core-ledger` declares no index but that unique one, so this feature's own migration creates them unless `core-ledger` has by then. §11 carries the way that goes wrong | §10, §11 |
+| Indexes | Thirteen, all on this feature's own tables: mapping profiles by shape key, **unique** (ADR-0009, one lookup per load); the rules by profile; each link table and the set-aside rows by run id; the FK index each foreign key needs. **Nothing is added to `transactions` or `transfers`** — `core-ledger/data-model.md` (2026-10-18) declares the indexes the candidate snapshot and the AC-17 check need, so ADR-0007's "adds tables and alters none" holds literally. `data-model.md` § Indexes carries the query-by-query reasoning and the escape hatch | §10, `data-model.md` |
```

### Proposed edit 5 — `sad.md` §11, the index risk row (finding 3)

```diff
-| **The index the candidate snapshot needs is declared nowhere.** … | Medium | This feature's own migration creates it unless `core-ledger` already has (§8). … | Andrey |
+| ~~The index the candidate snapshot needs is declared nowhere.~~ **Closed 2026-10-18** at the data-model stage: `core-ledger/data-model.md` declares `idx_transactions_occurred_on_account_id`, `idx_transactions_account_id` and both `transfers` account indexes, which serve the snapshot and the AC-17 check. Neither feature assumed the other would do it | — | The `EXPLAIN` assertion inside `MigrateHistory_1500RowsSeveralAccounts_P95Under60s` (§10) still guards it, and `data-model.md` § Indexes names `transactions(account_id, occurred_on)` as the index to add if that assertion ever fails | Andrey |
```

## 5. Breaking changes decomposed

**None — every table is new.** No rename, no drop, no re-type, no new `NOT NULL` on an existing
column, and no index on an existing table. There is no expand → backfill → contract sequence to
decompose, and no `CONCURRENTLY` anywhere.

That is ADR-0007 working as intended: keeping the bookkeeping in this feature's own tables makes
PRD §9's "nothing already recorded loses anything" true by construction rather than by care. The
one qualification is finding 6 — this feature adds no *column* to `core-ledger`, and it does add an
incoming foreign key that changes how a category delete behaves.

## 6. Self-check — 4/4

| Check | Result |
|---|---|
| **Naming** | 16 files, 16 of 16 match `^[0-9]{14}_(create\|add\|backfill\|set)_[a-z_]+\.(up\|down)\.sql$`. All 8 table names are plural `snake_case`: `mapping_profiles`, `mapping_profile_rules`, `import_runs`, `import_transaction_links`, `import_transfer_links`, `import_created_account_links`, `import_created_category_links`, `import_set_aside_rows`. Longest identifier is `ux_mapping_profile_rules_profile_source_category` at 48 characters, inside PostgreSQL's 63-character limit |
| **`down.sql` reversibility** | Counted per pair: 8 `CREATE TABLE` / 8 `DROP TABLE`, 7 `CREATE INDEX` / 7 `DROP INDEX`. `20261018190000_create_mapping_profiles` is the one pair with 0/0 indexes — its only index is PostgreSQL's backing index for a `UNIQUE` constraint, which is dropped with the table, and the `.down.sql` says so. Drop order verified by script in all 8 down files: no `DROP INDEX` appears after a `DROP TABLE` |
| **FK indexes** | 12 `FOREIGN KEY` clauses, 12 covered. `mapping_profile_rules.mapping_profile_id` → `ux_mapping_profile_rules_profile_source_category` (leads) · `.target_category_id` → `idx_mapping_profile_rules_target_category_id` · `import_runs.account_id` → `idx_import_runs_account_id` · `import_run_id` on all five child tables → its own `idx_*_import_run_id` · `transaction_id`, `transfer_id`, `account_id`, `category_id` on the four link tables → their `ux_*` unique constraints, which PostgreSQL backs with an index. Each of the four cascading foreign keys therefore has the index its `ON DELETE CASCADE` needs |
| **Forbidden features** | `grep` over all 16 files: 0 `CHECK (`, 0 `CREATE TRIGGER`, 0 `CREATE FUNCTION` / `CREATE PROCEDURE`. Every `DEFAULT` in the spec is `DEFAULT now()` — one distinct value across all files, on `created_at` only. No business literal default anywhere. The two rules a reader might expect as a `CHECK` — `target_category_id` set exactly when `target = 'Category'`, and a `HistoryMigration` run having no `account_id` — are commented in place as domain invariants |

`erDiagram` validated by the structural lint; `mmdc` is not installed on this machine. Checks run:
2 fence markers (even, 1 `mermaid` block), first non-empty line is `erDiagram`, 12 relationships
using only `||--o{` and `||--o|`, all 12 entities in a relationship carry their own attribute block,
entity names are the uppercased table names, no two relationships share a pair, no `<placeholder>`
or `TBD` inside the block.

## 7. TBDs and follow-ups

**TBDs left in `data-model.md`** — all four in § Open items, none of them changing a column:

| Line | What | Who closes it |
|---|---|---|
| 570 | The `history` qualifier in `external_ref` is stable for one export, not across two different ones. PRD §1 records that the Owner holds three sources | Andrey, before implementation starts — depends on PRD §8's "does either file carry a stable operation number?" |
| 578 | PRD §8's file-size limit. Decision 10's bounds refuse a wrongly-*shaped* row; the abuse case is millions of correctly-shaped rows, which is a streaming ceiling, not a column | Andrey, before implementation starts |
| 584 | Whether a run's set-aside list is paged, and how it is ordered. `row_number` is the column it will order by | The API-contract stage |
| 588 | ADR-0002's proximity-tier parameters. They land in the snapshot query's `WHERE` clause (ADR-0003), not in a column | Andrey, before implementation starts |

**Follow-ups for the owner:**

1. ~~Reword `PRD.md` AC-01.~~ **Done 2026-10-18**, together with `sad.md` §5.
2. ~~Apply proposed edits 2–5 to `PRD.md` §8 and `sad.md` §8 and §11.~~ **Done 2026-10-18.** All
   three documents now agree on what is settled and on which feature owns the snapshot index.
3. **Add the two missing acceptance criteria** (findings 4 and 5): a movement belongs to at most one
   import run, and confirming two rules for one of the file's categories is refused. Both
   constraints are in the schema already; without the criteria, the refusal messages never get
   written and the Owner meets a raw database error. Owned by the API-contract stage.
4. **Teach `core-ledger`'s DeleteCategory use case about mapping rules** (finding 6). It already
   refuses when history names the category; it must also refuse when a `mapping_profile_rules` row
   targets it, with its own message. Owned by `implement-tasks` for `core-ledger`, and it has to be
   on the list before `csv-import` ships.
5. **At `implement-tasks`, put a comment pointing at ADR-0008 in both link-table entity
   configurations.** SAD §11 asks for it by name: the day a movement gains an editable field, the
   fingerprint field list has to grow with it, and AC-18 silently stops noticing edits otherwise.
6. **Re-check this spec against `core-ledger`'s real EF Core migration** once that feature is built.
   This delta was computed against a staged document, not a database, and the two diverge the moment
   `core-ledger` ships.
7. **Re-score RICE before scheduling** — carried from PRD §1 and SAD §11, not an architecture item,
   repeated here so it is not lost between documents.
