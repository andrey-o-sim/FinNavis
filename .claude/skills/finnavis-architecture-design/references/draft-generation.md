# Draft generation — where each section comes from

Used in Phase 3. Everything here happens in memory; nothing is written until the section resolves in
Phase 4.

## Sources, in priority order

1. **`CONTEXT.md`** (repo root) — canonical for role and domain-term names. If anything contradicts
   it, `CONTEXT.md` wins and the contradiction becomes a question.
2. **`docs/features/<slug>/PRD.md`** — §2 Goals, §3 Non-goals, §4 user stories, §5 acceptance
   criteria, §6 NFR with numeric targets, §6.1 Security/privacy, §7 KPIs, §8 Open questions, §9 Data
   impact, §10 Refusal messages, and any §1 ¶4 decision line.
3. **The repo's own documents** — `CLAUDE.md`, `ARCHITECTURE.md`, `SPEC.md`, `docs/adr/`,
   `Directory.Build.props`, and the earlier features' `sad.md` + `adr/`. These are the architecture
   map; see [repo-conventions.md](repo-conventions.md).
4. **Earlier sections of this draft.** §4 constrains §5; §5 names the participants §6 uses; §1's
   quality goals are what §10 expands. Read your own draft, not the file on disk.

## Per section

### §1 Introduction and goals

- **Intent** — one paragraph from PRD §1 and §2. What is being built, for whom, and what it
  deliberately leaves to a later feature.
- **Top-3 quality goals** — one line each, ranked from PRD §6 NFR and the §2 goal that matters most.
  Three is the norm; four when the PRD genuinely carries four. Each one must be expandable into a
  §10 scenario — if it cannot be verified, it is a wish, not a quality goal.
- **Stakeholders** — two rows, one person. Owner (records and reads; sign-off No) and
  Architect / Tech Lead (carries the decisions; sign-off Yes), with a line underneath saying they
  are the same human and why the rows are still separate: the interests differ.

  Never invent a Security Lead, a PM, a reviewer or a DevOps. There is one person. A row for a role
  that does not exist is the single most common way this genre turns into cosplay.
- **¶4 Decision overrides** — empty at draft time. The Phase 5 critic resolution loop fills it.

### §2 Constraints

Assembled, not asked. Write it from the files Phase 0 read — `CLAUDE.md`, `ARCHITECTURE.md`,
`SPEC.md`, `Directory.Build.props`, `docs/adr/` — using
[repo-conventions.md](repo-conventions.md) section A to tell a closed topic from an open one. The
rules themselves come from the files, never from the reference: the reference classifies, the files
are the source. Four blocks:

- **Technical** — language, framework, datastore, architecture style, the type-level rules
  (`decimal`, `DateTimeOffset` UTC, enums as strings, `snake_case`). State versions where the repo
  pins them, and say plainly where it does not.
- **Organisational** — one developer, evenings, no deadline, no release window, no second reviewer.
  Then the honest part: which PRD §6 rows cannot be measured today because the harness does not
  exist. That sentence is what turns into the first tasks of the feature.
- **Conventions** — point at `CLAUDE.md` and `ARCHITECTURE.md` rather than restating them, then list
  only the conventions this feature actually leans on.
- **Regulatory / external** — never N/A. For this product it is: no external regulation, self-hosted,
  the owner's own data, data classification **confidential**, and whatever PRD §6.1 says about the
  missing authentication boundary.

**Ask only about what the repo leaves open.** A major version the repo never pinned, a package the
feature needs that nothing else uses, a tool the feature introduces. Everything else is stated.

### §3 Context and scope

- Two or three sentences of business context from PRD §1.
- A greenfield/brownfield note in an HTML comment: what exists in `src/` today and what this feature
  is the first to build.
- **External systems table** — actors from `CONTEXT.md`, systems from the scan. For this product it
  is usually two rows: the Owner, and PostgreSQL.
- **An explicit sentence about what is absent.** "No third-party system is involved, and that is a
  decision, not an omission" — then name the planned consumers (the PWA, import, export, assistant
  access) and say each is a later feature that will consume this boundary rather than change it. A
  reader six months from now cannot tell a deliberate absence from a forgotten one.
- **C4 Context (L1)** — see [c4-and-sequences.md](c4-and-sequences.md).

### §4 Solution strategy

**The target surface is the first decision, and it is always asked.** Two values exist in this repo:

- `backend-service` — the REST API plus its database. The default, and correct whenever PRD §3 says
  the client is a later feature.
- `web-frontend` — the React PWA in `src/web`.

`mobile-app`, `desktop-app`, `cli`, `worker` and `library-sdk` are not part of this product. If the
PRD asks for something that needs one, that is a scope contradiction with `SPEC.md`, not a surface
to invent — raise it as a question.

If `web-frontend` is selected, the follow-on UI-architecture decision must open by saying that
`src/web` is an empty folder (`SPEC.md`, "Planned, not built yet"). Choosing between rendering
strategies matters far less than the fact that the first screen is unproven ground — it is a §11 row
whatever the choice.

Write the resolved list into the frontmatter `target_surfaces`, and draw one §5 container per
surface.

**Then three or four strategic choices** — the seeds the ADRs grow from. Each is a heading plus two
or three sentences of rationale that cite a quality goal from §1 and a constraint from §2. Typical
shape of a strategic choice in this product:

- The storage shape for things the glossary treats as different — one table or two.
- Whether reads are computed on demand or materialised.
- Whether records are mutable in place or corrected by appending.
- Whether the feature's data can arrive from outside, and what identity that implies.

Each later tactical decision should trace to one of these. A tactical decision that contradicts one
is a §11 row, not a quiet exception.

### §5 Building block view

- One paragraph: Clean Architecture, four rings, already fixed — this feature fills them in rather
  than changing the structure. Say which rings it actually touches.
- The decomposition tree, rendered from `ARCHITECTURE.md` § Folder layout with the feature's own
  areas and operations substituted, the untouched branches dropped, and any folder that does not
  exist yet named as new → [repo-conventions.md](repo-conventions.md) section C.
- The aggregates, and which of them get a repository port — with the reason when one does not.
- Whether the read path is separate, and why.
- **C4 Container (L2)** — see [c4-and-sequences.md](c4-and-sequences.md).

Conventions that are already in `CLAUDE.md` are stated here as inherited, with a pointer, and are
never ADRs.

### §6 Runtime view

One `sequenceDiagram` per critical flow, and §6 covers **all** of them — there is no later stage in
this project to finish the section.

Derive the flow list from the PRD, not from imagination:

- every user story in PRD §4 that changes data, grouped where the mechanics are identical;
- every read that aggregates;
- every refusal in PRD §10 that is structurally different from the others — a refusal that is just
  "not found" on a different noun is the same flow;
- the correction path, if the feature allows edits or deletes.

Group them so an M-sized feature lands at four to six flows, not fifteen. Two user stories that
differ only in which noun they touch share one diagram, with the difference noted underneath.

Messages are semantic — what the ring is doing, not the HTTP verb. Paths, verbs and status codes
belong to the API-contract stage. Participants come from §5 by name.

### §7 Deployment view

Walked, not assumed. The project default is the first option in each question, with its reasoning:

| Topic | Project default | When it changes |
|---|---|---|
| Topology | One Docker Compose stack on the owner's machine: one API container, one PostgreSQL container, one named volume. One replica of each | A feature that adds a background worker or a second process |
| Network | Private Docker network; only the API port published, and only on the local network. With no authentication, this is the only access control there is | Never, until sign-in exists |
| What this feature creates | Name it if the feature is the first to need `docker-compose.yml`, the Testcontainers harness or CI — and schedule them as the **first** tasks, before feature code | A feature arriving after those exist inherits them |
| Backups | Nightly `pg_dump` into a host folder the owner's own backup already covers | If the feature makes recovery harder — hard delete with no audit trail makes the dump the only way back |
| Logs | Structured logs to stdout, read with `docker compose logs`. No aggregation, no retention policy | A feature whose failures are invisible in logs |
| Metrics / alerts / tracing | **None, deliberately.** No availability target, nobody to page, one process with no network hop to correlate | Say so explicitly; an empty row reads as an oversight |
| Scaling thresholds | Concrete numbers from PRD §6, plus the row count at which to look again | Always feature-specific |

For a feature that changes nothing about deployment, `<!-- N/A: feature reuses the existing
deployment unit -->` is allowed — with one sentence of justification above it.

### §8 Crosscutting concepts

A table of concept / convention / where defined. Walked row by row, each with the project default
offered first. The default set:

| Concept | Default for this product |
|---|---|
| Logging | Structured logs to stdout through `Microsoft.Extensions.Logging`. No PII scrubbing — every value is the owner's own data |
| Authentication | None. One owner, no sign-in. Network isolation is the only control → §11 |
| Authorization | None. No second actor to deny, no existence to hide → §11 |
| Error handling | A broken domain rule produces a domain error; Presentation maps it to an RFC 9457 Problem Details response. Refusal wording comes from PRD §10, not invented here |
| ID strategy | UUID v7 generated in the Application layer, stored as PostgreSQL `uuid` |
| Money | `decimal`, single currency, no currency code stored |
| Time | `DateTimeOffset` stored UTC; `IClock` in `Domain/Abstractions` keeps date rules testable |
| Validation | Shape in Presentation, use-case checks in Application, invariants inside Domain entities |
| Concurrency | None. One owner, one client, no optimistic concurrency token → §11 |
| Indexes | Every new read path the feature adds names the index it needs and what breaks without it → §10, §11 |
| Internationalisation | N/A — English only |
| Observability | N/A — see §7 for why |
| Outbox / events | N/A — one process, no module-to-module messaging |

Two rules on this table:

- A row that says N/A still says **why**, or points at the section that explains it.
- **The index row is not optional.** If §6 or §10 introduced a read path, this section names the
  index it needs, and §11 carries the consequence of losing it. The decision about the exact column
  list belongs to the data-model stage; naming the need belongs here.

### §9 Architecture decisions

Not drafted. Assembled at the end of the walk from the ADR-spawns log: one row per file, four
columns, plus two short paragraphs that a table cannot carry:

- **How the ADRs relate** — which ones form one contract together, which follows from which. Six
  independent-looking rows are usually three decisions and their consequences.
- **What did not become an ADR, and why.** This paragraph is what stops the next reader from
  re-litigating a settled choice. Name the decisions that were reversible in an afternoon, or already
  fixed by `CLAUDE.md`.

Close the table against the folder in both directions before writing it.

### §10 Quality requirements

One scenario per §1 quality goal, in When / Then / How-verify form.

- **Numbers come from PRD §6 verbatim.** Not rounded, not restated. `p95 ≤ 300 ms` stays
  `p95 ≤ 300 ms`.
- A goal the PRD gives no number for is stated as an invariant, and the scenario says so. Inventing
  a figure is worse than admitting there is none.
- **How verify** names the test, in `Method_Scenario_ExpectedResult` form, says where it runs, and
  says what makes it fail — see [repo-conventions.md](repo-conventions.md) section G.
- If PRD §6 deliberately records no availability target, say that here too, with the reason. It is a
  decision, and §7 follows from it.

### §11 Risks and technical debt

Assembled, then confirmed. Sources, in order:

1. Every `save_as_oq` from the walk — severity literal `Open question`, owner and due mandatory.
2. Every PRD §8 open question still open at this stage — carried, not silently dropped.
3. The unproven-ground list ([repo-conventions.md](repo-conventions.md) section F): no harness, no
   CI, no compose, no client.
4. The cost side of each ADR — the Negative consequences are risks by another name.
5. Contradictions found while reading `SPEC.md` against the PRD. The `core-ledger` pass found four,
   and all four were real.

Then two blocks below the table:

- **Accepted debt** — things that are wrong on purpose, with the reason. A reader has to be able to
  tell a deliberate choice from an oversight.
- **Open questions closed during this pass** — questions that were due at this stage and got
  answered. Showing them closed is what stops the next stage from re-asking them.

### §12 Glossary

Not walked. Two tables:

- Terms from `CONTEXT.md` that this document actually uses, copied with their boundary intact.
  `CONTEXT.md` is canonical — say so, and say that it wins on any contradiction.
- Terms this document introduces that are technical rather than domain words — a query port, an
  external reference. They do not belong in `CONTEXT.md`, and recording them here is what keeps the
  SAD readable on its own.

If the walk surfaced a genuine **domain** word that is not in `CONTEXT.md`, that is not a §12 row —
it is a missing glossary entry. Flag it in the handoff so it gets added with its NOT-boundary.

## Hygiene, before the walk starts

Self-check the in-memory draft. The critic is the second backstop; this is the first.

- §1 stakeholders and §3 actors use `CONTEXT.md` names verbatim. No invented `user`, `admin`,
  `reviewer`, `PM`.
- §2 contradicts nothing in `CLAUDE.md` / `ARCHITECTURE.md` / `SPEC.md`, or carries an explicit
  override pointing at §11.
- §5's tree matches `ARCHITECTURE.md` § Folder layout — no `Features/`, no central `Contracts/`,
  and anything that does not exist yet is marked as new rather than asserted.
- Every Mermaid block declares its elements before any `Rel` line.
- §6 participants exist in §5 by name.
- §10 numbers appear in PRD §6 character for character.
- §11 has at least one row from the unproven-ground list.

A hygiene failure regenerates that section in memory before the walk reaches it.
