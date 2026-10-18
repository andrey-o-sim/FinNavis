-- REFERENCE SPEC — reverses 20261018120004_create_transfers.up.sql

DROP INDEX IF EXISTS idx_transfers_occurred_on;
DROP INDEX IF EXISTS idx_transfers_destination_account_id;
DROP INDEX IF EXISTS idx_transfers_source_account_id;
DROP TABLE IF EXISTS transfers;
