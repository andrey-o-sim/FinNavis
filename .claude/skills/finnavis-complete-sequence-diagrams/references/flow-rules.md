# Flow rules — what makes a FinNavis §6 block worth its lines

Used in Phase 3, every time a block is drafted.

A happy path alone tells the reader nothing they could not have guessed from §5. Everything below
exists to make the block carry what the boxes cannot.

## Three shapes

Classify before drafting. The shape decides which participants appear.

| Shape | Looks like | Participants |
|---|---|---|
| **Write** | Record, edit, delete, confirm, undo — anything that changes the ledger | `Owner`, `Pres`, `App`, `Dom`, `Inf`, `PG` |
| **Read** | Balances, history, a breakdown, "what did this load do" | `Owner`, `Pres`, `App`, `Inf`, `PG` — **no `Dom`** |
| **Refusal-only** | The whole flow exists to show a refusal — "turn a transaction into a transfer" | Whatever the refusal needs, usually without the `PG` write |

## Refusals come from PRD §10, word for word

PRD §10 is a table of exactly what the owner is told. That wording is a product decision: it was
argued over at the PRD stage and it is what the endpoint will eventually return.

So a refusal branch quotes it:

```
alt no rule covers this row
    App-->>Pres: refuse and say no rule covers this row, so it was set aside instead of being recorded without one
    Pres-->>Owner: the row is set aside, nothing is recorded for it
end
```

Not «refuse with an error», not a tidier paraphrase. A paraphrase drifts from the PRD on the first
edit, and then two documents say different things about the same refusal.

Two rules that go with it:

- **Every refusal says what was not written.** «nothing is created, not even the unknown category»,
  «neither account changed», «no unit of work is committed». Without that line the reader cannot tell
  whether half the operation landed — and in a ledger, half is the failure that matters.
- **Cite the AC ids in the heading**, so the audit in the next pass can find them:
  `**Critical flow 4: correct a recorded entry** (US-08 — AC-21, AC-22, AC-23, AC-25)`.

## A write flow shows the unit of work

`IUnitOfWork` lives in Application (`CLAUDE.md`), and whether a change committed is the single most
important thing a write flow can say. Draw it:

```
App->>Inf: add the Transaction and commit the unit of work
Inf->>PG: insert one row
PG-->>Inf: committed
```

When a flow writes more than one thing, say that they commit together — that is the whole reason the
unit of work exists:

```
Inf->>PG: insert the movements and the links in one database transaction
```

And when a commit can fail, show the branch. A transfer that half-applied is the failure the design
is built to prevent, so the diagram should prove it cannot happen:

```
alt the commit fails
    PG-->>Inf: rolled back
    Inf-->>App: failure
    App-->>Pres: report the failure
    Pres-->>Owner: neither account changed
end
```

## A read flow goes through the query port and skips Domain

ADR-0004 (`separate the read path behind a query port`) is why reads do not load entities. The arrows
cannot show this — an arrow to `Inf` looks the same either way — so a note says it:

```
App->>Inf: ask ILedgerQueries for the period totals
Note over App,Inf: no repository is used here, so no entity is loaded into memory
Inf->>PG: one aggregate query over transactions, joined to the current category-to-group mapping
```

Three things follow:

- **No `Dom` participant** in a read flow. Declaring it and never using it suggests the read path
  loads entities, which is exactly what the ADR decided against.
- **Name the port.** `ILedgerQueries`, `IImportQueries` — these are names from §5, and they tell the
  reader which side of ADR-0004 this flow is on.
- **Say what the query does not touch** when that is a guarantee. «the transfers table is never read,
  so no transfer can be counted as spending or earning» is a line that stops a future bug.

## Messages are semantic

The message is what happens in product words, not how it travels:

- ✅ `record an expense with account, amount, date, category and optional note`
- ✅ `ask for the balance of each account and the total across all of them`
- ❌ `POST /transactions` · ❌ `200 OK` · ❌ `SELECT sum(amount) ... GROUP BY category_id`

Verbs, paths, status codes and page sizes arrive at the API-contract stage. A diagram carrying them
has to be rewritten the first time a route changes, and routes change more often than flows do.

Same for the use case: `Pres->>App: RecordTransaction use case` — the class name is real and stable,
and it is what a reader greps for.

## Notes that earn their place

A `Note over` is worth its line when it states something the arrows **imply but never say**:

- an invariant — «a transfer is one row and no balance is stored, so a half-applied transfer is not
  representable»;
- a precondition — «no balance is stored anywhere, so every balance here is computed from the
  movements at read time»;
- a postcondition — «history is read, never changed, so browsing can be repeated as often as the
  owner likes»;
- an index hint — «reads transactions and transfers by date and by account (informs data-model
  indexes)»;
- a deliberate absence — «the transfers table is never read».

A note that repeats the arrow under it (`Note over App,Inf: the use case calls the port`) is noise.
Delete it.

**Every index-hint note also goes into the Notes-flagged section** in Phase 5. The note helps the
reader of the diagram; the bucket helps the data-model stage, which will not read nine diagrams
looking for them.

## Branch depth

Nested `alt` inside `alt` is readable to two levels and unreadable at three. If a flow needs three,
it is two flows — split it and say under each block how they relate.

Close every `alt`, `else`, `opt` and `loop` with `end`. The lint counts them, because an unclosed
block swallows the rest of the diagram and renders as an error box.
