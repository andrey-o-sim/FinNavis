---
status: Accepted
slug: "csv-import"
stage: "03"
updated_at: "2026-10-17"
---

# 0006 — Make the file reader a thin port that streams rows and knows nothing about the ledger

- **Status:** Accepted
- **Date:** 2026-10-17
- **Deciders:** Andrey Simonenko

## Context

This is the first feature in the product that reads anything from outside the process, so it adds
the first outside-world port of its kind. `ARCHITECTURE.md` anticipated one and used the name
`IStatementImporter` as its illustration of an Application port — but it named it before anything
was designed, so the name is an example rather than a commitment.

Two things about that port have to be settled before §5 (Building block view) can be drawn, and both
are expensive to change afterwards because every caller sees them.

**How wide is it.** A port called an *importer* suggests it reads the file *and* turns the rows into
movements. That would put the Mapping profile rules, the duplicate match and the set-aside reasons
behind the port — which means inside Infrastructure.

**What it returns.** A list of rows, or a stream of them. PRD §6.1 keeps an abuse case: a file
handed over that is not a statement, huge, with millions of rows. PRD §8 leaves the size limit open
with a standing default of "none enforced".

## Decision drivers

- `ARCHITECTURE.md` § Ports and adapters — a port is an interface whose other side is outside the
  application; Application says *what* it needs and never *how*.
- SAD §2 Constraints — the dependency rule, enforced by `tests/FinNavis.ArchitectureTests`:
  Application may not reference a CSV library, so parsing has to be behind an interface.
- PRD §6.1 — the abuse case of a file that is not a statement; PRD §8 — the size limit is open.
- PRD §5 AC-03 — a file that does not name the date, the amount or the account is refused with the
  missing one named, before anything is recorded.
- ADR-0005 — one pipeline carries the three policy differences; keeping policy out of the reader is
  what lets both jobs share it.

## Considered options

1. **A thin reader that streams.** `IStatementReader` takes a stream and yields rows — the header it
   found, and then each row as the file's own raw values. It knows nothing about accounts,
   categories, rules or the ledger. The pipeline consumes the rows.
2. **A thin reader that returns a list.** Same boundary, but the whole file is parsed into memory
   and handed back as a collection.
3. **A wide importer.** The port takes a file and a profile and returns movements ready to record,
   so parsing and mapping both live behind it.

## Decision outcome

**Chosen: option 1, a thin reader that streams.**

**On the boundary.** Option 3 was rejected because it puts the mapping rules — the heart of the
feature, covered by AC-01, AC-02, AC-04, AC-06 and AC-08 — inside Infrastructure, where they can be
tested only against a real file. Keeping the port at "bytes to rows" leaves every rule in Domain and
Application, where it can be unit-tested with no file at all, and leaves the adapter with one job
that a library already does. It also means a second file format later is a second adapter rather
than a second copy of the rules. The port is therefore named `IStatementReader`, not
`IStatementImporter`: the port reads, the use case imports.

**On streaming.** Both shapes meet PRD §6 comfortably at ~1500 rows, so this is not a performance
choice. It is about which failure is possible. With option 2 the abuse case from PRD §6.1 — a file
with millions of rows — exhausts memory before any check can run, including the AC-03 check that is
supposed to refuse the file. With a stream the pipeline can count rows as it reads and refuse once a
configured ceiling is passed, with nothing recorded. The ceiling's actual value stays the open
question PRD §8 already carries, but the shape that makes a ceiling possible is fixed here, because
changing the port's signature later means changing every caller.

A cost worth stating: the confirm step has to read the file to report its categories (AC-01), and
the load step reads it again (ADR-0001). With a stream, "the distinct categories in this file" is a
pass over the stream rather than a query over a list. That is one extra pass over a small file, and
it is the whole price.

## Consequences

**Positive**
- Every mapping rule stays in Domain and Application and is unit-testable with no file.
- A row ceiling is expressible, so PRD §6.1's abuse case ends in a refusal rather than in an
  `OutOfMemoryException`.
- The adapter has one responsibility and the CSV library stays an Infrastructure detail, invisible
  to the dependency rule and replaceable without touching a use case.
- A second file format later is a second adapter behind the same port.

**Negative**
- A streamed row can only be looked at once. Anything needing two passes — the distinct categories
  for AC-01, a count before deciding — either re-reads or buffers, and the buffer brings back the
  memory question in a smaller form.
- The ceiling is configuration that must actually be enforced somewhere in the pipeline. The port
  makes it possible and does not make it happen; if nobody writes the check, option 1 behaves like
  option 2.
- An async stream is harder to assert over in tests than a list, and every consumer has to carry a
  `CancellationToken` through (`CLAUDE.md`).

**Neutral**
- The record does not pick a CSV library. That is an Infrastructure detail settled at
  implementation, and warnings-as-errors plus the architecture tests already constrain where it may
  appear.
- It says nothing about encoding, delimiter or culture detection. Those live in the adapter and will
  be driven by the real sample files PRD §9 asks for.

## Links

- PRD: [[../PRD.md]] — US-01, US-02, AC-01, AC-03, §6.1, §8 open questions
- SAD: [[../sad.md]] §2, §5, §6, §8, §11
- Related ADR: [[0001-never-store-the-uploaded-file]] · [[0005-run-both-import-jobs-through-one-pipeline-with-two-entry-points]]
