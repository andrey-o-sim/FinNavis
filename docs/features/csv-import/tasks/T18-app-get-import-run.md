---
id: T18
title: "Add the GetImportRun use case"
layer: "app"
deps: ["T05", "T09"]
acs: ["AC-13", "AC-14"]
files_hint: ["src/FinNavis.Application/Import/GetImportRun/", "tests/FinNavis.Application.UnitTests/Import/GetImportRun/"]
owner: "Andrey Simonenko"
estimate: "S"
status: "todo"
---

# T18 — Add the GetImportRun use case

## Why

[sad.md §6](../sad.md) flow 5 — US-04, "read what a load did". The smallest use case in the feature:
one aggregate read through its own repository, shaped into the run report.

Satisfies [AC-13](../PRD.md) (the counts and every set-aside row with its reason) and
[AC-14](../PRD.md) (a set-aside row is still listed, and is never recorded on its own).

## What

In `src/FinNavis.Application/Import/GetImportRun/`: load the run through `IImportRunRepository` and
return `recorded_row_count`, `already_present_row_count`, every set-aside row with its
`row_number`, `reason`, `source_category` and `raw_line`, and whether the run has been undone.

The field names come from [api-sync-report.md](../contracts/api-sync-report.md) decision 3, and the
two counts take their names from the columns that store them, so the contract and the schema cannot
drift apart on the wording.

**No query port is used here.** [sad.md §6](../sad.md) flow 5 says so in a note: this is one
aggregate read through its own repository, not an aggregation across the ledger.

## Definition of Done

- [ ] The report carries both counts and every set-aside row with its reason, row number, source
      category and raw line (AC-13) — unit tests in `tests/FinNavis.Application.UnitTests`
- [ ] It reports whether the run was undone
- [ ] Nothing in the use case can turn a set-aside row into a movement (AC-14)
- [ ] The read goes through `IImportRunRepository` and calls no method on `IImportQueries`
- [ ] A run id that does not exist produces the not-found outcome the contract maps to
      `Import.RunNotFound`
- [ ] `./build.sh test` is green

## Notes

- **`already_present_row_count` is stored, not counted.** [data-model.md](../data-model.md): a row
  judged already there leaves no trace in the ledger, so there is nothing to count after the fact.
  Read the column.
- **A run report is a record of one event, not a live view.** [sad.md §11](../sad.md) accepts that
  an old run keeps listing a row as set aside after a later run recorded it. AC-13 asks what the
  load *did*, which is what it keeps saying — do not try to refresh it.
- **Paging belongs to the endpoint, not here.** The load answers with the set-aside list in full;
  the read pages it by cursor ([api-sync-report.md](../contracts/api-sync-report.md), decision 2).
  T23 owns that; this use case returns the rows ordered by `row_number`.
- Runs in parallel with T19 and the whole T14 branch.
