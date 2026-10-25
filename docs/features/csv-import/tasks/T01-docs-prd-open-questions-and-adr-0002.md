---
id: T01
title: "Answer PRD §8's four file questions and fix ADR-0002's proximity parameters"
layer: "docs"
deps: []
acs: []
files_hint: ["docs/features/csv-import/PRD.md", "docs/features/csv-import/sad.md", "docs/features/csv-import/adr/0002-match-on-an-exact-external-reference-first-then-a-date-and-amount-window.md"]
owner: "Andrey Simonenko"
estimate: "M"
status: "todo"
---

# T01 — Answer PRD §8's four file questions and fix ADR-0002's proximity parameters

## Why

[PRD §8](../PRD.md) carries four questions marked *due before implementation starts*, and
[sad.md §11](../sad.md) repeats all four as Open questions. They cannot be answered from any
document — they need a real statement and a real export opened. `acs` is empty because answering a
question satisfies no acceptance criterion; what it does is unblock T14, whose matcher has no
parameters until this lands.

[ADR-0002](../adr/0002-match-on-an-exact-external-reference-first-then-a-date-and-amount-window.md)
fixes the *shape* of the duplicate rule and deliberately leaves the parameters open — see
[sad.md §9](../sad.md), "0002 and 0003 are one contract, split on purpose". This task closes the
open half.

## What

Open the real statement and the real export on the owner's own machine, answer the four questions,
then write the answers into the documents that carry them.

The four questions, from [PRD §8](../PRD.md):

1. What makes two rows name the same movement — how wide the date window is, whether the file's
   text takes part, how it is normalised, and what happens when one file row matches two ledger
   movements.
2. Does either file carry a stable operation number? This decides whether ADR-0002's exact tier
   ever fires.
3. Does the statement name a currency at all? If it does not, AC-07 cannot fire.
4. Does the old tracker's export name a row as a move between two accounts and name both? If it
   does not, [sad.md §6](../sad.md) flow 4 loses its Transfer branch and AC-11 is unreachable.

Then:

- amend ADR-0002 with the settled parameters, keeping its Status at Accepted and recording what
  changed;
- tick the four rows in [PRD §8](../PRD.md) and strike the matching rows from
  [sad.md §11](../sad.md);
- if question 4 is answered *no*, say so in [sad.md §6](../sad.md) flow 4 rather than deleting the
  branch — a removed branch loses its reason.

## Definition of Done

- [ ] All four PRD §8 rows are answered, with the answer written in the row rather than in a commit
      message
- [ ] ADR-0002 states the date window, whether the file's text takes part and how it is normalised,
      and what happens when one file row matches two ledger movements
- [ ] `sad.md` §11 no longer lists the four as Open questions
- [ ] If no stable operation number exists, ADR-0002 says the exact tier is dead code for both jobs
      and the proximity tier carries everything
- [ ] The real statement and the real export are **not** in the repository — `git status` is clean
      of them

## Notes

- **The real files never enter the repository.** `CLAUDE.md` Safety and [sad.md §7](../sad.md): one
  statement is a full month of the owner's spending with the shop names attached, and a commit of it
  cannot be taken back without rewriting history. They stay on the owner's own machine, and the
  synthetic fixtures of T12 are built from them.
- This task is the long pole of the three roots. It needs the owner to sit down with two files, not
  an evening of typing. Start it first; T03 and T02 can run beside it.
- **The answer may shrink T14.** If question 2 says no stable operation number, ADR-0002's exact
  tier never fires and T14's matcher is one tier, not two. That is a smaller task, not a blocked one.
- `sad.md` §11 names the risk this leaves open either way: *"a false duplicate match deletes real
  spending silently"*. The parameters can be made good; the heuristic stays a heuristic.
