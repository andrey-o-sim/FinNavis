-- REFERENCE SPEC — reverses 20261018190000_create_mapping_profiles.up.sql
--
-- Indexes first, then the table.

DROP INDEX IF EXISTS ux_mapping_profiles_shape_key;

DROP TABLE IF EXISTS mapping_profiles;
