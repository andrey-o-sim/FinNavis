-- REFERENCE SPEC — not an applied migration.
-- This repo uses EF Core migrations. implement-tasks writes the entity configuration, runs
-- `./build.sh migrate <Name>`, and diffs the generated SQL against this file.
-- See .claude/rules/migrations.md.
--
-- csv-import · mapping_profiles · SAD §5 · PRD US-01 · ADR-0009

CREATE TABLE IF NOT EXISTS mapping_profiles (
    id         UUID          NOT NULL,
    shape_key  VARCHAR(1000) NOT NULL,
    created_at TIMESTAMPTZ   NOT NULL DEFAULT now(),

    CONSTRAINT pk_mapping_profiles PRIMARY KEY (id)
);

-- shape_key is the normalised header row of the file, stored readable rather than hashed
-- (ADR-0009, data-model.md decision 2): each column name trimmed and lowercased, trailing empty
-- columns dropped, joined with '|', in the file's own column order. It IS the identity of a
-- profile — the Owner names nothing and picks nothing (AC-01).
--
-- The bound of 1000 is a working bound; the PRD states none. It becomes the validation rule in the
-- reader: a header longer than this is not a statement, and the file is refused before anything is
-- recorded (PRD §6.1 abuse case). 1000 characters is also safely inside PostgreSQL's btree index
-- entry limit, so the unique index below cannot fail on a long header.
--
-- No name column. ADR-0009 rejected one explicitly: a name that takes no part in the lookup can
-- only go stale, and AC-13 asks for counts and set-aside rows, never for a profile's name.
--
-- No updated_at. Re-confirming a shape replaces its rules; the profile row itself is never edited.

-- Query it serves: flows 1 and 2 look up one profile by the file's header row, once per load
-- (AC-01, AC-05). The unique index serves the lookup, so nothing further is added for it.
-- Uniqueness is a unique index rather than a UNIQUE table constraint. EF Core writes an index for
-- HasIndex().IsUnique(), and PostgreSQL enforces the two the same way. Same convention as
-- core-ledger's ux_accounts_name.
CREATE UNIQUE INDEX IF NOT EXISTS ux_mapping_profiles_shape_key
    ON mapping_profiles (shape_key);
