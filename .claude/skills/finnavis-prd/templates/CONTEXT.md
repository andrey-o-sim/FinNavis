---
status: Living
feature: "<slug>"
updated_at: "<today YYYY-MM-DD>"
---

# Domain Context — <slug>

<!--
This file is one feature's slice of the domain. It does NOT define terms. Terms are
defined once, in docs/GLOSSARY.md, which is canonical for every feature. Here we only
name which of those terms this feature works with, and record what holds for this
feature alone.

NO implementation detail (no datastore / broker / framework names, no API contracts, no
error codes) — only domain words and the boundaries between them. Implementation choices
live in sad.md and the ADRs; behaviour lives in PRD.md.

## Glossary is mandatory. Prune another section only when it is genuinely empty after the
walk — «Org-filter invariant» is the one exception, kept with its N/A note so the gap is
visible rather than silently absent.
-->

## Glossary

Terms are defined in [docs/GLOSSARY.md](../../GLOSSARY.md), which is canonical — if any
document in this folder contradicts it, the glossary wins. Never copy a definition here.

**Terms this feature uses:** <alphabetical list, each name verbatim from the glossary>

<!-- A new domain word that surfaces during this feature's walk is added to
     docs/GLOSSARY.md with its NOT-boundary, and only its name is listed above. -->

## Invariants

<!-- Domain rules that hold across the whole feature, phrased «X always ...» / «X never
     ...». These sit ABOVE any single acceptance criterion: do not restate an AC, state
     the rule it protects and cite the AC in brackets. Prune if there are none. -->

- <invariant in the form «X always must / can never»> (AC-NN)

## Sentinel errors

Error codes are not defined here. This feature's codes live in
[`contracts/openapi.yaml`](./contracts/openapi.yaml) and in the error registry in code —
one contract code equals one constant returning the same string. Spelling is
`Module.ErrorName` in PascalCase ([`docs/adr/0003`](../../adr/0003-spell-an-error-code-in-pascalcase.md));
the envelope is `{code, message, details?}`
([`docs/adr/0002`](../../adr/0002-return-a-flat-error-envelope-instead-of-problem-details.md)).

**Modules this feature refuses under:** <PascalCase aggregate names, singular>

## Org-filter invariant

Not applicable. The product has one `Owner`, no org and no sign-in, so no record carries an
owner or tenant column and no query filters by one. A `404` here is a plain "not found", not
existence-hiding — there is nobody to hide from (`.claude/rules/openapi.md` §1).

This section becomes live when the sign-in feature lands and adds a second actor.

## Out of scope

<!-- Concepts this feature explicitly places outside itself, each with a one-line reason,
     so nobody re-litigates them in six months. Usually drawn from PRD §3 Non-goals. -->

- <out-of-scope concept · reason it is excluded>
