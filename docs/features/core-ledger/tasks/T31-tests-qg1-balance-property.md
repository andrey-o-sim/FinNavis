---
id: T31
title: "Prove QG-1: the balance invariant over 1 000 randomised operation sequences"
layer: "tests"
deps: ["T25", "T28", "T29"]
acs: ["AC-14", "AC-15", "AC-21", "AC-22"]
files_hint: ["tests/FinNavis.Infrastructure.IntegrationTests/Quality/BalanceInvariantTests.cs"]
owner: "Andrey Simonenko"
estimate: "L"
status: "todo"
---

# T31 — Prove QG-1: the balance invariant over 1 000 randomised operation sequences

## Why

[sad.md §10 QG-1](../sad.md) and [PRD §6](../PRD.md): **0 balance discrepancies across at least
1 000 randomised sequences** of recordings, edits and deletions. This is quality goal 1 and the
only hard number the PRD gives for correctness.

[sad.md §11](../sad.md) explains why it cannot be a unit test: ADR-0001 made balance a three-part
sum across two tables, *"so the thing most likely to be wrong is the SQL, and an in-memory double
would never exercise it."*

## What

- `BalanceInvariant_AnyOperationSequence_MatchesSumOfMovements` — generate random sequences of
  recordings, edits and deletions across several accounts and categories, apply them through the
  real use cases, and compare the computed balance against an independently accumulated expected
  value.
- **Runs against PostgreSQL 18 in Testcontainers, not against a test double** (sad.md §10).
- The full 1 000 sequences run in CI; a smaller sample is enough locally.
- Also assert the total across all accounts is unchanged by any transfer (AC-15).

## Definition of Done

- [ ] the test generates at least 1 000 randomised sequences in CI and reports **0** discrepancies
      (AC-14)
- [ ] every sequence mixes recordings, edits and deletions of both transactions and transfers
      (AC-21, AC-22)
- [ ] `Total_AfterAnyNumberOfTransfers_Unchanged` passes (AC-15)
- [ ] a failing seed is printed, so a discrepancy can be reproduced rather than re-hunted
- [ ] the local run uses a smaller sample and finishes fast enough to run on every commit
- [ ] it runs against the Testcontainers fixture, never an in-memory provider

## Notes

- The expected value must be accumulated **independently** of the production query, or the test
  proves nothing. Do not reuse `ILedgerQueries` to compute the expectation.
- This is the test the Medium risk in [sad.md §11](../sad.md) is mitigated by. If it is weakened,
  the risk is no longer mitigated — say so rather than quietly lowering the count.
