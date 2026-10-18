-- REFERENCE SPEC — not an applied migration. See .claude/rules/migrations.md.
--
-- csv-import · import_set_aside_rows · SAD §5 · PRD US-04, US-05 · ADR-0001, ADR-0007

CREATE TABLE IF NOT EXISTS import_set_aside_rows (
    id              UUID          NOT NULL,
    import_run_id   UUID          NOT NULL,
    row_number      INTEGER       NOT NULL,
    reason          VARCHAR(40)   NOT NULL,
    source_category VARCHAR(200)  NULL,
    raw_line        VARCHAR(2000) NOT NULL,
    created_at      TIMESTAMPTZ   NOT NULL DEFAULT now(),

    CONSTRAINT pk_import_set_aside_rows PRIMARY KEY (id),
    CONSTRAINT fk_import_set_aside_rows_import_run
        FOREIGN KEY (import_run_id) REFERENCES import_runs (id)
);

-- row_number is the row's 1-based ordinal in the file. It gives the report a stable order and lets
-- the Owner find the line in their own copy of the file. SAD §6 "Left for the API-contract stage"
-- notes that nothing says whether this list is paged or ordered — this is the column it will order
-- by, so the schema is ready either way.
--
-- reason is SetAsideReason, stored as a string (CLAUDE.md). THREE values, the three branches SAD §6
-- flows 2 and 4 actually draw:
--   'NoRuleCovers'               — no rule covers this row's category (AC-08, AC-14)
--   'TransferBetweenOwnAccounts' — a move between the Owner's own accounts, on a statement that
--                                  names only one side (AC-06)
--   'ForeignCurrency'            — a currency the ledger does not keep (AC-07)
-- SAD §8 LISTED a FOURTH, "the named account is unknown". It is not here: AC-09 refuses the WHOLE
-- load when the named account does not exist, so no row is ever set aside for it. Reported as a
-- finding and corrected in SAD §8 on 2026-10-18 — data-model.md decision 9.
--
-- source_category is the file's own category text for this row. AC-08 names the uncovered category
-- back to the Owner, and this is what it reads — re-parsing raw_line to recover it would make the
-- report depend on the parser long after the file is gone. NULL means the file named no category
-- for this row. Bounded at 200, matching mapping_profile_rules.source_category.
--
-- raw_line is the file's own line, kept because AC-13 and AC-14 require the Owner to see a
-- set-aside row and act on it. THIS IS THE RAW BANK TEXT SAD §11 flags: shop names the Owner did
-- not write, classified confidential (SAD §2). ADR-0001 keeps whole statements off disk; this is
-- the part that stays, bounded to the rows that were NOT recorded. Never interpreted (PRD §6.1) and
-- never logged (SAD §7, §8 Logging — "never the rows").
-- Bounded at 2000 as a working bound; the PRD states none. It becomes the validation rule in the
-- reader: a line longer than this is not a statement line, and the file is refused before anything
-- is recorded (PRD §6.1 abuse case). VARCHAR rather than TEXT per .claude/rules/migrations.md —
-- the bound is the documentation.
--
-- NO foreign key into the ledger and NO status column. A set-aside row is never recorded on its own
-- (AC-14) — nothing turns one into a transaction. Only a later load of the same file can record it,
-- and that load writes its OWN run with its own rows. So there is no state for this row to be in.
--
-- NO ON DELETE CASCADE from import_runs, unlike the link tables: these rows were never in the
-- ledger, so an undo leaves them untouched and they stay readable on the undone run's report.
--
-- An old run keeps listing a row as set aside after a later run recorded it. SAD §11 accepts this:
-- a run report is a record of one event (CONTEXT.md "Import run"), not a live view.

-- Query it serves: flow 5 lists every set-aside row of one run, with its reason (AC-13, AC-14).
-- Also the FK index on import_run_id required by .claude/rules/migrations.md.
CREATE INDEX IF NOT EXISTS idx_import_set_aside_rows_import_run_id
    ON import_set_aside_rows (import_run_id);
