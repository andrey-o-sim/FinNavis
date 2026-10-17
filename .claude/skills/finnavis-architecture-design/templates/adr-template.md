---
status: Accepted
slug: "<slug>"
stage: "03"
updated_at: "<YYYY-MM-DD>"
---

# <NNNN> — <the decision, in decision form>

<!-- Spawned by finnavis-architecture-design when a decision crossed the blast-radius gate. -->
<!-- The title states the DECISION, not the problem: -->
<!--   ✓ 0001-keep-transactions-and-transfers-in-separate-tables.md -->
<!--   ✗ 0001-storage-strategy.md -->
<!-- Status is Accepted. This skill decides synchronously with the owner; there is nobody to -->
<!-- review it afterwards, so Proposed would be a status that never changes. -->

- **Status:** Accepted
- **Date:** <YYYY-MM-DD>
- **Deciders:** <name>

## Context

<2-4 sentences: what is happening and why this has to be decided now. Pull from sad.md §3 and the
section that triggered this record. Name the requirement that forces the question.>

## Decision drivers

<!-- Every bullet comes from PRD §6 NFR, SAD §2 Constraints, or a §1 quality goal. -->
<!-- Do not invent drivers — this list is the filter against deciding by personal taste. -->

- <PRD §6 — the number this decision has to respect>
- <SAD §2 — the constraint that narrows the field>
- <SAD §1 — the quality goal at stake>

## Considered options

<!-- Every option that was actually on the table, including the ones the owner rejected. -->
<!-- No scarecrow: an option an existing constraint already rules out is not an option. -->

1. **<option>** — <one sentence>.
2. **<option>** — <one sentence>.
3. **<option>** — <one sentence>.

## Decision outcome

**Chosen: option <N>, <short name>.**

<1-3 sentences on why it won, citing the drivers above.>

<One paragraph per rejected option, saying what it would have cost. "Rejected because it is worse"
is not a reason — name the specific price: the backfill, the lost budget, the ring it breaks.>

## Consequences

<!-- An honest record fills Negative and Neutral too, not only Positive. -->
<!-- Three upsides and no cost means the alternatives were never seriously held. -->

**Positive**
- <…>

**Negative**
- <…>

**Neutral**
- <…>

## Links

<!-- Without this section the record is an orphan. It lives in three relations: -->
<!--   up to the PRD (which story forced it), up to the SAD section, sideways to a sibling ADR. -->

- PRD: [[../PRD.md]] — <US-nn, AC-nn, §6 rows>
- SAD: [[../sad.md]] §<N>
- Related ADR: <[[NNNN-other]], if any>
