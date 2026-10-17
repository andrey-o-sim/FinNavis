---
status: Approved
slug: "csv-import"
stage: "02"
feature_size: "L"
depth: "medium"
updated_at: "2026-10-17"
source_brief: "./idea-brief.md"
---

# PRD — csv-import

> **Inputs:** [idea-brief](./idea-brief.md) · [CONTEXT](../../../CONTEXT.md) (repo root) · [SPEC](../../../SPEC.md)
> **Other channels read:** `docs/features/core-ledger/PRD.md`, as a wording and structure reference

## 1. Context

**What we're solving.** The owner records spending as it happens, up to 30 rows a month. The rest he
cannot record that way, so at the end of the month he reads a bank statement and re-types that batch
by hand. He also has three years of history in a tracker he wants to leave, and that tracker exports
but cannot import.

**Why now.** The ordering question. `core-ledger` asks whether a recorded entry should carry its own
identifier from day one, and that cannot be answered without knowing what loading a file needs.

**The committed approach.** Approach C — *Remembered import rules*. One path for any file: the owner
confirms once, per statement shape, what each of the file's own categories means; every movement a
load records remembers the load that brought it; handing the same file over again lands silently.

**Decisions taken here.**

- The brief deliberately left the approach open (§13 "Selected: TBD"). Settled here as C, the
  standing recommendation. The rule that decides when two rows name the same movement is **not**
  settled — §5 states the behaviour the owner observes, and §8 carries the rule.
- Both jobs stay in scope — the one-time move of the old history and the monthly statement — as
  separate user stories over one write path.
- The two jobs run over that one path and differ in **three** policies:
  1. **Creating what the ledger lacks.** A statement load creates no account and no category. Moving
     the old history takes the file's own names as the truth and creates what the ledger lacks.
  2. **The rules-confirmation step.** A statement shape is confirmed once before its first load.
     Moving the old history has no confirmation step — the export already names accounts and
     categories in the owner's own words.
  3. **A move between the owner's own accounts.** Moving the old history records it as a transfer,
     because the export names both accounts (AC-11). A statement load sets it aside, because a
     statement covers one account and names only one side (AC-06).
- A row in a currency the ledger does not keep is set aside. `SPEC.md` says single currency; a real
  card statement can still carry such a row, so §5 answers it.
- A load can be undone as a whole, and undoing a move of the old history also removes what it created.
- Changing a rule never changes what is already recorded.
- Moving the old history a second time is not blocked: the owner holds three sources, not two, and
  the repeated-load criteria already prevent a movement being recorded twice.
- **The work grew past what the brief priced.** The brief put Effort at 3 person-weeks from the
  effort signal M (§11), which produced RICE 0.33 and placed this feature above `core-ledger` at
  0.27. The scope written here is **L** — 30 evenings or more — because of the undo, the set-aside
  rows and the second job. The ranking is not re-scored here, but it must be re-checked before this
  feature is scheduled.

**Corrected on 2026-10-17, after the architecture stage** (`sad.md` §11 raised all three):

- **AC-03 and AC-09** were written assuming a statement names the account each of its rows belongs
  to. A statement covers one account (`CONTEXT.md`), so the owner names it when handing the file
  over, and the file is not read for it. Both criteria now say so, and §10 follows. The export of
  the old history is unchanged — it does name accounts per row.
- **§6's explanation of its two numbers** claimed that answering "is this row already in the ledger"
  one row at a time would miss both targets by a wide margin. That does not hold: ~1500 indexed
  lookups against a local database are a fraction of a second. The numbers stand as budgets for the
  whole load; the claim about why is removed.
- **§6's measurement prerequisite** said standing up the harness, the seeded ledger and the sample
  file is a task of this feature. `core-ledger` claims the same three. The note now says which
  feature builds them, and when that changes.

## 2. Goals

- The owner stops re-typing the month-end batch: those rows reach the ledger without being typed.
  Filing some of them under the right category stays manual — the file carries fewer categories than
  he has, and they do not line up one to one (idea-brief §2).
- The owner moves years of history out of the old tracker in one load, so that tracker can be closed.
- The owner can hand the same file over again without fear: nothing is recorded twice, and everything
  not recorded is listed with its reason.

## 3. Non-goals

- No splitting one of the file's categories into two of the owner's — a rule maps one of the file's
  categories to one of his, so rows under a bank category that covers two of his still need filing by
  hand (idea-brief §2). Ending that is a later feature.
- No learning from corrections — the owner asked for nothing to tune (idea-brief §17), and a rule
  that learns files quietly wrong.
- No pairing of a move between the owner's own accounts across two statements into one transfer — it
  needs both statements loaded, so it would fail exactly when only one is.
- No source other than a file handed over by the owner — no bank connection, no chat messages, no
  email, no receipt photographs. Each is its own feature over the same write path.
- No screen — there is no client yet; this feature ships the load itself.

## 4. User stories

### US-01: Confirm the rules for a statement shape

**As an** owner
**I want** to say once, per statement shape, what each of its categories means to me
**So that** every later statement of that shape needs no confirmation

### US-02: Load a monthly statement

**As an** owner
**I want** to hand over the month's file and have its rows recorded
**So that** I stop re-typing that batch

### US-03: Move my old history across

**As an** owner
**I want** to hand over the export and have it land as it stands
**So that** I can close the old tracker

### US-04: Read what a load did

**As an** owner
**I want** to see what was recorded, what was already there, and what was not
**So that** I trust the figures without re-checking the file

### US-05: Deal with the rows that were set aside

**As an** owner
**I want** to add a rule and hand the file over again
**So that** the waiting rows land and the rest stay as they were

### US-06: Undo a load

**As an** owner
**I want** to remove a whole load in one move
**So that** one bad rule does not cost me an evening

### US-07: Hand over the same file again

**As an** owner
**I want** to re-load a file without thinking
**So that** a repeated load is boring, not dangerous

## 5. Acceptance criteria

<!-- Coverage type `authorization` — N/A: one owner, no sign-in, no second person (see `Owner` in
CONTEXT.md), so no actor can lack permission. Tracked in §8; the sign-in feature owns this type. -->

### AC-01 (US-01) — happy

**Given** a statement of a shape never handed over before
**When** the owner says, per category the file carries, that it is one of their categories, a move
between their own accounts, or recorded with no category
**Then** the rules are remembered for that shape, and the next file of the same shape needs no
confirmation

### AC-02 (US-01) — cross-context

**Given** the owner is confirming the rules
**When** a rule points at a category that is not in the ledger
**Then** it is blocked, the unknown category is named back, and no category is created

### AC-03 (US-01) — error

**Given** a file that does not say the date or the amount of a row — and, when the file is an export
of the old history, one that does not say the account a row belongs to either
**When** the owner hands it over
**Then** nothing is recorded and the owner is told which of them is missing

> A statement covers one account, and the owner names that account when handing the file over
> (AC-05). So a statement is not required to name an account per row, and a column naming one is not
> read. An export covers every account, so it must name one per row (AC-10).

### AC-04 (US-01) — history integrity

**Given** rows recorded under a rule
**When** the owner changes that rule
**Then** the recorded rows keep the category they were given, and only later loads follow the new rule

### AC-05 (US-02) — happy

**Given** rules exist for this shape and the owner names the account the file covers
**When** the owner hands over the file
**Then** every row a rule covers is recorded on that account with the category the rule gives, and the
balance follows

### AC-06 (US-02) — domain invariant

**Given** a rule says one of the file's categories is a move between the owner's own accounts
**When** a row carries that category
**Then** the row is set aside, so the month's spending is not raised by money that only moved

### AC-07 (US-02) — error

**Given** a row the file names in a currency the ledger does not keep
**When** the file is loaded
**Then** the row is set aside and named back to the owner

### AC-08 (US-02) — happy

**Given** rules exist but the file carries a category no rule covers
**When** the file is loaded
**Then** the covered rows are recorded, the rest are set aside, and the uncovered category is named back

### AC-09 (US-02) — cross-context

**Given** the owner names an account that is not in the ledger as the one the file covers
**When** they hand the file over
**Then** nothing is recorded, the unknown account is named back, and no account is created

> This refuses the whole load rather than one row: every row of a statement lands on the one account
> the owner named, so if that account does not exist there is nothing to load onto. For an export,
> which names accounts per row, a missing account is created instead (AC-10).

### AC-10 (US-03) — happy

**Given** an export naming accounts and categories in the owner's own words
**When** the owner hands it over to move the old history
**Then** every row is recorded on the account it names with the category it names, keeping its own
date, and every account and category the file names that the ledger lacks is created

### AC-11 (US-03) — happy

**Given** the export names a row as a move between two of the owner's own accounts and names both
**When** the history is moved across
**Then** it is recorded as a transfer and the owner's total is unchanged

### AC-12 (US-03) — history integrity

**Given** the export carries rows dated in months the owner already recorded by hand
**When** the load finishes
**Then** those months include the loaded rows, and nothing typed by hand is changed or removed

### AC-13 (US-04) — happy

**Given** a load has finished
**When** the owner reads what it did
**Then** they see how many rows were recorded, how many were already in the ledger, and every
set-aside row with its reason

### AC-14 (US-05) — domain invariant

**Given** a set-aside row
**When** the owner does nothing
**Then** it is never recorded on its own, and it is still listed with its reason

### AC-15 (US-05) — duplicate safety

**Given** rows set aside for an uncovered category
**When** the owner adds the rule and hands over the same file again
**Then** the waiting rows are recorded, and the rows the earlier load recorded are not recorded a
second time

### AC-16 (US-06) — history integrity

**Given** a load recorded rows
**When** the owner undoes it
**Then** every row it recorded disappears, and every balance returns to what it would have been had
the load never happened

### AC-17 (US-06) — history integrity

**Given** a load that created accounts and categories
**When** the owner undoes it
**Then** those are removed too, except any that a movement from outside the load now names

### AC-18 (US-06) — error

**Given** a load whose rows the owner has since changed by hand
**When** the owner undoes it
**Then** the undo is blocked and the changed rows are named back

### AC-19 (US-06) — history integrity

**Given** a load was undone
**When** the same file is handed over again
**Then** every row is recorded again — an undone load does not look already recorded

### AC-20 (US-07) — duplicate safety

**Given** a file already fully loaded
**When** the owner hands it over again
**Then** nothing is recorded twice, and the owner is told how many rows were already there

### AC-21 (US-07) — duplicate safety

**Given** a file whose rows overlap movements the owner typed by hand on the day
**When** the file is loaded
**Then** a row naming an already-recorded movement is not recorded again, and it counts as already
there rather than being set aside

### AC-22 (US-02) — history integrity

**Given** a load that is part-way through recording its rows
**When** it fails before it finishes
**Then** the ledger looks as it did before the load started — none of its rows is recorded, every
balance is unchanged, and the owner is told the load did not happen

## 6. Non-functional requirements

Only the rows that change a decision while this is built. Throughput, availability and safe schema
change are settled for the whole product in `SPEC.md` and are not repeated here.

| Aspect | Target | Measurement |
|---|---|---|
| Load the old history (~1500 rows, several accounts), p95 | ≤ 60 s | timed in the integration suite against a seeded file |
| Undo a load of 1500 rows, p95 | ≤ 30 s | timed in the integration suite |

Both rows exist because "is this row already in the ledger" is asked once per row, so a load of the
old history asks it ~1500 times, and the undo walks the same movements again (§9). The numbers are a
ceiling on the whole load, not a test of that single lookup, and they are deliberately generous —
the one-time move of the old history should not cost an evening of waiting.

> **Measurement prerequisite.** Both rows need the integration test harness, a seeded ledger and a
> sample file. None of the three exists today, and `core-ledger` claims the same three as its own
> first tasks. Whichever feature starts first builds them — today that is `core-ledger`, because
> this one cannot begin without it (§9). If that order ever changes, they are built here, before any
> feature code.

## 6.1 Security / privacy

- **Data classification:** confidential — one person's complete financial history, plus files produced
  by their bank.
- **Personal data touched:** the owner's own records, and now the raw text of a bank statement — shop
  names and descriptions the owner did not write. The name the file came with is kept too.
- **AuthZ/AuthN impact:** none added. The absence is the main exposure, tracked in §8.
- **Abuse cases:**
  - A file handed over that is not a statement — huge, or with millions of rows: nothing is recorded.
    What the size limit is, is an open question in §8.
  - Text from the bank shown back later: stored and shown as plain text, never interpreted.
  - Anyone who reaches the installation can load a file into the ledger: accepted for now, tracked in
    §8; the installation stays off public networks.
- **Security review:** Required — for the first time the product reads a file from outside itself. The
  question to read it again with: what does a file that is not what it claims to be do before anything
  is recorded?

## 7. Metrics / KPIs

Two, and both answer themselves — the owner learns the answer by living a month, not by counting.

- **Rows re-typed by hand from a statement** — baseline ~30 a month, target 0, from the second
  statement onward. This is the feature's reason to exist: if he still re-types, it failed.
- **Old tool still open** — previous tracker still used in parallel. Baseline yes, target no, within
  90 days of the load that moves the old history. This is the kill metric for the whole move.

## 8. Open questions

- [ ] What makes two rows name the same movement? Default now: undecided — §5 states the behaviour,
      not the rule. — owner: Andrey, due: before implementation starts (idea-brief §15)
- [ ] Open a real statement and a real export: does either carry a stable operation number? — owner:
      Andrey, due: before implementation starts (idea-brief §15)
- [ ] Should a recorded entry carry its own identifier from day one? Default now: no. — owner: Andrey,
      due: core-ledger data-model stage (idea-brief §15; core-ledger §8 carries the same row)
- [ ] Does the statement name a currency at all? If it does not, AC-07 cannot fire. Default now: AC-07
      applies only when the file names one. — owner: Andrey, due: before implementation starts
- [ ] Does the old tracker's export name a row as a move between two accounts and name both? Default
      now: AC-11 applies when it does. — owner: Andrey, due: before implementation starts
- [ ] Should a move between the owner's own accounts be paired across two statements into one
      transfer? Default now: no — set aside. — owner: Andrey, due: after the first three monthly loads
      (idea-brief §15)
- [ ] Is there a limit on the size of a file that may be handed over? Default now: none enforced. —
      owner: Andrey, due: before implementation starts
- [ ] §5 carries no criterion of the authorization type. Default now: accepted; the installation stays
      off public networks. — owner: Andrey, due: when the sign-in feature is scoped

## 9. Data impact

- **New data:** a load is remembered as a whole — the name the file came with, the day it was handed
  over, whether it moved the old history or loaded a statement, and the account it was loaded onto
  when the file names only one. Every movement a load records remembers that load. The rules confirmed
  for a statement shape are remembered, each rule saying what one of the file's own categories means.
  A set-aside row is kept with the reason it was not recorded.
- **Changed data:** nothing already recorded loses anything. A recorded movement now also answers
  "which load brought me" — a movement typed by hand answers "none".
- **Already-recorded history:** no reshaping. Movements recorded by hand before this feature keep
  everything they have; the absence of a load is itself the answer "typed by hand". No pass over
  existing history is needed, and the owner sees nothing happen to what they already recorded.
- **New read paths:**
  - "is this row already in the ledger" — asked once per row, so up to ~1500 times in one load of the
    old history; it looks at movements on the named account around the row's date.
  - "everything one load brought" — walks the movements of a single load; used to show what a load did
    and to undo it.
  - "the rules for this statement shape" — a small lookup, once per load.
  - "the set-aside rows of a load" — walks a short list.
- **Prerequisites:** the `core-ledger` ledger must exist first — accounts, categories, transactions,
  transfers and balances. idea-brief §12 records this as a hard ordering constraint, and `core-ledger`
  is a PRD with no code behind it. Also a local ledger to run against, the integration test harness
  with seeded data, and one real statement plus one real export kept as sample files — every §6 row
  and three §8 questions need them.
- **Left to the implementation stage:** how the "is this row already in the ledger" question is
  answered fast enough for a load of the old history; the storage shape for a load, a rule set and a
  set-aside row; how the change reaches a store that already holds rows; and the machine names for the
  refusals listed in §10.

## 10. Refusal messages

| AC | What the owner is told |
|---|---|
| AC-02 | There is no category with that name, so a rule cannot point at it. |
| AC-03 | This file does not say the date or the amount, so nothing can be recorded from it. (For an export of the old history, add: or the account each row belongs to.) |
| AC-06 | This row is a move between your own accounts, so it was set aside instead of being recorded as spending. |
| AC-07 | This row is in another currency, so it was set aside. |
| AC-09 | You do not have an account with that name, so there was nothing to load this file onto. Nothing was recorded and no account was created. |
| AC-14 | No rule covers this row, so it was set aside instead of being recorded without one. |
| AC-18 | Some rows from this load were changed by hand afterwards, so the load cannot be undone as a whole. |
