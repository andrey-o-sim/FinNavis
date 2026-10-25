---
id: T06
title: "Declare IStatementReader and IImportQueries in Application/Abstractions"
layer: "app"
deps: ["T03"]
acs: []
files_hint: ["src/FinNavis.Application/Abstractions/IStatementReader.cs", "src/FinNavis.Application/Abstractions/IImportQueries.cs", "src/FinNavis.Application/Abstractions/Reads/"]
owner: "Andrey Simonenko"
estimate: "S"
status: "todo"
---

# T06 — Declare IStatementReader and IImportQueries in Application/Abstractions

## Why

The two outside-world ports [sad.md §5](../sad.md) adds. `IStatementReader` is
[ADR-0006](../adr/0006-make-the-file-reader-a-thin-streaming-port.md) — a thin port that turns bytes
into rows and knows nothing about the ledger, the rules or the matching. `IImportQueries` holds the
two reads that belong to no single aggregate: the candidate snapshot of
[ADR-0003](../adr/0003-take-one-snapshot-of-candidate-movements-before-the-run-writes.md) and the
AC-17 check.

`acs` is empty: two interface declarations satisfy no acceptance criterion. The criteria arrive with
the adapters (T10, T11, T12) and the use cases that call them.

## What

**`IStatementReader`** — read the header row, then yield rows one at a time as an
`IAsyncEnumerable<T>` taking a `CancellationToken`. It must be possible to consume a 1500-row file
without holding it in memory, and to stop part-way. It returns the file's own text; it does not
parse a category into one of the owner's and does not know a ledger exists.

**`IImportQueries`** — two methods:

- the candidate snapshot: every ledger movement that could match a row in the file, bounded by the
  accounts the file names and the date range it covers, fetched once before the run writes anything;
- the AC-17 check: which of a run's created accounts and categories a movement from **outside** the
  run now names.

Their return types are read models under `Application/Abstractions/Reads/`, not Domain entities.

## Definition of Done

- [ ] `IStatementReader` yields rows as an `IAsyncEnumerable<T>` and every method takes a
      `CancellationToken`
- [ ] Neither interface mentions EF Core, CSV, `DbContext` or a file path
- [ ] The snapshot method takes the accounts and the date range as parameters — the port cannot be
      called without saying what it is bounded by
- [ ] `src/FinNavis.Application` references **Domain only**: no new `PackageReference`, checked in
      the `.csproj` rather than only through `tests/FinNavis.ArchitectureTests`
- [ ] `./build.sh build` is green — with `TreatWarningsAsErrors` that is also the lint gate
- [ ] `./build.sh test` is green

## Notes

- **The name `IStatementImporter` is wrong and `ARCHITECTURE.md` uses it as an illustration.**
  [sad.md §5](../sad.md) says so directly: a port that *imports* would pull the mapping rules into
  Infrastructure. The real port is narrower — it reads, nothing more.
- **`IImportQueries`, not `core-ledger`'s `ILedgerQueries`.** [sad.md §9](../sad.md) explains why:
  the alternative would make the ledger's own reporting port know about imports.
  `core-ledger` ADR-0004 anticipated the split.
- **`IUnitOfWork` is `core-ledger`'s and is reused unchanged.** Do not declare a second commit
  abstraction here ([sad.md §5](../sad.md)).
- Two tasks unblock from here in parallel: T12 needs only this, and T10/T11 need this plus the
  migration.
