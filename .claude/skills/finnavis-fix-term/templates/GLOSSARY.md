---
status: Living
updated_at: "<today YYYY-MM-DD>"
---

# Domain Glossary — FinNavis

<!--
This file is the canonical glossary for the whole repository. Every feature reads it;
no feature copies a term out of it. If any document contradicts a definition here, this
file wins.

NO implementation detail here (no datastore/broker/framework names, no API contracts,
no error codes) — only domain words and the boundaries between them. Implementation
choices live in the SAD and the ADRs; behaviour lives in PRD.md.

What belongs to one feature does not live here. Each feature carries its own
docs/features/<slug>/CONTEXT.md, which names the terms that feature uses and holds its
own invariants and out-of-scope list. A term is defined once — in this file.

Terms get fixed inline, the moment they surface in an interview / PRD / review — never
batched «I'll consolidate later».
-->

## Glossary

<!-- One line per term: name · one-sentence canonical definition · the boundary (what it is NOT
     and how it differs). The NOT-boundary is mandatory — there is always a nearest neighbour to
     draw the line against. Alphabetical, case-insensitive. First word capitalised, the rest
     lowercase unless a proper noun. -->
- <Term> — <one-sentence definition in the owner's words>. NOT <Other> (<how it differs>).
