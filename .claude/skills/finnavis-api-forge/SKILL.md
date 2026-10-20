---
name: finnavis-api-forge
description: >
  Derives the HTTP contract of a feature for a solo, self-hosted .NET pet project (FinNavis and
  projects shaped like it): writes docs/features/<slug>/contracts/openapi.yaml — OpenAPI 3.1, one
  operation per PRD §4 user story, every field traced to a data-model.md column, every error
  response traced to a sad.md §6 alt-branch — plus contracts/api-sync-report.md with an explicit
  Verdict and a 6-point drift checklist. The contract is never hand-written and never served:
  ASP.NET Core generates its own document at /openapi/v1.json, and implement-tasks diffs the
  generated one against this spec. Tuned to a repo where the API conventions are already fixed by
  CLAUDE.md, .claude/rules/openapi.md and the ADRs — no sign-in so no 401/403, money as a string,
  an opaque cursor over a UNION ALL history, a flat {code, message, details?} error envelope, a
  finished batch as a 200 — so none of them is ever re-asked or re-decided. Use this whenever the
  user wants an API contract, OpenAPI, endpoints, request or response shapes, status codes or error
  codes for a feature in this repo — including phrasings like "контракт API для {slug}", "OpenAPI
  для {slug}", "опиши эндпоинты", "какие коды ошибок", "зафиксируй интерфейс",
  "/finnavis-api-forge {slug}" — even when they do not say the words contract or OpenAPI.
  Hard-refuses if docs/features/<slug>/PRD.md, sad.md or the repo-root CONTEXT.md is missing.
argument-hint: "[slug] [--reconcile] [--drift-only]"
---

# API contract — solo project

Stage 05. `finnavis-architecture-design` settles the containers and the ports;
`finnavis-complete-sequence-diagrams` settles which flows exist and where each one refuses;
`finnavis-generate-data-model` settles the typed columns. This skill projects all three onto one
HTTP surface — and records the interface-level decisions none of them was in a position to make.

Two outputs:

- `docs/features/<slug>/contracts/openapi.yaml` — OpenAPI 3.1, one operation per user story.
- `docs/features/<slug>/contracts/api-sync-report.md` — verdict, field origins, 6-point drift
  checklist, deferrals.

## The one thing to understand before anything else

**ASP.NET Core generates its own OpenAPI document. The contract here is the specification that
document must match.**

`CLAUDE.md` says the OpenAPI document is generated at `/openapi/v1.json` in Development. That one is
produced from the Minimal API endpoints by the framework, at runtime, in whatever shape the code
happens to be. It is a mirror of the code, so it can never disagree with the code and can never tell
you the code is wrong.

This `openapi.yaml` is the opposite: written before the code, from the PRD, the sequences and the
data model. At `implement-tasks` the endpoints are written under
`Presentation/Endpoints/<Resource>/<Operation>/`, the app is started, and `/openapi/v1.json` is
diffed against this file. A difference means the handler or the contract is wrong — resolved before
the endpoint is considered done.

It is the same discipline as the staged SQL in `finnavis-generate-data-model`: a specification that
a generator must be made to reproduce, not a document that is applied.

Every generated contract opens with that fact, so a file found on its own is not mistaken for the
served document:

```yaml
# REFERENCE SPEC — not the document served at /openapi/v1.json. See .claude/rules/openapi.md.
```

## When to use

- "контракт API для `<slug>`", "OpenAPI для `<slug>`", "опиши эндпоинты", "какие коды ошибок",
  "зафиксируй интерфейс", and the same asks in any language the user writes in.
- `/finnavis-api-forge <slug>` as an explicit call.
- After `data-model.md` exists — that is scenario A, the only one that can reach a plain `PASS`.
- `--reconcile` — an upstream document changed (usually `data-model.md` arrived or was tightened).
  Re-derives, tightens loose types, and surfaces any field whose inferred origin now **disagrees**
  with the model. That disagreement is real drift, not stale incompleteness.
- `--drift-only` — compare an existing contract against PRD, SAD, data model and `src/`. No
  generation, just the report.

## When not to use

- **To write the endpoint classes.** That is `implement-tasks`. This stage designs the surface; it
  does not touch `src/`.
- **To decide an aggregate, a flow or a column.** SAD §5, SAD §6 and `data-model.md` settled those.
  If one is wrong, fix it there — a contract built on a wrong flow gets rewritten, not patched.
- **To re-decide a repo convention.** See the table below. Those are closed.
- **When the contract exists and nothing upstream moved.** Re-running overwrites a reviewed design.
  Use `--drift-only`, or edit the one operation that changed.

## Hard gates (stop, do not improvise)

| Missing | Message |
|---|---|
| `<slug>` | Ask for it, or list the folders under `docs/features/` that have a `sad.md` and no `contracts/openapi.yaml`. |
| `docs/features/<slug>/PRD.md` | «Run `finnavis-prd <slug>` first — without user stories and acceptance criteria the endpoint list is invented.» |
| `docs/features/<slug>/sad.md` | «Run `finnavis-architecture-design <slug>` first — §6 is where the error responses come from.» |
| `CONTEXT.md` at the repo root | «The glossary is missing. Without it the schema and field names are invented words.» |
| `docs/features/<slug>/data-model.md` | Not a refusal — this is scenario B. Say it once: types lose their bounds and error codes lose their constraints. Offer `finnavis-generate-data-model <slug>` first, and ask whether to continue. |
| `sad.md` §6 has no flows | Not a refusal — a warning. Error responses would come from the PRD only, which lists the happy path and one or two refusals. Offer `finnavis-complete-sequence-diagrams <slug>` first. |

## Language

Project rule, loaded from [`.claude/rules/language.md`](../../rules/language.md): ask in the user's
language, write the artifact in English, keep identifiers as names, explain the trade-off rather
than the vocabulary. Paths, `operationId`, status codes, schema names and error codes never
translate, in any language.

## Inputs

- `<slug>` · `PRD.md` (§1 why, §4 user stories, §5 acceptance criteria, §6.1 out of scope, §8 open
  questions, §10 refusal wording) · `sad.md` (§5 containers and ports, §6 flows and their
  `alt`-branches, §8 crosscutting, §11 risks and open items) · `CONTEXT.md` — **required**.
- `data-model.md` — scenario A when present. Every typed column, bound and enum crosses into the
  schemas unchanged.
- `CLAUDE.md`, `.claude/rules/openapi.md`, `ARCHITECTURE.md`, `docs/features/<slug>/adr/` — read
  every run. The rules file is the house style; it is **read, never written**.
- **Every earlier feature's contract** under `docs/features/*/contracts/` — a resource an earlier
  feature already exposed is reused, never re-declared with a different shape. Two features that
  both describe `/api/v1/accounts` must describe it identically.
- `src/FinNavis.Presentation/Endpoints/` — read for drift point 6. A skeleton until the first
  feature is built; that is a normal, reported outcome, not a failure.

## What is already decided — never ask it again

The repo fixed these. Re-asking them wastes the user's time and invites an answer that contradicts a
committed document. Each row names where it is written down.

| Topic | Decided | Source |
|---|---|---|
| Authentication | None. `security: []` at the root, no `securitySchemes`, **no `401` and no `403` anywhere** | `PRD.md` §3, `sad.md` §8, rules §1 |
| Error shape | Flat `{code, message, details?}`, `code` = `module.error_name` | `docs/adr/0002`, `CLAUDE.md` §API |
| Money on the wire | `type: string`, pattern `^-?\d{1,15}(\.\d{1,4})?$` — never a JSON number | rules §2 |
| List paging | Cursor, opaque `string`, wrapper with `prev_cursor` and `next_cursor` | rules §3, ADR-0001 |
| Batch outcome | A batch that finished is a `200` with counts and per-item reasons | `CLAUDE.md` §API, rules §6 |
| Versioning | `/api/v1/...` in the path | rules Defaults |
| JSON keys | snake_case | rules Defaults |
| Ids | `string`, `format: uuid` — UUID v7 minted in Application | `core-ledger` ADR-0005 |
| Timestamps | `string`, `format: date-time`, UTC | `CLAUDE.md` |
| Enums | string values, never ints | `CLAUDE.md` |
| Async / idempotency | No queue, no worker, no background job in this project today. No `Idempotency-Key`, no `events.md` — unless a SAD §6 flow actually shows an async actor | `core-ledger/sad.md` §6, `csv-import/sad.md` §6 |

If the user asks to change one of these, that is an ADR, not an answer inside this run. Say so and
offer `finnavis-architecture-design` or a standalone ADR.

## Protocol

### 0. Setup (read-only)

Read the gates, `.claude/rules/openapi.md`, `CLAUDE.md` §API, the feature's ADRs, and every earlier
feature's contract. Read the two templates. Then decide the scenario:

- **A** — `data-model.md` exists. Every field traces to a typed column. Only A can reach `PASS`.
- **B** — it does not. Fields are inferred from PRD field names and §6 message names;
  `unresolved_origins` is populated, and the best verdict is `PASS (with deferrals)`.

Say which one it is, in one line, before generating anything.

With `--drift-only`, stop the protocol here and jump to step 5. Nothing is generated and nothing is
written except the report — the point is to learn whether the existing contract still holds.

### 0.5. One question (AskUserQuestion — mandatory)

**Commits** — *commit it yourself* / *just print the command* (recommended). Depth is not asked:
the surface is as big as the user stories, and nothing here is optional detail.

### 1. Operations from PRD §4

One operation per user story, sometimes more. Each one gets an `operationId` and the
`Presentation/Endpoints/<Resource>/<Operation>/` folder it will become, written into the operation's
`description`. Resource naming, the folder mapping and what a story that needs no endpoint looks
like → [references/deriving-operations.md](references/deriving-operations.md).

A story with no operation, and an operation with no story, are both findings — not something to
quietly resolve. They go to drift point 4.

### 2. Schemas from `data-model.md`

One schema per entity the API surfaces. Every property copies its column's type, bound and
nullability — `varchar(200)` becomes `maxLength: 200`, a nullable column becomes
`type: [string, "null"]`, never `nullable: true`. Money and ids follow the table above.

**Never invent a field with no origin in any input.** Ask the user where it comes from. An entity
that no operation surfaces is either a finding or an explicit "intentionally internal" with its
reason — `core-ledger`'s `transactions.external_ref` is the worked example.

### 3. Error responses from `sad.md` §6

Every `alt … else … end` branch in a flow becomes one response. This is the step that closes the
PRD's blind spot: §5 lists the happy path and one or two refusals, while the sequences hold the
state and concurrency branches. Status classes, how a code is derived from a constraint, and the
registry rule → [references/error-catalog.md](references/error-catalog.md).

Refusal wording comes from the PRD — §10 in `csv-import`, the acceptance criteria elsewhere. It is
never invented here, because the owner reads that sentence, not this file.

### 4. Write `contracts/openapi.yaml`

From [`templates/openapi.yaml`](templates/openapi.yaml). Every operation carries a request example,
a success example and an error example, with placeholder values only — `Test User`,
`<name>@example.test`. **Never a real merchant name, a real amount or a real account**
(`CLAUDE.md` Safety).

Then run the checks in [references/openapi-lint.md](references/openapi-lint.md) by hand. `spectral`
is not wired into `build.sh` yet, so nothing else will catch a broken `$ref` or a 3.0-style
`nullable`.

### 5. Drift check — six points, both directions

Points 1–3 are **core**, 4–6 **supporting**. Point 6 is the one the generic version of this stage
does not have, and the one that matters most once `src/` stops being a skeleton. The full checklist,
how each point is evidenced, and the conflict table →
[references/drift-and-report.md](references/drift-and-report.md).

A core point failing, or three or more flags in total, pauses the run. Resolve each with the user:
**Accept as is** / **Fix the contract** / **Save as a deferral** / **Fix the source first**. Never
silently edit the PRD, the SAD or the data model — surface the mismatch and let the user pick which
artifact is wrong.

### 6. Verdict and the report

Write `contracts/api-sync-report.md` from
[`templates/api-sync-report.md`](templates/api-sync-report.md), **on every run, `FAIL` included** —
a verdict that lives only in the chat is gone at the next `/clear`.

| Verdict | When |
|---|---|
| `PASS` | Scenario A, all six points ✓, nothing parked. |
| `PASS (with deferrals)` | Core 1–3 ✓, something consciously parked — scenario B, a deferred supporting point, a sequence gap. Every parked item has a row in Deferrals with its next step. |
| `FAIL` | A core point still ✗, three or more open flags, or a field with no origin. |

The verdict is derived from the checklist, never typed from a feeling. A `FAIL` still writes the
contract — it is reviewable, and `--reconcile` only has to fix what the report names — but it does
**not** hand off to implementation.

### 7. Handoff

```md
## ✅ finnavis-api-forge — <slug>

**What I did**
- `contracts/openapi.yaml` — <N> operations, <M> schemas, <E> error codes (scenario <A|B>)
- `contracts/api-sync-report.md` — verdict **<PASS | PASS (with deferrals) | FAIL>**, drift <n>/6
- <Q> open questions closed; PRD §8 and SAD §11 updated / left for you

**Review before continuing**
- the operations table — one row per user story, and the stories that got none
- the error codes list — it becomes the registry at `implement-tasks`
- the report's Deferrals — each row is a decision you are postponing on purpose

**Run next**
1. `/clear`
2. on PASS → the task breakdown, then implementation
   on FAIL → fix the source the report names, then `/finnavis-api-forge <slug> --reconcile`
```

## Definition of Done

Each line is also the anti-pattern it rules out — the failure mode is named after the dash.

- The contract opens with the REFERENCE SPEC header — *a file found alone must not be mistaken for
  the document served at `/openapi/v1.json`*.
- Nothing was written into `src/` — *this stage designs a surface; a half-designed surface must not
  be reachable by `./build.sh run`*.
- Every operation names its `Presentation/Endpoints/<Resource>/<Operation>/` folder — *so
  `implement-tasks` has a one-to-one map and does not invent a layout*.
- Every operation maps to a PRD §4 user story, and every story to an operation or a stated reason —
  *no endpoint nobody asked for, and no story that silently got no surface*.
- Every field traces to a column (A) or carries an explicit inferred origin (B) — *no invented
  field, and no field quietly dropped when it left the model*.
- Every error response traces to a §6 `alt`-branch, an acceptance criterion or a named constraint —
  *the PRD's blind spot is the authorization and concurrency branches it never lists*.
- Error codes are `module.error_name`, each one appearing exactly once — *two codes for one refusal
  is how a frontend `switch` goes stale*.
- `security: []`, no `securitySchemes`, no `401`, no `403` — *there is no second actor, so a status
  that denies one is a lie in the contract* (`sad.md` §8).
- Money is a `string` with the decimal pattern — *a JSON number loses the fourth decimal place*.
- Cursors are opaque strings and the page wrapper carries both cursors — *`before` without
  `prev_cursor` is a parameter no client can use*.
- A batch operation answers `200` with counts and per-item reasons — *a skipped row is a normal
  outcome, not a fault* (`CLAUDE.md` §API).
- Examples carry placeholder data only — *never a real merchant, amount or account*
  (`CLAUDE.md` Safety).
- The lint checks pass and the report is written with its verdict — *a `FAIL` kept in the chat is
  lost at the next `/clear`*.
- Repo conventions were read, not re-decided; a request to change one became an ADR — *a contract
  that quietly disagrees with `CLAUDE.md` makes both of them untrustworthy*.

## References

| File | Used in | Holds |
|---|---|---|
| [references/deriving-operations.md](references/deriving-operations.md) | 1, 2 | Story → operation, resource naming, the endpoint-folder map, `$ref` vs per-operation C# records, what gets no endpoint |
| [references/error-catalog.md](references/error-catalog.md) | 3 | Status classes without 401/403, code naming, deriving a code from a constraint or an `alt`-branch, the registry rule |
| [references/drift-and-report.md](references/drift-and-report.md) | 5, 6 | The six drift points with their evidence, the conflict table, verdict rules, reconcile semantics |
| [references/openapi-lint.md](references/openapi-lint.md) | 4 | The hand checks that stand in for `spectral` until `build.sh check` exists |
| [templates/openapi.yaml](templates/openapi.yaml) · [templates/api-sync-report.md](templates/api-sync-report.md) | 4, 6 | The artifacts |
