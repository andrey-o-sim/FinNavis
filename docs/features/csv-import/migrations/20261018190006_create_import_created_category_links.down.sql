-- REFERENCE SPEC — reverses 20261018190006_create_import_created_category_links.up.sql
--
-- Indexes first, then the table.

DROP INDEX IF EXISTS idx_import_created_category_links_import_run_id;

DROP TABLE IF EXISTS import_created_category_links;

-- ux_import_created_category_links_category_id is a UNIQUE constraint, dropped with the table.
