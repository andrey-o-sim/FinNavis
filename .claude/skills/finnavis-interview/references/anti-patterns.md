# Banned terms — Phase 13 self-check

The brief is a **product** document. It answers what and why. The stack answers how, and it
lives in `ARCHITECTURE.md`, `docs/adr/` and the task file that `my-toolkit:task-develop`
writes. A brief that names the stack has already made the architecture decision before the
problem was agreed, and that decision then never gets reviewed — it just gets inherited.

## The regex

Run over the body of the written file. Word boundaries on, case-insensitive.

```
\b(Postgres|PostgreSQL|Npgsql|EF ?Core|DbContext|IEntityTypeConfiguration|LINQ|Dapper|Redis|Kafka|RabbitMQ|SQLite|MongoDB|JSONB|snake_case|Minimal ?API|ASP\.?NET|Kestrel|Swagger|OpenAPI|xUnit|AwesomeAssertions|Testcontainers|Moq|Playwright|React|TypeScript|PWA|service worker|IndexedDB|Docker|docker-compose|CQRS|MediatR|p99|SLO|SLA)\b
```

Word boundaries matter. Without `\b`:

- `API` matches inside `rapid`,
- `PWA` matches nothing useful but `React` matches inside `Reaction`,
- `SLA` matches inside `translate`.

The original skill this one is derived from shipped a regex without boundaries and banned
`SM-2`, `FSRS` and `Leitner` — terms from a flashcard app, not this project. Keep this list
tied to what this repo actually uses; a banned-term list from someone else's product catches
nothing and wastes a check.

## Exemptions

Three places are allowed to break the rule, and nowhere else:

1. **The DoD self-check line** that names the rule itself.
2. **§12 Feasibility** — may cite **module paths** from this repo as evidence
   (`Application/Features`, `Infrastructure/Persistence/Migrations`). Paths only. "Tech ☑ —
   same shape as the transaction write path in `Application/Features`" is fine. "Tech ☑ — we
   already use EF Core" is not: that is the stack, and it is true of every feature, so it
   proves nothing.
3. **§6 Competitive analysis** — may name another product's interface if that is the gap
   ("access means running the app's own library headless"). It may not name this project's.

## Near-misses that are allowed

These read like stack names but are product words here. Do not flag them:

- **Account, Category, Category Group, Transaction, Transfer** — domain terms, defined in
  `docs/GLOSSARY.md`.
- **Balance, period, statement, import, export** — things the owner asks for.
- **Interface** in the sense of "what the owner touches". The ban is on naming the technology,
  not on discussing shape.
- **"no API at all"** quoted as a competitor's gap. This is the single most important finding
  in `core-ledger` §6 and banning it would be absurd. The exemption is §6 only.

## What to do when a term is found

Do not delete the sentence. Find out what the sentence was trying to say and rewrite it at
product altitude:

| Found | Usually means | Rewrite as |
|---|---|---|
| "store it in Postgres" | the data has to survive restarts | "the record is kept, not recomputed" |
| "add a JSONB column" | the shape is not fixed yet | "the entry carries extra detail whose shape is not settled" |
| "p99 under 200ms" | it must feel instant | "the answer arrives while the owner is still looking at it" |
| "a React screen" | a person, not a script, will use it | "the owner does this by hand, not through an integration" |
| "run a migration" | existing records need changing | "every record already entered has to be revisited" |

That last one is worth care: the "backfill over everything already entered" risk is a real
product-level concern on this project and belongs in §10. It just has to be said in the
owner's language, not the schema's.

## Length check

Same phase, same spirit. Target ≤ 5 pages (~2200 words, ±10%). When over, compress in this
order:

1. **§6 Competitive analysis** — the table is the value, the prose around it usually is not.
2. **§7 approach paragraphs** — each is capped at the six fields; anything beyond them goes.
3. **§9 edge cases** — keep the 5-8 that any approach must survive, drop the variations.

Do not compress §13. The rationale carrying four citations is the reason the document exists.
