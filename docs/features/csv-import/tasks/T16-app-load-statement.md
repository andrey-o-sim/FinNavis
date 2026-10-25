---
id: T16
title: "Add the LoadStatement use case with the statement policy"
layer: "app"
deps: ["T14"]
acs: ["AC-05", "AC-06", "AC-07", "AC-08", "AC-09", "AC-15", "AC-22"]
files_hint: ["src/FinNavis.Application/Import/LoadStatement/", "tests/FinNavis.Application.UnitTests/Import/LoadStatement/"]
owner: "Andrey Simonenko"
estimate: "L"
status: "todo"
---

# T16 — Add the LoadStatement use case with the statement policy

## Why

[sad.md §6](../sad.md) flow 2 — US-02, the monthly job this feature exists for. It is the pipeline
of T14 with the **statement policy**: create nothing the ledger lacks, consult the mapping profile,
and set a move between the owner's own accounts aside because a statement names only one side of it.

Satisfies seven criteria — [AC-05](../PRD.md), [AC-06](../PRD.md), [AC-07](../PRD.md),
[AC-08](../PRD.md), [AC-09](../PRD.md), [AC-15](../PRD.md) and [AC-22](../PRD.md). That is most of
US-02, which is why this is an `L`.

## What

In `src/FinNavis.Application/Import/LoadStatement/`: the use case and the statement policy it hands
to the pipeline.

**The owner names the account the file covers; the file is not read for it.** A statement covers one
account by definition (`GLOSSARY.md`), so no column naming an account is read —
[sad.md §6](../sad.md) states this and PRD AC-03/AC-09 were corrected to match.

**Two refusals that stop the whole load**, before any write:

- the named account is not in the ledger — refuse, name it back, create nothing (AC-09);
- no rules exist for this file's shape — refuse and say so.

**Three set-aside reasons**, each a normal outcome of a working load:

- the rule says `TransferBetweenOwnAccounts` — a statement names only one side (AC-06);
- the row is in a currency the ledger does not keep (AC-07);
- no rule covers the row's category, and the category is named back (AC-08).

Everything else is recorded on the named account with the rule's category, and the balance follows
(AC-05). The whole run commits in one `IUnitOfWork`, so a failure part-way leaves the ledger exactly
as it was (AC-22).

## Definition of Done

- [ ] An unknown named account refuses the whole load, names it back, and creates no account
      (AC-09) — unit tests in `tests/FinNavis.Application.UnitTests`
- [ ] An unconfirmed shape refuses the whole load and says which of the two is missing
- [ ] A covered row is recorded on the named account with the category the rule gives (AC-05)
- [ ] A `TransferBetweenOwnAccounts` rule sets the row aside with that reason, so the month's
      spending is not raised by money that only moved (AC-06)
- [ ] A foreign-currency row is set aside and named back (AC-07)
- [ ] An uncovered category sets its rows aside and names the category back, while the covered rows
      are still recorded (AC-08)
- [ ] A failure before the commit leaves no row recorded and every balance unchanged, and the owner
      is told the load did not happen (AC-22)
- [ ] A second load after a rule is added records only the waiting rows (AC-15)
- [ ] No column of the file is read to find an account
- [ ] `./build.sh test` is green

## Notes

- **AC-09 refuses the whole load rather than one row, and the reason matters.** Every row of a
  statement lands on the one account the owner named — if that account does not exist there is
  nothing to load onto. An export, which names accounts per row, **creates** a missing account
  instead (T17, AC-10). The two jobs differ here on purpose.
- **AC-07 may never fire.** PRD §8 is open on whether the statement names a currency at all.
  Default now: AC-07 applies only when the file names one. Write it so, and do not infer a currency.
- **An unknown account is not a set-aside reason.** The data-model stage removed that fourth value
  — no flow draws it and no criterion reaches it, because AC-09 refuses the whole load.
- **AC-22 costs nothing to guarantee** because ADR-0004 commits the whole run at once. The test that
  earns it is in T25, with a fault injected at the commit.
- Runs in parallel with T17 — both fan out from T14 and own separate folders.
