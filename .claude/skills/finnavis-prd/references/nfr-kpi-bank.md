# NFR rows and KPIs for a self-hosted project with one user

Used in Phase 5. Two banks: the §6 default rows and the §7 metric bank, plus what each one replaces.

## §6 — the default NFR table

Start from these rows. Keep the ones that apply, drop the ones that do not **and say why in one line**,
add feature-specific rows as needed.

| Aspect | Default target | Measurement |
|---|---|---|
| Latency p95 — record or change one entry | ≤ 300 ms | timed in the integration suite |
| Latency p95 — list one page of history, filtered | ≤ 400 ms | timed in the integration suite |
| Latency p95 — a period report or aggregate | ≤ 500 ms over ~20 000 movements (about 3 years) | timed in the integration suite against seeded data |
| Correctness under any order of operations | 0 discrepancies across ≥ 1 000 randomised sequences of writes, edits and deletes | property-based test in the suite |
| Data durability | a restore from backup loses at most one day; the restore path is exercised at least once before the feature is called done | manual, recorded in §11 |
| Schema change safety | `./build.sh db-update` applies on a database that already holds data, without losing or corrupting a row | integration test on a seeded database |

Tune the numbers to the feature; do not copy them blindly. A feature that only reads has no write-latency
row. A feature that touches no schema has no schema-change row.

### Rows the generic template has and this one drops

- **Throughput** — "≥ N requests/s per instance". One user cannot generate contention. Keep it only as a
  crude guard against an accidental O(n²) (`≥ 10 req/s on one instance, smoke test`), and only when the
  feature does heavy aggregation. Otherwise: `N/A — one user, no concurrent load`.
- **Availability SLO** — "99.9%, monthly window". Nobody is owed it. Write
  `N/A — self-hosted, one user; downtime costs only the owner's own evening`.
- **Scalability / multi-tenancy** — no tenants exist. Drop without ceremony.

### The measurement rule

A target is only real if someone can check it. What exists in this repo **today**:

- `./build.sh test` — the whole suite.
- `tests/FinNavis.Domain.UnitTests`, `tests/FinNavis.Application.UnitTests` — no database, no network.
- `tests/FinNavis.Infrastructure.IntegrationTests` — Testcontainers plus PostgreSQL, needs Docker running.
- `tests/FinNavis.ArchitectureTests` — the dependency rule.
- A manual run of the API.

What does **not** exist: CI, a metrics stack, dashboards, alerting, an APM, a load-test harness, a
seeded performance dataset. So "p95 measured in Grafana" is fiction. Either the measurement names
something from the list above, or the row carries the prerequisite explicitly:

> **Measurement prerequisite.** These rows are measured by an integration harness with seeded data that
> does not exist yet. Standing it up is a task of this feature, not a side effect — it goes into §11.

The `core-ledger` PRD had to invent that paragraph on its own. Here it is part of the contract.

## §7 — the solo KPI bank

At least three metrics, each with baseline → target → timeframe. Baseline 0 is normal for a new feature.
Pick from the bank, or write a feature-specific one in the same shape.

| Metric | What it measures | Typical shape |
|---|---|---|
| **Entry habit** | does the ledger actually get used | days per month with ≥ 1 recorded movement; 0 → ≥ 20 of 30, within 60 days |
| **Ledger trust** | does the data agree with reality | times a balance disagreed with the real account during the monthly reconciliation; unknown → 0, within 90 days |
| **Migration depth** | is the history actually moving over | months of history recorded here; 0 → ≥ 3, within 90 days |
| **Old tool still open** | the honest kill metric | is the previous tool still used in parallel; yes → no, within 90 days |
| **Time to answer** | the question the feature exists to answer | minutes to answer "where did my money go in March"; N → under 1 |
| **Hands-off share** | for anything automated | share of rows that needed no manual touch; 0 → ≥ 90% |
| **Evenings to first use** | the cost side | evenings from first commit to first real use; estimate → actual |

### Metrics that are banned here, and why

| Banned | Why it cannot work |
|---|---|
| Adoption rate, "% of active users who…" | the denominator is 1 |
| Retention / cohort return rate | one person does not form a cohort |
| DAU / MAU, engagement uplift | same |
| NPS, CSAT | you would be rating yourself |
| Churn | there is nobody to leave |
| Support tickets, incident count, MTTR | no support channel, no on-call |

If a metric needs a second user to compute, it is not a metric for this project. The self-check in
Phase 8 rejects them.

## §6.1 — security and privacy, solo shape

Four lines, honest ones:

- **Data classification** — one person's complete financial history is `confidential`. Say it plainly.
- **Personal data touched** — the owner's own records. Name any new free-text field, because free text is
  where surprises end up.
- **AuthZ / AuthN impact** — today, usually "none added, and that absence is the main exposure". Track it
  in §8 rather than pretending the feature closed it.
- **Abuse cases** — the real ones for this project: anyone who reaches the installation has full access;
  free text rendered later by a client; an unbounded field used to bloat storage; the same request
  arriving twice. Each with the business response, not a status code.
- **Security review** — there is no security team. Either the feature needs a careful second pass by the
  owner (say what to look at), or `N/A` with a concrete reason.
