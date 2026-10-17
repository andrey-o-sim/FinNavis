---
status: Accepted
slug: "csv-import"
stage: "03"
updated_at: "2026-10-17"
---

# 0005 — Run both import jobs through one pipeline with two entry points and a policy object

- **Status:** Accepted
- **Date:** 2026-10-17
- **Deciders:** Andrey Simonenko

## Context

PRD §1 commits to two jobs over one write path: the monthly bank statement (US-02) and the one-time
move of three years of history out of the old tracker (US-03). It also says exactly how they differ
— three policies, and nothing else:

1. **Creating what the ledger lacks.** A statement load creates no account and no category (AC-02,
   AC-09). A history migration takes the file's names as the truth and creates what is missing
   (AC-10).
2. **The rules-confirmation step.** A statement shape is confirmed once before its first load
   (AC-01). A history migration has none — the export already names accounts and categories in the
   Owner's own words.
3. **A move between the Owner's own accounts.** A history migration records it as a Transfer,
   because the export names both sides (AC-11). A statement load sets it aside, because a statement
   covers one account and names only one side (AC-06).

Everything else is identical: reading the file, matching against the ledger (ADR-0002, ADR-0003),
setting rows aside with a reason, recording the Import run, and undoing it. So the question is where
the three differences live.

## Decision drivers

- PRD §1 — the two jobs share the write path and differ in three named policies.
- PRD §5 AC-02, AC-06, AC-09, AC-10, AC-11 — the criteria that state those differences.
- SAD §1 quality goal 1 — nothing is recorded twice; the matching logic must not exist in two copies
  that can drift.
- `CLAUDE.md` § API — one folder per operation, each owning its own request and response records,
  never shared between operations.
- SAD §2 Constraints — one developer. Two copies of the same logic is two places to fix a bug in,
  by the same person, a year apart.

## Considered options

1. **One pipeline, two entry points.** Two use cases — `LoadStatement` and `MigrateHistory` — each
   with its own endpoint folder and its own contracts, both calling one pipeline and passing a
   different policy object that carries the three differences.
2. **One use case with a mode.** A single use case and a single endpoint, with the job named by a
   field in the request.
3. **Two separate pipelines.** Each job implemented end to end on its own, sharing nothing.

## Decision outcome

**Chosen: option 1, one pipeline with two entry points.**

It puts each difference in exactly one place and keeps the two jobs apart where being apart matters
— at the edge. Loading a monthly statement and migrating a tracker export are not two settings of
one operation: one happens every month and must never create anything, the other happens once in the
product's life and creates accounts and categories wholesale. Separate endpoints mean a monthly load
cannot start creating accounts because of one wrong field in a request. The three policies sit
together in one object, so a reader asking "how do these two jobs differ?" gets a list rather than a
search.

Option 2 was rejected on that exact point. With a mode field, the most destructive capability in the
feature — creating ledger structure from a file — is one typo away from the routine monthly path,
and the request shapes are not really the same anyway: a statement load names the account the file
covers (AC-05), a history migration does not.

Option 3 was rejected because the four things that would be duplicated are the four most expensive
things here to get wrong twice: the duplicate match, the Import run record, the set-aside rows, and
undo. Each is covered by its own acceptance criteria, and two copies means two sets of tests that
pass separately while the behaviours drift apart.

## Consequences

**Positive**
- The three differences from PRD §1 are a list in one object, so the document and the code say the
  same thing and a fourth difference has an obvious home.
- The matching rule, the run record, the set-aside rows and undo exist once and are tested once.
- A monthly statement load structurally cannot create an account or a category: it is a different
  endpoint calling the same pipeline with a policy that forbids it.
- The two endpoints carry their own request and response records in their own folders, which is what
  `CLAUDE.md` asks for anyway.

**Negative**
- Two use-case classes that look nearly identical at a glance. A reader who does not find the policy
  object first will think one of them is redundant, which is why it is written here.
- A policy object is an indirection. Reading one job end to end means following the policy into the
  pipeline rather than reading one method top to bottom.
- A difference that is *not* one of the three — a fourth behaviour one job needs and the other does
  not — will be tempting to add to the policy object, and the object will grow into a flag bag if
  nobody pushes back.

**Neutral**
- The pipeline is an Application-layer concern. It is not a port: both sides of it are inside the
  application, so it is plain polymorphism rather than an edge (`ARCHITECTURE.md` § Ports and
  adapters).
- The record says nothing about how many endpoints the confirm step has. That is §5 (Building block
  view), and it follows from the policy rather than from this choice.

## Links

- PRD: [[../PRD.md]] — US-01, US-02, US-03, AC-01, AC-02, AC-05, AC-06, AC-09, AC-10, AC-11, §1
- SAD: [[../sad.md]] §4, §5, §6
- Related ADR: [[0001-never-store-the-uploaded-file]]
