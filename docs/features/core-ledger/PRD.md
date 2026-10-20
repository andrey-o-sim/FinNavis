---
status: Approved
owner: "Andrey Simonenko"
reviewers: ["Tech Lead", "Security Lead"]
updated_at: "2026-10-20"
feature_size: "M"
stage: "03"
ticket: "none"
---

# PRD — core-ledger

> **Inputs (required):** [idea-brief](./idea-brief.md) · [CONTEXT](../../../CONTEXT.md) (the glossary lives at the repo root, not in this folder)
> **Reference module:** N/A — green-field mode
> **External context channels used:** None — only CONTEXT + idea-brief, plus `SPEC.md`, read to resolve one scope contradiction

## 1. Context

**What we're solving.** The owner tracks their money in another personal-finance tool today and named
eight concrete gaps with it. This feature closes one of them directly — there is no way to ask "show
expenses for a period, broken down by category" — and partly a second, because that tool offers no
programmatic access of any kind, so nothing can read the data except a human. The only user is the
repo owner, tracking their own money (idea-brief §2, §3).

**Why now.** The repository is a skeleton: scaffolding only, no entities, no schema, no recorded
movement. Every other item on the draft business-requirements list — import, export, summaries,
assistant access — reads from or writes to a ledger that does not exist yet. Nothing else in the
product can start until it does (idea-brief §4).

**The committed approach.** Approach C, *Group Rollup Ledger* (idea-brief §13): a ledger of accounts,
flat categories with optional groups over them, transactions and transfers, plus one filtered history
and one period- and account-filtered breakdown that answers at the group level and the category level
in a single request. The shape is built to take more filter dimensions later without breaking
existing callers.

**Traceability and decisions taken here.** Green-field — no reference module exists to borrow patterns
from (idea-brief §12 confirms every current source file is scaffolding). These scope decisions were
taken during the PRD interview and are not in the idea-brief:

- Balance is in scope. `SPEC.md` names it as a product goal while the idea-brief §13 locked-in
  pointer omits it; resolved in favour of `SPEC.md`.
- Deleting a category or an account that recorded history references is blocked.
- The breakdown reports an explicit **Default group** bucket for categories with no group.
- Recorded entries can be edited and deleted, but a transaction can never become a transfer.
- A transaction carries an optional free-text note.
- The breakdown covers earning as well as spending, kept apart.

**Stated deviation.** §5 carries no acceptance criterion of the *authorization* coverage type. The
product has one owner, no sign-in, and no second person, so there is no actor to deny and no
existence to hide — a criterion of that type could only be invented. The slot is marked N/A in §5
with the gap tracked in §8, so the later sign-in feature inherits an honest hole rather than a
checkbox that was already ticked.

**Size re-check.** The size stayed **M** after the additions above. None of them introduces a new
module, a new consumer, or a breaking change; they share one write model with what the idea-brief
already carried, so no size signal crossed into L.

**Amendment — 2026-10-20.** `/sdlc-api-forge` could not finish because `category_groups` existed in
the data model with no operation behind it, which left AC-11 ("with or without a group") and AC-13
("moves it to another group") half-unreachable: nothing could produce a group to name. This
amendment is **append-only** — §1 to §3, US-01 to US-08 and AC-01 to AC-25 are unchanged, so every
citation from `sad.md`, `data-model.md` and the six ADRs still resolves. Decisions taken here:

- The owner keeps their category groups: creates them, lists them, renames them, removes them
  (US-09). This is the gap that stopped the contract.
- **A group belongs to one side of the ledger — spending or earning — exactly as a category does.**
  Expenses and income do not cross anywhere in the model, and a group was the one place they still
  could. This changes the schema: groups carry no side today, so `/sdlc-generate-data-model` has to
  run again before a contract can be generated.
- **Removing a group that still holds categories is allowed.** The categories keep every recorded
  movement and are reported under the Default group from then on. This deliberately does *not*
  follow AC-09 and AC-12, which block: those protect recorded money from being orphaned, and a
  group holds no money — only a reporting arrangement over categories that keep their own history.
- Renaming a category is in scope (US-10). This settles a disagreement found at the contract gate,
  where `sad.md` §5 declared a `Categories/Rename` use case that no user story had asked for —
  resolved in the SAD's favour, so that declaration now has a story behind it.
- The owner can list their categories, optionally narrowed to one group (US-10). Without it a
  category that has never been used appears in no breakdown and can never be named again.
- **AC-35 and AC-36 state uniqueness rules the schema already enforces.** `data-model.md` flagged
  its own decision 4 as *"an added rule"* that no acceptance criterion covered; these close it.
  **AC-31 is different.** A group name is unique across every group today, with no notion of
  sides, so AC-31 asking for uniqueness *per side* — mirroring categories, where a spending
  "Gifts" and an earning "Gifts" may both exist — is a **second schema change, not documentation**.

**What this amendment forces upstream.** The PRD itself is append-only, but artifacts behind it no
longer match it. Listed here so none of them is discovered late:

| Artifact | What no longer matches | Resolved by |
|---|---|---|
| `CONTEXT.md` | Category Group was defined with no side. **Updated in this same change**, because `sad.md` §12 makes the glossary canonical — *"if anything in this document contradicts it, `CONTEXT.md` wins"* | done here |
| `sad.md` §5 | Gives `CategoryGroup` no repository of its own, reasoning that *"US-04 treats creating a category, putting it in a group and moving it between groups as one job"*. US-09 makes group lifecycle a job in its own right. The Application and Presentation trees also carry no group operations and no list operation | re-run §5 |
| `sad.md` §6 | States *"nine flows cover all eight §4 user stories and all 25 §5 acceptance criteria"*. There are now ten stories and 36 criteria | `/sdlc-complete-sequence-diagrams` |
| `sad.md` §12 | Repeats the old Category Group wording in its own glossary table | re-run §12 |
| `data-model.md` | **Two** gaps. `category_groups` carries no side column, and its name is unique globally rather than per side. Separately, the category-to-group link has no cascade, so AC-29 needs a group's categories unlinked before the group row is removed — nothing describes that today | `/sdlc-generate-data-model` |

**Size re-check after the amendment — not settled here.** The amendment adds two user stories,
eleven criteria, roughly six operations on top of the fifteen already contracted, a schema change,
and most likely a new repository port. The earlier **M** can no longer be asserted without testing
it against the size signals. `/sdlc-classify-size core-ledger` re-runs them and writes the answer;
until it does, `.size`, this document's frontmatter and `sad.md` all still read M.

## 2. Goals

- The owner records every money movement — income, expense, and transfer between their own
  accounts — in one place, and each account's balance always agrees with the movements recorded on it.
- The owner answers "where did my money go in this period" in a single request that reports both
  group totals and category totals, without re-adding numbers themselves.
- The owner reshapes their category structure as their habits change, and the figure for each
  individual category in a closed period never changes; group figures follow the current grouping.

## 3. Non-goals

- No web client, installable app, or offline reading — this feature ships programmatic access only;
  the client is a separate later feature that will consume what is built here.
- No sign-in and no OAuth — one owner, one dataset, so records carry no owner field; a login lock is
  its own feature when it is needed.
- No import from a file, no period-scoped export, no spreadsheet or cloud-drive integration — each is
  a separate feature that depends on this ledger existing first.
- No charts, statistics, monthly summaries, or custom category icons — the breakdown here returns
  figures; presenting them is the client's job.
- No multi-currency, bank integrations, budgets, or forecasts — single currency, manual entry,
  explicitly out of scope per idea-brief §5.

## 4. User stories

### US-01: Record an expense or income

**As an** owner
**I want** to record a transaction on one account with an amount, a date, a category and an optional free-text note
**So that** my spending and earning are captured where I can query them later

### US-02: Move money between accounts

**As an** owner
**I want** to record a transfer from one of my accounts to another
**So that** moving my own money does not look like spending it

### US-03: Keep my accounts

**As an** owner
**I want** to create, rename and delete the accounts my money sits in
**So that** the ledger matches the places I actually hold money

### US-04: Keep my categories

**As an** owner
**I want** to create categories, optionally put them in a group, and move them between groups later
**So that** I can file spending the way I think about it

### US-05: See what an account holds

**As an** owner
**I want** to see the current balance of each account and the total across all of them
**So that** I can check the ledger against the real account without adding up history

### US-06: Browse my history

**As an** owner
**I want** to list recorded movements filtered by period, account and category
**So that** I can find a specific entry and check what a period contained

### US-07: See where the money went

**As an** owner
**I want** one period breakdown that totals by group and by category at once
**So that** I get the answer my old tool could never give me, in one request

### US-08: Correct a mistake

**As an** owner
**I want** to change or remove an entry I recorded wrongly
**So that** a typo at 11pm does not live in my ledger forever

<!-- US-09 and US-10 were appended by the 2026-10-20 amendment (see §1). US-01 to US-08 above are
     unchanged, so every citation from sad.md, data-model.md and the ADRs still resolves. -->

### US-09: Keep my category groups

**As an** owner
**I want** to create category groups on one side of the ledger, see the ones I have, rename them, and remove ones I no longer use
**So that** I can shape how my spending and my earning roll up, without touching the categories underneath

### US-10: See and correct my category structure

**As an** owner
**I want** to list my categories, narrow that list to one group, and rename a category
**So that** I can see what I actually have and fix a bad name without disturbing recorded history

## 5. Acceptance criteria

<!-- Coverage type `authorization` — N/A: the product has one owner, no sign-in, and no second
     person (see the `Owner` entry in CONTEXT.md), so no actor can lack permission and no existence
     can be hidden. Tracked as a gap in §8; the later sign-in feature owns this type. -->

### AC-01 (US-01) — happy path

**Given** the owner has an account and an expense category
**When** the owner records an expense naming that account and category, with an amount, a date and optionally a note
**Then** the system records the expense, confirms it to the owner, keeps the note with the entry, and the account balance falls by that amount

### AC-02 (US-01) — error

**Given** the owner is recording a transaction
**When** the amount given is zero or below zero
**Then** the system blocks the recording and tells the owner the amount must be above zero

### AC-03 (US-01) — cross-context

**Given** the owner is recording a transaction that names a category which is not in the ledger
**When** the owner submits it
**Then** the system blocks the recording, names the unknown category back to the owner, and does not create that category

### AC-04 (US-02) — happy path

**Given** the owner has two different accounts
**When** the owner records a transfer between them
**Then** the source account falls and the destination account rises by the same amount, and the total across all accounts is unchanged

### AC-05 (US-02) — domain invariant

**Given** the owner is recording a transfer
**When** the source and the destination are the same account
**Then** the system blocks it and tells the owner a transfer needs two different accounts

### AC-06 (US-02) — domain invariant

**Given** the owner is recording a transfer
**When** the recording cannot be completed
**Then** neither account changes — a transfer is never applied to one side only

### AC-07 (US-03) — happy path

**Given** the owner has no account by that name
**When** the owner creates one
**Then** it appears in the account list with a balance of zero

### AC-08 (US-03) — happy path

**Given** an account that recorded history already names
**When** the owner renames it
**Then** the system accepts the new name, and every recorded movement, balance and breakdown continues to refer to the same account under its new name

### AC-09 (US-03) — domain invariant

**Given** an account that at least one transaction or transfer is recorded against
**When** the owner tries to delete it
**Then** the system blocks the deletion and tells the owner that recorded history depends on it

### AC-10 (US-03) — happy path

**Given** an account with no transaction and no transfer recorded against it
**When** the owner deletes it
**Then** the system removes it

### AC-11 (US-04) — happy path

**Given** the owner wants a new way to file spending
**When** the owner creates a category, with or without a group
**Then** it becomes available to name when recording a transaction

### AC-12 (US-04) — domain invariant

**Given** a category that at least one transaction names
**When** the owner tries to delete it
**Then** the system blocks the deletion and tells the owner that recorded history depends on it

### AC-13 (US-04) — happy path

**Given** a category that already has recorded history
**When** the owner moves it to another group, or out of every group
**Then** the system accepts the change, the figure for that category in any period stays as it was, and group figures from then on follow the new placement

### AC-14 (US-05) — domain invariant

**Given** any sequence of recordings, edits and deletions on an account
**When** the owner reads that account's balance
**Then** it equals the sum of every transaction and transfer recorded against that account

### AC-15 (US-05) — domain invariant

**Given** the owner transfers money between two of their own accounts
**When** the owner reads the total balance across all accounts
**Then** the total is unchanged

### AC-16 (US-06) — happy path

**Given** the ledger holds recorded movements
**When** the owner lists history filtered by period, account and category
**Then** the matching movements are returned, each one marked as a transaction or a transfer, and each transaction showing its note

### AC-17 (US-06) — happy path

**Given** a period in which nothing was recorded
**When** the owner lists history for it
**Then** an empty list is returned and the request is not treated as a failure

### AC-18 (US-07) — happy path

**Given** the ledger holds spending and earning in a period
**When** the owner asks for the breakdown of that period
**Then** one answer returns totals per group and totals per category, with spending and earning kept apart

### AC-19 (US-07) — domain invariant

**Given** a breakdown covering a period
**When** the owner adds up the group totals together with the Default group total
**Then** the sum equals the period total, on the spending side and on the earning side alike

### AC-20 (US-07) — domain invariant

**Given** a period containing transfers between the owner's own accounts
**When** the owner reads the breakdown of that period
**Then** no transfer is counted as spending or as earning

### AC-21 (US-08) — happy path

**Given** a recorded entry with a wrong amount
**When** the owner changes the amount
**Then** every balance affected by that entry follows the new amount

### AC-22 (US-08) — happy path

**Given** a recorded entry the owner no longer wants
**When** the owner removes it
**Then** it disappears from history, and every balance it affected returns to what it would have been had the entry never existed

### AC-23 (US-08) — error

**Given** a recorded transfer
**When** the owner edits it and tries to give it a category
**Then** the system blocks the change and tells the owner that transfers carry no category

### AC-24 (US-08) — error

**Given** a recorded transaction
**When** the owner edits it and tries to name a second account, which would turn it into a transfer
**Then** the system blocks the change and tells the owner that a transaction and a transfer are different kinds of entry, and that the entry must be removed and recorded again as the other kind

### AC-25 (US-08) — error

**Given** an account, category or entry that is not part of this ledger
**When** the owner tries to read or change it
**Then** the system refuses, reports it as unknown, and creates nothing implicitly

<!-- AC-26 to AC-36 were appended by the 2026-10-20 amendment (see §1). AC-01 to AC-25 above are
     unchanged and keep their numbers, because sad.md §6, sad.md §10, data-model.md and the ADRs
     all cite them by number. -->

### AC-26 (US-09) — happy path

**Given** the owner wants several categories to roll up together
**When** the owner creates a group, naming the side of the ledger it belongs to
**Then** the group becomes available to place categories of that same side in, and it holds no categories yet

### AC-27 (US-09) — happy path

**Given** the owner has created groups
**When** the owner lists them
**Then** every group is returned, each with its name and the side of the ledger it belongs to

### AC-28 (US-09) — happy path

**Given** a group holding categories that already have recorded history
**When** the owner renames the group
**Then** the breakdown reports that group under its new name, and the figure for every category in it is unchanged

### AC-29 (US-09) — happy path

**Given** a group that still holds categories
**When** the owner removes the group
**Then** the group is gone, every category it held keeps all of its recorded history, and from then on the breakdown reports those categories under the Default group

### AC-30 (US-04) — domain invariant

**Given** a group that belongs to one side of the ledger
**When** the owner tries to place a category from the other side into it
**Then** the system blocks it and tells the owner that a group and the categories inside it must be on the same side

### AC-31 (US-09) — error

**Given** a group on one side of the ledger already carries a name
**When** the owner creates or renames another group on that same side with that same name
**Then** the system blocks it and tells the owner the name is already taken, while that name stays free for a group on the other side

### AC-32 (US-04) — cross-context

**Given** the owner is creating a category, or moving one into a group
**When** the named group is not in the ledger
**Then** the system blocks the change, names the unknown group back to the owner, and does not create that group

### AC-33 (US-10) — happy path

**Given** the owner has categories, some placed in a group and some not
**When** the owner lists them, either all of them or only those sitting in one named group
**Then** each category is returned with its name, the side of the ledger it belongs to, and the group it currently sits in, if any

### AC-34 (US-10) — happy path

**Given** a category that already has recorded history
**When** the owner renames it
**Then** the system accepts the new name, every recorded movement, balance and breakdown continues to refer to the same category under its new name, and its figure for a closed period is unchanged

### AC-35 (US-03) — error

**Given** an account already carries a name
**When** the owner creates or renames another account with that same name
**Then** the system blocks it and tells the owner the name is already taken

### AC-36 (US-04) — error

**Given** a category on one side of the ledger already carries a name
**When** the owner creates or renames another category on that same side with that same name
**Then** the system blocks it and tells the owner the name is already taken, while that name stays free for a category on the other side

## 6. Non-functional requirements

| Aspect | Target | Measurement |
|---|---|---|
| Latency p95 — record a movement | ≤ 300 ms | timed in the integration suite |
| Latency p95 — period breakdown | ≤ 500 ms over 3 years of history (~20 000 movements) | timed in the integration suite against seeded data |
| Latency p95 — history page | ≤ 400 ms per page | timed in the integration suite |
| Throughput | ≥ 10 requests/s on one instance | smoke test in CI |
| Availability | no formal target — self-hosted, one user, downtime costs only the owner's own evening | N/A with reason |
| Correctness under any operation order | 0 balance discrepancies across at least 1 000 randomised sequences of recordings, edits and deletions | property-based test in the suite |

> **Measurement prerequisite.** Every row above except Availability is measured by an integration
> harness and a CI pipeline that do not exist yet — `SPEC.md` lists both under "Planned, not built
> yet", and idea-brief §12 records that no local database setup exists. Standing these up is a
> prerequisite task, not a side effect of this feature; `/sdlc-break-tasks` must schedule it before
> any of these targets can be checked.

## 6.1 Security / privacy

- **Data classification:** confidential — one person's complete financial history.
- **Personal data touched:** the owner's own records (amounts, dates, free-text notes). No
  third-party personal data and no card numbers; an account is a label the owner chooses, not a bank
  identifier.
- **AuthZ/AuthN impact:** none added — this feature introduces no permission check. That absence is
  itself the main exposure and is tracked in §8.
- **Abuse cases:**
  - Anyone who can reach the installation reads and edits the whole ledger: accepted risk for now,
    tracked in §8; the owner keeps the installation off public networks until the sign-in feature ships.
  - A note used to smuggle content a future client might render: the note is stored and returned as
    plain text and is never interpreted.
  - An unbounded note used to bloat storage: a note is capped at 500 characters, which closes this
    off. The bound was settled by `data-model.md` decision 2 and the question is marked closed in §8.
  - A repeated identical request creating a second movement: out of scope here — de-duplication is
    its own feature (idea-brief §5).
- **Security review:** Required — the feature holds confidential personal financial data with no
  authentication boundary.

## 7. Metrics / KPIs

- **Entry habit** — days in a month with at least one recorded movement. Baseline: 0, target: at
  least 20 of 30 days, within 60 days of first use.
- **Answer in one request** — share of "where did my money go" questions answered without the owner
  doing arithmetic by hand. Baseline: 0 (impossible in the current tool), target: all of them, from
  first release.
- **Ledger trust** — occasions on which a balance in FinNavis disagreed with the real account.
  Baseline: unknown, measured by the owner reconciling each account once a month. Target: 0, within
  90 days.
- **Migration depth** — months of history recorded in FinNavis. Baseline: 0, target: at least 3
  months, within 90 days.

## 8. Open questions

- [x] What is the maximum length of a transaction note? → **Closed** at 500 characters by `data-model.md` decision 2 (2026-10-18), which also answers the storage-bloat abuse case in §6.1.
- [x] How are stored timestamps reconciled with the owner's local calendar month, so a late-evening purchase near a boundary lands in the right period? → **Closed** by `data-model.md` decision 1 (2026-10-18): the date an entry is filed under is a calendar day with no time and no timezone, so it cannot slide across a month boundary.
- [x] Should an entry carry a client-supplied identifier from day one, so later de-duplication, import and message capture need no backfill? → **Closed** as yes by ADR-0006 (2026-10-17): a nullable outside reference ships now, unused, so a later import feature needs no migration against a table holding real history.
- [ ] The ledger has no authentication, so anyone who reaches the installation has full access, and §5 therefore carries no acceptance criterion of the authorization type. Default now: accepted; the installation stays off public networks. — owner: Andrey, due: when the OAuth feature is scoped
- [ ] `SPEC.md` Non-goals says "no authentication, one owner" while its draft requirements list Google OAuth and a secured public API. Settled here as "one owner now, OAuth later", but the two sections still read as contradictory. — owner: Andrey, due: when the OAuth feature is scoped (carried from idea-brief §15)
- [x] Is a dimension outside period / account / category — payee, merchant, free tag — needed soon? → **Closed** as no by `sad.md` §8 and §11 (2026-10-16): the optional note covers finding an entry by eye. `sad.md` §11 carries the counter-pressure from `SPEC.md` as a separate live question, to be settled before csv-import is scoped.

<!-- Added by the 2026-10-20 amendment (§1). -->

- [ ] Should a group that currently holds no categories appear in the period breakdown with a total of zero, or be left out of it entirely? Default now: left out — a group with no categories has nothing to total. It matters because AC-19 requires group totals plus the Default group to equal the period total, and a zero row neither helps nor breaks that. — owner: Andrey, due: before `/sdlc-api-forge`
- [ ] When a group is removed and its categories fall to the Default group (AC-29), should the owner be told how many categories were affected? Default now: no — the removal is confirmed and the owner reads the new grouping back with AC-33. — owner: Andrey, due: before `/sdlc-api-forge`
