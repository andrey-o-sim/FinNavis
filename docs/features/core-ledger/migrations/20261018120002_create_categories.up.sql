-- REFERENCE SPEC — not an applied migration. See .claude/rules/migrations.md.
--
-- core-ledger · categories · SAD §5 · PRD US-04, US-10

CREATE TABLE IF NOT EXISTS categories (
    id                UUID         NOT NULL,
    name              VARCHAR(100) NOT NULL,
    kind              VARCHAR(16)  NOT NULL,
    category_group_id UUID         NULL,
    created_at        TIMESTAMPTZ  NOT NULL DEFAULT now(),

    CONSTRAINT pk_categories PRIMARY KEY (id),
    CONSTRAINT fk_categories_category_group
        FOREIGN KEY (category_group_id) REFERENCES category_groups (id)
);

-- A unique index, not a UNIQUE table constraint — same reason as ux_accounts_name.
CREATE UNIQUE INDEX IF NOT EXISTS ux_categories_name_kind
    ON categories (name, kind);

-- kind is CategoryKind (Expense | Income), stored as a string, never as an int (CLAUDE.md).
-- No CHECK on it: the allowed values are a domain rule, enforced by the enum in code.

-- category_group_id is nullable because a group is optional (SAD §12). Moving a category between
-- groups updates this column only, so AC-13 holds: no transaction row is rewritten and the
-- category's figure for a closed period cannot change.

-- No ON DELETE CASCADE anywhere. AC-12 blocks deleting a category that history names, and that
-- check runs in Application before the delete (flow 6). A cascade would do the opposite.

-- No ON DELETE SET NULL either, even though AC-29 clears this column when a group is removed.
-- The removing use case does it explicitly, in one transaction: UPDATE the categories to NULL,
-- then DELETE the group. Keeping it in Application means AC-29 is covered by a use-case test, and
-- a forgotten UPDATE fails loudly on the FK instead of quietly rewriting rows the DB chose for us.

-- FK index — required by .claude/rules/migrations.md.
-- Query it serves: flow 3 joins categories to their current group for group totals (AC-18, AC-19);
-- AC-33 lists categories narrowed to one group; AC-29 finds the categories to clear before the
-- group row is removed.
CREATE INDEX IF NOT EXISTS idx_categories_category_group_id
    ON categories (category_group_id);
