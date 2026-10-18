-- REFERENCE SPEC — reverses 20261018190004_create_import_transfer_links.up.sql
--
-- Indexes first, then the table.

DROP INDEX IF EXISTS idx_import_transfer_links_import_run_id;

DROP TABLE IF EXISTS import_transfer_links;

-- ux_import_transfer_links_transfer_id is a UNIQUE constraint, dropped with the table.
