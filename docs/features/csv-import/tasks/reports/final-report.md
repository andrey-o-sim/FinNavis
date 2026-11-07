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
