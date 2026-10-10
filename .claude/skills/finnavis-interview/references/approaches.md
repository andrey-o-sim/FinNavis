# Strategic approaches — sub-agent prompts and the usual alternatives

Used in Phase 5. Three sub-agents, one message, one shared prompt with a different stance each.

## Shared prompt template

Fill the bracketed parts from session memory, then send the same body to all three with a
different `STANCE`.

```
You are proposing ONE strategic approach for a feature of a single-owner, self-hosted
personal finance project. The owner is the only user. There is no team, no customers, no
release deadline. The budget is evenings.

CONTEXT
- Raw idea (owner's own words): <§1 verbatim>
- Problem and facts: <§2>
- What the owner uses today and what it lacks: <from Phase 2>
- Out of scope: <§5>
- Competitive findings: <§6 table + the findings paragraph>

YOUR STANCE: <STANCE>

Return exactly this, nothing else:
- Name: 3-5 words, product language
- Thesis: one sentence, what this approach is
- For whom: the owner in which concrete situation
- Outcome metric: one KPI, baseline -> target
- Key trade-off: one line, what this costs or gives up
- Effort signal: S, M or L (S = 1-2 person-weeks, M = 3-5, L = 6-12)

HARD RULES
- Product language only. No datastore, framework, library or protocol names. No table
  schemas, no endpoint shapes, no latency numbers. If you need to describe a capability,
  describe what the owner can ask for, not how it is stored.
- One approach, not a menu. Do not hedge with "or we could also".
- The outcome metric must be checkable by one person on their own data.
- "Effort" is build time plus the time to keep it correct, not just build time.
```

## The three stances

- **A — Simplicity.** The shortest path to the success metric from Phase 2. Fewest moving
  parts. The version that could be finished in a week of evenings and then left alone.
- **B — Differentiation.** The version that makes building it worthwhile instead of buying or
  installing something. Usually the one that does something no existing tool does.
- **C — Balanced.** The trade-off between A and B. Not "A plus a bit" — a distinct shape that
  hits the metric and leaves one named direction open.

## The alternatives that keep showing up

On a self-hosted personal finance project the same three axes come up again and again. Give
the sub-agents these as a hint when the idea sits on one of them — it stops all three from
converging on the same proposal in different words.

### Axis 1 — fixed answer vs query grammar

- **A-shape:** one fixed question answered well. "Spend per category last month."
- **B-shape:** a general grammar where any dimension combines with any other.
- **C-shape:** a fixed shape designed to take more dimensions without breaking the caller.

Seen in `core-ledger`: Flat Ledger / Query-Any-Slice / Group Rollup. B lost because there was
no client yet to prove which slices mattered, and the test burden grows combinatorially. That
is the standard outcome of this axis on a project with no users — note it, do not assume it.

### Axis 2 — manual vs automatic vs assisted

- **A-shape:** the owner does the work, the app just records it correctly.
- **B-shape:** the app infers, learns rules, guesses categories.
- **C-shape:** the app proposes, the owner confirms once, the confirmation is remembered.

B is where a solo project usually dies: a learning system needs a rule store, a correction
flow, and a way to explain itself before it is usable at all. If a sub-agent proposes B here,
its Effort signal should almost never be S.

### Axis 3 — closed app vs open interface

- **A-shape:** the feature works inside the app and nowhere else.
- **B-shape:** everything is reachable from outside — spreadsheets, scripts, an assistant.
- **C-shape:** one interface, documented, with the inside built on top of the same path.

This axis matters more here than on most projects: "no API at all" is the single gap the owner
named against the tool they are leaving. An approach that closes that gap is worth more than
its effort signal suggests, and §13 should say so.

### Axis 4 — now vs no-backfill-later

- **A-shape:** store the minimum this feature needs.
- **B-shape:** store everything any planned feature might need.
- **C-shape:** store the minimum plus the one field that is expensive to add later.

The C-shape here is specifically the "where did this record come from" field. Adding it after
a few hundred records exist means a backfill before any visible work starts. Phase 8 names
this as an attack vector; Phase 5 should have already offered an approach that avoids it.

## What the sub-agents must not do

- **Propose the same thing three times.** If two returns are the same idea in different words,
  re-run the odd one with an explicit "your approach must differ from this one: <text>".
- **Leak the stack.** Any datastore or framework name in the output means re-running that
  sub-agent. Phase 13 catches it later, but fixing it here is cheaper.
- **Price Effort by lines of code.** Effort includes keeping the thing correct: more
  combinations means more ways to be silently wrong, and on a one-person project nobody else
  will notice.
