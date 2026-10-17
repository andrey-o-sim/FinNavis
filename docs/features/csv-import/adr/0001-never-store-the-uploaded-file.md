---
status: Accepted
slug: "csv-import"
stage: "03"
updated_at: "2026-10-17"
---

# 0001 — Never store the uploaded file; have it handed over again for the load

- **Status:** Accepted
- **Date:** 2026-10-17
- **Deciders:** Andrey Simonenko

## Context

Loading a statement of a shape never seen before is two steps, and AC-01 is what forces it. The
Owner confirms, per category the file carries, what that category means in the ledger — and they
can only do that once the system has read the file and can show which categories are in it. So the
first load of a new shape is: read the file and report its categories, then confirm the rules, then
load.

Between the first step and the third, the file either lives somewhere or it does not. That choice
decides whether this feature introduces a file store — a port, an adapter, a volume in the
deployment unit and a lifetime policy — or introduces nothing at all.

PRD §6.1 raises the stakes: the raw text of a bank statement is the main new exposure this feature
creates. It is the first data in the product that the Owner did not write.

## Decision drivers

- PRD §5 AC-01 — the rules for a shape are confirmed before the first load, which requires the
  file's categories to be known first.
- PRD §6.1 — data classification confidential; the raw statement text and the file's own name are
  named as the new personal data this feature touches.
- SAD §2 Constraints — one developer working evenings; anything built here is also maintained here.
- SAD §3 Context and scope — the system talks to nothing outside itself except its own database.
  A file store would be the first thing to change that.
- `SPEC.md` "Planned, not built yet" — `docker-compose.yml` does not exist; a volume would be one
  more thing `core-ledger` has to stand up before this feature can run.

## Considered options

1. **Store nothing.** The first call reads the file, returns the distinct categories it carries and
   keeps none of it. The Owner confirms the rules, which creates the Mapping profile. The second
   call hands the same file over again and loads it.
2. **Store the file under an upload id.** The first call writes the bytes — to a volume or to a
   `bytea` column — and returns an id. The confirm step and the load step reference that id, so the
   file is uploaded once.
3. **Keep the file only for the length of the exchange**, in memory or in a temporary table, with a
   short expiry.

## Decision outcome

**Chosen: option 1, store nothing.**

The thing option 2 buys is one fewer upload, and it buys it once per bank, for the lifetime of the
installation. Every statement after the first lands straight on an existing Mapping profile with no
confirmation step at all (AC-01), so the second upload happens only when a new bank appears. Against
that it costs an `IFileStorage` port with an adapter behind it, a volume in §7 (Deployment view), a
rule for how long an abandoned upload lives and something that enforces it, and — the part that
decides it — complete bank statements sitting on disk next to the ledger. PRD §6.1 calls that text
the feature's main new exposure. Not having it is the cheapest protection available, and it needs no
code.

Option 3 was rejected because it is option 2 with a timer. It still needs somewhere to put the
bytes, still needs the expiry enforced, and adds a new failure the Owner can hit: confirming the
rules slowly enough that the upload is gone.

There is one thing option 2 would make possible later and option 1 will not: showing the Owner the
original file row next to the entry it produced. Nothing in the PRD asks for that — AC-13 asks for
counts and for the set-aside rows with their reasons, which §5 keeps inside the Import run — so it
is not paid for here.

## Consequences

**Positive**
- No `IFileStorage` port, no adapter, no volume, no cleanup job, no abandoned-upload lifetime.
- No complete bank statement is ever written to disk. §3 (Context and scope) stays at two external
  boxes, and the trust boundary a file crosses closes again the moment the request ends.
- The read step has no side effect at all, so handing a file over to see what is in it is safe and
  repeatable.
- Reading the file twice costs nothing worth measuring: at ~1500 rows the parse is far below the
  PRD §6 budget of ≤ 60 s for the whole load.

**Negative**
- The first file of a new bank is uploaded twice. The Owner has to understand why, and the API has
  to make it obvious rather than looking like a lost upload.
- The two calls can disagree. Nothing stops the Owner confirming rules against one file and loading
  a different one. The load handles this already — a category no rule covers is set aside and named
  back (AC-08) — but it is a real class of confusion that option 2 would not have.
- Showing the Owner the original file line next to a recorded entry is not possible later without
  revisiting this record.

**Neutral**
- The decision says nothing about what the *load* keeps. The Import run still stores the file's
  name, the set-aside rows and their raw text (PRD §9) — that is the text the Owner needs to act on,
  not the whole file.
- It is cheap to reverse in one direction: adding a file store later is additive. It is this
  direction, removing one once statements are on disk, that would be awkward.

## Links

- PRD: [[../PRD.md]] — US-01, US-02, AC-01, AC-08, §6.1
- SAD: [[../sad.md]] §3, §4, §5, §7
- Related ADR: [[0005-run-both-import-jobs-through-one-pipeline-with-two-entry-points]]
