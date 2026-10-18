# Language rules

Applies to every conversation in this repo, and to every skill under `.claude/skills/`.

## Ask in the language the user wrote in

Mirror what is already on screen. Every question, option label and option description follows the
language the user opened in. Do not switch mid-run, and do not ask which language they prefer — it
is already visible.

## Write the artifact in English

`sad.md`, `PRD.md`, `idea-brief.md`, ADRs, code, comments and commit messages are English, to match
the rest of the repo — whatever language the conversation is in.

One exception: **quotes from the user stay verbatim**, in the language they said it.

## The skill files are English, and they are not a script

Every `SKILL.md`, reference and template is written in English because the repository is, and a
half-translated skill rots. None of it is text to read out. The English examples in those files show
the **shape** of a good question, not its wording — write the words fresh in the user's language.

A question that is grammatically perfect in the wrong language has already failed.

## Keep names as names

Identifiers are not jargon to translate, in any language: `CLAUDE.md`, `ILedgerQueries`,
`IUnitOfWork`, `target_surfaces`, `AC-04`, `US-02`, `§11`, `UUID v7`.

## Name the section, never just number it

`§5 (Acceptance criteria)`, not a bare `§5` — on every mention inside a question, not only the
first. The user is reading a question, not the document, so a bare number makes them reconstruct a
numbering they have no reason to hold in their head.

## Explain the trade-off, not the vocabulary

The reader wrote `ARCHITECTURE.md`. Glossing "index", "migration", "port", "aggregate" or
"invariant" wastes their time and reads as condescension. Explaining what the choice costs — what
has to be rewritten later, which data needs a backfill, how many evenings the other option adds —
does not.

Keep sentences short and direct. One idea per sentence. Plain words over clever ones.
