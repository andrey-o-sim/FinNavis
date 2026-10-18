-- REFERENCE SPEC — reverses 20261018190005_create_import_created_account_links.up.sql
--
-- Indexes first, then the table.

DROP INDEX IF EXISTS idx_import_created_account_links_import_run_id;

DROP TABLE IF EXISTS import_created_account_links;

-- ux_import_created_account_links_account_id is a UNIQUE constraint, dropped with the table.
