# Acceptance-criteria coverage — 4 mandatory + 3 conditional

Used in Phase 5 (draft), Phase 6 (the §5 walk) and Phase 8 (self-check).

An acceptance criterion is a **business-observable outcome from the owner's point of view**, written
Given / When / Then. It says what the owner can see happen. It never says how the code does it.

## Why this model differs from the generic one

The generic five types are happy / error / authorization / domain invariant / cross-context, all five
mandatory. On this project **authorization is empty by construction**: one owner, no sign-in, no second
person. The generic run on `core-ledger` had to write a paragraph of apology ("stated deviation") for a
type it could not fill honestly.

Two things replace it, both of which are real risks for a ledger and neither of which the generic model
names: a figure that silently changes after it was already read, and the same movement recorded twice.

## The model

### Always mandatory (4)

| Type | The owner does | The system |
|---|---|---|
| **happy** | the main flow | records it and confirms |
| **error** | submits input that cannot be right (zero amount, missing date) | blocks it and says what must be true instead, in plain words |
| **domain invariant** | violates a rule named in `CONTEXT.md` (a transfer needs two different accounts; a transfer carries no category) | blocks it and names the rule plainly |
| **cross-context** | acts on something whose state belongs elsewhere in the ledger (an unknown category, an account history references) | enforces the rule across that boundary |

### Conditional (at least one must be present; each must be resolved explicitly)

| Type | Mandatory when | N/A when |
|---|---|---|
| **history integrity** | the feature writes or re-reads recorded movements — which in this project is almost everything | the feature touches nothing already recorded (a pure settings screen) |
| **duplicate safety** | data arrives from outside (a file, a message, another tool) or the action can be retried | the action is a single manual entry the owner sees the result of |
| **authorization** | `CONTEXT.md` has more than one actor role, **or** any glossary term about sign-in, session or token exists | neither is true — today, with `Owner` the only actor |

The authorization trigger is deliberately read off the glossary rather than hard-coded. When the sign-in
feature lands and adds a second actor to `CONTEXT.md`, this type becomes mandatory by itself. No edit to
this skill is needed.

**history integrity** examples: a figure already reported for a closed period does not change when the
owner edits a category's group; deleting an entry returns every balance to what it would have been had
the entry never existed; an edit to an amount flows into every balance it affected.

**duplicate safety** examples: loading the same file twice does not create a second transaction; a retried
request does not record the movement twice.

### The floors (checked after every §5 resolution, at every depth)

1. All four mandatory types present.
2. At least one conditional type present.
3. Every conditional type **resolved**: present, or marked `N/A` with a one-line reason in the §5 header
   comment. A type that is N/A because of a gap the project will close later also gets a §8 row with
   owner + due — an honest hole, not a ticked box.
4. Every **retained** §4 user story has at least one AC. Dropping a whole user story is a legitimate
   de-scope and does not fire this floor; a retained story losing its last AC does.

Criteria moved to §8 as open questions do **not** count toward any floor — they live in §8 now.

## Writing an AC

```
### AC-NN (US-XX) — <type>

**Given** <the owner's situation: what exists, what was recorded before>
**When** <the owner does something, in their words>
**Then** <what the owner observes: it is recorded / it is blocked and why / the figure stays as it was>
```

Allowed inside an AC: glossary role names, glossary domain objects, invariant names as plain phrases
("a transfer needs two different accounts", "no category may be named by a transfer").

## Forbidden tokens

Zero tolerance in **every section**. Two exceptions, both for the same reason — naming the real test
setup is their job:

- §9's Prerequisites bullet;
- §6's Measurement column and its prerequisite note. A target with no named way to check it is a wish.

Neither exception covers a test plan. Which test proves a criterion belongs to the stage that writes
the code, not to this document.

### Hard ban (rewrite, always)

| Class | Examples |
|---|---|
| HTTP verbs | `GET`, `POST`, `PUT`, `PATCH`, `DELETE` |
| URL paths | `/accounts`, `/transactions/{id}`, `/api/v1/...` |
| Status numerics | `200`, `201`, `400`, `401`, `403`, `404`, `409`, `500`, `503`, `5xx` |
| Failure codes | anything matching `[a-z_]+\.[a-z_]+` — `ledger.transfer_same_account` is a name for a machine; the PRD states the refusal in words and leaves the naming to the stage that writes the code |
| JSON / payload fragments | `{amount, date}`, `{"status": "draft"}` |
| SQL / schema constructs | `UNIQUE`, `FK`, `INSERT`, `SELECT`, `UPDATE`, constraint names, column names |
| This repo's stack | `EF Core`, `DbContext`, `Npgsql`, `PostgreSQL`, `Postgres`, `ASP.NET`, `minimal API`, `xUnit`, `Testcontainers`, `React`, `PWA`, `Docker` |
| This repo's code vocabulary | `repository`, `DTO`, `entity`, `migration`, `index`, `enum`, `decimal`, `DateTimeOffset`, `CancellationToken`, `snake_case`, `DI` |

### Warn tier (flag, ask the user to rephrase, allow with a reason)

`request`, `endpoint`, `API`, `client`, `response`, `payload`. These leak an interface the PRD has not
chosen yet. Usually there is an owner-language version one word away: "in one step", "in one answer",
"without adding the numbers up by hand". Allowed when the feature genuinely *is* the programmatic access
and the brief says so — `core-ledger` §2 is the real case.

### The regex scan (Phase 8)

Run over §1-§7 only, word boundaries on — without `\b`, `API` matches inside `rapid`:

```
\b(GET|POST|PUT|PATCH|DELETE)\b
\s/[a-z][a-z0-9_/{}-]*
\b(200|201|400|401|403|404|409|500|503)\b
\b[a-z_]+\.[a-z_]+\b
\b(EF Core|DbContext|Npgsql|PostgreSQL|Postgres|ASP\.NET|xUnit|Testcontainers|SQL|UNIQUE|INSERT|SELECT|UPDATE)\b
\b(repository|DTO|migration|index|enum|decimal|DateTimeOffset|CancellationToken)\b
```

A hit that the user explicitly overrode during the critic resolution stays, and the override bullet in
§1 ¶4 is its record. Every other hit re-opens one `AskUserQuestion` for that line.

## Worked examples (from `core-ledger`, which ran before this skill existed)

Good — the owner's view, the invariant named in plain words:

```
### AC-05 (US-02) — domain invariant
**Given** the owner is recording a transfer
**When** the source and the destination are the same account
**Then** the system blocks it and tells the owner a transfer needs two different accounts
```

Good — history integrity, the type the generic model has no slot for:

```
### AC-13 (US-04) — history integrity
**Given** a category that already has recorded history
**When** the owner moves it to another group, or out of every group
**Then** the system accepts the change, the figure for that category in any period stays as it was,
and group figures from then on follow the new placement
```

Bad — the same rule, leaking:

```
**Then** the system returns 409 with ledger.transfer_same_account and no row is INSERTed
```

Three hits: a status numeric, a failure code, an SQL construct. None of them belongs anywhere in this
document — the refusal is stated in words in §5, collected in §10, and named by the stage that writes
the code.
