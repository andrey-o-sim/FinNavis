# api-sync-report — <slug>

**Verdict:** <PASS | PASS (with deferrals) | FAIL>
**Run at:** <YYYY-MM-DD> (run <N>, mode: <first run | --reconcile | --drift-only>)
**Sources read:** PRD.md <✓|✗> · data-model.md <✓|✗> · sad.md §5/§6/§8/§11 <✓|✗> · adr/<range> <✓|✗> · CONTEXT.md <✓|✗> · `.claude/rules/openapi.md` <✓>
**Scenario:** <A — data-model.md exists, every field traces to a column | B — inferred from PRD and sequences>
**Contract version:** `info.version: <x.y.z>`, OpenAPI 3.1.0
**Surface:** `backend-service` — read from `sad.md` frontmatter `target_surfaces`, not re-derived.

<!-- Only on a FAIL. Delete this block otherwise. Two or three sentences, before anything else. -->
## Why FAIL

> **<the one sentence that names the failing thing>**
> <why it fails, and which of the four actions was chosen>

The contract **was still written** — <N> operations covering everything derivable today, so it is
reviewable now and `--reconcile` only has to add what this report names. **No handoff to
implementation.**

---

## Section A — field origins

One row per `operation.field`. `confidence` is **high** when the origin is a typed column,
**medium** when it is an acceptance criterion or a named convention, **low** when it is inferred.

| path | origin | confidence |
|---|---|---|
| `<createAccount.name>` | `data-model.md` → `accounts.name varchar(100) NOT NULL` | high |
| `<getHistory.items[].amount>` | `data-model.md` → `transactions.amount NUMERIC(19,4)`; string on the wire per rules §2 | high |
| `<getHistory.next_cursor>` | convention — opaque keyset cursor over `(occurred_on, id)`, rules §3 + ADR-0001 | medium |

---

## Section B — drift findings (6-point checklist)

Mark each ✓ · ✓ (deferred: one line) · ✗ (one-line diagnostic) · N/A (with the reason).

### 1. Operation ↔ data model *(core)* — <✓|✗>

<evidence, or the one-line diagnostic>

### 2. Error code ↔ definition in the repo *(core)* — <✓|✗|✓ (deferred)>

<when `src/` is a skeleton: "deferred — no registry exists yet", then the full code list below, so
`implement-tasks` transcribes rather than re-derives>

| code | status | operation(s) | source |
|---|---|---|---|
| `<module>.<error_name>` | `409` | `<operationId>` | `data-model.md` UNIQUE (<cols>) |

### 3. Validation ↔ constraint *(core)* — <✓|✗>

<every bound in the contract against its column>

### 4. Coverage: AC ↔ operation, entity ↔ operation *(supporting)* — <✓|✗>

- PRD §5 acceptance criteria with no operation: <list, or "none">
- operations with no §4 user story: <list, or "none">
- `data-model.md` entities no operation surfaces: <list with "intentionally internal — reason", or "none">

### 5. OpenAPI ↔ sequence *(supporting)* — <✓|✗>

- §6 `alt`-branches with no response: <list, or "none">
- responses with no §6 branch (**sequence gaps**): <list, or "none">

### 6. Contract ↔ the served document *(supporting, FinNavis)* — <✓|✗|N/A>

<before implementation: "N/A — `src/FinNavis.Presentation/Endpoints/` is a skeleton. This point is
checked at `implement-tasks`, when `/openapi/v1.json` can be diffed against this contract.">

<after: paths, methods, status codes, the endpoint folder tree, and whether the code returns the
flat envelope rather than `application/problem+json`>

---

## Section C — unresolved_origins

<Scenario A: "**Empty.** Every field in Section A traces to a column, a PRD line, or a named
convention with its reason stated.">

<Scenario B: one row per field whose origin is inferred and needs confirming when `data-model.md`
arrives. This is declared incompleteness, not an error.>

---

## Section D — Deferrals

| what | why parked | next step |
|---|---|---|
| <the parked item> | <the reason, in the user's own words where they gave one> | <the exact command or edit> |

**Accepted as is (no action, reason recorded):**

- <finding> — <why this is correct on purpose>

---

## Decisions settled at this gate

Open questions the upstream documents handed to this stage. Each one is now closed, with its reason.

1. **<the question>.** Source: `<PRD §8 | sad.md §11 | data-model.md Open items>`.
   → **<the answer>.** <why — the budget, the constraint or the acceptance criterion behind it>

---

## What `--reconcile` will read first

1. <the finding the verdict turns on>
2. <the next one>
