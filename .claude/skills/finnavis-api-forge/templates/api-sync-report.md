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

One **tree per operation**, inside a fenced block so the columns stay aligned. Not one row per
dotted path: a path like `confirmMappingProfile.request.rules[]` hides the type, and the fields of
that type then sit in unrelated rows further down. The tree puts the type on the node, so everything
indented under `Rules[]  MappingRule[]` is visibly a `MappingRule`.

**Names are the C# records and properties** the operation folder will hold — `ShapeKey`, not
`shape_key`. The wire keys stay snake_case in `openapi.yaml`; this section is read by whoever writes
`Presentation/Endpoints/<Resource>/<Operation>/`, so it uses their names. The mapping is mechanical.

**Types are the contract's**, not C#'s — `string` for money, never `decimal`. Drift point 3 checks
the contract type against the column, so that is the type this column has to carry. `?` marks the
3.1 union `type: [x, "null"]`.

`conf` is **high** when the origin is a typed column, **med** when it is an acceptance criterion or
a named convention, **low** when inferred (scenario B).

A `$ref`'d schema is expanded **once**, under the first operation that carries it. Later uses name
the type and point back: `← see confirmMappingProfile`.

Column order on every leaf: **`name · type · conf · ← origin`**. The three left columns are padded
to a fixed width so they line up down the whole block; only the origin runs free.

**The origin stays on the row it belongs to.** When it does not fit, wrap it under itself, indented
to the `←` column, keeping the tree guide (`│`) in front so the tail is visibly part of that node.
Two things not to do instead:

- **No footnote markers.** A superscript renders tiny and off-baseline in a monospace font, and it
  makes a reviewer jump to the bottom of the block and back for every claim.
- **No shortening.** An origin a reviewer cannot verify is worse than a long line.

Keep a wrapped line at about 115 characters, so nothing scrolls sideways.

### `<confirmMappingProfile>` — `POST /api/v1/<mapping-profiles>`

```text
request  CreateMappingProfileRequest
├─ ShapeKey            string(1000)   high ← mapping_profiles.shape_key. Passed back from inspectFile
│                                            unchanged; the normalisation is data-model.md decision 2
└─ Rules[]             MappingRule[]  high ← mapping_profile_rules, one row per rule
   ├─ SourceCategory   string(200)    high ← mapping_profile_rules.source_category
   ├─ Target           enum(2)        high ← mapping_profile_rules.target VARCHAR(30) — two values,
   │                                         data-model.md decision 1
   └─ TargetCategoryId uuid?          high ← mapping_profile_rules.target_category_id

response 201  MappingProfileResponse
├─ Id                  uuid           high ← mapping_profiles.id, UUID v7 (ADR-0005)
└─ CreatedAt           date-time      high ← mapping_profiles.created_at
```

### `<getHistory>` — `GET /api/v1/<history>`

```text
query
├─ After               string?        med  ← opaque keyset cursor, rules §3 + ADR-0001
├─ Before              string?        med  ← the same, backwards
└─ Limit               int(1..200)    med  ← default 50, PRD §6 latency budget

response 200  HistoryPage
├─ Items[]             HistoryEntry[] high ← one recorded movement
│  ├─ Id               uuid           high ← transactions.id / transfers.id
│  ├─ Amount           string         high ← transactions.amount NUMERIC(19,4). A string on the wire,
│  │                                         decimal in C# — rules §2; a JSON number would lose the
│  │                                         fourth decimal
│  └─ OccurredOn       date           high ← transactions.occurred_on
├─ HasNext             bool           med  ← page wrapper, rules §3
├─ HasPrev             bool           med  ← page wrapper, rules §3
├─ NextCursor          string?        med  ← page wrapper, rules §3
└─ PrevCursor          string?        med  ← page wrapper, rules §3
```

The `Amount` row is the one to copy the shape from: three lines, the tail indented to the `←`
column with `│  │` in front, and nothing abbreviated.

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
arrives. This is declared incompleteness, not an error. Name each field the way Section A does —
`<Operation>.<Record>.<Property>` in C# names — so the two sections can be read side by side.>

| field | inferred from | what would confirm it |
|---|---|---|
| `<confirmMappingProfile.CreateMappingProfileRequest.ShapeKey>` | PRD AC-01 wording | the column, once `data-model.md` lands |

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
