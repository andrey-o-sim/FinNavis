-- REFERENCE SPEC — not an applied migration. See .claude/rules/migrations.md.
--
-- core-ledger · transactions · SAD §5 · PRD US-01, US-06, US-07, US-08 · ADR-0001, ADR-0006

CREATE TABLE IF NOT EXISTS transactions (
    id           UUID          NOT NULL,
    account_id   UUID          NOT NULL,
    category_id  UUID          NOT NULL,
    amount       NUMERIC(19,4) NOT NULL,
    occurred_on  DATE          NOT NULL,
    note         VARCHAR(500)  NULL,
    external_ref VARCHAR(200)  NULL,
    created_at   TIMESTAMPTZ   NOT NULL DEFAULT now(),

    CONSTRAINT pk_transactions PRIMARY KEY (id),
    CONSTRAINT fk_transactions_account
        FOREIGN KEY (account_id)  REFERENCES accounts (id),
    CONSTRAINT fk_transactions_category
        FOREIGN KEY (category_id) REFERENCES categories (id)
);

-- amount is always above zero. AC-02 is a domain invariant enforced in the Transaction entity,
-- not a CHECK constraint. Direction comes from categories.kind, so a stored negative would
-- contradict the acceptance criterion it is meant to satisfy.

-- occurred_on is a calendar DATE, not a timestamp. It closes the PRD §8 open question about local
-- calendar months: a period filter is a plain date range, so a late-evening purchase cannot slide
-- into the next month. created_at stays TIMESTAMPTZ for record-keeping.

-- note is bounded at 500 characters. Closes the other PRD §8 open question and the storage-bloat
-- abuse case in PRD §6.1. Stored and returned as plain text, never interpreted.

-- external_ref is ALWAYS NULL in this feature (ADR-0006). csv-import and message capture fill it.

-- Query it serves: flow 3 period breakdown filters on the period alone (leading column); flow 8
-- history page filters on period AND account. One index covers both (AC-18, AC-16).
CREATE INDEX IF NOT EXISTS idx_transactions_occurred_on_account_id
    ON transactions (occurred_on, account_id);

-- Query it serves: flow 7 sums every transaction per account over all time, and flow 5 checks
-- whether any transaction names an account before deleting it (AC-14, AC-09). Neither filters by
-- date, so the composite above cannot serve them. Also the FK index on account_id.
CREATE INDEX IF NOT EXISTS idx_transactions_account_id
    ON transactions (account_id);

-- Query it serves: flow 6 checks whether any transaction names a category before deleting it
-- (AC-12). Also the FK index on category_id.
CREATE INDEX IF NOT EXISTS idx_transactions_category_id
    ON transactions (category_id);

-- Required by ADR-0006. Unused here; it makes "this outside thing is already recorded" a database
-- guarantee for csv-import rather than a check that can race. PostgreSQL treats nulls as distinct
-- in a unique index, so any number of null rows is allowed — which is every row this feature writes.
CREATE UNIQUE INDEX IF NOT EXISTS ux_transactions_external_ref
    ON transactions (external_ref);
