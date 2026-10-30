---
status: Accepted
owner: "Andrey Simonenko"
reviewers: ["Andrey Simonenko"]
updated_at: "2026-10-30"
scope: project-wide
ticket: "none"
---

# 0004 — The module of a `*.NotFound` code names the aggregate that was not found

- **Status:** Accepted
- **Date:** 2026-10-30
- **Deciders:** Andrey Simonenko
- **Scope:** project-wide — every HTTP surface in this repository, both features

## Context

`docs/adr/0003` fixed the **spelling** of an error code: `Module.ErrorName` in PascalCase. It did not
say what the module is when the refusal is "not found". Two readings were both in use, and nothing
chose between them:

- the module names the aggregate that was **not found** — `CategoryGroup.NotFound`;
- the module names the aggregate the request was **operating on** — `Transaction.AccountNotFound`.

`core-ledger/contracts/api-sync-report.md` Section D parks this as the one finding run 3 opened and
could not close. It has to be settled before `T24` writes the error registry, because that task
hard-codes every string; deciding afterwards means rewriting the registry and every endpoint test
that asserts a code.

What the two readings produce today. Six codes carry one of two identical messages:

| the one refusal | codes advertising it |
|---|---|
| *"No account in the ledger has id …"* | `Account.NotFound`, `Transaction.AccountNotFound`, `Transfer.SourceAccountNotFound`, `Transfer.DestinationAccountNotFound` |
| *"No category in the ledger has id …"* | `Category.NotFound`, `Transaction.CategoryNotFound` |

`csv-import` has the same shape in `Import.AccountNotFound` and `MappingProfile.CategoryNotFound`,
which is why this is a project-wide record and not a contract edit inside one feature.

One decision is already on the books and leans one way. On 2026-10-22 run 3 of
`/finnavis-api-forge core-ledger --reconcile` merged `Category.GroupNotFound` into
`CategoryGroup.NotFound` and wrote the principle into the `Error` schema of the committed contract:
*the module names the aggregate that was not found, not the aggregate the request was operating on.*
That sentence is a statement about one pair of codes. This record decides whether it is the rule.

Nothing is implemented. `src/FinNavis.Presentation/Endpoints/` holds a `.gitkeep`, and no registry
exists. As with ADR-0002 and ADR-0003, this is the cheapest moment the decision will ever have.

## Decision drivers

- **One refusal, one code.** `.claude/rules/openapi.md` already forbids two codes for one refusal,
  because the second one is a branch that exists only to be forgotten. Four codes for *"no account
  with that id"* break that rule four ways.
- **The code says what failed; the operation the client called says where.** A client always knows
  which request it sent. It does not always know which of several ids in that request was wrong, so
  that is the part the code has to carry.
- **The repository must not carry two conventions.** `core-ledger` and `csv-import` move in one
  change or a reader has to learn which feature they are in before reading a code.
- **A rule with no exception is a rule a generator can apply.** `finnavis-api-forge` derives codes
  from foreign keys; the derivation has to be mechanical or the next feature drifts.
- **Reversing the `CategoryGroup.NotFound` merge has a price.** It is committed, it is written into
  the contract's `Error` schema, and undoing it brings back a duplicate the previous gate removed on
  four stated reasons.

## Considered options

1. **The module names the aggregate that was not found.** `Transaction.AccountNotFound` becomes
   `Account.NotFound`.
2. **The module names the aggregate being operated on.** `CategoryGroup.NotFound` goes back to
   `Category.GroupNotFound` at the sites that reference a group.
3. **Option 1, plus the role in `details`.** One code `Account.NotFound` everywhere, and a transfer
   says which side failed through `details`.

## Decision outcome

**Chosen: option 1, with one clause for the case option 1 alone cannot express.**

> In a `*.NotFound` code the module is the aggregate that was **not found**, never the aggregate the
> request was operating on; the error name is `NotFound`, and it carries a role qualifier only when
> one request can fail to find that same aggregate in more than one place.

The qualifier is derived from the request field, not invented: drop `_id`, drop the aggregate name,
PascalCase what is left, and put it in front of `NotFound`.

| field in the request | code |
|---|---|
| `account_id` | `Account.NotFound` |
| `category_id` | `Category.NotFound` |
| `category_group_id` | `CategoryGroup.NotFound` |
| `source_account_id` | `Account.SourceNotFound` |
| `destination_account_id` | `Account.DestinationNotFound` |

`setCategoryGroup` is the one operation whose `404` carries two different refusals, and it still
tells them apart: `Category.NotFound` for the entry being moved, `CategoryGroup.NotFound` for the
target group. Two aggregates, two modules, no qualifier needed.

**Option 2 was the real alternative and its cost is not small.** It reads better operation by
operation — `Transaction.AccountNotFound` says, in one string, both what the client was doing and
what went wrong, and the transfer pair needs no special clause at all. The honest cost of taking it:
it reverses a decision made eleven days ago and already committed, so `Category.GroupNotFound` comes
back alongside `CategoryGroup.NotFound` and the duplicate the last gate removed is a duplicate
again; it leaves four codes and two features advertising the same sentence, so a client that wants
to ask "was it a missing account?" has to know every operation's prefix; and it makes the code
depend on the caller rather than on what actually failed, which means adding an operation that
references an account adds a code instead of reusing one. Option 1's own cost is paid once, below.

**Option 3 is rejected for one concrete reason.** `CLAUDE.md` §API reserves `details` for
field-level validation. A transfer with an unknown source account is not a validation failure of a
field's shape — the id is a well-formed UUID — so putting "which side" there overloads `details`
with a second meaning, and a client would have to read two levels of the body to decide which
picker to flag. The role is part of which refusal happened, so it belongs in the code.

## The mapping, in full

Rule: the referenced aggregate becomes the module, the role qualifier survives only where one
request names the same aggregate twice.

**`core-ledger` — 27 codes become 25**

| today | becomes | why |
|---|---|---|
| `Transaction.AccountNotFound` | `Account.NotFound` | merged — same refusal, same sentence |
| `Transaction.CategoryNotFound` | `Category.NotFound` | merged — same refusal, same sentence |
| `Transfer.SourceAccountNotFound` | `Account.SourceNotFound` | renamed — the role stays, the module moves |
| `Transfer.DestinationAccountNotFound` | `Account.DestinationNotFound` | renamed — same |

The five codes that already followed the rule are untouched: `Account.NotFound`, `Category.NotFound`,
`CategoryGroup.NotFound`, `Transaction.NotFound`, `Transfer.NotFound`. Every code that is not a
`*.NotFound` is out of scope and unchanged.

**`csv-import` — 12 codes stay 12, three are re-spelled**

| today | becomes | why |
|---|---|---|
| `Import.AccountNotFound` | `Account.NotFound` | the same code `core-ledger` returns |
| `MappingProfile.CategoryNotFound` | `Category.NotFound` | the same code `core-ledger` returns |
| `Import.RunNotFound` | `Import.NotFound` | the import run **is** the `Import` aggregate, so the name repeats it |

Repo-wide the distinct count drops from 39 to 35: `Account.NotFound` and `Category.NotFound` are now
one code each, returned by both features.

**A shared code does not mean a shared sentence.** `core-ledger` answers *"No account in the ledger
has id …"*; `csv-import` answers *"You do not have an account with that name, so there was nothing
to load this file onto…"*, which is its own PRD §10 wording. Both are the same refusal — the
account named by an id in the request is not in the ledger, `404`, nothing written — so they share
the `code` a client switches on, and each keeps the `message` its acceptance criterion gives the
owner. That split is what the envelope is for.

The nine modules of `.claude/rules/openapi.md` §7 are unchanged — `Account`, `Breakdown`, `Category`,
`CategoryGroup`, `History`, `Import`, `MappingProfile`, `Transaction`, `Transfer`. No module is added
and none falls out of use. The `Error.code` pattern `^[A-Z][A-Za-z0-9]*\.[A-Z][A-Za-z0-9]*$` is
unchanged and every new string matches it.

## What has to change, in one change

| # | file | what |
|---|---|---|
| 1 | `docs/adr/0004` | this record |
| 2 | `core-ledger/contracts/openapi.yaml` | 4 codes, the `Error` schema description, the header note |
| 3 | `core-ledger/contracts/api-sync-report.md` | the code table, Section D, the run header |
| 4 | `csv-import/contracts/openapi.yaml` | 3 codes, the `Error` schema description, the header note |
| 5 | `csv-import/contracts/api-sync-report.md` | the code table and the run header |
| 6 | `CLAUDE.md` §API | the worked example named a code that no longer exists |
| 7 | `.claude/rules/openapi.md` | the Hard rules example and §7's foreign-key derivation |
| 8 | `.claude/skills/finnavis-api-forge/references/error-catalog.md` | the derivation rule and its examples |
| 9 | both features' `tasks/T*.md` that quote a renamed code | so the implementing task does not write the superseded string |

Row 8 is the one ADR-0003 learned the hard way: the skill reference is what a **first** run on a new
feature reads, so leaving the old derivation there would produce a contract in the old convention
next week while every other file said otherwise.

## Consequences

**Positive**
- Four codes for one refusal become one. A client switching on `code` has one branch to write for
  "no account with that id", in either feature.
- The registry gets shorter and the shared refusals get one constant each. `AccountErrors.NotFound`
  returns `"Account.NotFound"` for `core-ledger` and for `csv-import`.
- The derivation is mechanical, so `finnavis-api-forge` can apply it without a judgement call: the
  foreign key names the module.
- The `CategoryGroup.NotFound` merge stops being a one-off and becomes an instance of the rule.

**Negative**
- **A code no longer says which operation refused.** `Account.NotFound` out of context does not say
  whether a transaction or a transfer was being recorded. The operation the client called says it,
  and the `message` says it to the owner, but the string alone says less than it did.
- **`Account.SourceNotFound` puts a transfer word on the `Account` module.** "Source" means nothing
  to an account on its own; it is the slot in the transfer request. This is the clause that keeps the
  rule total, and it is the ugliest part of it.
- **Seven strings move across two features**, plus the task files that quote them. Cheap now because
  nothing returns them; a breaking change for every error response once the PWA switches on them.
- **A second gate decision is revisited.** Run 3's merge was stated as a finding about one pair; this
  record promotes it to a rule, which widens its blast radius after the fact.

**Neutral**
- JSON keys stay snake_case, the envelope shape is unchanged, and no status code moves. ADR-0002 and
  ADR-0003 stand in full — this record fills a gap ADR-0003 left open rather than superseding any
  clause of it.
- No code changes. `T24` builds the registry from the contracts' lists, which now carry the new
  strings.

## Links

- Fills a gap left open by: [[0003-spell-an-error-code-in-pascalcase]]
- Envelope: [[0002-return-a-flat-error-envelope-instead-of-problem-details]]
- Rules: [[../../.claude/rules/openapi.md]] §7 · [[../../CLAUDE.md]] §API
- Contracts: [[../features/core-ledger/contracts/openapi.yaml]] · [[../features/csv-import/contracts/openapi.yaml]]
- Reports: [[../features/core-ledger/contracts/api-sync-report.md]] Section D · [[../features/csv-import/contracts/api-sync-report.md]]
