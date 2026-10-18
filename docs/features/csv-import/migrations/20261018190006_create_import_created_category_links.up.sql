-- REFERENCE SPEC — not an applied migration. See .claude/rules/migrations.md.
--
-- csv-import · import_created_category_links · SAD §5 · PRD US-03, US-06 · ADR-0007

CREATE TABLE IF NOT EXISTS import_created_category_links (
    id            UUID        NOT NULL,
    import_run_id UUID        NOT NULL,
    category_id   UUID        NOT NULL,
    created_at    TIMESTAMPTZ NOT NULL DEFAULT now(),

    CONSTRAINT pk_import_created_category_links PRIMARY KEY (id),
    CONSTRAINT ux_import_created_category_links_category_id UNIQUE (category_id),
    CONSTRAINT fk_import_created_category_links_import_run
        FOREIGN KEY (import_run_id) REFERENCES import_runs (id),
    CONSTRAINT fk_import_created_category_links_category
        FOREIGN KEY (category_id)   REFERENCES categories (id) ON DELETE CASCADE
);

-- The categories half of AC-17, same shape and same reasoning as the account links.
--
-- Only a HISTORY MIGRATION writes here. A statement load creates no category: AC-02 blocks a rule
-- that points at a category the ledger does not hold, and names the unknown category back.
--
-- The AC-17 check at undo time — "does a movement outside this run name this category?" — is served
-- by core-ledger's existing idx_transactions_category_id. Transfers carry no category at all
-- (CONTEXT.md), so only one table has to be asked.
--
-- A CATEGORY GROUP is never created by an import. An export names categories; CONTEXT.md makes a
-- group optional, so a created category simply has none, and the breakdown reports it under the
-- Default group (core-ledger data-model.md). There is no import_created_category_group_links table
-- for that reason.
--
-- ON DELETE CASCADE on category_id for the same reason as the account link: a category the Owner
-- deleted by hand leaves undo nothing to remove.

-- Query it serves: flow 6 walks one run's created categories to decide which to remove (AC-17).
-- Also the FK index on import_run_id required by .claude/rules/migrations.md.
CREATE INDEX IF NOT EXISTS idx_import_created_category_links_import_run_id
    ON import_created_category_links (import_run_id);

-- Query it serves: the FK index on category_id, needed by ON DELETE CASCADE on every category
-- delete. Backed by the UNIQUE constraint above, so nothing is added here.
