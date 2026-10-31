-- REFERENCE SPEC — not an applied migration. See .claude/rules/migrations.md.
--
-- csv-import · mapping_profile_rules · SAD §5 · PRD US-01, US-05 · ADR-0009

CREATE TABLE IF NOT EXISTS mapping_profile_rules (
    id                 UUID         NOT NULL,
    mapping_profile_id UUID         NOT NULL,
    source_category    VARCHAR(200) NOT NULL,
    target             VARCHAR(30)  NOT NULL,
    target_category_id UUID         NULL,
    created_at         TIMESTAMPTZ  NOT NULL DEFAULT now(),

    CONSTRAINT pk_mapping_profile_rules PRIMARY KEY (id),
    CONSTRAINT fk_mapping_profile_rules_mapping_profile
        FOREIGN KEY (mapping_profile_id) REFERENCES mapping_profiles (id) ON DELETE CASCADE,
    CONSTRAINT fk_mapping_profile_rules_category
        FOREIGN KEY (target_category_id) REFERENCES categories (id)
);

-- source_category is the file's OWN category text, exactly as the file writes it — not one of the
-- Owner's categories. Bounded at 200 to match transactions.external_ref's existing bound; the PRD
-- states none (data-model.md decision 10).
--
-- target is MappingRuleTarget, stored as a string and never as an int (CLAUDE.md). TWO values:
--   'Category'                   — file this row under target_category_id (AC-01, AC-05)
--   'TransferBetweenOwnAccounts' — a statement load sets the row aside (AC-06); a history
--                                  migration records a Transfer (AC-11)
-- PRD AC-01 reads as if a third target exists — "recorded with no category". It cannot be stored:
-- transactions.category_id is NOT NULL and GLOSSARY.md says every transaction names exactly one
-- category. Confirmed with the owner on 2026-10-18 that a category is required and there is no
-- default category. A file category the Owner does not want to file gets no rule at all, and its
-- rows are set aside with reason 'NoRuleCovers' (AC-08, AC-14). See data-model.md decision 1.
-- AC-01 and SAD §5 were both corrected on 2026-10-18 to describe two rule targets.
--
-- target_category_id is set EXACTLY when target = 'Category', and null otherwise. That is a domain
-- invariant refused in the MappingRule entity, NOT a CHECK constraint — the database stays dumb
-- storage (.claude/rules/migrations.md). AC-02 is what makes the FK worth having: a rule pointing
-- at a category the ledger does not hold is refused before the row is built, and the FK makes a
-- stale pointer unrepresentable afterwards.
--
-- ON DELETE CASCADE on mapping_profile_id only: a rule has no life outside its profile, and
-- re-confirming a shape replaces its rules wholesale. The FK into categories carries NO cascade —
-- deleting a category must never silently rewrite the rules the Owner confirmed.
--
-- No recorded movement points at a rule. That is the whole of AC-04: changing a rule cannot change
-- anything already recorded, because nothing recorded knows a rule exists (SAD §5).

-- Query it serves: flows 1, 2 and 3 load every rule of one profile — mapping_profile_id is the
-- filter and leads this index. Also the FK index on mapping_profile_id required by
-- .claude/rules/migrations.md. The uniqueness stops two rules disagreeing about the same file
-- category (AC-01); no acceptance criterion refuses a duplicate, so it is recorded in the audit.
-- Uniqueness is a unique index rather than a UNIQUE table constraint. EF Core writes an index for
-- HasIndex().IsUnique(), and PostgreSQL enforces the two the same way. Same convention as
-- core-ledger's ux_accounts_name.
CREATE UNIQUE INDEX IF NOT EXISTS ux_mapping_profile_rules_profile_source_category
    ON mapping_profile_rules (mapping_profile_id, source_category);

-- Query it serves: the FK index on target_category_id required by .claude/rules/migrations.md.
-- Without it, deleting or updating a category scans this table.
CREATE INDEX IF NOT EXISTS idx_mapping_profile_rules_target_category_id
    ON mapping_profile_rules (target_category_id);
