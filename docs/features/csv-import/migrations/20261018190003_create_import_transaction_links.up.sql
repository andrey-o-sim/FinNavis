-- REFERENCE SPEC — not an applied migration. See .claude/rules/migrations.md.
--
-- csv-import · import_transaction_links · SAD §5 · PRD US-04, US-06 · ADR-0007, ADR-0008

CREATE TABLE IF NOT EXISTS import_transaction_links (
    id             UUID        NOT NULL,
    import_run_id  UUID        NOT NULL,
    transaction_id UUID        NOT NULL,
    fingerprint    VARCHAR(64) NOT NULL,
    created_at     TIMESTAMPTZ NOT NULL DEFAULT now(),

    CONSTRAINT pk_import_transaction_links PRIMARY KEY (id),
    CONSTRAINT fk_import_transaction_links_import_run
        FOREIGN KEY (import_run_id)  REFERENCES import_runs (id),
    CONSTRAINT fk_import_transaction_links_transaction
        FOREIGN KEY (transaction_id) REFERENCES transactions (id) ON DELETE CASCADE
);

-- This table is ADR-0007: import bookkeeping lives in this feature's own tables, pointing at
-- core-ledger's rows by id with a real foreign key. NO column is added to transactions, so this
-- feature's migration adds tables and alters none — the strongest form of SPEC.md's safe-schema-
-- change rule, and what makes PRD §9's "nothing already recorded loses anything" true by
-- construction.
--
-- One link table per target rather than one table with nullable foreign keys (data-model.md
-- decision 3). ADR-0007 framed the alternative as "nullable FKs plus a check constraint", but
-- .claude/rules/migrations.md forbids CHECK, so that alternative degrades to a rule the database
-- cannot see. Here every FK is NOT NULL and a link pointing at the wrong kind of thing is
-- unrepresentable — the same argument core-ledger ADR-0001 used to keep transactions and transfers
-- apart.
--
-- ON DELETE CASCADE on transaction_id, and NOT on import_run_id. This is a deliberate departure
-- from core-ledger's "no ON DELETE CASCADE anywhere" and it is what makes ADR-0008 true: that
-- record states a movement the Owner deleted by hand "is simply absent at undo time… so undo
-- proceeds for the rest". Without the cascade PostgreSQL would refuse that delete outright, and
-- core-ledger AC-22 — a removed entry disappears from history — would stop working for every
-- imported row. Nothing deletes an import run (SAD §11: undone runs accumulate), so the other FK
-- needs no cascade.
--
-- fingerprint is ADR-0008: SHA-256 over the fields the run wrote, rendered as 64 lowercase hex
-- characters. The field list is fixed in data-model.md § "The content fingerprint" and is, in this
-- exact order, joined with '|':
--     account_id | category_id | amount | occurred_on | note
-- with UUIDs lowercase 8-4-4-4-12, amount as ToString("F4", InvariantCulture), occurred_on as
-- yyyy-MM-dd, and note as the empty string when null.
-- SAD §11 warns that the day a movement gains an editable field, AC-18 silently stops noticing
-- edits to it. The entity configuration must carry a comment pointing at ADR-0008 for that reason.
-- Written by the import, read only if the run is ever undone — most of it is never used, and
-- ADR-0008 accepts that.
--
-- The fingerprint lives here and not on transactions (ADR-0007): it describes what THIS LOAD WROTE,
-- not what the movement IS.

-- Query it serves: flow 5 reads everything one run brought (AC-13) and flow 6 walks the same links
-- to delete them (AC-16). Also the FK index on import_run_id required by
-- .claude/rules/migrations.md.
CREATE INDEX IF NOT EXISTS idx_import_transaction_links_import_run_id
    ON import_transaction_links (import_run_id);

-- Query it serves: the FK index on transaction_id, which ON DELETE CASCADE needs on every
-- transaction delete. The unique index below serves it, so no second index is added. The
-- uniqueness itself says a transaction belongs to at most one run — no acceptance criterion
-- requires that, and it is still right, because two runs claiming the same row
-- would make undo ambiguous. Recorded as a finding in the audit report.
-- Uniqueness is a unique index rather than a UNIQUE table constraint. EF Core writes an index for
-- HasIndex().IsUnique(), and PostgreSQL enforces the two the same way. Same convention as
-- core-ledger's ux_accounts_name.
CREATE UNIQUE INDEX IF NOT EXISTS ux_import_transaction_links_transaction_id
    ON import_transaction_links (transaction_id);
