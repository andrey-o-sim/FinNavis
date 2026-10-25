---
id: T09
title: "Add EfImportRunRepository and EfMappingProfileRepository"
layer: "infra"
deps: ["T08"]
acs: []
files_hint: ["src/FinNavis.Infrastructure/Repositories/EfImportRunRepository.cs", "src/FinNavis.Infrastructure/Repositories/EfMappingProfileRepository.cs", "tests/FinNavis.Infrastructure.IntegrationTests/Repositories/"]
owner: "Andrey Simonenko"
estimate: "M"
status: "todo"
---

# T09 — Add EfImportRunRepository and EfMappingProfileRepository

## Why

The adapters for the two repository ports T04 and T05 declared ([sad.md §5](../sad.md)).
They are written together because both commit through `core-ledger`'s `IUnitOfWork` and both are
proved the same way — a round-trip against a real PostgreSQL.

`acs` is empty: a repository satisfies no acceptance criterion by itself. The criteria arrive with
the use cases in T15, T16, T17, T18 and T19.

## What

**`EfImportRunRepository`** — get one run **with its five child collections** in one read (flows 5
and 6 both need all of them), add a run with everything it did, and save a status change.

**`EfMappingProfileRepository`** — find one profile by shape key with its rules, add a profile, and
replace a profile's rules wholesale.

Both in `src/FinNavis.Infrastructure/Repositories/`, both committing through `core-ledger`'s
`IUnitOfWork` — no second commit abstraction ([sad.md §5](../sad.md)).

## Definition of Done

- [ ] Both ports round-trip against PostgreSQL 18 in Testcontainers — write a run with links,
      creation links and set-aside rows, read it back, and get every child row
- [ ] Loading one run brings its five child collections without a second round trip per collection
- [ ] The profile lookup by shape key uses `ux_mapping_profiles_shape_key` — assert on the plan, and
      let the test fail when the index is dropped
- [ ] A failed commit through `IUnitOfWork` leaves **no** run row, link or set-aside row behind
      (the structural half of AC-22, proved end to end in T25)
- [ ] Replacing a profile's rules leaves the profile's `Id` unchanged and removes every old rule
- [ ] `./build.sh test` is green

## Notes

- **The index assertion is the line people skip.** `CLAUDE.md` says to check for a needed index
  after adding a query; a test that only checks the returned profile passes on a sequential scan.
  The profile lookup runs once per load, so it is cheap either way — but the assertion is what keeps
  the index from being dropped by a later migration nobody re-read.
- **`import_set_aside_rows` does not cascade from the run.** When the repository deletes a run's
  movements at undo time, those rows must still be there (T05, T07).
- **Reading a run's set-aside rows is not a query port.** [sad.md §5](../sad.md) is explicit: it is
  one aggregate read through its own repository, not an aggregation across the ledger.
- Runs in parallel with T10 and T11 — all three unblock from T08 and touch different files.
