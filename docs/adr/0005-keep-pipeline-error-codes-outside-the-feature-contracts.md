---
status: Accepted
owner: "Andrey Simonenko"
reviewers: ["Andrey Simonenko"]
updated_at: "2026-11-08"
scope: project-wide
ticket: "none"
---

# 0005 — Keep the pipeline error codes outside the feature contracts

- **Status:** Accepted
- **Date:** 2026-11-08
- **Deciders:** Andrey Simonenko
- **Scope:** project-wide — every HTTP surface in this repository, both features

## Context

`docs/adr/0002` says every error response is the flat envelope `{code, message, details?}`, and
`core-ledger` T24 turns off `application/problem+json` so that this holds for every failure,
including the ones that never reach a handler. An envelope needs a `code` and a `message`.

`CLAUDE.md` §API also says one `code` in a contract equals one constant in the code, and that
`message` is the refusal wording from the PRD, not invented. That works for a business refusal. It
does not work for six failures that stop a request **before any use case runs**:

- a path no endpoint answers (404);
- a method the endpoint does not accept (405);
- a content type the endpoint does not accept (415);
- a body or a value that cannot be read or bound (400);
- a field validation failure from the framework (400, with field-level `details`);
- an unexpected server fault (5xx).

No feature contract and no PRD names them, because the owner never causes one through the product.
Only a client with a bug does. The first `core-ledger` T24 run gave them codes, and its review
stopped because the codes were not in the contract.

## Decision drivers

- `docs/adr/0002` — one envelope for every failure, wherever in the pipeline it happens.
- `CLAUDE.md` §API — a client switches on `code`, so a code must be stable and defined in one place.
- Two feature contracts exist today, `core-ledger` and `csv-import`, and more will follow. These six
  failures look the same in every one of them.

## Considered options

1. **Keep them outside the feature contracts.** The six codes are shared by the whole API and are
   defined in one place in Presentation. No feature contract lists them.
2. **Add them to every feature contract** as shared responses, and extend the drift test to cover
   them.

## Decision outcome

**Chosen: option 1.**

The six codes belong to the HTTP pipeline, not to a feature. In option 2, every contract carries the
same six codes. A change to one of them must then be made in every contract, and the drift test of
each feature checks codes that feature does not own. That is the drift the one-code-one-place rule
exists to prevent.

The codes and their messages, as the owner confirmed them:

| Status | Code | Message |
|---|---|---|
| 404 | `Route.NotFound` | No endpoint answers this path. |
| 405 | `Request.MethodNotAllowed` | This endpoint does not accept that HTTP method. |
| 415 | `Request.UnsupportedMediaType` | This endpoint does not accept that content type. |
| 400 | `Request.Invalid` | The request could not be read. |
| 400 | `Request.ValidationFailed` | Some fields in the request are not valid. |
| 5xx | `Server.Unexpected` | The server could not complete the request. |

They follow `docs/adr/0003`: `Module.ErrorName` in PascalCase. Their messages are not PRD wording,
because no PRD has a row for them. They stay short and neutral, since only a developer reads them.

## Consequences

**Positive**
- Every failure answers the one envelope, as `docs/adr/0002` requires.
- Each pipeline code is defined once, for every feature.
- A feature's drift test compares only the codes that feature owns.

**Negative**
- These six codes appear in no contract, so a client author learns them from this record and the
  code, not from `openapi.yaml`.
- `CLAUDE.md` §API's "one code in a contract" rule now has a written exception. A reader has to know
  it exists.

**Neutral**
- A business refusal still needs its code in the feature contract and its message from the PRD. This
  record changes nothing for those.
- If the API ever becomes public, the reason this record gives no longer holds. Then the six codes
  should go into a shared contract that every feature contract references.

## Links

- `docs/adr/0002-return-a-flat-error-envelope-instead-of-problem-details.md`
- `docs/adr/0003-spell-an-error-code-in-pascalcase.md`
- `docs/features/core-ledger/tasks/T24-ports-error-envelope-and-registry.md`
- `.claude/rules/openapi.md` §5
