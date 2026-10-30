---
id: T04
title: "Decide by ADR what the module names in a *.NotFound error code"
layer: "docs"
deps: []
acs: []
files_hint: ["docs/adr/", "docs/features/core-ledger/contracts/openapi.yaml", "docs/features/core-ledger/contracts/api-sync-report.md", "docs/features/csv-import/contracts/openapi.yaml"]
owner: "Andrey Simonenko"
estimate: "S"
status: "done"
attempts: 1
---

# T04 — Decide by ADR what the module names in a `*.NotFound` error code

## Why

[`contracts/api-sync-report.md`](../contracts/api-sync-report.md) Section D parks this as the one
finding run 3 opened and could not close: four codes advertise *"No account in the ledger has
id …"* and two advertise *"No category …"*, and nothing says whether the module names the aggregate
that was **not found** or the aggregate being **operated on**.

It has to be settled before [T24](./T24-ports-error-envelope-and-registry.md) writes the registry.
The registry hard-codes all 27 strings; deciding after it means rewriting them, plus every endpoint
test that asserts a code.

## What

- A **project-wide** ADR under `docs/adr/`, following the MADR shape of
  `docs/adr/0003-spell-an-error-code-in-pascalcase.md`. It amends ADR-0003 or supersedes the
  relevant clause — state which.
- Apply the decision to both contracts in one change: `core-ledger` and `csv-import`. The sync
  report is explicit that the convention binds `csv-import` too, and that the repo must never carry
  two conventions.
- Re-run `--reconcile` on both so each `api-sync-report.md` reflects the new state.

## Definition of Done

- [x] the ADR states the rule in one sentence, with the honest cost of the option not taken
- [x] every `*.NotFound` code in both contracts matches the rule
- [x] both contracts still satisfy the `Error.code` pattern `^[A-Z][A-Za-z0-9]*\.[A-Z][A-Za-z0-9]*$`
- [x] `core-ledger/contracts/api-sync-report.md` no longer lists the convention under Section D
- [x] `CLAUDE.md` §API and `.claude/rules/openapi.md` §7 are updated if the rule changes what they
      say

## Notes

- Documentation only. No code is written here, which is why it can start in parallel with every
  domain task.
- `setCategoryGroup` is the one operation whose `404` carries two different refusals. Whatever the
  ADR decides must still let that operation tell them apart.
