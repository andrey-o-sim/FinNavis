-- Template for the staged spec. One pair per table or small aggregate, written into
-- docs/features/<slug>/migrations/ as:
--   <YYYYMMDDhhmmss>_<verb>_<entity>.up.sql   +   <YYYYMMDDhhmmss>_<verb>_<entity>.down.sql
-- Never into src/FinNavis.Infrastructure/Persistence/Migrations — that is EF Core's, and
-- implement-tasks puts it there.
--
-- The comments are not decoration. A human diffs this file against `dotnet ef migrations script`
-- output, and a bare DDL line gives them nothing to check the configuration against.

-- ============================== <timestamp>_create_<entity>.up.sql ==============================

-- REFERENCE SPEC — not an applied migration. See .claude/rules/migrations.md.
--
-- <slug> · <entity> · SAD §5 · PRD US-<nn>, US-<nn> · ADR-<nnnn>

CREATE TABLE IF NOT EXISTS <entity> (
    id          UUID          NOT NULL,
    <col>       VARCHAR(<n>)  NOT NULL,
    <col>       NUMERIC(19,4) NOT NULL,
    <col>       DATE          NOT NULL,
    <col>       VARCHAR(<n>)  NULL,
    <fk>_id     UUID          NOT NULL,
    created_at  TIMESTAMPTZ   NOT NULL DEFAULT now(),

    CONSTRAINT pk_<entity> PRIMARY KEY (id),
    CONSTRAINT uq_<entity>_<col> UNIQUE (<col>),
    CONSTRAINT fk_<entity>_<parent>
        FOREIGN KEY (<fk>_id) REFERENCES <parent> (id)
);

-- <One comment per non-obvious column: the AC, ADR or recorded decision behind it. Especially:
--  - why a value is bounded at N, and that the bound becomes the validation rule in app code;
--  - why a date is DATE and not TIMESTAMPTZ;
--  - why an invariant is NOT a CHECK — it is refused in the Domain entity;
--  - what a nullable column's absence means, and which feature fills it if not this one.>

-- Query it serves: <which SAD §6 flow, which filter, which AC>. <If it is also the FK index
-- required by .claude/rules/migrations.md, say both — one index, two reasons.>
CREATE INDEX IF NOT EXISTS idx_<entity>_<cols>
    ON <entity> (<cols>);

-- ============================= <timestamp>_create_<entity>.down.sql =============================

-- REFERENCE SPEC — not an applied migration. See .claude/rules/migrations.md.
--
-- Reverses <timestamp>_create_<entity>.up.sql exactly: indexes first, then the table.

DROP INDEX IF EXISTS idx_<entity>_<cols>;

DROP TABLE IF EXISTS <entity>;

-- =============================== changing an existing table ====================================
--
-- A table an earlier feature defined follows the zero-downtime rules in
-- .claude/rules/migrations.md, each phase a separate file:
--
--   <timestamp+0>_add_<col>_to_<entity>.up.sql     ALTER TABLE ... ADD COLUMN <col> ... NULL;
--   <timestamp+1>_backfill_<col>.up.sql            UPDATE ... (batched; app code dual-writes)
--   <timestamp+2>_set_<col>_not_null.up.sql        ALTER TABLE ... ALTER COLUMN ... SET NOT NULL;
--
-- A new index on an existing table is CONCURRENTLY, which EF Core does not generate — at
-- implement-tasks it is a raw migrationBuilder.Sql(...) in a migration marked as not wrapped in a
-- transaction. Note that in the spec so the configuration author is not surprised:
--
--   CREATE INDEX CONCURRENTLY IF NOT EXISTS idx_<entity>_<col> ON <entity> (<col>);
--
-- Then say in the audit what the ceremony cost here, and that it buys safety for a database that is
-- not deployed yet. The rule is applied either way; the user decides whether to keep paying for it.

-- ======================================= seeds =================================================
--
-- Lookup and bootstrap seeds are their own file, idempotent:
--
--   INSERT INTO <table> (id, <col>) VALUES ('<deterministic uuid v7>', '<value>')
--   ON CONFLICT DO NOTHING;
--
-- No real-looking personal data anywhere. Test fixtures are builders in the test projects, never a
-- migration.
