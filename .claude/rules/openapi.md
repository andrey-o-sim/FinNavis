# API contract rules — baseline

<!-- Bootstrapped by sdlc/plugin/skills/api-forge on the first HTTP run. Edit freely. -->
<!-- These rules are an opinionated default — see "Defaults" in SKILL.md. A team edit here, or an ADR, is the only way to deviate; api-forge reads this file on every run and records deviations in api-sync-report.md. -->

## Where the contract lives

- `docs/features/<slug>/contracts/openapi.yaml` is the source of truth for the HTTP surface. Handlers, clients, mocks and contract tests follow it — never the other way round.
- The contract is **derived** (`data-model.md` + `sad.md` §6 sequences + `PRD.md` AC) by `/finnavis-api-forge <slug>` (the repo-tuned skill; `/sdlc-api-forge` is the generic one). After a hand edit or an upstream change, run it with `--reconcile` so `api-sync-report.md` stays true.
- **The contract is not the served document.** ASP.NET Core generates its own OpenAPI document at `/openapi/v1.json` in Development (`CLAUDE.md` §API). That one is produced *from* the code; this one states what the code *must* produce. `implement-tasks` diffs them.
- `info.version` bumps only with an explicit CHANGELOG line, never silently.

## Hard rules

- OpenAPI **3.1.0**. Nullability via `type: [string, null]`; `nullable: true` (3.0 style) is forbidden.
- Every error response is the envelope `{code, message, details?}`. `code` is `Module.ErrorName` in PascalCase (`Transaction.CategoryNotFound`, `Account.HasRecordedHistory`) — never free text like `{"error": "failed"}`. Pattern: `^[A-Z][A-Za-z0-9]*\.[A-Z][A-Za-z0-9]*$`. The case is a FinNavis deviation from this baseline — see §7 below.
- One error `code` in the contract = one definition in the repo's error registry (constants / enum / sentinel module — whatever the repo uses), returning the **same string** the contract advertises.
- Shared types via `$ref` only; no inline duplicated schemas.
- No `additionalProperties: true` on response shapes. Exception only by ADR (e.g. a polymorphic payload) and noted in the report.
- No real PII in `example` blocks: `<name>@example.test`, `+380 00 000 00 00`, `Test User`.

## Defaults

- URL versioning `/api/v1/...`; never a `?v=2` query param.
- `BearerAuth` global; a public endpoint declares explicit `security: []`.
- Lists paginate by **cursor** (`?after=&before=&limit=`), wrapped in `{items, has_next, has_prev, next_cursor}`; never offset.
- JSON keys snake_case. Schema names use the domain language of `data-model.md` (`Transaction`, `CreateAccountRequest`) — not a Go/TS/Python idiom, not a driver's error type.
- A mutating endpoint behind a retry or an async actor requires an `Idempotency-Key` header (state the TTL).
- Status by class: `400` validation, `401` no/invalid token, `403` not allowed, `404` existence-hiding, `409` state or uniqueness conflict, `429` rate limit, `5xx` server. Every `4xx`/`5xx` carries the envelope.

## Checks

- `spectral lint docs/features/<slug>/contracts/openapi.yaml` is in the project's check target (`./build.sh check` once that task exists).
- A change to the contract and the matching handler/client land in the same PR, with `api-sync-report.md` re-run (`--reconcile`) and its `**Verdict:**` at `PASS` or `PASS (with deferrals)`.

---

## FinNavis deviations from the baseline above

<!-- Added on the first api-forge run (core-ledger, 2026-10-20). A deviation lives here so the
     drift check reads it as a deliberate team rule rather than as drift. Each one names the
     source that forced it. -->

### 1. No authentication — `security: []` globally, no `securitySchemes`

The baseline says `BearerAuth` global. FinNavis has no sign-in and no second actor, so there is
no token to require and no scheme to declare.

- `PRD.md` §3 Non-goals: "No sign-in and no OAuth — one owner, one dataset".
- `sad.md` §8: "Authentication: None. One owner, no sign-in. Network isolation is the only control."
- `sad.md` §11 carries this as the top High risk, owned by the later OAuth feature.

A contract therefore declares `security: []` at the root and no `401` / `403` responses. The
baseline's `404` is plain "not found" here, not existence-hiding — there is nobody to hide from.
`429` appears only when an acceptance criterion asks for a rate limit; none does today.

When the OAuth feature ships, that feature flips this rule back and every contract is re-run with
`--reconcile`.

### 2. Money crosses the wire as a `string`, never a JSON number

`CLAUDE.md` says money is `decimal`, never `double` or `float`. A JSON number is parsed as an IEEE
double by most clients, which loses the fourth decimal place `NUMERIC(19,4)` stores. So every money
field is `type: string` with a digits-and-dot `pattern`, and the consumer parses it into its own
decimal type.

Pattern for `NUMERIC(19,4)`: `^-?\d{1,15}(\.\d{1,4})?$`. The leading `-?` is for a signed figure
such as a balance; an amount the owner enters is always positive and uses the pattern without it.

### 3. A cursor is an opaque `string`, not a bare UUID

The baseline types `after` / `before` as `format: uuid`, which assumes one table ordered by one
time-ordered key. History is a `UNION ALL` of `transactions` and `transfers` (ADR-0001), ordered by
`(occurred_on, id)`, so a single id cannot locate a page boundary. The cursor is an opaque token the
server mints; a client passes it back unread and never builds one.

The page wrapper also carries `prev_cursor` alongside `next_cursor`, because the baseline's `before`
parameter is unusable without it.

### 4. A shared `$ref` schema does not mean a shared C# record

These two rules sit at different levels and look contradictory if read together:

- This file: "Shared types via `$ref` only; no inline duplicated schemas."
- `CLAUDE.md` §API: "Do not share response records between operations, even when two look identical today."

The `$ref` rule governs the **contract** — one definition per type, so the document cannot drift
against itself. The no-sharing rule governs the **C# code** — each
`Presentation/Endpoints/<Resource>/<Operation>/` folder owns its own request and response records,
even when two serialise to the same shape. `implement-tasks` follows both: generate a record per
operation folder from a single `$ref`'d schema.

### 5. Resolved — the flat error envelope, not RFC 9457 Problem Details

The envelope above (`{code, message, details?}`) was chosen for core-ledger at the api-forge gate
and once conflicted with two committed sources that both named **RFC 9457 Problem Details**:
`sad.md` §8 Crosscutting concepts (status Approved) and `CLAUDE.md` §API.

**Settled 2026-10-20 by `docs/adr/0002-return-a-flat-error-envelope-instead-of-problem-details.md`,
in favour of the envelope.** Both sources were amended in the same change: the `sad.md` §8 Error
handling row now states the envelope and cites the ADR, and `CLAUDE.md` §API carries the rule
directly. This file is unchanged — it was already right.

Why the envelope won, in one line: every refusal here is a business refusal the owner reads, so the
one field that matters is a stable `code` the client switches on, and Problem Details would make
that field a non-standard extension member while adding `type`, `instance` and `title`, none of
which this product has a use for. The ADR carries the full reasoning and the condition that would
reopen it — a public API, which PRD §3 currently rules out.

**One consequence carries into implementation and is easy to miss: ASP.NET Core returns
`application/problem+json` by default.** It does so for routing 404s, model-binding failures,
`Results.ValidationProblem`, and anything under `AddProblemDetails`. That default has to be turned
off and every failure routed through the one envelope — otherwise the API answers with two
different error shapes depending on how deep into the pipeline the request got, and a client that
switches on `code` breaks on exactly the requests that never reached a handler.

### 6. A batch operation that finished is a `200`, never an error

`CLAUDE.md` §API: *"A batch operation that finished is a success, even when it could not process
every item. The response body carries the counts plus one entry per skipped item with its reason."*

So an import that set 12 rows aside is a `200` with `{imported, skipped, set_aside: [{row, reason}]}`
— not a `207`, not a `4xx`, and not a `200` that hides the skipped rows. The error envelope only
describes a refusal that **stopped** the whole operation.

The reason belongs to a closed set the PRD names, not to free text. `csv-import/sad.md` §8 states
the same rule and names its three set-aside reasons.

### 7. An error code is PascalCase — `Module.ErrorName`

The baseline above spelled the code in snake_case (`transaction.category_not_found`). **Settled
2026-10-22 by `docs/adr/0003-spell-an-error-code-in-pascalcase.md`** in favour of
`Transaction.CategoryNotFound`. ADR-0003 supersedes the spelling clause of ADR-0002 and nothing
else: the envelope, the status classes and the batch rule are unchanged.

- Pattern: `^[A-Z][A-Za-z0-9]*\.[A-Z][A-Za-z0-9]*$`.
- The module is the aggregate in the singular, PascalCase: `Account`, `Transaction`, `Transfer`,
  `Category`, `CategoryGroup`, `History`, `Breakdown`, `Import`, `MappingProfile`.
- The derivation rules in the skill's `error-catalog.md` keep their meaning, only their rendering
  changes: a UNIQUE constraint gives `<Entity>.Duplicate<Field>`, a foreign key gives
  `<Entity>.<Ref>NotFound`.

Why, in one line: the code is an identifier the owner never reads and a client only compares, so it
is spelled like the constant it maps to — `CategoryErrors.GroupNotFound` returns
`"Category.GroupNotFound"`, class for module, member for name, with no translation rule between the
contract and the registry.

**Two things this does not change.** JSON keys stay snake_case, including every key inside
`details` — the code is the one PascalCase value in the body. And the dot is not parsed by anything;
it is there so a person can see which aggregate refused.

ADR-0003 records the honest cost: the combination of a dot with PascalCase matches no widely used
API, so it is internal fit chosen over recognisability. The condition that reopens it is a public
API, which `PRD.md` §3 rules out today.
