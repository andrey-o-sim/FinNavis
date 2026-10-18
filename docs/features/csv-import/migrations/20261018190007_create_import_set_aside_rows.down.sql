-- REFERENCE SPEC — reverses 20261018190007_create_import_set_aside_rows.up.sql
--
-- Indexes first, then the table.

DROP INDEX IF EXISTS idx_import_set_aside_rows_import_run_id;

DROP TABLE IF EXISTS import_set_aside_rows;
