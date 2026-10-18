-- REFERENCE SPEC — not an applied migration. See .claude/rules/migrations.md.
--
-- csv-import · import_transfer_links · SAD §5 · PRD US-03, US-04, US-06 · ADR-0007, ADR-0008

CREATE TABLE IF NOT EXISTS import_transfer_links (
    id            UUID        NOT NULL,
    import_run_id UUID        NOT NULL,
    transfer_id   UUID        NOT NULL,
    fingerprint   VARCHAR(64) NOT NULL,
    created_at    TIMESTAMPTZ NOT NULL DEFAULT now(),

    CONSTRAINT pk_import_transfer_links PRIMARY KEY (id),
    CONSTRAINT ux_import_transfer_links_transfer_id UNIQUE (transfer_id),
    CONSTRAINT fk_import_transfer_links_import_run
        FOREIGN KEY (import_run_id) REFERENCES import_runs (id),
    CONSTRAINT fk_import_transfer_links_transfer
        FOREIGN KEY (transfer_id)   REFERENCES transfers (id) ON DELETE CASCADE
);

-- The transfers half of the movement links. A separate table from import_transaction_links because
-- core-ledger ADR-0001 keeps transactions and transfers apart, so anything covering both covers two
-- (ADR-0007). Every FK stays NOT NULL — see data-model.md decision 3.
--
-- Only a HISTORY MIGRATION ever writes a row here. A statement load sets a move between the Owner's
-- own accounts aside instead (AC-06), because a statement names only one side of it. A history
-- migration records it as a Transfer, because the export names both (AC-11).
--
-- ON DELETE CASCADE on transfer_id for the same reason as the transaction link: ADR-0008 states a
-- hand-deleted movement is simply absent at undo, which is only true if the link goes with it.
-- No cascade on import_run_id — nothing deletes a run.
--
-- fingerprint is ADR-0008, SHA-256 as 64 lowercase hex characters. FOUR fields here, not five,
-- joined with '|' in this exact order:
--     source_account_id | destination_account_id | amount | occurred_on
-- transfers carries no category and no note (core-ledger data-model.md), so the shorter list is a
-- consequence of the schema rather than an omission. Same formatting rules: UUIDs lowercase
-- 8-4-4-4-12, amount as ToString("F4", InvariantCulture), occurred_on as yyyy-MM-dd.
--
-- A transfer carries no external_ref. core-ledger ADR-0006 put that column on transactions only, so
-- a transfer recorded here is reachable by ADR-0002's proximity tier alone. SAD §11 records it;
-- nothing is added here, because the export is loaded once.

-- Query it serves: flow 5 reads everything one run brought (AC-13) and flow 6 walks these links to
-- delete the transfers (AC-16). Also the FK index on import_run_id required by
-- .claude/rules/migrations.md.
CREATE INDEX IF NOT EXISTS idx_import_transfer_links_import_run_id
    ON import_transfer_links (import_run_id);

-- Query it serves: the FK index on transfer_id, needed by ON DELETE CASCADE on every transfer
-- delete. Backed by the UNIQUE constraint above, so nothing is added here. The uniqueness says a
-- transfer belongs to at most one run — same reasoning, and the same audit finding, as the
-- transaction link.
