-- REFERENCE SPEC — not an applied migration. See .claude/rules/migrations.md.
--
-- core-ledger · category_groups · SAD §5 · PRD US-04, US-09

CREATE TABLE IF NOT EXISTS category_groups (
    id         UUID         NOT NULL,
    name       VARCHAR(100) NOT NULL,
    kind       VARCHAR(16)  NOT NULL,
    created_at TIMESTAMPTZ  NOT NULL DEFAULT now(),

    CONSTRAINT pk_category_groups           PRIMARY KEY (id),
    CONSTRAINT ux_category_groups_name_kind UNIQUE (name, kind)
);

-- kind is CategoryKind (Expense | Income), stored as a string, never as an int (CLAUDE.md).
-- A group belongs to one side of the ledger, exactly as a category does (AC-26, AC-30, CONTEXT.md).
-- No CHECK on it: the allowed values are a domain rule, enforced by the enum in code.

-- UNIQUE is (name, kind), not (name) alone — AC-31. A spending "Gifts" and an earning "Gifts" are
-- two separate groups, mirroring ux_categories_name_kind on the categories table.

-- AC-30 (a group and the categories inside it share a side) is NOT enforced here. It is a domain
-- invariant, refused by the entity before the row is written — the same treatment AC-02 and AC-05
-- get (data-model.md decision 6). A composite FK on (id, kind) was considered and rejected: it
-- would make the rule structural at the cost of a second unique index and a frozen kind column.

-- The Default group is NOT a row here. The breakdown produces it for categories with no group
-- (SAD §12, flow 3); it cannot be renamed, deleted or picked when filing a transaction. AC-29
-- leaves a category with no group at all, which is how it falls there — nothing is assigned to it.
