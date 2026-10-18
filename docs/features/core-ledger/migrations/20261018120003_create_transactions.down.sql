-- REFERENCE SPEC — reverses 20261018120003_create_transactions.up.sql

DROP INDEX IF EXISTS ux_transactions_external_ref;
DROP INDEX IF EXISTS idx_transactions_category_id;
DROP INDEX IF EXISTS idx_transactions_account_id;
DROP INDEX IF EXISTS idx_transactions_occurred_on_account_id;
DROP TABLE IF EXISTS transactions;
