# TDD loop — csv-import

## Run 1 — 2026-11-07 17:59

Result: stopped at T10 — Gate 2 (check 5: no stub left): GetCreationsNamedOutsideRunAsync is still a NotImplementedException stub in EfImportQueries.cs; it belongs to T11 and no T10 test reaches it. Continue on error: no.
Tried 2 tasks: 1 done, 1 failed.

### Done
- T09 — Add EfImportRunRepository and EfMappingProfileRepository

### Failed
- T10 — Implement the candidate snapshot in EfImportQueries. Gate 2, check 5 (no stub left): GetCreationsNamedOutsideRunAsync still throws NotImplementedException; it belongs to T11. Rolled back by c4990ee. See [reports/T10.md](T10.md).

### Needs attention
- T09 — FOLLOW_UP: ContainerResolutionTests.cs still lists IMappingProfileRepository and IImportRunRepository in PortsWithoutAnAdapterYet. Both now have an adapter in DI, so move them to the active set in a tests change.
- T09 — The GREEN commit registered both adapters in InfrastructureServiceCollectionExtensions, so part of T13 is already done.
- T09 — A stale RED commit a970b3a may remain on branch feature/csv-import-repositories.
- T09 — Run ./build.sh test before the PR. The gates ran only FinNavis.Infrastructure.IntegrationTests and FinNavis.ArchitectureTests. See [reports/T09.md](T09.md).
- T10 — Decide whether Gate 2 check 5 should allow a stub for a member that another todo task owns. T10 and T11 share EfImportQueries, so the T10 stub for T11's method can never pass check 5. Or merge or reorder T10 and T11.
- T10 — DI registration of EfImportQueries as IImportQueries is not done. T13 owns it.

### Not tried
- T11 — not reached, the loop stopped
- T12 — not reached, the loop stopped
- T15 — not reached, the loop stopped
- T18 — not reached, the loop stopped
- T19 — not reached, the loop stopped
- T13 — waits for T10, failed in this run
- T14 — waits for T01 (todo, docs layer) and T10, failed in this run
- T16 — waits for T14 (todo)
- T17 — waits for T14 (todo)
- T01 — docs layer
- T02 — docs layer
- T27 — docs layer
- T20 — ports layer
- T21 — ports layer
- T22 — ports layer
- T23 — ports layer
- T24 — tests layer
- T25 — tests layer
- T26 — tests layer

## Run 2 — 2026-11-07 18:45

Result: finished. Continue on error: yes.
Tried 6 tasks: 5 done, 1 failed.

### Done
- T10 — Implement the candidate snapshot and the AC-17 still-in-use check in EfImportQueries
- T12 — Add the CSV adapter behind IStatementReader and the synthetic sample files
- T15 — Add the InspectFile and ConfirmMappingProfile use cases
- T18 — Add the GetImportRun use case
- T19 — Add the UndoImportRun use case

### Failed
- T13 — Register the four import adapters in the Infrastructure container. Gate 1: the test-writer found no honest RED test, because T09, T10 and T12 already registered all four adapters, so nothing new could fail first. Nothing to roll back. See [reports/T13.md](T13.md).

### Needs attention
- T13 — the work is already in code. Only the skipped resolve test in tests/FinNavis.Infrastructure.IntegrationTests/DependencyInjection/ContainerResolutionTests.cs is left. Unskip it in one test(csv-import) commit with trailer `Task: csv-import/T13`, outside finnavis-tdd, then mark T13 done.
- T10 — ALREADY_GREEN: LedgerTables_AfterEveryMigration_CarryOnlyTheLedgerOwnIndexes (structural rule, does not call EfImportQueries).
- T10 — IImportQueries is still listed in PortsWithoutAnAdapterYet in ContainerResolutionTests. It has an adapter now, so remove the entry in a test change.
- T12 — ALREADY_GREEN: ImportAdapter_DependsOnNoLedgerOrMappingRuleType (structural rule).
- T12 — ALREADY_GREEN: SampleStatement_CommittedFile_IsNamedTestStatement (structural fact about the fixture).
- T12 — FOLLOW_UP: IStatementReader does not name the row-ceiling refusal type. The adapter throws System.IO.InvalidDataException. The Application task that maps it to Import.FileNotReadable must agree on that type, or the port should name it, with a test.
- T12 — The fixture headers and the recognised column names (date, amount, account) are invented. T01 is still todo. The owner must check them against the real file shape (sad.md §11 residual risk).
- T12 — IStatementReader is registered in DI, but ContainerResolutionTests.PortsWithoutAnAdapterYet still lists it.
- T15 — ALREADY_GREEN: ConfirmMappingProfile_Signature_TakesTheShapeKeyAndNoFile (reflection check).
- T15 — Domain change: DomainRuleViolationException got a new constructor and a Details property. core-ledger uses this type too, so the owner must confirm it.
- T15 — New codes and messages live in src/FinNavis.Application/Import/ImportErrors.cs (Import.MissingRequiredColumn, Category.NotFound with csv-import wording). T20 must take them into account.
- T15 — The test files are in tests/FinNavis.Application.UnitTests/Import/, not in the subfolders named in files_hint. IDE0130 with a namespace named like the class gives CS0118. T16 to T19 will run into the same clash.
- T15 — FOLLOW_UP: InspectFile reads the same stream twice. The real CsvStatementReader does not rewind, so the second pass may start mid-file. Fix it in the reader or the caller, and cover it with an integration test.
- T15 — FOLLOW_UP: the namespaces FinNavis.Application.Import.InspectFile and ...Import.ConfirmMappingProfile have the same names as their classes. Renaming is a public API change and needs a new cycle.
- T15 — The AC-04 test can only catch a regression once a transaction repository is added to the use case.
- T18 — ALREADY_GREEN: Constructor_Dependencies_AreTheImportRunRepositoryOnly (structural rule).
- T18 — ALREADY_GREEN: ReportedSetAsideRow_Shape_CarriesNoReferenceIntoTheLedger (structural rule).
- T19 — FOLLOW_UP: ImportRun.RequireUnchangedByHand has no direct test in FinNavis.Domain.UnitTests. It is covered only through the Application tests. The owner decides whether to add one.
- T19 — GREEN and REFACTOR changed src/FinNavis.Domain/Entities/ImportRun.cs. Domain.UnitTests is not a gate project for an app task.
- T10, T12, T15, T18, T19 — Run ./build.sh test before the PR. The gates ran only a subset of the test projects.

### Changed since an earlier run
- T10 — failed in run 1, done now.

### Not tried
- T01 — docs layer
- T02 — docs layer
- T27 — docs layer
- T14 — waits for T01 (todo, docs)
- T16 — waits for T14 (todo)
- T17 — waits for T14 (todo)
- T20 — ports layer, no HTTP test harness yet
- T21 — ports layer, no HTTP test harness yet
- T22 — ports layer, no HTTP test harness yet
- T23 — ports layer, no HTTP test harness yet
- T24 — tests layer, no production code to drive
- T25 — tests layer, no production code to drive
- T26 — tests layer, no production code to drive
