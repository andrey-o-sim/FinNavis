---
id: T12
title: "Add the CSV adapter behind IStatementReader and the synthetic sample files"
layer: "infra"
deps: ["T06"]
acs: ["AC-03"]
files_hint: ["src/FinNavis.Infrastructure/Import/", "tests/FinNavis.Infrastructure.IntegrationTests/Import/", "tests/FinNavis.Infrastructure.IntegrationTests/Fixtures/"]
owner: "Andrey Simonenko"
estimate: "L"
status: "todo"
---

# T12 — Add the CSV adapter behind IStatementReader and the synthetic sample files

## Why

The adapter for [ADR-0006](../adr/0006-make-the-file-reader-a-thin-streaming-port.md), in
`Infrastructure/Import/` — the folder [sad.md §5](../sad.md) added for an adapter that is neither
persistence, repository nor query. It also builds the **synthetic sample files** the whole
integration suite runs off, which [sad.md §7](../sad.md) makes a prerequisite for both PRD §6
measurements.

Satisfies [AC-03](../PRD.md) at the point where it is actually detectable: the adapter reads the
header row and is the only thing that can tell the date, amount or account column is missing.

## What

**The adapter.** Streams rows from the uploaded stream without holding the file in memory, and
yields the file's own text row by row. It knows nothing about the ledger, the rules or the matching
— that is the whole of ADR-0006.

Three things it does own:

- **The shape key** ([ADR-0009](../adr/0009-recognise-a-statement-shape-by-its-header-row.md),
  [data-model.md](../data-model.md) decision 2): the normalised header row — trimmed, lowercased,
  trailing empty columns dropped, joined with `|`, original column order kept.
- **The missing-column check** (AC-03): which of date, amount or — for an export — the account
  column is absent. Reported before anything is recorded.
- **The row ceiling hook** ([sad.md §8](../sad.md), PRD §6.1): enforced while the file streams, so a
  file that is not a statement is refused before anything is recorded. The **value** is still open
  in PRD §8 — make the ceiling expressible and leave it unset.

**The synthetic sample files**, under `tests/.../Fixtures/`: one statement and one export. Same
header row, same column order, same date and amount formats as the real files, with invented
merchants and amounts. A 1500-row export for T25 and T26 to time against.

## Definition of Done

- [ ] The adapter streams a 1500-row file without loading it into memory, and stops part-way when
      the `CancellationToken` is cancelled
- [ ] A file naming no date column, no amount column, or — for an export — no account column is
      refused, and **which** one is missing is named (AC-03)
- [ ] The shape key for a given header row matches `data-model.md` decision 2 exactly, including
      the dropped trailing empty columns — unit tests over several header rows
- [ ] The adapter references no ledger type and no mapping-rule type (ADR-0006)
- [ ] The committed sample files carry invented merchants, invented amounts and the file name
      `test-statement.csv` — **no real statement and no real export is in the repository**
- [ ] `./build.sh test` is green

## Notes

- **`CLAUDE.md` Safety, and it cannot be taken back.** One real statement is a full month of the
  owner's spending with the shop names attached. A commit of it needs history rewritten to undo.
  The fixtures copy the **shape** only.
- **Build the fixtures from the real files, not from imagination.**
  [sad.md §11](../sad.md) carries the residual risk: *"a synthetic file may be wrong about the real
  shape, so the suite passes against a format no bank produces."* T01 is when the real files are
  open — do these two in the same sitting if it helps.
- **The date and number formats inside a file are a parsing concern of this adapter**, not a product
  language setting ([sad.md §8](../sad.md)). The owner's text is stored and shown back as plain
  text and **never interpreted** (PRD §6.1).
- **The row ceiling has no value yet.** PRD §8: *"Is there a limit on the size of a file that may be
  handed over? Default now: none enforced."* The refusal code `Import.FileNotReadable` already
  exists in the contract to carry it when the number arrives.
- Runs in parallel with the whole T07 → T08 → T09/T10/T11 arm — it needs only T06.
