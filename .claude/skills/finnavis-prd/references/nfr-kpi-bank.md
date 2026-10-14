# NFR and KPI — what to write, what to skip

Used in Phase 5 for §6, §6.1 and §7.

## The rule

Write a row only if the number changes a decision.

- An **NFR row** changes how the feature is built.
- A **KPI** tells you the job is done, or what to build next.

If no decision depends on the number, leave it out.

## What does not belong in §6

| Looks like an NFR row | What it really is | Where it goes |
|---|---|---|
| "a repeated load records nothing twice" | correctness | a §5 criterion |
| "a load never lands half-way" | correctness | a §5 criterion |
| throughput, availability, safe schema change | a decision about the whole project | `SPEC.md`, written once |

Two rows in §6 is normal. Zero rows is also fine — then write one line saying why.

## §6 — rows to consider

| Row | Keep it when | Example target |
|---|---|---|
| A bulk action: import, undo, recompute | almost always — a per-row question hides here | 1500 rows in ≤ 60 s |
| Reading a list or a report | the data it reads keeps growing | ≤ 400 ms over 20 000 movements |
| Writing one entry | almost never — one write forces no choice | — |
| Correctness under any order of actions | the feature has actions that can mix | 0 wrong balances over 1000 random sequences |

## How to measure

The measurement must name a test that exists in this repo today:

- `./build.sh test` — the whole suite
- domain and application unit tests — no database, no network
- integration tests — Testcontainers with PostgreSQL, needs Docker running
- a manual run of the API

There is no CI, no metrics stack, no dashboard and no load test. "p95 in Grafana" is not a
measurement. If the test does not exist yet, say so under the table and put the task in §9:

> **Measurement prerequisite.** These rows need <what is missing>. Building it is a task of this
> feature, listed in §9.

## §7 — KPI

Write one to three. Each one must be visible without counting. If you have to sit down with the file
once a month and count rows, you will not do it, and the target stays empty.

Good:

- a chore that stops — "rows re-typed by hand: ~30 a month → 0"
- the job is done — "old tracker still in use: yes → no"
- a habit you can see in the ledger — "days with at least one entry: 0 → 20 of 30"

Skip:

- any share or percentage — nobody counts it
- anything that needs a second user: adoption rate, retention, DAU/MAU, NPS, churn. There is one user.

Each KPI is baseline → target → timeframe. A baseline of 0 or "unknown" is fine.

## §6.1 — security and privacy

Four lines, plus abuse cases:

- **Data classification** — one person's full financial history is `confidential`. Say it plainly.
- **Personal data touched** — the owner's own records. Name any new free-text field.
- **AuthZ / AuthN impact** — usually "none added, and that absence is the main risk". Track it in §8.
- **Abuse cases** — the real ones here: anyone who reaches the installation has full access; free
  text shown back later; a large field used to fill up storage; the same action arriving twice. Give
  the business answer for each, not a status code.
- **Security review** — there is no security team. Either the owner reads the feature again with one
  concrete question in mind (say which), or `N/A` with a reason.
