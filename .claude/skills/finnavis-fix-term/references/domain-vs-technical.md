# Domain word or technical word

`docs/GLOSSARY.md` holds words the **owner** would use about their own money. Everything else has
a better home, and sending the word there is more useful than refusing it.

## The test

> Could the owner say this word in a sentence about their money, without knowing how the product
> is built?

- "I moved money from my card to my savings **account**." → `Account` is domain.
- "That **statement** from the bank has last month on it." → `Statement` is domain.
- "The **cursor** for the next page." → not a word the owner has. Technical.

A second test catches the rest: **if `ARCHITECTURE.md`, `CLAUDE.md`, `.claude/rules/` or an ADR
already explains the word, it is not a glossary term.** Those files are the explanation. A second
one here is a copy that will go stale — and the glossary's own header rules implementation out.

## Refuse these, and say where they belong

| Word | Belongs in |
|---|---|
| port, adapter, ring, aggregate, composition root, dependency rule | `ARCHITECTURE.md` — it already defines all of them |
| unit of work, repository, query port, `IClock` | `ARCHITECTURE.md` §Ports and adapters, `CLAUDE.md` §Where interfaces go |
| error envelope, error code, `Module.ErrorName` | `docs/adr/0002`, `docs/adr/0003`, `.claude/rules/openapi.md` |
| cursor, pagination, page wrapper | `.claude/rules/openapi.md` §3 |
| migration, snapshot, staged spec | `.claude/rules/migrations.md` |
| PostgreSQL, EF Core, Npgsql, Minimal API, Testcontainers, xUnit, Docker | a SAD §2, or an ADR if it is still a choice |
| snake_case, PascalCase, UUID v7, `NUMERIC(19,4)`, `decimal` | `CLAUDE.md`, `data-model.md`, or the feature's ADR |
| endpoint, handler, DTO, request record, response record | `CLAUDE.md` §API |
| HTTP, JSON, cache, queue, index | nowhere — general vocabulary, not this product's |
| idempotency, retry, rate limit | a SAD, or an ADR — none of them exists in this product today |

Phrase the refusal with the destination, not just the verdict:

> `cursor` is how a page boundary is carried, not something the owner has. It is already settled in
> `.claude/rules/openapi.md` §3. Nothing to add to the glossary.

## A rule is not a term

"A transfer never carries a category" is true, important, and **not a definition**. It belongs in:

- `PRD.md` §5 as an acceptance criterion, and
- the feature's `docs/features/<slug>/CONTEXT.md` under `## Invariants`.

The glossary carries what a word means. A rule reaches it only as the NOT-boundary of a term — and
even then as a boundary, not as a requirement.

## The grey cases this repo has already decided

These look technical and are not, or look domain and are not. Do not re-litigate them.

| Word | Verdict | Why |
|---|---|---|
| `Balance` | **Domain** — already in the glossary | The owner asks "what is my balance". That it is computed and never stored is a `data-model.md` decision and belongs there |
| `Shape` | **Domain** — already in | It names the column arrangement a bank or a tracker chose. The owner meets it as "this file looks different" |
| `Import run` | **Domain** — already in | It is the thing the owner reads back and undoes. The pipeline that executes it is architecture |
| `Duplicate` | **Domain** — already in | What counts as the same movement is a product question the PRD answers, not a matching algorithm |
| `Default group` | **Domain** — already in | A reporting concept the owner sees. That it is never a row is the interesting half, and it lives in the boundary |
| `Ledger` | **Domain** — already in | "In the ledger" means recorded. It is the word the whole product is about |
| `Movement` | **Domain** — already in | The umbrella over transaction and transfer. Three other entries lean on it |
| `Fingerprint` | **Technical** | `csv-import` ADR-0008 settles it as a stored content hash. The owner never meets the word |
| `Statement shape recognition` | **Technical** | ADR-0009. The owner meets `Shape`, not the recognition |
| `Owner` | **Domain** — already in | It names the only actor. Note what the entry says: there is no user account, so it is a fact about the product, not stored data |

## Borderline: a word that is both

Some words have a domain sense and a technical sense in the same repo. `Account` is the clean
example — the owner's savings account, and a sign-in account that does not exist here.

The glossary takes the domain sense and uses the boundary to kill the other one. The committed
`Owner` entry does exactly this: *"NOT a user account (there is no sign-in and no second person
…)"*.

So an overloaded word is a reason to write the entry, not a reason to refuse it.

## When the user insists

If the user says the technical word genuinely needs a definition somewhere, they are usually
right — and the answer is an ADR or a line in `ARCHITECTURE.md`, not the glossary. Offer
`finnavis-architecture-design` or a standalone ADR and move on.

Do not store it here "just so it is written down": every other skill treats this file as the domain
language, and a technical word in it teaches them the wrong vocabulary.
