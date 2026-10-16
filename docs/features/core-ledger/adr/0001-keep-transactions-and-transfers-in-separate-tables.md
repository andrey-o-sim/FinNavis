---
status: Accepted
owner: "Andrey Simonenko"
reviewers: ["Andrey Simonenko"]
updated_at: "2026-10-16"
feature_size: M
stage: "04-05"
ticket: "none"
---

# 0001 — Keep transactions and transfers in separate tables

- **Status:** Accepted
- **Date:** 2026-10-16
- **Deciders:** Andrey Simonenko

## Context

The ledger records two kinds of money movement. A transaction is a single income or expense on
exactly one account and always carries a category. A transfer moves money between two of the
owner's own accounts, touches two accounts, carries no category, and leaves the owner's total
unchanged (CONTEXT.md). Both kinds show up together in the same reads — the account balance
(AC-14), the filtered history (AC-16) and the period breakdown (AC-18, AC-20).

How the two kinds are stored sets the schema, every read query, and what the later csv-import
feature inherits. It has to be settled before the data model can be designed.

## Decision drivers

- Quality goal 1 — ledger correctness: a balance always equals the sum of the movements recorded on
  the account, under any order of recording, editing and deleting (PRD §6, 0 discrepancies across
  at least 1 000 randomised sequences).
- AC-15 — a transfer between the owner's own accounts leaves the total across all accounts unchanged.
- AC-20 — no transfer is ever counted as spending or as earning in a period breakdown.
- PRD §6 — period breakdown p95 ≤ 500 ms over ~20 000 movements; history page p95 ≤ 400 ms.
- Repo convention (CLAUDE.md): PostgreSQL through EF Core, one entity configuration per entity.
- Transfers are rare in this ledger compared with transactions, and may be withdrawn entirely at
  some later point.

## Considered options

1. **One `movements` table with a kind column** — a transfer is a single row carrying both
   `account_id` and `counter_account_id`; a transaction leaves the counter-account null.
2. **Two separate tables, `transactions` and `transfers`** — each table holds only the columns its
   kind needs, with no discriminator and no nullable counter-account.
3. **Double-entry lines** — a `movements` row is the business event and owns one or more `entries`
   rows of (account, signed amount); a transaction has one entry, a transfer has two that sum to
   zero.

## Decision outcome

**Chosen: option 2, two separate tables.**

Each table holds exactly the columns its kind needs, so neither carries a nullable counter-account
nor a discriminator, and the two concepts stay as distinct in the schema as they are in the
glossary. Option 3 would have made balance a single indexed `SUM` and made AC-15 structurally true,
which is the strongest argument against this choice and is recorded honestly below. It was not
taken because transfers are rare here, so the correctness pressure option 3 solves is low in
practice, while its costs — an extra table, an extra concept, a join and a fold on every history
read, and a translation layer between the API's "expense of 500" and a signed line — are paid on
every feature forever.

Option 2 also keeps a door open that options 1 and 3 close: if transfers are later withdrawn from
the product, removing them is one migration against one table, rather than a rewrite of the table
that holds everything.

Option 1 was rejected because its balance query matches on `account_id OR counter_account_id`,
which PostgreSQL cannot serve from a single index, and because the sign of each row then lives in
a `CASE` ladder inside the query rather than in the model.

## Consequences

**Positive**
- AC-20 holds structurally: the breakdown reads `transactions` only and never joins `transfers`, so
  a transfer cannot leak into a category or group total.
- No nullable counter-account and no kind discriminator — each table is readable on its own, and
  invalid states such as "a transfer with a category" are not representable in the transfers table.
- Dropping transfers later costs one migration against one table.
- Each table gets its own indexes tuned to its own queries.

**Negative**
- Account balance is three sums added together: transactions on the account, minus transfers out of
  it, plus transfers into it. AC-14 and AC-15 are therefore enforced by application code and tests,
  not by the shape of the data. This is the main risk the property-based test in §10 exists to cover.
- The filtered history (AC-16) is a `UNION ALL` of two different row shapes, which makes paging over
  the combined stream harder than paging one table.
- Every future reader — csv-import, export, the PWA — has to know both tables exist. That cost grows
  with each new reader.

**Neutral**
- Moving to double-entry later is possible but is a full schema migration plus a rewrite of every
  read path. It is not a change that can be made incrementally.
- Both tables are written through their own aggregate, so transfer atomicity (AC-06) comes from a
  single `SaveChanges` on one aggregate rather than from a cross-table transaction.

## Links

- PRD: [[../PRD.md]] — US-01, US-02, AC-04, AC-06, AC-14, AC-15, AC-16, AC-20
- SAD: [[../sad.md]] §4
- Related ADR: [[0002-compute-every-read-on-demand]] · [[0003-edit-and-delete-ledger-rows-in-place]]
