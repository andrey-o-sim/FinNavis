# Drift and the audit report

Used in Phase 7, and the whole of the run when `--drift-only` is passed.

Drift is run in three directions. The first two are the usual ones. The third is the one that finds
real gaps in this project, because a schema is where an unstated business rule finally has to be
written down.

## Direction 1 — schema versus source

Map each field on a `src/FinNavis.Domain` entity and each line in
`src/FinNavis.Infrastructure/Persistence/Configurations` to a column. Report:

- a field with no column,
- a column with no field,
- a type mismatch,
- a nullability mismatch.

For each, propose a fix under `docs/features/<slug>/migrations/_drift/` — staged, same discipline as
the rest.

**When Domain holds only `AssemblyMarker.cs`, this check has no input.** Say so plainly and move on:
"skipped — the Domain project is a skeleton, so there is nothing to map against." That is the
expected answer until the first feature is actually built, and reporting it as skipped is more
useful than silence, which reads as "ran and found nothing".

## Direction 2 — model versus spec

Cross-check `data-model.md` against PRD §4 / §5 and SAD §5:

- an entity in the PRD or SAD with no table,
- a table with no PRD or SAD origin.

A table with no origin is not automatically wrong. `transactions.external_ref` has no PRD origin at
all — its origin is an ADR, recorded in the SAD as deliberately unused debt. The finding is
"explained", not "clean" and not "drift": say which artefact explains it.

## Direction 3 — a constraint no acceptance criterion asks for

The one that matters most here, and the one a generic version of this stage does not look for.

Walk every `UNIQUE`, every `NOT NULL` on a column the PRD never mentions, and every FK that encodes
a rule rather than a structure. For each, find the acceptance criterion that requires it.

Often there is none, and the constraint is still right. An AC phrased *"Given the owner has no
account by that name"* implies a name identifies an account — but implying is not requiring, and
nothing in §5 refuses a duplicate. Two accounts called "Cash" make a ledger unusable, so the
constraint belongs in the schema.

The honest outcome is both: **add the constraint, and record the finding** at medium severity, with
what the PRD would have to say. The next stage that owns refusal wording is where it gets closed.
Adding it silently means the refusal message for a duplicate name is never written, and the owner
meets a raw database error instead.

## Open questions due at this stage

Separate from drift, and easy to forget.

Read PRD §8 and SAD §11 for questions whose owner or due date points at this stage — note lengths,
date semantics, whether a value is bounded. Those are schema questions, and this document is where
they get answered.

Having answered one, the upstream document still reads as open. Show the user the exact edit for
PRD §8 and SAD §11 — the row to remove or rewrite — and apply it after a yes. Two documents that
disagree about what is settled is how a closed question gets reopened.

List each closed question in the audit with its answer, even after the upstream edit. The audit is
the record of what this pass decided.

## The audit report

`docs/features/<slug>/_audit/data-model-<date>.md`, from
[`../templates/audit-report.md`](../templates/audit-report.md). Seven sections, each one answering a
question a reviewer would otherwise have to ask:

1. **Generated files** — every path, and plainly: the SQL is a reference spec, EF Core produces the
   real migration at `implement-tasks`, where the two are diffed. Also which baseline the delta was
   computed against — the EF migrations or the earlier features' staged specs.
2. **Convention divergences** — where this pass departed from `.claude/rules/migrations.md` or from
   an earlier feature's shape, and why. Including the rules applied at a cost: three migrations
   instead of one for a new NOT NULL, `CONCURRENTLY` outside a transaction, both for a database
   nobody has deployed.
3. **Drift findings** — the three directions above, as a table with severity and resolution.
4. **Open questions closed** — question and answer, plus whether PRD §8 and SAD §11 were updated.
5. **Breaking changes decomposed** — any three-step sequence, or "none, every table is new".
6. **Self-check** — the four checks with evidence, not just a tick. "Every `CREATE TABLE` has a
   `DROP TABLE`; counts match per file" is evidence. "Pass" is not.
7. **Follow-ups for the owner** — what is left, each one actionable by a person, each naming the
   stage or document that closes it.

Every `<!-- TBD -->` left in `data-model.md` is listed with its line. A TBD that is only in the
document is a TBD nobody finds again.
