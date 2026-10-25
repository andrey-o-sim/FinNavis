# Glossary — format and worked examples

Used in Phase 3 (collect) and Phase 12 (write). The target file is `docs/GLOSSARY.md`, section
`## Glossary` — the canonical glossary, shared by every feature. A term is defined there once.

This stage never writes a feature's own `docs/features/<slug>/CONTEXT.md`: that file is written
later, by `finnavis-prd`, and it carries no definitions — only the list of terms the feature uses,
plus its invariants and out-of-scope list.

## The format

One line per term, three parts:

```
- <Term> — <one-sentence canonical definition>. NOT <the term it gets confused with> (<one
  sentence saying what that other thing does instead>).
```

**The NOT-boundary is the point.** A definition without one is a label: it tells you what the
word means in isolation, not where it stops. Most domain confusion is two words whose edges
overlap, and the boundary is the only part of the entry that resolves it.

## The four terms already in docs/GLOSSARY.md

These are the house examples. Match their shape.

- **Category** — a label the owner files a transaction under; each category belongs to either
  expenses or income, and every transaction names exactly one. NOT Category Group (a group
  holds categories and is never named by a transaction).
- **Category Group** — an optional container holding related categories for reporting, for
  example utilities over electricity, water and gas; a category belongs to at most one group.
  NOT Category (a transaction always names a category, never a group).
- **Transaction** — a single income or expense recorded on exactly one account, with an
  amount, a date, and a category. NOT Transfer (a transfer moves money between two of the
  owner's own accounts, so it touches two accounts, carries no category, and leaves the
  owner's total unchanged).
- **Transfer** — moves money between two different accounts of the owner: it decreases one and
  increases the other by the same amount, carries no category, and leaves the owner's total
  unchanged. NOT Transaction (a transaction touches one account, has a category, and changes
  the owner's total).

Read these before writing a new one. Notice that Transaction and Transfer define each other
from both sides — that mutual boundary is what makes the pair unambiguous, and a new term that
sits next to an existing one should do the same.

## Which terms qualify

**Capture** a word when it is a thing in the owner's world that the code will have to model,
and two people could reasonably read it differently:

- `Statement` — a file from a bank covering a period. Confusable with `Export`.
- `Duplicate` — a record that represents money already recorded. Confusable with "two
  identical purchases on the same day", which is not a duplicate at all.
- `Balance` — confusable with "total", "available", and "balance as of a date".
- `Period` — confusable with calendar month versus billing month versus "last 30 days".

**Skip** generic technical vocabulary: HTTP, JSON, cache, queue, index, endpoint, migration.
These are not domain terms, they are implementation words, and `docs/GLOSSARY.md` explicitly keeps
implementation out.

**Skip** words the user used once in passing with no modelling consequence.

## Writing them in Phase 12

- Keep the list alphabetical.
- Append only. Never rewrite an existing definition as part of a brief — if an existing term
  turned out to be wrong, that is a finding for §15 Open questions, and it gets fixed
  deliberately in its own change. Silently redefining a term in a feature commit is how two
  parts of a codebase end up meaning different things by the same word.
- Bump `updated_at` in the `docs/GLOSSARY.md` frontmatter.
- Commit the glossary change together with the brief — they were produced by the same
  interview, and splitting them loses the reason the term was added.

## What goes in the brief instead

If a term surfaced but the interview did not settle it, do **not** invent a definition to fill
the glossary. Put it in §15 Open questions:

```
- [ ] "Period" — a calendar month, or 30 days from a date? Owner: Andrey, due: data-model stage.
```

An unresolved term in `docs/GLOSSARY.md` is worse than no entry: later work will trust it.
