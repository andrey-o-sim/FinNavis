---
status: Living
updated_at: "2026-10-09"
---

# Domain Context — FinNavis

<!--
CONTEXT.md is the domain glossary — not a PRD and not a scratch pad. NO implementation
detail here (no datastore/broker/framework names, no API contracts) — only domain words
and the boundaries between them. Implementation choices live in the SAD and ADRs;
behaviour lives in PRD.md.

Terms get fixed inline, the moment they surface in an interview / PRD / review — never
batched «I'll consolidate later».
-->

## Glossary

<!-- One line per term: name · one-sentence canonical definition · one-sentence boundary
     (what it is NOT / the concept it gets confused with). Alphabetical once there are a few. -->
- Category — a label the owner files a transaction under; each category belongs to either expenses or income, and every transaction names exactly one. NOT Category Group (a group holds categories and is never named by a transaction).
- Category Group — an optional container holding related categories for reporting, for example utilities over electricity, water and gas; a category belongs to at most one group. NOT Category (a transaction always names a category, never a group).
- Transaction — a single income or expense recorded on exactly one account, with an amount, a date, and a category. NOT Transfer (a transfer moves money between two of the owner's own accounts, so it touches two accounts, carries no category, and leaves the owner's total unchanged).
- Transfer — moves money between two different accounts of the owner: it decreases one and increases the other by the same amount, carries no category, and leaves the owner's total unchanged. NOT Transaction (a transaction touches one account, has a category, and changes the owner's total).
