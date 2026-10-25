---
id: T14
title: "Build the shared import pipeline, the policy object and the two-tier duplicate matcher"
layer: "app"
deps: ["T01", "T04", "T05", "T06", "T10"]
acs: ["AC-15", "AC-20", "AC-21"]
files_hint: ["src/FinNavis.Application/Import/Pipeline/", "tests/FinNavis.Application.UnitTests/Import/Pipeline/"]
owner: "Andrey Simonenko"
estimate: "L"
status: "todo"
---

# T14 — Build the shared import pipeline, the policy object and the two-tier duplicate matcher

## Why

The core of the feature.
[ADR-0005](../adr/0005-run-both-import-jobs-through-one-pipeline-with-two-entry-points.md) is the
umbrella over everything else: *"one pipeline is why 0002, 0003, 0004, 0007 and 0008 exist once
rather than twice"*. The matcher is
[ADR-0002](../adr/0002-match-on-an-exact-external-reference-first-then-a-date-and-amount-window.md)
over the snapshot of
[ADR-0003](../adr/0003-take-one-snapshot-of-candidate-movements-before-the-run-writes.md).

Satisfies [AC-20](../PRD.md) and [AC-21](../PRD.md) — the matching itself — and [AC-15](../PRD.md),
because the pipeline is what lets a row an earlier load set aside land once a rule covers it.

## What

In `src/FinNavis.Application/Import/Pipeline/`:

- **The pipeline.** Take the snapshot once, walk the rows the reader yields, ask the policy what to
  do with each, build the movements and the `ImportRun` with all its links and set-aside rows, and
  commit the whole thing in **one** `IUnitOfWork` (ADR-0004).
- **The policy object.** The three differences between the two jobs
  ([PRD §1](../PRD.md)): whether what the ledger lacks is created, whether a mapping profile is
  consulted, and what a move between the owner's own accounts becomes. T16 and T17 supply the two
  implementations.
- **The matcher**, two tiers in order:
  1. **exact** — the file gives the row an identifier and a movement already carries it as its
     `external_ref`;
  2. **proximity** — a movement in the snapshot is on the same account, inside the date window, for
     the same amount, with the parameters **T01 settled**.

Ids are UUID v7 from `Guid.CreateVersion7()` in this layer — `core-ledger` ADR-0005, and
[sad.md §8](../sad.md) names this feature as the caller that record was written for.

## Definition of Done

- [ ] A row whose identifier matches a movement's `external_ref` is counted **already there** and
      not recorded again (AC-20)
- [ ] A row matching a movement the owner typed by hand — which has no `external_ref` — is found by
      the proximity tier and counted already there rather than set aside (AC-21)
- [ ] **Two identical rows inside one file are both recorded**, because the snapshot was taken
      before the run wrote anything. The test fails the moment anything lets the snapshot see the
      run's own work
- [ ] A row an earlier load set aside is recorded once a rule covers it, while the rows that load
      recorded are not recorded a second time (AC-15)
- [ ] The matcher's parameters are the ones T01 settled, cited in a comment pointing at ADR-0002
- [ ] Every movement the run records gets a link carrying its `MovementFingerprint` (ADR-0008)
- [ ] The whole run commits in one `IUnitOfWork` — one call, not one per row
- [ ] `src/FinNavis.Application` references no EF Core
- [ ] `./build.sh test` is green

## Notes

- **Blocked by T01 and that is the point.** The proximity tier has no date window, no text rule and
  no answer for "one row matches two movements" until PRD §8 is answered. Starting here first
  produces a matcher that has to be rewritten.
- **If T01 finds no stable operation number, this task gets smaller.** The exact tier never fires
  and the matcher is one tier. Write it that way rather than keeping dead code; ADR-0002 will say
  so.
- **The riskiest code in the feature.** [sad.md §11](../sad.md), High: *"a false duplicate match
  deletes real spending silently... nothing is shown to the owner when a row is judged already
  there."* The identical-rows test is the structural half of the guard; the rest is T01.
- **One folder per use case under `Import/`.** This task owns `Pipeline/` only. T15–T19 each own
  theirs, which is what keeps the six Application tasks out of a single lane.
- **The snapshot is taken once, before the first write** — not refreshed mid-run, not re-read per
  row. ADR-0003's whole argument depends on ADR-0004 committing the run at once.
