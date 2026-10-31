-- REFERENCE SPEC — not an applied migration. See .claude/rules/migrations.md.
--
-- csv-import · import_created_account_links · SAD §5 · PRD US-03, US-06 · ADR-0007

CREATE TABLE IF NOT EXISTS import_created_account_links (
    id            UUID        NOT NULL,
    import_run_id UUID        NOT NULL,
    account_id    UUID        NOT NULL,
    created_at    TIMESTAMPTZ NOT NULL DEFAULT now(),

    CONSTRAINT pk_import_created_account_links PRIMARY KEY (id),
    CONSTRAINT fk_import_created_account_links_import_run
        FOREIGN KEY (import_run_id) REFERENCES import_runs (id),
    CONSTRAINT fk_import_created_account_links_account
        FOREIGN KEY (account_id)    REFERENCES accounts (id) ON DELETE CASCADE
);

-- This table IS AC-17. Without it, undo cannot tell an account the run created from one the Owner
-- made years ago, and "those are removed too" has no way to know which "those" are.
--
-- Only a HISTORY MIGRATION writes here. A statement load creates no account: AC-09 refuses the
-- whole load when the account the Owner named does not exist, because every row of a statement
-- lands on that one account and there is nothing to load onto without it.
--
-- Whether a created account is actually removed is a SECOND question, answered at undo time by
-- IImportQueries — "does a movement outside this run name it?" (AC-17, flow 6). That query needs no
-- index of its own: core-ledger already declares idx_transactions_account_id,
-- idx_transfers_source_account_id and idx_transfers_destination_account_id.
--
-- ON DELETE CASCADE on account_id: if the Owner deletes a created account by hand, undo has nothing
-- left to remove, which is the same reasoning ADR-0008 gives for a hand-deleted movement. Without
-- it, the delete would be refused and core-ledger AC-09's refusal message would be wrong about why.
-- No cascade on import_run_id — nothing deletes a run.
--
-- No columns beyond the two ids. What the account is called, and when it was made, already live on
-- accounts; copying either here would be a second truth that can drift.

-- Query it serves: flow 6 walks one run's created accounts to decide which to remove (AC-17). Also
-- the FK index on import_run_id required by .claude/rules/migrations.md.
CREATE INDEX IF NOT EXISTS idx_import_created_account_links_import_run_id
    ON import_created_account_links (import_run_id);

-- Query it serves: the FK index on account_id, needed by ON DELETE CASCADE on every account delete.
-- The unique index below serves it, so no second index is added. The uniqueness says an account
-- was created by at most one run, which is a fact about creation rather than a business rule.
-- Uniqueness is a unique index rather than a UNIQUE table constraint. EF Core writes an index for
-- HasIndex().IsUnique(), and PostgreSQL enforces the two the same way. Same convention as
-- core-ledger's ux_accounts_name.
CREATE UNIQUE INDEX IF NOT EXISTS ux_import_created_account_links_account_id
    ON import_created_account_links (account_id);
