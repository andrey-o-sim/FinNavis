-- REFERENCE SPEC — reverses 20261018190000_create_mapping_profiles.up.sql

DROP TABLE IF EXISTS mapping_profiles;

-- ux_mapping_profiles_shape_key is a UNIQUE constraint rather than a standalone index, so it is
-- dropped with the table. There is no separate DROP INDEX for it.
