---
status: Draft
slug: "<slug>"
stage: "02"
feature_size: "<XS/S/M/L from .size>"
depth: "<easy/medium/hard from Phase 0.5>"
updated_at: "<today YYYY-MM-DD>"
source_brief: "./idea-brief.md"
---

<!-- Skill instruction: frontmatter rules.
`status` is ALWAYS written as `Draft`. The owner flips it to `Approved` by hand after reading — that
is the human gate, and the skill never does it for them, not even on a clean critic run.
No `owner` field: there is one owner and the repo knows who they are. No `reviewers`: there is no
review board. No `ticket`: there is no tracker. Inventing these is the first anti-pattern. -->

# PRD — <slug>

> **Inputs:** [idea-brief](./idea-brief.md) · [CONTEXT](../../../CONTEXT.md) (repo root) · [SPEC](../../../SPEC.md)
> **Other channels read:** <the ones picked in Phase 4, named concretely — "ARCHITECTURE.md §Layout",
> "docs/features/core-ledger/PRD.md as reference", "src/FinNavis.Domain" — or "None">

## 1. Context

<!-- Skill instruction: 4 paragraphs, each headed in bold.
¶1 **What we're solving** — the concrete problem, from idea-brief §2, for the owner.
¶2 **Why now** — the trigger: what is blocked until this exists.
¶3 **The committed approach** — 1-2 sentences, from idea-brief §13 Recommendation. Name the approach.
¶4 **Decisions taken here** — everything settled during this interview that the brief does not carry:
   scope contradictions with SPEC.md and how they were resolved, rules the brief never covered, and the
   bullets emitted by critic `Override` resolutions in the form
   «Decision override: <headline> — rationale: <reason>».
WHAT and WHY only. No datastore, no framework, no entity-class name — §10 is where technology starts. -->

## 2. Goals

<!-- Skill instruction: 2-3 strategic outcomes as bullets, each a manifestation of §1 ¶3.
Written as what the owner can do that they could not do before. No numbers — numbers are §7. -->

## 3. Non-goals

<!-- Skill instruction: 3-5 bullets, each one sentence plus its reason. Source: idea-brief out-of-scope.
«No X — it is a separate feature that depends on this one» is a reason. «No X» alone is not. -->

## 4. User stories

<!-- Skill instruction: as many as it takes to cover every §2 goal. Format:

### US-NN: <3-6 word action title>
**As an** <role, verbatim from the CONTEXT.md glossary — today that is `owner`>
**I want** <action>
**So that** <the benefit the owner observes>

Never invent `user`, `admin`, `member` or `reviewer`. The glossary is canonical.
The title names the action, not the entity: «Record an expense», not «Expense recording».
Every story has at least one criterion in §5 — checked at draft time and after every change. -->

## 5. Acceptance criteria

<!-- Skill instruction: the coverage model lives in references/ac-coverage.md.
Four types always: happy · error · domain invariant · cross-context.
At least one of: history integrity · duplicate safety · authorization — and EVERY one of those three
is resolved explicitly. A type that does not apply gets an HTML comment at the top of this section
saying so in one line, plus a §8 row when its absence is a real gap the project will close later.
The wording to copy, as an example — written as a comment in the PRD, not as visible text:

    Coverage type `authorization` — N/A: one owner, no sign-in, no second person (see `Owner` in
    CONTEXT.md), so no actor can lack permission. Tracked in §8; the sign-in feature owns this type.

Format:

### AC-NN (US-XX) — <type>
**Given** <what exists, what was recorded before>
**When** <what the owner does, in their words>
**Then** <what the owner observes>

Business language only. The forbidden-token list (HTTP verbs, paths, status numbers, dotted error
codes, JSON, SQL, stack names, code vocabulary) is in references/ac-coverage.md and is scanned before
the write. The machine name of a failure goes in §10, not here. -->

## 6. Non-functional requirements

<!-- Skill instruction: start from the default rows in references/nfr-kpi-bank.md. Numeric targets only
— «fast» is never approvable. Every measurement names something that exists in this repo today
(`./build.sh test`, the integration suite on Testcontainers, a manual run) or the row carries a
prerequisite note. Throughput and availability are dropped by default with a one-line reason: one
user, self-hosted. -->

| Aspect | Target | Measurement |
|---|---|---|
|  |  |  |

## 6.1 Security / privacy

<!-- Skill instruction: four honest lines plus abuse cases — see references/nfr-kpi-bank.md §6.1.
There is no security team. «Security review: Required» means the owner reads it again with a specific
question in mind — say which question. -->

- **Data classification:** <...>
- **Personal data touched:** <...>
- **AuthZ/AuthN impact:** <...>
- **Abuse cases:**
  - <...>: <the business response>
- **Security review:** <what to look at again / N/A with a reason>

## 7. Metrics / KPIs

<!-- Skill instruction: at least 3, from the solo bank in references/nfr-kpi-bank.md. Each is
baseline → target → timeframe. Baseline 0 is fine. Nothing that needs a second user to compute:
no adoption rate, no retention cohort, no DAU/MAU, no NPS, no churn. -->

- **<metric>** — baseline: <...>, target: <...> within <...>.

## 8. Open questions

<!-- Skill instruction: every unresolved idea-brief §15 question that was not answered in the body,
plus every `save as open question` from the walk, plus every gap a coverage-type N/A leaves behind.
Owner and due are mandatory. A due is a date or a stage trigger. A lone «TBD» is not an entry. -->

- [ ] <question>? Default now: <...>. — owner: <name>, due: <date or stage>

## 9. Data impact

<!-- Skill instruction: IMPACT, not design. Derived from §5 and §6, confirmed as a whole with one
question. This section does NOT decide the schema, the lookups or how the change reaches an existing
database. At PRD time there is nothing to decide that against, and a guess written here would be read
later as a decision. It names what the feature does to the data and hands the open points to the
stage that builds it.

Domain language, same as §5. The words «index», «migration», «column», «table», «schema», «enum» and
any stack name do not belong here — except in the Prerequisites bullet, where naming the real harness
is the point.

- **New data** — what the feature starts recording, in glossary words. «A loaded file keeps the name
  it came from and the day it was loaded», not a field list with types.
- **Changed data** — what already exists and gains or loses something.
- **Already-recorded history** — does anything recorded before this feature have to be reshaped or
  filled in afterwards? This is the expensive, hard-to-reverse answer, so state it plainly even when
  it is «no». If yes, say what the owner would see while it happens.
- **New read paths** — which new questions the data now has to answer, and roughly how much of it
  each one walks: «history for one account over a date range», «every movement of a period, grouped».
  The implementation stage needs this to decide lookup behaviour. It is not that decision.
- **Prerequisites** — what must exist before this can be built or measured: a local database setup, a
  seeded dataset, the integration harness. Name them; they become tasks, not assumptions.
- **Left to the implementation stage** — a short list of what this section deliberately does not
  settle: storage shape, lookup behaviour for the read paths above, how the change reaches a database
  that already holds data, and the machine names for the refusals listed in §10. -->

- **New data:** <...>
- **Changed data:** <...>
- **Already-recorded history:** <no / what has to be reshaped and what the owner sees>
- **New read paths:** <...>
- **Prerequisites:** <...>
- **Left to the implementation stage:** <...>

## 10. Refusal messages

<!-- Skill instruction: one row per §5 criterion of type error, domain invariant or cross-context —
the sentence the owner reads when the system blocks them, all of them together so they sound like one
product. Rules in references/refusal-messages.md.

NO CODES. `finnavis.transfers.same_account` is a name for a machine; naming failures belongs to the
stage that writes the code, and §9 hands it over explicitly. No status numbers, no field names, no
payload shapes. Two columns, and do not restate the Given/When from §5. -->

| AC | What the owner is told |
|---|---|
|  |  |

## 11. Verification plan

<!-- Skill instruction: every §5 criterion gets at least one row, and every §6 row that is measured by
a test gets one too. Levels that exist in this repo: domain unit · application unit · integration
(Testcontainers + PostgreSQL, needs Docker) · architecture test · manual run. There is no CI and no
e2e suite — if a check needs one, that is a prerequisite in §9, not a level you may name here.
Finish with the command: `./build.sh test`. -->

| AC / NFR row | Level | What the test asserts |
|---|---|---|
|  |  |  |
