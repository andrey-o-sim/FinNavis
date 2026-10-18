-- REFERENCE SPEC — reverses 20261018190003_create_import_transaction_links.up.sql
--
-- Indexes first, then the table.

DROP INDEX IF EXISTS idx_import_transaction_links_import_run_id;

DROP TABLE IF EXISTS import_transaction_links;

-- ux_import_transaction_links_transaction_id is a UNIQUE constraint, dropped with the table.
