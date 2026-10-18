<!-- Written to docs/features/<slug>/_audit/data-model-<YYYY-MM-DD>.md.
     Seven sections, each answering a question a reviewer would otherwise have to ask. -->

# Data model audit — <slug>

**Date:** <YYYY-MM-DD> · **Stage:** 04 · **Baseline:** <EF Core migrations | earlier features'
staged specs — say which, and why>

## 1. Generated files

| File | What it is |
|---|---|
| `docs/features/<slug>/data-model.md` | The design: ER, decisions, entities, indexes, seeds, fixtures |
| `docs/features/<slug>/migrations/<timestamp>_<verb>_<entity>.up.sql` + `.down.sql` | Reference spec, one pair per table |

**These migrations are a specification, not something to run.** This repo's migrations are EF Core.
At `implement-tasks`: write the entities and their `IEntityTypeConfiguration<T>` files, run
`./build.sh migrate <Name>`, then diff `dotnet ef migrations script` output against the spec above.
A difference means the configuration or the spec is wrong — resolve it before applying.

The staged timestamps and EF's migration timestamp will not match, and EF may generate constraint
names in a different case. The diff is about columns, types, nullability, keys and indexes — not
about identifier casing.

## 2. Convention divergences

<!-- Where this pass departed from .claude/rules/migrations.md, from CLAUDE.md, or from an earlier
feature's shape. Also where a rule was applied at a real cost — name the cost, so the user can
decide whether to keep paying it. "None" is a valid section, written as one line. -->

### <N>. <The divergence, as a heading>

<What the rule says · what was done · why · what it costs · who decides.>

## 3. Drift findings

### Schema versus source

<Mapped `src/FinNavis.Domain` entities and `Persistence/Configurations` against the columns. Or:
skipped — the Domain project holds only `AssemblyMarker.cs`, so there is nothing to map against.
Say which, plainly. Any `_drift/*.sql` proposals go here.>

### Model versus spec, and constraints without an acceptance criterion

| # | Finding | Severity | Resolution |
|---|---|---|---|
| 1 | <Every SAD §5 aggregate has exactly one table; no orphan table> | — | Clean |
| 2 | **<A UNIQUE / NOT NULL no AC asks for.>** <Which AC implies it without requiring it> | Medium | Added anyway — <why the schema needs it>. **Needs an AC, or the constraint should be dropped.** Closed at <which stage> |
| 3 | <A table or column with no PRD origin> | — | Explained: its origin is ADR-<nnnn>, recorded in SAD §<n>. Not drift |

## 4. Open questions closed at this stage

| Question | Where it was open | Answer |
|---|---|---|
| <question> | PRD §8 / SAD §11 | <the answer, matching data-model.md §"Decisions taken"> |

**Upstream updated:** <yes — PRD §8 row removed and SAD §11 row rewritten, after confirmation | no
— left for the owner, see follow-ups>.

## 5. Breaking changes decomposed

<Any three-step expand → backfill → contract sequence, with the file names. Or: none — every table
is new, so there is no rename, drop or re-type.>

## 6. Self-check — <n>/4

| Check | Result |
|---|---|
| **Naming** | <evidence: how many files match the pattern, all tables plural snake_case> |
| **`down.sql` reversibility** | <evidence: every CREATE has its DROP, counts match per file, drops ordered indexes-before-table> |
| **FK indexes** | <evidence: each FK column named, and the index where it leads> |
| **Forbidden features** | <evidence: no `CHECK (`, no `CREATE TRIGGER`, no stored procedure, the only `DEFAULT` is `now()`> |

<A tick is not evidence. Name what was counted.>

`erDiagram` validated by <`mmdc` | the structural lint, `mmdc` is not installed on this machine>.

## 7. TBDs and follow-ups

**TBDs left in `data-model.md`:** <each with its line number, and which stage owns it.>

**Follow-ups for the owner:**

1. <Actionable by a person, naming the document or stage that closes it.>
