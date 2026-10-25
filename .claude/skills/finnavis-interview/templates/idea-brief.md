---
status: Draft | Confirmed | Frozen
owner: "<name>"
reviewers: []
updated_at: "<YYYY-MM-DD>"
feature_size: <XS|S|M|L|XL>     # set later, not here
stage: "01"
depth: easy | medium | hard     # interview depth used
ticket: "none"                  # no tracker on this project
value_score:
  rice: <number>                # computed by Claude, Reach pinned to 1
  state: proposed | confirmed
  confirmed_at: "<YYYY-MM-DD>"
feasibility_state: proposed | confirmed
evenings_estimate: <number>     # effort in the unit actually spent
---

<!-- Stage 01. Written by the finnavis-interview skill (.claude/skills/finnavis-interview). -->
<!-- Why this exists: capture the idea before it is forgotten or retold differently. -->

<!-- Self-check reminder, not user-facing: this is a PRODUCT document. No datastore,
     framework or library names, no table shapes, no endpoint shapes, no latency targets.
     Full banned list in references/anti-patterns.md. The stack lives in ARCHITECTURE.md,
     docs/adr/ and the task file. -->

# Idea Brief — <feature name>

## 1. Raw idea

<One paragraph, the owner's own words from Phase 1, verbatim. Do not translate, do not tidy.
If the idea changed during the interview, quote where it settled and say it changed.>

## 2. Problem

<1-3 sentences, facts and numbers. What the owner does today instead, how often, how long it
takes. If the current tool was named, list its gaps and state plainly how many of them this
feature closes — and how many it does not.>

## 3. Who it serves

<One person: the owner. Say so, and say in which situation they hit this. No segments, no
personas, no market. If the feature is only ever touched by a script and never by the owner
directly, that is worth saying too.>

## 4. Why now

<The trigger. Another feature is blocked, the pain crossed a threshold, something in the repo
just became possible. "I felt like it" is an acceptable answer on a pet project — write it
plainly rather than inventing urgency.>

## 5. Out of scope

- <bullet — the near-miss things deliberately not in this feature>
- <each one is a boundary that stops this growing until it stalls>

## 6. Competitive analysis

| # | Product · URL | Relevant features | Value (1-5) | Gap |
|---|---|---|---|---|
| 1 | <the tool being replaced> · <url> | <features> | <1-5> | <what it cannot do> |
| 2 | <name> · <url> | <features> | <1-5> | <gap> |

<Footnote: date searched and the query used, per row.>

**Findings.** <The combination nobody offers. This is the wedge, and §13 cites it. If the idea
has no market at all, write `N/A — internal plumbing` with the reason instead of the table.>

## 7. Strategic approaches

### Approach A — <3-5 word name>
- **Thesis**: <one sentence, product language>
- **For whom**: <the owner, in which situation>
- **Outcome metric**: <one KPI, baseline → target>
- **Key trade-off**: <one line>
- **Effort signal**: S / M / L
- **Recommended?** ◯ / ●  <filled in §13>

### Approach B — <name>
<same six fields>

### Approach C — <name>
<same six fields>

## 8. Three-lens review

### Builder — tonight
- <3-5 bullets: build cost vs correctness cost, shared vs extra work, testing burden>

### Future-me — six months out
- <3-5 bullets: what gets re-opened, what was built and never used, what stops being remembered>

### User-me — Tuesday evening
- <3-5 bullets: steps to the one thing, is the answer ready to use, what breaks trust>

### Synthesis matrix

|        | Builder | Future-me | User-me |
|--------|:-------:|:---------:|:-------:|
| App. A | **+** <six-word reason> | **0** <six-word reason> | **−** <six-word reason> |
| App. B | | | |
| App. C | | | |

## 9. Trade-offs and edge cases

| Approach | Pros | Cons |
|---|---|---|
| A | | |
| B | | |
| C | | |

### Edge cases
- <5-8 items any approach must survive>
- <bias toward: history changing after it was read; entities renamed or deleted under existing
  records; local calendar versus stored time>

## 10. Risks

- **Top risk (devil's advocate).** <The most critical attack vector from Phase 8. Name what
  breaks and how the owner would notice — decaying entry rate, a balance that cannot be
  explained, still using the old tool in parallel.>
- <other risks, each with its signal>

## 11. RICE

- **Reach (R)**: 1 — one owner, one user. Fixed on this project, never varied.
- **Impact (I)**: <0.25 | 0.5 | 1 | 2 | 3> — rationale cites §2 and the Future-me lens (§8).
- **Confidence (C)**: <0.5 | 0.7 | 0.8 | 1.0> — rationale cites the count of open questions (§15).
- **Effort (E)**: <person-weeks> — from the effort signal in §7. ≈ <N> evenings.
- **RICE = 1 × I × C / E = <number>**
- **Honest caveat**: at Reach 1 the absolute score means nothing. It is useful only for ranking
  this feature against the other features of this project, which all share Reach 1.
- **State**: proposed | confirmed

## 12. Feasibility

- [☑/☐] **Tech**: <cite an adjacent module from the repo scan, or admit there is none.
  Scaffolding is not proof.>
- [☑/☐] **Evenings**: <cite elapsed time of a comparable feature from the git history, or
  admit there is no baseline.>
- [☑/☐] **Interest**: <from the owner's own answer about what they want to learn here.>
- **State**: proposed | confirmed

<Skills is deliberately not a box: on a learning project, not knowing how is the reason to
build it. A skill gap big enough to matter goes in §10 with an honest size. A box closing ☐
does not block the brief — honest ☐ boxes become the first concrete tasks.>

## 13. Recommendation

**Selected: Approach <X> — <name>.**

<3-5 sentences. MUST cite all four: the §6 competitive gap, at least one §8 matrix cell, the
§11 RICE score, the §12 feasibility state. Without the four it is "I feel like C".>

**Locked-in pointer**: <one sentence — what the next stage inherits as settled, and what it
explicitly does not.>

## 14. Parked and rejected

| # | Approach | Status | Reason | Revisit trigger |
|---|---|:---:|---|---|
| A | <name> | parked | <reason> | <what would bring it back> |
| B | <name> | parked | <reason> | <trigger> |
| — | <name> | rejected | <reason> | — |

## 15. Open questions

- [ ] <question> — owner: <name>, due: <stage or date>
- [ ] <unresolved domain term goes here, not into docs/GLOSSARY.md>

## 16. What this unlocks

<The priority signal on a one-user project, where Reach cannot provide one. List the later
features that cannot start until this exists — by name, from SPEC.md or an earlier brief's
§16. Then say whether this feature gives the owner relief immediately, or only opens the road
for others.

"Nothing — this is relief on its own" is a valid and good answer. A blank is not: a pure
enabler that gives no immediate relief is the classic abandonment shape and §11 has to price
it that way.>

## 17. Learning goal

<Why build this by hand instead of installing something that already does it. What the owner
wants to learn here, and which part is the boring obligation. This is the Interest box in §12
written out, and on a pet project it predicts completion better than effort does.

If the honest answer is "I don't want to learn anything here, I just want it to exist" — write
that. It is a strong argument for the smallest approach in §7, or for not building it at all.>

## Related

- `docs/GLOSSARY.md` — glossary, canonical. Terms added by this interview: <list>
- `SPEC.md` — scope and the draft requirement list
- `ARCHITECTURE.md`, `docs/adr/` — layering and past decisions
- earlier briefs this one depends on: <list>

## DoD self-check

- [ ] 17 sections present, plus Related
- [ ] No banned terms in the body (regex per references/anti-patterns.md, word boundaries on)
- [ ] Length ≤ 5 pages (~2200 words)
- [ ] Frontmatter status: Confirmed
- [ ] §11 states Reach = 1 and carries the caveat
- [ ] RICE confirmed (state: confirmed)
- [ ] Feasibility confirmed (state: confirmed)
- [ ] §13 rationale cites §6, §8, §11, §12
- [ ] §16 filled — unlocks named, or explicitly nothing
- [ ] §17 filled
- [ ] New domain terms appended to docs/GLOSSARY.md, each with its NOT-boundary
