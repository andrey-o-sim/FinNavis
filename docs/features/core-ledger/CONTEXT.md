---
status: Living
feature: "core-ledger"
updated_at: "2026-10-25"
---

# Domain Context — core-ledger

<!--
This file is core-ledger's slice of the domain. It does not define terms — docs/GLOSSARY.md
does, and it is canonical. Here we name the terms this feature works with and record what
holds for this feature alone.
-->

## Glossary

Terms are defined in [docs/GLOSSARY.md](../../GLOSSARY.md), which is canonical — if any
document in this folder contradicts it, the glossary wins. Never copy a definition here.

**Terms this feature uses:** Account, Balance, Breakdown, Category, Category Group,
Default group, History, Ledger, Movement, Owner, Transaction, Transfer.

<!-- Duplicate, Export, Import run, Mapping profile, Set-aside row, Shape and Statement are
     glossary terms this feature does not work with. They appear in some of its ADRs only as
     forward-looking reasons for a decision, and belong to csv-import. -->

## Invariants

- An account's balance always equals the sum of every transaction and transfer recorded
  against it, after any sequence of recordings, edits and deletions (AC-14).
- A transfer always touches two different accounts, and is never applied to one side only —
  either both accounts move or neither does (AC-05, AC-06).
- A transfer never carries a category and is never counted as spending or as earning
  (AC-20, AC-23).
- Moving money between the owner's own accounts never changes the total across all accounts
  (AC-04, AC-15).
- A recorded entry can never change kind. A transaction never becomes a transfer, and the
  owner must remove it and record it again as the other kind (AC-24).
- An account or a category that recorded history refers to can never be deleted (AC-09, AC-12).
- A category group and every category inside it always sit on the same side of the ledger,
  spending or earning (AC-30).
- The group totals of a period, taken together with the Default group total, always equal the
  period total — on the spending side and on the earning side separately (AC-19).
- Renaming never changes a recorded figure. An account, category or group keeps every movement
  and every closed-period figure it had under the old name (AC-08, AC-28, AC-34).
- Nothing is ever created implicitly. Naming an account, category or group the ledger does not
  hold refuses the whole change (AC-03, AC-25, AC-32).
- A name is unique within the scope that owns it: account names across all accounts, category
  and group names per side of the ledger, so the same word may name a spending thing and an
  earning thing at once (AC-31, AC-35, AC-36).

## Sentinel errors

Error codes are not defined here. This feature's codes live in
[`contracts/openapi.yaml`](./contracts/openapi.yaml) and in the error registry in code —
one contract code equals one constant returning the same string. Spelling is
`Module.ErrorName` in PascalCase ([`docs/adr/0003`](../../adr/0003-spell-an-error-code-in-pascalcase.md));
the envelope is `{code, message, details?}`
([`docs/adr/0002`](../../adr/0002-return-a-flat-error-envelope-instead-of-problem-details.md)).

**Modules this feature refuses under:** `Account`, `Breakdown`, `Category`, `CategoryGroup`,
`History`, `Transaction`, `Transfer`.

## Org-filter invariant

Not applicable. The product has one `Owner`, no org and no sign-in, so no record carries an
owner or tenant column and no query filters by one. A `404` here is a plain "not found", not
existence-hiding — there is nobody to hide from (`.claude/rules/openapi.md` §1).

This section becomes live when the sign-in feature lands and adds a second actor. PRD §5 marks
the `authorization` coverage type N/A for the same reason, and §8 tracks it as a gap.

## Out of scope

Drawn from PRD §3 Non-goals.

- No web client, installable app or offline reading · this feature ships programmatic access
  only; the client is a later feature that consumes what is built here.
- No sign-in and no OAuth · one owner, one dataset, so records carry no owner field.
- No import from a file and no period-scoped export · each depends on this ledger existing
  first. Loading a file is `csv-import`.
- No charts, statistics, monthly summaries or category icons · the breakdown returns figures;
  presenting them is the client's job.
- No multi-currency, bank integrations, budgets or forecasts · single currency and manual
  entry, per idea-brief §5.
