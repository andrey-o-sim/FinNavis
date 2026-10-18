# Coverage — the audit, the matrix, the two header lines

Used in Phases 1, 2 and 4. This is the part of the pass that has value even when no diagram is
written.

## Why the audit is adversarial

The flows in §6 were drawn by the same kind of agent that is now checking them, from the same PRD,
in the same style. Agreeing with them is the default outcome and it is worthless. So the audit is
run against the **blocks**, not against the headings, and the question is always "which line shows
this?" rather than "does this look covered?".

A heading like:

```
**Critical flow 2: load a monthly statement** (US-02 — AC-05, AC-06, AC-07, AC-08, AC-22)
```

asserts five acceptance criteria. Five times, find the line.

## Pass 1 — user stories (§4)

List every `### US-NN: <title>` in PRD §4. For each, name the flow or flows that realise it.

A user story with no flow is a gap. There are only two honest outcomes: draw a flow for it, or send
it back through `finnavis-prd` as de-scoped. Silently skipping is not one.

A user story realised by **part** of a flow is fine and common — `US-07: hand over the same file
again` is one branch of the flow that loads a statement. Record which branch.

## Pass 2 — acceptance criteria (§5)

List every `### AC-NN (US-NN) — <type>` in PRD §5. For each, assign one of four verdicts.

| Verdict | Means | What happens next |
|---|---|---|
| **shown** | A specific step, branch or note in a named block demonstrates it | Record where. Nothing to do |
| **claimed but not shown** | A flow heading cites it, but no line in the block demonstrates it | Phase 3, usually a targeted edit — a branch inside the block that already claims it |
| **uncovered** | No flow cites it and no block shows it | Phase 3, usually a new flow |
| **non-runtime N/A** | It is not about how a request moves at runtime | Only the user grants this, in Phase 2, with a one-line reason recorded in the matrix |

**`claimed but not shown` is the worst of the four.** An uncovered AC is visible to anyone reading
the matrix. A claimed one reads as done, in the document, forever.

### What counts as *shown*, by AC type

The PRD marks each AC with a type (`happy`, `error`, `cross-context`, `history-integrity`,
`duplicate-safety`, and so on). The type tells you what to look for:

- **happy** — a step on the main path, ending in something the owner sees.
- **error** / **cross-context** — an `alt` or `else` branch that refuses, **and** a line saying what
  was not written. A refusal with no "nothing was created" line leaves the reader guessing whether
  half of it landed.
- **history-integrity** / **duplicate-safety** — usually a `Note over` stating the invariant, because
  it is a property of the whole flow rather than one step. An arrow sequence alone cannot say "in
  whatever order this happened".
- **authorization** — in this product, normally non-runtime N/A: one owner, no sign-in. The PRD
  already says so in §5's header comment; cite that, do not re-derive it.

### Legitimate non-runtime N/A

Grant it when the AC is about something no request flows through:

- a file-format or input rule enforced before any use case is entered,
- a build-time or configuration guarantee,
- a database-level constraint the schema enforces,
- authorization in a single-owner product.

Each one gets a one-line reason in the matrix. «AC-15: a format rule the reader enforces before any
flow starts» is a reason. «not applicable» is not.

## The matrix

Render it in the chat in Phase 2. Keep the user-story pass short and give the AC pass a row each —
it is the one the user actually checks.

```
§4 user stories — 7 of 7 realised
  US-01 → flow 1   US-02 → flow 2   US-03 → flow 4   US-04 → flow 5
  US-05 → flow 3 (branch)           US-06 → flow 6   US-07 → flow 3

§5 acceptance criteria — 19 shown · 2 claimed but not shown · 1 uncovered
| AC | Type | Verdict | Where |
|---|---|---|---|
| AC-05 | happy | shown | flow 2, the step that records the movements |
| AC-07 | error | claimed, not shown | flow 2 cites it; no branch on currency exists |
| AC-15 | error | uncovered | nothing in §6 touches it |
```

Do not write the matrix into `sad.md`. It is a snapshot of one pass and it would be stale the moment
the PRD changes. What goes into the document is the **coverage line**, which stays true because it
counts rather than enumerates.

## Grouping — when one flow covers several criteria

`finnavis-architecture-design` already says: group flows that differ only in which noun they touch.
That holds here.

Four acceptance criteria about keeping accounts — add, rename, archive, refuse to delete one that
has movements — are one flow with branches, not four diagrams that share nine lines out of ten. Cite
all four AC ids in the heading, and make sure each one has its own branch inside, or the grouping has
just created four `claimed but not shown` verdicts at once.

Do **not** group across different nouns to save space. Accounts and categories look similar and
refuse for different reasons; one diagram covering both hides exactly the part worth drawing.

## The two header lines

Both live directly under the §6 intro paragraph and are rewritten on every full run (not in
single-flow mode).

**Coverage line.** Count, do not enumerate:

> Six flows cover all 7 §4 user stories and 21 of the 22 §5 acceptance criteria. AC-15 is a
> file-format rule enforced before any flow starts, so it has no runtime path.

If everything is covered, say it plainly — «nine flows cover all eight §4 user stories and all 25 §5
acceptance criteria» — with no caveat the matrix does not support.

**Reading-order line.** Flows are appended in the order they were drawn, and that is rarely §4's
order. Without this line a reader hunts:

> Reading order by user story: US-01 flow 1 · US-02 flow 2 · US-03 flow 5 · US-04 flow 6 ·
> US-05 flow 7 · US-06 flow 8 · US-07 flow 3 · US-08 flows 4 and 9.

If a flow was appended by this pass, also keep the one-line provenance sentence that already exists
in §6 — which flows came from stage 03 and which were appended later, and that the numbers were left
alone on purpose. It is the sentence that stops the next reader from "fixing" the ordering.
