---
status: Accepted
owner: "Andrey Simonenko"
reviewers: ["Andrey Simonenko"]
updated_at: "2026-10-22"
scope: project-wide
ticket: "none"
---

# 0003 — Spell an error code in PascalCase as `Module.ErrorName`

- **Status:** Accepted
- **Date:** 2026-10-22
- **Deciders:** Andrey Simonenko
- **Scope:** project-wide — every HTTP surface in this repository, not one feature

## Context

`docs/adr/0002` settled the **shape** of an error response: the flat envelope
`{code, message, details?}`, not RFC 9457 Problem Details. In passing it restated the **spelling**
of `code`, inherited from `.claude/rules/openapi.md`: `module.error_name` in snake_case, for example
`transaction.category_not_found`.

The spelling was never the question in that record. It arrived as an input and was carried forward
unexamined. This record examines it, after the owner asked where that format comes from.

**It does not come from this stack.** Line 16 of `.claude/rules/openapi.md` sits in the
`# API contract rules — baseline` section — the generic part of that file, written for any
repository, above the `## FinNavis deviations` section. Neither `CLAUDE.md` nor any `sad.md` derived
it from .NET; they repeated it.

**Dotted snake_case is a translation-key format.** It is what Laravel (`validation.required`,
`auth.failed`), Symfony and Rails i18n use for message keys. The REST APIs usually cited for
snake_case error codes — Stripe, Django — are **flat**: `card_declined`, `resource_missing`. They
group with a separate `type` field, not with a dot inside `code`.

What .NET-shaped APIs put in that field:

| API | format | examples |
|---|---|---|
| ASP.NET Core Identity (`IdentityError.Code`) | one PascalCase token | `DuplicateUserName`, `PasswordTooShort` |
| Azure Storage | one PascalCase token | `BlobNotFound`, `ContainerAlreadyExists` |
| Azure Resource Manager | one PascalCase token | `ResourceGroupNotFound`, `InvalidTemplate` |
| Microsoft Graph | one camelCase token | `itemNotFound`, `accessDenied` |
| ASP.NET Core `ProblemDetails` | no `code` field at all | `type` is a URI |

Two things follow. There is **no .NET rule governing a string value inside a JSON body** — the .NET
naming guidelines are about identifiers in code, not payload content. And **no widely used API
combines a dot with PascalCase**, which is what this record chooses: the case has a precedent, the
separator has one, the combination does not.

What exists in the repository today:

| where | what it says |
|---|---|
| `CLAUDE.md` §API, line 147 | the rule, with two examples |
| `.claude/rules/openapi.md`, Hard rules, line 16 | the same rule (baseline section) |
| `docs/adr/0002`, line 23 | restates it as an input |
| `core-ledger/sad.md` §8, Error handling row | "`code` is `module.error_name` in snake_case" |
| `csv-import/sad.md` §8, Error handling row | the envelope, citing ADR-0002 — does not name the case |
| `core-ledger/contracts/openapi.yaml` | **28 distinct codes** + `pattern: "^[a-z_]+\\.[a-z_]+$"` |
| `csv-import/contracts/openapi.yaml` | **12 distinct codes** + the same pattern |
| `.claude/skills/finnavis-api-forge/references/error-catalog.md` | nine examples and the derivation table |

No `PRD.md` and no `data-model.md` names a single code. `csv-import/PRD.md` §9 says so outright:
"the machine names for the refusals listed in §10" are left to the implementation stage. `src/` is a
skeleton — nothing parses these strings. As with ADR-0002, this is the cheapest moment the decision
will ever have.

## Decision drivers

- **A `code` is an identifier, not prose and not data.** The owner never reads it — `message` is for
  that — and a client only compares it against a constant. Its case is an identifier convention, not
  a JSON-body convention.
- **One `code` equals one definition in the repo** (`.claude/rules/openapi.md`). That definition is a
  C# constant. The closer the string is to the constant, the fewer places a typo can hide.
- **The module is worth keeping addressable.** It says which aggregate refused, and a dot leaves that
  readable to a person and splittable by a client.
- **JSON keys stay snake_case either way.** `CLAUDE.md` fixes the key convention; this record is
  about a *value*. `code`, `message`, `details` and every key inside `details` are untouched.
- **Nothing is implemented.** Changing 40 strings in two contracts is an edit. Changing them after
  handlers return them and a PWA switches on them is a migration of both sides.
- **Two conventions at once would be worse than either.** `core-ledger` shipped its contract at
  commit `2605a61`, so both features move in the same change.

## Considered options

1. **Keep dotted snake_case** — `transaction.category_not_found`. No change anywhere.
2. **Dotted PascalCase** — `Transaction.CategoryNotFound`.
3. **Mixed** — `transaction.CategoryNotFound`.
4. **One PascalCase token** — `TransactionCategoryNotFound`, as ASP.NET Core Identity and Azure do.

## Decision outcome

**Chosen: option 2, `Module.ErrorName` in PascalCase.** Decided by the owner on 2026-10-22, during
`/finnavis-api-forge csv-import --reconcile`, with option 4 considered and set aside on the way.

It keeps both halves of what the field has to do. The case makes the string an identifier, which is
what it is — never read by the owner, only compared against a constant. The dot keeps the module
addressable: a person sees which aggregate refused without parsing the name, and a client that ever
wants to group refusals by aggregate can still split on it.

The property that decides it against option 4 is the mapping to C#. A grouped registry lands on the
code exactly:

```csharp
public static class CategoryErrors
{
    public const string GroupNotFound = "Category.GroupNotFound";
}
```

The class is the module, the member is the name, and `nameof`-level discipline keeps the two sides
equal. Option 4 can also match a constant exactly, but only with a single flat `ErrorCodes` class
holding all 40 in one list — which is the shape that gets unreadable first.

**Option 4 was the strongest alternative and is recorded as such.** It is the only form with a
precedent inside this stack: ASP.NET Core Identity spells its codes exactly that way. It was set
aside for two concrete reasons rather than taste. It collapses `category.group_not_found` and
`category_group.not_found` onto the same token, and it drops the module as a separately readable
piece. The owner accepted that `Module.ErrorName` is an unusual combination — no API in the table
above uses it — and that the repository is choosing internal fit over recognisability. That is a
defensible trade in a product with one consumer, written by one person; it would be a weaker one in
a public API, which `PRD.md` §3 rules out today. **That is the condition that reopens this.**

Option 1 keeps the grain of a snake_case JSON document, and 40 strings stay put — a real argument,
and the cheapest option by a wide margin. It loses because it has no precedent in this stack either:
it is a PHP/Ruby translation-key format, so the choice was never "convention versus invention".

Option 3 asks a reader to hold two conventions inside one string and gives nothing back.

**This record supersedes the spelling clause only.** `docs/adr/0002` stands in full for the shape of
the envelope, the choice against Problem Details, the status-code classes, and the rule that a
finished batch is a success.

## A finding this record surfaced but does not resolve

While checking the 40 codes, two in `core-ledger` turned out to describe **one refusal under two
names**:

| code | used by |
|---|---|
| `category.group_not_found` → `Category.GroupNotFound` | `createCategory`, `setCategoryGroup` |
| `category_group.not_found` → `CategoryGroup.NotFound` | `listCategories`, `renameCategoryGroup`, `deleteCategoryGroup` |

Both carry the identical message — *"No category group in the ledger has id …"* — and both mean the
same thing. The module records which aggregate the request was operating on rather than what was
refused, which is how the duplication stayed invisible.

`.claude/rules/openapi.md` forbids this: one `code` per refusal, because two codes for one refusal
is how a client's `switch` goes stale. **Under this record they stay distinct and nothing breaks**,
so merging them is not part of this change. It belongs to `core-ledger`'s own
`--reconcile`, and the recommendation there is to keep `CategoryGroup.NotFound` and `$ref` it from
all five sites.

Recorded here so the finding is not lost between runs. No other pair collides, under either the
dotted or the single-token form.

## The mapping, in full

The whole migration, so the edit is mechanical. Rule: PascalCase the module, PascalCase the name,
keep the dot.

**`core-ledger` — 28**

| today | becomes |
|---|---|
| `account.duplicate_name` | `Account.DuplicateName` |
| `account.has_recorded_history` | `Account.HasRecordedHistory` |
| `account.name_invalid` | `Account.NameInvalid` |
| `account.not_found` | `Account.NotFound` |
| `breakdown.invalid_period` | `Breakdown.InvalidPeriod` |
| `category.duplicate_name` | `Category.DuplicateName` |
| `category.group_kind_mismatch` | `Category.GroupKindMismatch` |
| `category.group_not_found` | `Category.GroupNotFound` |
| `category.has_recorded_history` | `Category.HasRecordedHistory` |
| `category.name_invalid` | `Category.NameInvalid` |
| `category.not_found` | `Category.NotFound` |
| `category_group.duplicate_name` | `CategoryGroup.DuplicateName` |
| `category_group.name_invalid` | `CategoryGroup.NameInvalid` |
| `category_group.not_found` | `CategoryGroup.NotFound` |
| `history.invalid_filter` | `History.InvalidFilter` |
| `transaction.account_not_found` | `Transaction.AccountNotFound` |
| `transaction.amount_not_positive` | `Transaction.AmountNotPositive` |
| `transaction.category_not_found` | `Transaction.CategoryNotFound` |
| `transaction.kind_change_not_allowed` | `Transaction.KindChangeNotAllowed` |
| `transaction.not_found` | `Transaction.NotFound` |
| `transaction.note_too_long` | `Transaction.NoteTooLong` |
| `transfer.amount_not_positive` | `Transfer.AmountNotPositive` |
| `transfer.category_not_allowed` | `Transfer.CategoryNotAllowed` |
| `transfer.destination_account_not_found` | `Transfer.DestinationAccountNotFound` |
| `transfer.not_found` | `Transfer.NotFound` |
| `transfer.not_recorded` | `Transfer.NotRecorded` |
| `transfer.same_account` | `Transfer.SameAccount` |
| `transfer.source_account_not_found` | `Transfer.SourceAccountNotFound` |

**`csv-import` — 12**

| today | becomes |
|---|---|
| `import.account_not_found` | `Import.AccountNotFound` |
| `import.already_undone` | `Import.AlreadyUndone` |
| `import.file_not_readable` | `Import.FileNotReadable` |
| `import.invalid_paging` | `Import.InvalidPaging` |
| `import.mapping_profile_not_confirmed` | `Import.MappingProfileNotConfirmed` |
| `import.missing_required_column` | `Import.MissingRequiredColumn` |
| `import.movements_changed_by_hand` | `Import.MovementsChangedByHand` |
| `import.not_recorded` | `Import.NotRecorded` |
| `import.run_not_found` | `Import.RunNotFound` |
| `mapping_profile.category_not_found` | `MappingProfile.CategoryNotFound` |
| `mapping_profile.duplicate_source_category` | `MappingProfile.DuplicateSourceCategory` |
| `mapping_profile.invalid_rule` | `MappingProfile.InvalidRule` |

Nine modules in total: `Account`, `Breakdown`, `Category`, `CategoryGroup`, `History`, `Import`,
`MappingProfile`, `Transaction`, `Transfer`. Longest code is
`MappingProfile.DuplicateSourceCategory` at 38 characters. No collisions.

The new pattern is `^[A-Z][A-Za-z0-9]*\.[A-Z][A-Za-z0-9]*$`.

## What has to change, in one change

Applied on 2026-10-22, in the change that accepted this record. Listed so the edit is reviewable
file by file.

| # | file | what |
|---|---|---|
| 1 | `CLAUDE.md` §API | the rule and its two examples |
| 2 | `.claude/rules/openapi.md` | the Hard rules bullet and §5 |
| 3 | `core-ledger/sad.md` §8 | the row says "in snake_case" — amend, cite this record |
| 4 | `csv-import/sad.md` §8 | add the citation; the row does not name the case today |
| 5 | `core-ledger/contracts/openapi.yaml` | 28 codes, the `Error.code` pattern, the schema description |
| 6 | `core-ledger/contracts/api-sync-report.md` | the code table |
| 7 | `csv-import/contracts/openapi.yaml` | 12 codes, the pattern, the schema description |
| 8 | `csv-import/contracts/api-sync-report.md` | Section A's `Error` tree and Section B.2's table |
| 9 | `.claude/skills/finnavis-api-forge/references/error-catalog.md` | nine examples and the derivation table |
| 10 | `.claude/skills/finnavis-api-forge/templates/openapi.yaml` | the `Error` schema example |
| 11 | `docs/adr/0002` | a pointer at the top, so a reader does not take its Context as current |
| 12 | `.claude/skills/finnavis-api-forge/SKILL.md` | the "already decided" table and the Definition of Done both named the old spelling |

Row 12 was **missed in the first pass** and caught by the `csv-import` reconcile on the same day.
It mattered more than its size suggests: that table and that checklist are what a **first** run on a
new feature reads, so a contract written next week would have been derived in snake_case while every
other file said PascalCase.

Both contracts are re-run with `--reconcile` afterwards, so each `api-sync-report.md` records the
change instead of drifting silently against its contract.

**The derivation rules in `error-catalog.md` keep their meaning**, only their rendering changes: a
UNIQUE constraint still gives `<Entity>.Duplicate<Field>`, a foreign key still gives
`<Entity>.<Ref>NotFound`, an `alt`-branch is still named after the branch. The `module` half is
still the aggregate in the singular, now capitalised.

## Consequences

**Positive**
- The code maps onto a C# registry with no translation rule: `CategoryErrors.GroupNotFound` is
  `"Category.GroupNotFound"`, class for module, member for name. One place for a typo instead of
  two spellings to keep in step.
- The module stays readable and splittable, so "which aggregate refused" survives in the wire
  format rather than living only in a table.
- Done now it costs two contract edits and no migration. Nothing parses these strings yet.
- One duplicate refusal in `core-ledger` was surfaced on the way, and written down.

**Negative**
- **A PascalCase value inside a snake_case JSON document.** Every key stays snake_case, so
  `{"code": "Import.AccountNotFound"}` reads inconsistently at a glance. This is the real cost and
  it does not go away.
- **The combination is unusual — no API in the Context table uses it.** `Module.ErrorName` reads
  like a C# type reference, which it is not. An outside reader meets the convention before the
  reasoning. Accepted knowingly by the owner: this product has one consumer, written by the same
  person who writes the API. It becomes a liability the day the API is public.
- **40 strings move at once**, 28 of them in a contract already committed. Both features land in the
  same change or the repository carries two conventions, which is worse than either.
- **A second Approved document is amended after approval** — `core-ledger/sad.md` §8, for the second
  time after ADR-0002. Recorded in its §8 and §9 so the change is visible.
- **`docs/adr/0002` needs reading alongside this record.** Its Context restates the snake_case rule,
  so a reader who stops there gets the superseded spelling. Item 11 above fixes that.
- **Reversing this is a breaking change to every error response**, exactly as ADR-0002 noted for the
  envelope itself. Cheap today; not cheap once the PWA switches on these strings.

**Neutral**
- JSON keys are untouched: `code`, `message`, `details` and everything inside `details` stay
  snake_case, as does every request and response key in both contracts.
- HTTP status codes are untouched. The envelope still says *which* refusal; the status still says
  which class.
- No code changes. `implement-tasks` still builds the registry from the codes the contracts list.

## Links

- Supersedes the spelling clause of: [[0002-return-a-flat-error-envelope-instead-of-problem-details]]
- Rules: [[../../.claude/rules/openapi.md]] — the envelope and the `Module.ErrorName` convention
- Project rules: [[../../CLAUDE.md]] §API
- SAD: [[../features/core-ledger/sad.md]] §8 · [[../features/csv-import/sad.md]] §8
- Contracts: [[../features/core-ledger/contracts/openapi.yaml]] · [[../features/csv-import/contracts/openapi.yaml]]
- Related ADR: [[0001-initial-setup]]
