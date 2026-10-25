---
status: Living
feature: "csv-import"
updated_at: "2026-10-25"
---

# Domain Context — csv-import

<!--
This file is csv-import's slice of the domain. It does not define terms — docs/GLOSSARY.md
does, and it is canonical. Here we name the terms this feature works with and record what
holds for this feature alone.
-->

## Glossary

Terms are defined in [docs/GLOSSARY.md](../../GLOSSARY.md), which is canonical — if any
document in this folder contradicts it, the glossary wins. Never copy a definition here.

**Terms this feature uses:** Account, Balance, Category, Category Group, Duplicate, Export,
History, Import run, Ledger, Mapping profile, Movement, Owner, Set-aside row, Shape,
Statement, Transaction, Transfer.

<!-- Seven of those are this feature's own: Duplicate, Export, Import run, Mapping profile,
     Set-aside row, Shape, Statement. The rest it reads and writes through core-ledger, and
     their definitions do not change here. -->

## Invariants

- A set-aside row is never recorded on its own. It stays listed with its reason until the
  owner acts on it (AC-14).
- A row naming a movement already in the ledger is never recorded a second time, no matter how
  often the same file is handed over (AC-15, AC-20, AC-21).
- A load either records all of its rows or none of them. A load that fails part-way leaves the
  ledger exactly as it was (AC-22).
- Changing a rule never changes what is already recorded. Only later loads follow the new rule
  (AC-04).
- A load never changes or removes anything the owner typed by hand (AC-12), and undoing a load
  removes only what that load recorded (AC-16, AC-18).
- Undoing a load clears its own trace completely: handing the same file over again records every
  row, because an undone load never looks already recorded (AC-19).
- A load never creates what the ledger lacks unless the file is an export. A statement load
  refuses an unknown account and sets aside an uncovered category; an export creates both
  (AC-02, AC-09, AC-10).
- A move between the owner's own accounts is never recorded as spending or earning. A statement
  names one side only, so the row is set aside; an export names both, so it becomes a transfer
  (AC-06, AC-11).
- A load that finished is a success even when it set rows aside. The report carries the counts
  and one entry per set-aside row with its reason (AC-13, and `.claude/rules/openapi.md` §6).

<!-- The rule that decides when two rows name the same movement is NOT settled here. PRD §8
     carries it as open; `adr/0002` proposes exact external reference first, then a date and
     amount window. -->

## Sentinel errors

Error codes are not defined here. This feature's codes live in
[`contracts/openapi.yaml`](./contracts/openapi.yaml) and in the error registry in code —
one contract code equals one constant returning the same string. Spelling is
`Module.ErrorName` in PascalCase ([`docs/adr/0003`](../../adr/0003-spell-an-error-code-in-pascalcase.md));
the envelope is `{code, message, details?}`
([`docs/adr/0002`](../../adr/0002-return-a-flat-error-envelope-instead-of-problem-details.md)).

**Modules this feature refuses under:** `Import`, `MappingProfile`.

A set-aside row is not an error and carries no code. Its reason belongs to the closed set named
in PRD §5 and `sad.md` §8, and travels in the `200` body of a finished load.

## Org-filter invariant

Not applicable. The product has one `Owner`, no org and no sign-in, so no record carries an
owner or tenant column and no query filters by one. A `404` here is a plain "not found", not
existence-hiding — there is nobody to hide from (`.claude/rules/openapi.md` §1).

This section becomes live when the sign-in feature lands and adds a second actor. PRD §5 marks
the `authorization` coverage type N/A for the same reason, and §8 tracks it as a gap.

## Out of scope

Drawn from PRD §3 Non-goals.

- No splitting one of the file's categories into two of the owner's · a rule maps one category
  to one category, so rows under a bank category covering two of his still need filing by hand.
- No learning from corrections · the owner asked for nothing to tune, and a rule that learns
  files quietly wrong.
- No pairing a move between the owner's own accounts across two statements into one transfer ·
  it needs both statements loaded, so it would fail exactly when only one is.
- No source other than a file the owner hands over · no bank connection, no chat messages, no
  email, no receipt photographs. Each is its own feature over the same write path.
- No screen · there is no client yet; this feature ships the load itself.
