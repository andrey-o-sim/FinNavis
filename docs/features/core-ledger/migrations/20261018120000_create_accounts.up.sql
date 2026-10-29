-- REFERENCE SPEC — not an applied migration.
-- This repo uses EF Core migrations. implement-tasks writes the entity configuration, runs
-- `./build.sh migrate <Name>`, and diffs the generated SQL against this file.
-- See .claude/rules/migrations.md.
--
-- core-ledger · accounts · SAD §5 · PRD US-03

CREATE TABLE IF NOT EXISTS accounts (
    id         UUID         NOT NULL,
    name       VARCHAR(100) NOT NULL,
    created_at TIMESTAMPTZ  NOT NULL DEFAULT now(),

    CONSTRAINT pk_accounts PRIMARY KEY (id)
);

-- Uniqueness is a unique index rather than a UNIQUE table constraint. EF Core writes an index for
-- HasIndex().IsUnique(), and PostgreSQL enforces the two the same way — data-model.md §Indexes
-- says as much. The alternative, HasAlternateKey, would declare a key nothing ever references.
CREATE UNIQUE INDEX IF NOT EXISTS ux_accounts_name ON accounts (name);

-- No balance column: AC-14 computes it from the movements at read time (ADR-0002).
-- No updated_at: nothing in the PRD reads a "last changed" value.
