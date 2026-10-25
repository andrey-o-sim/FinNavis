---
status: Accepted
slug: "csv-import"
stage: "03"
updated_at: "2026-10-17"
---

# 0009 — Recognise a statement shape by its header row, with no name the Owner has to pick

- **Status:** Accepted
- **Date:** 2026-10-17
- **Deciders:** Andrey Simonenko

## Context

`GLOSSARY.md` defines a *Mapping profile* as the rules remembered for one shape of file, so the next
file of the same shape needs no confirmation. AC-01 states the promise in the Owner's terms: once
the rules for a shape are confirmed, the next statement of that shape needs no confirmation at all.

For that promise to hold, something has to decide that two files are "the same shape". Either the
system works it out from the file, or the Owner tells it. This decides which, and the answer
becomes the identity of the `MappingProfile` aggregate — the thing every later statement is looked
up by.

## Decision drivers

- PRD §5 AC-01 — the next file of the same shape needs **no confirmation**.
- `GLOSSARY.md` *Mapping profile* — rules for one shape of file, which outlive every load.
- PRD §2 Goals — the Owner stops re-typing the month-end batch; a monthly step that still needs a
  decision is a smaller version of the same chore.
- ADR-0001 — the file is never stored, so whatever identifies a shape has to be derivable from the
  file as it is read.
- SAD §2 Constraints — a bank controls the shape of its statement and the Owner cannot change it
  (`GLOSSARY.md` *Statement*).

## Considered options

1. **The header row is the identity.** The set and order of the file's column names, normalised and
   reduced to a key. The profile is looked up by that key, and the Owner picks nothing.
2. **The Owner names the profile and selects it.** A profile is called "Monobank card", and every
   load says which profile to apply.
3. **Header row for matching, plus a name the Owner gives.** The lookup is automatic as in option 1,
   and the name exists only so the profile is recognisable in a response.

## Decision outcome

**Chosen: option 1, the header row is the identity.**

It is the only option that delivers AC-01 literally. The criterion says the next file of the same
shape needs no confirmation, and under option 2 every load still needs the Owner to choose a
profile — a smaller chore than re-typing thirty rows, but the same kind, and PRD §2's goal is to end
that kind. The header row is also the one thing about a statement that is stable and outside the
Owner's control: the bank fixes it, and it is present in every file by definition.

Option 2 was rejected for the AC-01 mismatch above. It is more predictable — nothing can ever match
the wrong profile — and if header matching turns out to misfire in practice, option 2 is where to
retreat to.

Option 3 was rejected as paying for something nothing asks for. A name that does not take part in
the lookup can only get stale: the Owner calls a profile "Monobank", a second bank turns out to
produce the same headers, and the name is then actively misleading. AC-13 asks for counts and
set-aside rows, not for the name of the profile that was applied.

**Two things this gets wrong, deliberately.** If a bank changes one column, the key changes and the
shape is new, so the Owner confirms the rules once more — annoying, and correct, because the file
really did change and the old rules may not fit. If two different banks produce identical headers,
their profiles merge into one, and a rule confirmed for one applies to the other. Both are carried
into §11 (Risks and technical debt). Neither can silently corrupt the ledger: a rule that does not
fit produces either a category the Owner would not have chosen — visible in the run report (AC-13)
and undoable as a whole (AC-16) — or a set-aside row with its reason (AC-08).

## Consequences

**Positive**
- AC-01 holds exactly as written: from the second file of a shape onwards, the Owner hands the file
  over and nothing else.
- The identity is derived from the file, so it works with ADR-0001's decision not to store anything.
- Nothing to name, nothing to pick, nothing to keep tidy — no profile list the Owner has to curate.
- A changed statement format cannot be applied with stale rules without the Owner noticing: it
  simply reads as a new shape.

**Negative**
- Two banks with identical headers share one profile, and the system cannot tell them apart. §11
  carries it.
- A bank adding or renaming one column forces a re-confirmation that feels like a regression to the
  Owner. §11 carries it.
- The normalisation rules — case, whitespace, column order, a trailing empty column — become part of
  the identity, so changing them later changes which files match which profile, which is a
  migration over the profile table rather than a tweak.
- Profiles accumulate silently. Nothing deletes the profile of a format a bank stopped using.

**Neutral**
- How the key is computed — the ordered column names joined, or a hash of them — is a data-model
  question for the next stage. Either way it needs a unique index, named in §8 (Crosscutting
  concepts).
- The history migration (US-03) has no confirmation step at all (PRD §1), so it uses no profile and
  this record does not touch it.

## Links

- PRD: [[../PRD.md]] — US-01, US-02, AC-01, AC-04, AC-08, §2 Goals
- SAD: [[../sad.md]] §5, §6, §8, §11
- Related ADR: [[0001-never-store-the-uploaded-file]] · [[0005-run-both-import-jobs-through-one-pipeline-with-two-entry-points]]
