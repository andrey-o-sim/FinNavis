-- REFERENCE SPEC — reverses 20261018190001_create_mapping_profile_rules.up.sql
--
-- Indexes first, then the table.

DROP INDEX IF EXISTS idx_mapping_profile_rules_target_category_id;

DROP TABLE IF EXISTS mapping_profile_rules;

-- ux_mapping_profile_rules_profile_source_category is a UNIQUE constraint, dropped with the table.
