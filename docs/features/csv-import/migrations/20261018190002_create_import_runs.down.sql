-- REFERENCE SPEC — reverses 20261018190002_create_import_runs.up.sql
--
-- Indexes first, then the table.

DROP INDEX IF EXISTS idx_import_runs_account_id;

DROP TABLE IF EXISTS import_runs;
