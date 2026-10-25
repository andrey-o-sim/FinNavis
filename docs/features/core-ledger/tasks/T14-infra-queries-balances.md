---
id: T14
title: "Implement the balance query and the two delete existence checks"
layer: "infra"
deps: ["T12", "T10"]
acs: ["AC-09", "AC-12", "AC-14", "AC-15"]
files_hint: ["src/FinNavis.Infrastructure/Queries/LedgerBalanceQueries.cs", "tests/FinNavis.Infrastructure.IntegrationTests/Queries/BalanceQueryTests.cs"]
owner: "Andrey Simonenko"
estimate: "M"
status: "todo"
---

# T14 — Implement the balance query and the two delete existence checks

## Why

Derives from [PRD §AC-09, §AC-12, §AC-14, §AC-15](../PRD.md),
[sad.md §6 flows 5, 6 and 7](../sad.md), ADR-0002 and ADR-0004.
[sad.md §11](../sad.md) flags this as a **Medium risk**: balance correctness lives in SQL across
two tables rather than in the shape of the data.

## What

- **Balance per account** — a three-part sum (ADR-0001): transactions on the account, minus
  transfers out of it, plus transfers into it. Transactions join `categories` to learn whether a
  row adds or subtracts, because `amount` is always positive and direction comes from the
  category's kind ([`data-model.md` decision 3](../data-model.md)).
- Nothing is stored and nothing is cached (ADR-0002). This is what makes AC-21 and AC-22 free —
  an edited or deleted entry has no stored total to correct.
- **Two existence checks**: does any transaction or transfer name this account (AC-09, flow 5), and
  does any transaction name this category (AC-12, flow 6). They only ask whether a row exists, so
  neither needs to aggregate.

## Definition of Done

- [ ] `Balance_AccountWithExpensesIncomeAndTransfers_EqualsSumOfMovements` passes against
      PostgreSQL 18 in Testcontainers (AC-14)
- [ ] `Balance_TransferBetweenOwnAccounts_TotalUnchanged` passes (AC-15)
- [ ] `AnyMovementNamesAccount` is true for an account with a transaction, true for one with only
      a transfer on either side, false otherwise (AC-09)
- [ ] `AnyTransactionNamesCategory` behaves the same way for categories (AC-12)
- [ ] the queries use `idx_transactions_account_id`, `idx_transfers_source_account_id`,
      `idx_transfers_destination_account_id` and `idx_transactions_category_id`
- [ ] `./build.sh test` is green

## Notes

- **No entity is loaded into memory here** (sad.md §6 flows 5, 6, 7): aggregation runs in
  PostgreSQL and only result rows travel back. That is the whole point of ADR-0004.
- The property-based proof of AC-14 across 1 000 randomised sequences is
  [T31](./T31-tests-qg1-balance-property.md), not here. This task proves the query on fixed data.
