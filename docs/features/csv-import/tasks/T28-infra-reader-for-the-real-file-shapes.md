---
id: T28
title: "Rebuild the CSV adapter and the sample files for the real file shapes"
layer: "infra"
deps: ["T12"]
acs: ["AC-03"]
files_hint: ["src/FinNavis.Application/Abstractions/IStatementReader.cs", "src/FinNavis.Application/Abstractions/Reads/", "src/FinNavis.Infrastructure/Import/", "tests/FinNavis.Infrastructure.IntegrationTests/Import/", "tests/FinNavis.Infrastructure.IntegrationTests/Fixtures/"]
owner: "Andrey Simonenko"
estimate: "L"
status: "todo"
---

# T28 — Rebuild the CSV adapter and the sample files for the real file shapes

## Why

T12 built the adapter and the synthetic sample files before the real files were opened. T01 opened
them on 2026-11-07, and neither real file has the shape T12 assumed. [sad.md §7](../sad.md) now
records both real shapes. Until this task lands, the whole suite passes against a format no bank
produces — exactly the residual risk [sad.md §11](../sad.md) names.

Satisfies [AC-03](../PRD.md) again, for the real header rows: the adapter must still name which of
date, amount or account is missing.

## What

The real shapes, from [sad.md §7](../sad.md):

- **Bank statement:** UTF-8 with a BOM, comma, CRLF. One title line **before** the header row.
  Ukrainian column names. Date with a time (`dd.MM.yyyy HH:mm:ss`). Two amounts — the card amount,
  signed, and the purchase amount, unsigned — each with its own currency column. Fields with a
  comma are quoted. Rows of several cards in one file.
- **Old tracker's export:** UTF-16 LE with a BOM, **tab**, CRLF, although the file is named `.csv`.
  Date `yyyy.MM.dd`. The `Currency` column is always empty. The last header name carries a stray BOM
  character. Rows with the category `Account balance`, and trailing rows with no date.

The adapter has to:

- read both encodings and both separators;
- take the kind of file as an input — a statement or an export. `IStatementReader` gains it, and
  every caller passes it. For a statement the first line is always skipped and never read; for an
  export the first line is the header row. Decided by the owner on 2026-11-07: the title line is no
  use to the business logic, and guessing it from its content is fragile — the real one ends with
  9 commas;
- keep the shape key of
  [ADR-0009](../adr/0009-recognise-a-statement-shape-by-its-header-row.md) built from the header row
  only — the title line names a period and changes every month;
- recognise the date, amount and account columns under the real names, not only `date`, `amount`
  and `account`;
- expose what the pipeline needs to take the purchase amount, take its sign from the card amount,
  and read the purchase currency (T16). Whether that changes `StatementHeader` or `StatementColumn`
  is this task's decision; the port stays free of ledger types (ADR-0006).

The synthetic sample files are rebuilt with the same header rows, the same title line, the same
encodings, separators, date and amount formats as the real files, with invented merchants and
amounts. The 1500-row export for T25 and T26 is rebuilt the same way.

## Definition of Done

- [ ] A statement's first line is skipped without being read, whatever it holds — including a title
      line padded with trailing commas, as the real one is; its shape key is the same for two months
      whose title lines differ
- [ ] An export's first line is read as the header row
- [ ] A UTF-16 LE, tab-separated export is read, and the stray BOM in the last header name does not
      change the shape key
- [ ] A file missing the date, the amount or — for an export — the account column is refused, and
      **which** one is missing is named (AC-03), under the real column names
- [ ] A quoted field containing a comma is read as one value
- [ ] A row with no date at the end of the export is not yielded
- [ ] The committed sample files copy the real shapes and carry invented merchants and amounts —
      **no real statement and no real export is in the repository**
- [ ] `./build.sh test` is green

## Notes

- **The export's trailing rows with no date are skipped by the adapter, silently.** Decided by the
  owner on 2026-11-07. They are not movements, so they are neither recorded nor set aside, and the
  set-aside reasons stay a closed set. The `Account balance` rows are not this task's: the adapter
  yields them, and T17 drops them.
- **The real files never enter the repository** (`CLAUDE.md` Safety). They stay on the owner's own
  machine. Copy the shape only.
- **The row ceiling stays unset.** PRD §8 was answered on 2026-11-07: no limit on the size of a file.
- T16 and T17 depend on this task, because the columns they read come from it.
