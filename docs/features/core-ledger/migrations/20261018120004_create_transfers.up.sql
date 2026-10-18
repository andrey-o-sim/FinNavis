-- REFERENCE SPEC — not an applied migration. See .claude/rules/migrations.md.
--
-- core-ledger · transfers · SAD §5 · PRD US-02 · ADR-0001

CREATE TABLE IF NOT EXISTS transfers (
    id                     UUID          NOT NULL,
    source_account_id      UUID          NOT NULL,
    destination_account_id UUID          NOT NULL,
    amount                 NUMERIC(19,4) NOT NULL,
    occurred_on            DATE          NOT NULL,
    created_at             TIMESTAMPTZ   NOT NULL DEFAULT now(),

    CONSTRAINT pk_transfers PRIMARY KEY (id),
    CONSTRAINT fk_transfers_source_account
        FOREIGN KEY (source_account_id)      REFERENCES accounts (id),
    CONSTRAINT fk_transfers_destination_account
        FOREIGN KEY (destination_account_id) REFERENCES accounts (id)
);

-- One row, not two. AC-06 says a transfer is never applied to one side only; a single row makes a
-- half-applied transfer unrepresentable rather than merely prevented (flow 2).

-- One amount for both sides, so AC-04 and AC-15 hold by construction: the source falls and the
-- destination rises by the same figure, and the total across all accounts is unchanged.

-- source_account_id <> destination_account_id is NOT a CHECK. AC-05 is a domain invariant, refused
-- in the Transfer entity before the row is ever built.

-- No category_id and no note. A transfer carries no category (SAD §12), which is what makes AC-23
-- a refusal rather than a validation rule — the column it would need does not exist. The breakdown
-- never reads this table, so AC-20 holds structurally.

-- Query it serves: flow 7 subtracts transfers out of an account, and flow 5 checks them before
-- deleting the account (AC-14, AC-09). Also the FK index.
CREATE INDEX IF NOT EXISTS idx_transfers_source_account_id
    ON transfers (source_account_id);

-- Query it serves: flow 7 adds transfers into an account, and flow 5 checks them too
-- (AC-14, AC-09). Also the FK index.
CREATE INDEX IF NOT EXISTS idx_transfers_destination_account_id
    ON transfers (destination_account_id);

-- Query it serves: flow 8 reads the transfers half of the history page by period (AC-16).
CREATE INDEX IF NOT EXISTS idx_transfers_occurred_on
    ON transfers (occurred_on);
