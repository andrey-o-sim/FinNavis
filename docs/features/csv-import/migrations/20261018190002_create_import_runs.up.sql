-- REFERENCE SPEC — not an applied migration. See .claude/rules/migrations.md.
--
-- csv-import · import_runs · SAD §5 · PRD US-02, US-03, US-04, US-06 · ADR-0004, ADR-0005, ADR-0007

CREATE TABLE IF NOT EXISTS import_runs (
    id                        UUID         NOT NULL,
    file_name                 VARCHAR(255) NOT NULL,
    kind                      VARCHAR(20)  NOT NULL,
    status                    VARCHAR(20)  NOT NULL,
    account_id                UUID         NULL,
    recorded_row_count        INTEGER      NOT NULL,
    already_present_row_count INTEGER      NOT NULL,
    created_at                TIMESTAMPTZ  NOT NULL DEFAULT now(),

    CONSTRAINT pk_import_runs PRIMARY KEY (id),
    CONSTRAINT fk_import_runs_account
        FOREIGN KEY (account_id) REFERENCES accounts (id) ON DELETE SET NULL
);

-- file_name is "the name the file came with" (PRD §9). Stored and shown back as plain text, never
-- interpreted (PRD §6.1). Bounded at 255 — the common filesystem limit, a working bound; the PRD
-- states none (data-model.md decision 10).
--
-- kind is ImportRunKind, stored as a string (CLAUDE.md): 'Statement' | 'HistoryMigration'. These
-- are the two entry points of ADR-0005's one pipeline.
--
-- status is ImportRunStatus, stored as a string: 'Completed' | 'Undone'. Read by flow 5 (does the
-- report say the run was undone?) and by flow 6's already-undone refusal.
-- There is NO 'Failed' value. ADR-0004 commits the whole run in one database transaction, so a load
-- that fails leaves no row here at all — which is exactly what AC-22 promises the Owner.
--
-- account_id is the account a statement was loaded onto (PRD §9). ALWAYS NULL when
-- kind = 'HistoryMigration', because an export names an account per row instead (AC-10).
-- ON DELETE SET NULL, so a null is read together with kind (data-model.md decision 8):
--   kind='HistoryMigration' + null  → there never was one
--   kind='Statement'        + null  → the account has been deleted since
-- Without SET NULL, an old run would block an account delete for ever, and core-ledger AC-09 has no
-- refusal message written for that case.
--
-- recorded_row_count and already_present_row_count are STORED, not derived (AC-13, decision 6).
-- "Already there" is derivable from nothing: a row judged already present leaves no trace in the
-- ledger. "Recorded" looks derivable by counting the link tables, but undo deletes those links
-- while this row survives with status 'Undone', so a counted value would silently become 0 and the
-- run report would start lying. set_aside_row_count is deliberately NOT stored — those rows survive
-- undo, and flow 5 reads all of them anyway.
--
-- created_at is "the day it was handed over" (PRD §9) — a record-keeping moment, so TIMESTAMPTZ and
-- not DATE. No separate handed_over_on column: the run is written in the same commit as everything
-- it did (ADR-0004), so the two values could never differ.
--
-- No mapping_profile_id: nothing reads it. AC-13 asks for counts and set-aside rows, and ADR-0009
-- rejected naming profiles at all.
-- No undone_at: flow 5 asks WHETHER a run was undone, never when.
-- No updated_at (.claude/rules/migrations.md default — nothing reads a last-changed value).

-- Query it serves: the FK index on account_id required by .claude/rules/migrations.md. Without it,
-- ON DELETE SET NULL scans this table on every account delete. No SAD §6 flow filters runs by
-- account — flow 5 reads one run by id.
CREATE INDEX IF NOT EXISTS idx_import_runs_account_id
    ON import_runs (account_id);
