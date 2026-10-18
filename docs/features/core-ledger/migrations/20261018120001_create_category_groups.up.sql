-- REFERENCE SPEC — not an applied migration. See .claude/rules/migrations.md.
--
-- core-ledger · category_groups · SAD §5 · PRD US-04

CREATE TABLE IF NOT EXISTS category_groups (
    id         UUID         NOT NULL,
    name       VARCHAR(100) NOT NULL,
    created_at TIMESTAMPTZ  NOT NULL DEFAULT now(),

    CONSTRAINT pk_category_groups      PRIMARY KEY (id),
    CONSTRAINT ux_category_groups_name UNIQUE (name)
);

-- The Default group is NOT a row here. The breakdown produces it for categories with no group
-- (SAD §12, flow 3); it cannot be renamed, deleted or picked when filing a transaction.
