---
status: Accepted
owner: "Andrey Simonenko"
reviewers: ["Andrey Simonenko"]
updated_at: "2026-10-20"
scope: project-wide
ticket: "none"
---

# 0002 — Return a flat error envelope instead of RFC 9457 Problem Details

- **Status:** Accepted
- **Date:** 2026-10-20
- **Deciders:** Andrey Simonenko
- **Scope:** project-wide — every HTTP surface in this repository, not one feature

## Context

Three committed files disagreed about the shape of an error response, and the disagreement blocked
`/sdlc-api-forge core-ledger --reconcile`.

- `.claude/rules/openapi.md` requires the envelope `{code, message, details?}`, where `code` is
  `module.error_name` in snake_case. The contract at
  `docs/features/core-ledger/contracts/openapi.yaml` was written against it and carries 24 such
  codes.
- `docs/features/core-ledger/sad.md` §8, Error handling row, says Presentation maps a domain error
  to an **RFC 9457 Problem Details** response. That document's status is Approved.
- `CLAUDE.md` §API says *"Problem Details is for refusals that stop the whole operation."*

`docs/features/core-ledger/contracts/api-sync-report.md` recorded this as a deferral needing an ADR
and did not edit any of the three. Nothing is implemented yet — `src/` is a skeleton with no
endpoint and no error registry — so this is the cheapest moment the decision will ever have.

The choice is genuinely open. RFC 9457 is the standard way to describe an HTTP error, and the §8
row was not written carelessly: it was the default a reader would expect. The envelope was chosen
later, at the api-forge gate, without the §8 row being reconsidered at the same time.

## Decision drivers

- One shape, stated once. Three files disagreeing is the actual problem; either answer beats the
  disagreement.
- The product has **one consumer**: a React PWA that does not exist yet, written by the same person
  who writes the API. There is no third-party client and no public API (PRD §3 Non-goals).
- `.claude/rules/openapi.md` is read by every later feature's contract, so whichever shape wins has
  to be the one written there.
- Nothing is built. Changing the contract costs an edit; changing it after handlers and a client
  parse it costs a migration of both sides.
- `CLAUDE.md` §API already fixes a separate rule that must survive either choice: a batch operation
  that finished is a success even when it skipped items, so skipped items are reported in the
  success body and never as an error.

## Considered options

1. **The flat envelope `{code, message, details?}`** — amend `sad.md` §8 and `CLAUDE.md` to match
   `.claude/rules/openapi.md`.
2. **RFC 9457 Problem Details** — revert `.claude/rules/openapi.md` and regenerate the contract.
3. **Both** — Problem Details on the wire, with `code` carried in an extension member.

## Decision outcome

**Chosen: option 1, the flat envelope `{code, message, details?}`.**

The deciding factor is what a client actually does with an error here. Every refusal in this product
is a business refusal the owner must read and act on — the amount is not above zero, the group is on
the other side of the ledger, recorded history depends on this account. The single field that
matters is a stable machine-readable `code` the client switches on to pick wording. The envelope
puts that field at the top level, named `code`, where it cannot be missed.

Problem Details' own fields do not earn their place in this product. `type` wants a dereferenceable
URI documenting the error class; there is no documentation site and no plan for one. `instance`
identifies the occurrence; there is no support process that would ever quote it. `title` and
`detail` duplicate what `message` says once. That leaves Problem Details supplying `status`, which
the HTTP status line already carries, plus a `code` smuggled in as an extension member — which is
option 3, and is strictly more machinery than option 1 for the same information.

Option 2 was the stronger candidate than its rejection suggests. It is the standard, it is what a
future integrator would expect, and `application/problem+json` tells a generic client what it is
looking at without reading our docs. It loses on a narrower point: this product has no future
integrator in scope. PRD §3 rules out a public API, and the one planned consumer is the owner's own
PWA. Paying for interoperability nobody will use, in a shape that makes the one field that matters a
non-standard extension, is the wrong trade here.

Option 3 was rejected as the worst of both: the ceremony of the standard without the simplicity of
the envelope, and a `code` that lives in an extension member where a generic Problem Details client
would ignore it anyway.

**This ADR supersedes the Error handling row in `docs/features/core-ledger/sad.md` §8**, which is
amended to point here rather than being left to contradict the contract. `CLAUDE.md` §API and
`.claude/rules/openapi.md` §5 are amended in the same change.

**If a public API is ever scoped, this decision is revisited, not inherited.** That is the condition
that changes the answer, and it is written down here so the next reader does not have to rediscover
it.

## Consequences

**Positive**
- Three files stop disagreeing. `/sdlc-api-forge core-ledger --reconcile` is unblocked on this
  point, and the 24 codes already in the contract stand as written.
- The field a client switches on is top-level and named `code`, not an extension member.
- Every later feature's contract inherits one shape from `.claude/rules/openapi.md` with no re-litigation.

**Negative**
- A non-standard error shape. Anyone who later integrates against this API has to read our document
  instead of recognising `application/problem+json` on sight. That cost is real and lands entirely
  on a future that PRD §3 currently rules out.
- An Approved document (`sad.md` §8) is amended after approval. The amendment is recorded in §8 and
  in §9's table so the change is visible rather than silent.
- If the OAuth feature ever exposes this API publicly, this is one more thing to change alongside
  the auth work — not a blocker, but it is on that feature's list.

**Neutral**
- The HTTP status code keeps its meaning. The envelope says *which* refusal; the status says which
  class it belongs to, per the table in `.claude/rules/openapi.md`.
- `details?` stays optional and is used for field-level validation only. It is not a free-form bag.
- The batch-operation rule in `CLAUDE.md` §API is unaffected: a finished batch is still a success,
  and skipped items are still reported in the success body, never as an error.
- No code changes. `src/` is a skeleton; `implement-tasks` builds the error registry from the codes
  the contract already lists.

## Links

- Rules: [[../../.claude/rules/openapi.md]] — the envelope and the `module.error_name` convention
- SAD: [[../features/core-ledger/sad.md]] §8 (the superseded row), §9
- Contract: [[../features/core-ledger/contracts/openapi.yaml]] · [[../features/core-ledger/contracts/api-sync-report.md]] Section D
- Project rules: [[../../CLAUDE.md]] §API
- Related ADR: [[0001-initial-setup]]
