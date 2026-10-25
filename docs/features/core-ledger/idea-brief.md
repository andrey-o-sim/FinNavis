---
status: Confirmed
owner: "Andrey Simonenko"
reviewers: []
updated_at: "2026-10-09"
feature_size: TBD     # set by sdlc:classify-size, not here
stage: "01"
depth: medium
ticket: "none"
value_score:
  rice: 0.27
  state: confirmed
  confirmed_at: "2026-10-09"
feasibility_state: confirmed
---

# Idea Brief — Core Ledger

## 1. Raw idea

Build the base money-tracking layer of FinNavis: manage accounts and categories, record
income, expenses and transfers, and view and filter transaction history. The category model
changed twice during the interview and settled in the author's own words: «есть только
категории и все. Но есть возможность группировать категории, например, группа - коммуналка,
категории: свет, вода, ... Это удобно для структурирования данных и фильтрации.» So: flat
categories with optional groups over them. REST API only — no web client here.

## 2. Problem

The author tracks money today in **MoneyOk** and listed eight concrete gaps: export is
all-data only and cannot be scoped to a month; no way to ask "show expenses for a period
broken down by category"; no custom category icons; no integration with other apps such as
a spreadsheet; no statistics or charts; no way to connect an LLM; no CSV import; no web
application.

Stated plainly: **this feature closes only gap #2, and partly gap #8.** The other six are
separate later features. Core Ledger is the foundation they all stand on — nothing can be
exported, imported, charted or queried by an assistant until the ledger exists. The brief
treats it as an enabler, not a painkiller, and §11 prices it that way.

## 3. Users

One person: the repo owner, tracking their own money. Single-user, self-hosted, single
currency. No second segment, no customer, no market — the only consumer of this feature is
its author, calling the API directly.

## 4. Why now

The repository is a skeleton: 11 source files, all scaffolding, six commits, no entities, no
schema, no endpoints. Every other item on the draft business-requirements list depends on
this one, so nothing else in the product can start until the ledger exists.

## 5. Out of scope

- Web client, installable app, offline reading — a separate later feature.
- Authentication and Google OAuth. Settled in the interview: one owner, so records carry no
  owner field; a login lock comes later as its own feature.
- CSV import, period-scoped export, spreadsheet/Drive integration.
- Charts, statistics, monthly summaries; custom category icons; plans to buy and sell.
- Capturing expenses from messages; de-duplication of repeated API or assistant requests.
- Multi-currency, bank integrations, budgets and forecasts.

## 6. Competitive analysis

| # | Product · URL | Relevant features | Value (1-5) | Gap |
|---|---|---|---|---|
| 1 | MoneyOk · https://apps.apple.com/us/app/money-ok-personal-finance/id606031670 | One subcategory level; in-app date-range charts; CSV export | 2 | No API at all; export is all-data, never period-scoped |
| 2 | YNAB · https://api.ynab.com/v1 | Two levels (group → category); endpoints per category and month with date-range filters | 4 | Split transactions return as sub-rows, so period×category needs client-side reassembly |
| 3 | Actual Budget · https://actualbudget.org/docs/api/reference/ | Category groups containing categories; amounts roll up to the group | 3 | No conventional interface — access means running the app's own library headless |
| 4 | Firefly III · https://docs.firefly-iii.org/references/firefly-iii/search/ | Flat categories; search combines category + date range in one query | 3 | No real hierarchy — "subcategories" are only a naming convention |
| 5 | Buxfer · https://www.buxfer.com/help/api | Free-form tags instead of categories; period-over-period comparison | 2 | No enforced hierarchy, so group totals are not guaranteed |

All rows searched 2026-10-09, one query per product, each naming the product plus
"categories subcategories API filter by date".

**Hierarchy findings.** Two levels (group → category) is the common shape among tools that
model hierarchy at all; YNAB and Actual Budget both use it, independently matching the model
the author settled on. Firefly III and Buxfer skip hierarchy entirely. No researched product
combines a real two-level model with a single direct call returning a period-sliced category
breakdown — **that combination is the wedge.**

## 7. Strategic approaches

### Approach A — Flat Ledger, One Report
- **Thesis**: accounts, categories, transactions and transfers, plus one filtered history
  call and one fixed period-by-category breakdown — nothing else.
- **For whom**: the owner who only wants MoneyOk's missing answer, "where did my money go
  last month?"
- **Outcome metric**: requests for a period's expenses split by category — impossible in one
  shot today → 1.
- **Key trade-off**: one fixed shape; group totals, account slicing and comparisons each
  become a new feature.
- **Effort signal**: S
- **Recommended?** ◯

### Approach B — Query-Any-Slice Ledger
- **Thesis**: any recorded fact — period, category, group, account, kind — combines into one
  aggregated answer, so later features reuse that capability instead of inventing their own.
- **For whom**: the owner who left MoneyOk because every new question meant a manual export
  and a spreadsheet.
- **Outcome metric**: share of recurring money questions answerable in one request — ~0
  today → all of them, with no new endpoint per question.
- **Key trade-off**: a general slice grammar costs design time up front and risks
  over-building before a client exists to prove which slices matter.
- **Effort signal**: L
- **Recommended?** ◯

### Approach C — Group Rollup Ledger
- **Thesis**: one filtered history and one breakdown answering both levels — totals per
  group and per category from the same request, filtered by period and account, shaped to
  take more dimensions later without breaking callers.
- **For whom**: the owner who looks at spending by theme ("utilities") and drills into the
  detail ("electricity") when a number looks wrong.
- **Outcome metric**: requests to answer "spend per category this period, grouped, drill
  into any one" — not possible in MoneyOk → 1, with reconciliation done by the ledger.
- **Key trade-off**: only period, account and category are sliceable at launch; the contract
  is built to take more.
- **Effort signal**: M
- **Recommended?** ●

## 8. Multi-perspective feedback

### Engineer
- A and C share one write model; only the read contract differs. C costs a grouping key and
  a nested response shape over A, not a second subsystem.
- B's cost is the combination grammar, not the queries: legal dimension sets, result-size
  bounds, every combination correct. Test burden grows combinatorially.
- A's fixed breakdown is hardest to extend without a second versioned endpoint; C's named
  filters extend additively; B is already open.

### Executive
- The currency is evenings, and the risk is stalling before any dependent feature exists.
- A ships soonest but pays again at the first new question — and six later features all ask
  new questions.
- B spends evenings on slices nobody has demanded; the client that would prove which slices
  matter does not exist yet.
- A foundation is judged by what it unlocks next month, not by launch day.

### UX-researcher
- Here the owner's interface *is* the response shape: a good shape answers "where did my
  money go" without client-side arithmetic.
- A forces the caller to re-aggregate category totals into group totals — redoing work the
  server already did.
- C answers both levels in one call, matching the success metric and handing the later web
  client a ready hierarchy.
- B makes everything askable but nothing obvious.

### Synthesis matrix

|        | Engineer | Executive | UX |
|--------|:--------:|:---------:|:--:|
| App. A | **+** smallest surface, trivial tests | **0** fast now, repays later | **−** caller re-aggregates group totals |
| App. B | **−** combinatorial correctness, unbounded results | **−** evenings spent on unproven slices | **0** powerful, must learn grammar |
| App. C | **+** modest cost, additive extension | **+** buys foundation at sane cost | **+** both levels, one request |

## 9. Trade-offs and edge cases

| Approach | Pros | Cons |
|---|---|---|
| A | Fastest to something working; smallest test surface | Owner does the server's arithmetic; every later question is a new endpoint |
| B | Never needs re-opening; later features reuse one capability | Unbounded results, no legal-combination rule, no stopping rule |
| C | Hits the success metric directly; extends additively | Only period, account and category sliceable at launch |

### Edge cases
- A category with no group: reports need an explicit "ungrouped" bucket, or the sum of
  groups silently stops equalling total spend.
- Renaming, re-grouping or deleting a category or account that already has history — the
  same month regenerated later shows different numbers.
- Transfers in history: a separate entity carrying no category, so the view must place them
  without letting them count as spending.
- A transfer where only one side is applied — the two accounts disagree.
- Backdated or edited entries after a balance has been read.
- A late-evening purchase near a month boundary, stored UTC, read as a local month.
- Period filter matching no rows — an empty breakdown, not an error.

## 10. Risks

- **Top risk (devil's advocate, most critical).** The foundation delivers no relief, so the
  project is abandoned half-entered. Core Ledger ships API-only with no capture shortcut, no
  import and no charts, while every pain the author named is deferred — so the author must
  hand-enter into both MoneyOk and FinNavis. Signal: entry rate decays after two to three
  weeks, the ledger holds a few dozen rows with a gap in the middle, and the next commit is
  months away. Hits B hardest, A least.
- Balance drifts from its own history — a backdated entry, an edited amount or a
  half-applied transfer. Found weeks later as a small unexplained gap, with no way to tell
  which entry diverged; trust in the app collapses at that point.
- No identity on an entry, and no field recording where it came from, blocks the planned
  de-duplication, import and message-capture features; adding either later means a backfill
  over every existing row before any visible work starts.
- Rename or delete of a category or account silently rewrites historical reports.
- UTC storage versus the owner's local calendar month misfiles entries near a boundary,
  making "any period" wrong in a way that looks like a rounding error.

## 11. RICE — confirmed

- **Reach (R)**: 1 — the sole owner (§3). Every feature here has Reach 1.
- **Impact (I)**: 1 of 3 — closes 2 of 8 stated pains directly (§2); its value is enabling,
  and the Executive lens (§8) judges a foundation by what it unlocks next.
- **Confidence (C)**: 0.8 — scope, metric, category model and ownership all settled in this
  interview; five open questions remain (§15).
- **Effort (E)**: 3 person-weeks — signal M from Approach C (§7). On evenings, closer to two
  or three calendar months.
- **RICE = 1 × 1 × 0.8 / 3 ≈ 0.27**
- **Honest caveat**: at Reach 1 the absolute score means nothing. It is useful only to rank
  Core Ledger against the other draft business requirements, which all share Reach 1.
- **State**: confirmed

## 12. Feasibility — confirmed

- [☑] **Skills**: the owner personally built the layered skeleton, the architecture tests
  enforcing the dependency rule, and cross-platform build scripts — evidence in this repo,
  not an assumption.
- [☐] **Tech**: no adjacent shipped feature to cite. All 11 source files are scaffolding; no
  migration has ever run, no repository has been written, and no local database setup exists
  yet — while integration tests are designed to need one.
- [☐] **Time**: no completed feature in the history; six scaffolding commits give no signal
  about pace, so the 3-person-week estimate has no baseline.
- **State**: confirmed — the owner accepted this honest 1-of-3 reading. The two empty boxes
  are the first concrete tasks, not a blocker.

## 13. Recommendation

**Selected: Approach C — Group Rollup Ledger.**

Competitive analysis (§6) shows no product combines a real two-level model with a single
direct call returning a period-sliced category breakdown — MoneyOk has no API, YNAB forces
client-side reassembly, Actual Budget hides its hierarchy behind a non-standard interface.
That gap is exactly what C targets. The synthesis matrix (§8) gives C a plus in all three
lenses, the only approach to do so, and the UX lens marks A down for making the caller
re-aggregate group totals — which would leave the success metric only half met. RICE (§11)
is 0.27 at Effort 3; B's L signal would push Effort higher and the score lower, for
capability nobody has yet demanded. Feasibility (§12) is 1 of 3 with the technical path
unproven, so committing to the largest approach would be unwise.

**Locked-in pointer**: the PRD stage inherits a ledger of accounts, flat categories with
optional groups, transactions and transfers, plus one filtered history and one
period/account-filtered breakdown answering group and category levels in a single request.
No web client, no authentication, no owner field.

## 14. Parked & rejected approaches

| # | Approach | Status | Reason | Revisit trigger |
|---|---|:---:|---|---|
| A | Flat Ledger, One Report | parked | Caller re-aggregates group totals, so the success metric is only half met | If evenings get scarce and shipping anything beats shipping the right shape |
| B | Query-Any-Slice Ledger | parked | Effort L on slices nobody has demanded; no stopping rule | Once a client exists and shows which slices are actually asked for |
| — | Two-level categories, leaf-only posting | rejected | Breaks retroactively: adding the first subcategory to a category that already holds expenses strands them on a non-leaf | — replaced by flat categories with optional groups |

## 15. Open questions

- [ ] Is a dimension outside period/account/category — payee, merchant, free tag — needed
      soon? The one case where Approach C is exposed. Owner: Andrey, due: architecture-design.
- [ ] How are UTC timestamps reconciled with the owner's local calendar month? Owner:
      Andrey, due: data-model stage.
- [ ] Should an entry carry a client-supplied identifier from day one, so de-duplication,
      CSV import and message capture need no backfill later? Owner: Andrey, due:
      architecture-design.
- [ ] Custom category icons and charts are in the author's pain list but **not** in the
      draft business requirements. Add or drop? Owner: Andrey, due: next feature scoping.
- [ ] `SPEC.md` Non-goals still says "no authentication, one owner" while the draft
      requirements list Google OAuth and a secured public API. Settled here as "one owner
      now, OAuth later", but the two sections still read as contradictory. Owner: Andrey,
      due: when the OAuth feature is scoped.

## Related

- `GLOSSARY.md` — glossary, canonical. Holds `Category`, `Category Group`, `Transaction`,
  `Transfer`.
- `SPEC.md` — product scope and draft business requirements.
- `ARCHITECTURE.md`, `docs/adr/0001-initial-setup.md` — layering and the dependency rule.

## DoD self-check

- [x] 15 sections present
- [x] No anti-pattern terms (datastore/framework/library names, latency targets)
- [x] Length ≤ 5 pages (~2400 words)
- [x] Frontmatter status: Confirmed
- [x] RICE confirmed (state: confirmed)
- [x] Feasibility confirmed (state: confirmed)
- [x] Recommendation present, rationale cites §6, §8, §11, §12
