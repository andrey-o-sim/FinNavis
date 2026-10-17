---
status: Draft
slug: "csv-import"
stage: "03"
feature_size: L
depth: medium
updated_at: "2026-10-17"
source_prd: "./PRD.md"
target_surfaces: [backend-service]   # decided in §4. One container in §5.
---

# Software Architecture Document — csv-import

<!-- Arc42, 12 sections. An empty section is not allowed; <!-- N/A: <one-line reason> --> is. -->
<!-- C4 Context (L1) lives inline in §3. C4 Container (L2) lives inline in §5. -->
<!-- §6 covers every critical flow — no later stage finishes it in this project. -->
<!-- Numbers in §10 come from PRD §6 verbatim. No rounding, no inventing. -->
<!-- Anything in CLAUDE.md / ARCHITECTURE.md is an inherited constraint here, never a choice. -->

## 1. Introduction and goals

<!-- Why: in a year nobody remembers which three qualities this feature was built around. -->
<!-- Write: one intent paragraph + the top-3 quality goals as one-liners + the stakeholder table. -->
<!-- Watch: one person owns this. Two rows, both the same human. No invented reviewer or PM. -->

**Intent.** Load a file the Owner hands over into the ledger. Two jobs run over one write path and
differ in three policies (PRD §1): the monthly bank statement, where the rules for a shape of file
are confirmed once and nothing is created that the ledger lacks; and the one-time move of three
years of history out of the old tracker, where the file's own names are the truth and the missing
accounts and categories are created. Every movement a load records remembers the load that brought
it, so a load can be read back and undone as a whole; handing the same file over again records
nothing a second time. There is no screen — PRD §3 keeps the client as a separate later feature, so
this one ships the load itself.

**Top-3 quality goals (1-liners; full scenarios in §10):**

1. **Nothing is recorded twice** — a row that names a movement already in the ledger is counted as
   already there and is not recorded again, whether that movement came from an earlier load or was
   typed by hand (AC-15, AC-20, AC-21).
2. **A load is all-or-nothing, and reversible** — a load that fails part-way leaves the ledger
   exactly as it was, and a finished load can be removed as a whole, with undo of 1500 rows at
   p95 ≤ 30 s (AC-16 — AC-22, PRD §6).
3. **A bulk load fits inside one evening's patience** — the move of the old history, about 1500 rows
   across several accounts, at p95 ≤ 60 s (PRD §6).

**Stakeholders.**

| Role | Interest | Sign-off owner? |
|---|---|---|
| Owner | Hands over a statement or an export; confirms what each of the file's categories means; reads what a load did; undoes a load that went wrong | No |
| Architect / Tech Lead (Andrey Simonenko) | SAD and ADR approval | Yes |

> Solo project: the same person fills both rows. They are kept apart because the interests differ —
> the Owner cares that the figures are right, the Architect carries the decisions recorded here.

<!-- ¶4 Decision overrides — filled by the Phase 5 critic resolution loop, empty otherwise. -->
<!-- Each: «Decision override: <headline> — rationale: <reason>» so a later reader sees the choice. -->

## 2. Constraints

<!-- Why: §4 only works once §2 has fixed what was already fixed. These are inputs, not choices. -->
<!-- Write: four blocks — Technical / Organisational / Conventions / Regulatory. -->
<!-- Watch: assembled from the repo, not asked. Mark anything this feature pins for the first time. -->

These are inputs, not choices made here. Everything below was already fixed by the repo or by an
earlier feature before this one started, with the two exceptions marked.

**Technical.**
- .NET 10 / C# 14; nullable enabled; implicit usings; `TreatWarningsAsErrors`
  (`Directory.Build.props`).
- ASP.NET Core Minimal APIs; OpenAPI document served at `/openapi/v1.json` in Development.
- **PostgreSQL 18** through EF Core + Npgsql. The major version was pinned by `core-ledger`
  (`core-ledger/sad.md` §2 and §7), not by a root file — the root `SPEC.md` still says only
  "PostgreSQL". Cite that feature, not `SPEC.md`, for the number.
- Clean Architecture, four projects, dependency rule enforced by `tests/FinNavis.ArchitectureTests`
  (`docs/adr/0001-initial-setup.md`).
- Money is `decimal`. Timestamps are `DateTimeOffset`, stored UTC. Enums are stored as strings.
  Tables and columns are `snake_case`.
- **Pinned here for the first time:** a CSV reading library in Infrastructure. The repo names none,
  and this feature is the first to need one. Domain and Application never see it — it sits behind
  an Application port (§5), so the package choice stays an Infrastructure detail and is settled at
  implementation rather than here.

**Inherited from `core-ledger`, not re-decided here.** Each is an Accepted ADR of a feature that
ships before this one. Re-opening any of them would be a new ADR that supersedes the old one, not a
fresh choice:

- UUID v7 primary keys generated in the Application layer (ADR-0005). The deciding argument in that
  record was this feature: an Import run can build its rows and the links between them in memory
  and commit them as one unit, because the identifiers exist before the insert.
- `transactions.external_ref`, nullable with a unique index, present before any row exists
  (ADR-0006). It was added for this feature and `core-ledger` writes null into it everywhere. That
  record deliberately left *how* a duplicate is recognised to this feature.
- The ledger is corrected by editing and deleting rows in place, with no audit trail (ADR-0003).
  Undo here is therefore a real delete rather than a reversing entry. That is inherited, not chosen.
- Reads that aggregate go through a query port in `Application/Abstractions`, not through the
  repositories (ADR-0004).
- Transactions and transfers live in two separate tables (ADR-0001), so anything that has to cover
  both kinds of movement covers two tables.

**Organisational.**
- One developer, working evenings. No deadline, no release window, no second reviewer.
- **`core-ledger` has no code.** PRD §9 names the ledger as a hard prerequisite — accounts,
  categories, transactions, transfers, balances. Today `src/` holds four `AssemblyMarker.cs` files
  and `Program.cs`: no entity, no migration, no endpoint. This feature cannot begin until that one
  ships, and every inherited ADR above describes a table that does not exist yet. Carried into §11.
- Both PRD §6 rows need the integration test harness, a seeded ledger and a real sample file. None
  of the three exists (`SPEC.md` "Planned, not built yet"; PRD §6 says the same in its own
  measurement note). Carried into §7 and §11.
- PRD §1 records that the scope grew past what the idea-brief priced — RICE was scored from effort
  signal M, and the scope written is L. That ranking must be re-checked before this feature is
  scheduled. Not an architecture matter; carried into §11 so it is not lost.

**Conventions.**
- `CLAUDE.md` — layout, dependency rules, DI, API, database and test conventions.
- `ARCHITECTURE.md` — the four rings, and what counts as a port versus a plain interface.
- Repository ports live in `Domain/Abstractions`; outside-world ports in `Application/Abstractions`.
  `ARCHITECTURE.md` names `IStatementImporter` as its example of the second kind — this feature is
  the first to actually add one.
- Business areas sit at the root of the Application project; each endpoint folder holds its own
  request and response records. No `Features/` wrapper, no central `Contracts/`.
- One `IEntityTypeConfiguration<T>` per entity. Migrations are append-only — never edit an applied
  one.
- `SPEC.md` "Safe schema change" — a change reaching a database that already holds rows must lose
  or alter no row. This feature is the first whose own writes land next to history the owner typed
  by hand, so the rule is load-bearing here rather than theoretical (AC-12).

**Regulatory / external.**
- No external regulation applies. Self-hosted, one owner, the owner's own data. No bank
  integration: the file is handed over by the owner, so no third party is called and no credential
  is held.
- Data classification: **confidential**, and this feature widens what that covers. For the first
  time the product stores text the owner did not write — shop names and descriptions produced by
  their bank — together with the name the file came with (PRD §6.1).
- Security review: **required** (PRD §6.1), for a reason specific to this feature: it is the first
  time the product reads a file from outside itself. The question to read the design with is the
  PRD's own — what does a file that is not what it claims to be do before anything is recorded?
- No authentication boundary, so anyone who reaches the installation can load a file into the
  ledger, not only read it. Accepted for now; the installation stays off public networks. Carried
  into §11.

## 3. Context and scope

<!-- Why: draws the system boundary — who talks to it, where the trust boundary ends. -->
<!-- Write: 2-3 sentences of business context + the external-systems table + the C4Context block. -->
<!-- Watch: name what is deliberately absent. A reader cannot tell an absence from an omission. -->

The Owner hands a file to the system — either a statement their bank produced for one account, or
an export of three years of history from the tracker they are leaving — and the system turns its
rows into movements in the ledger. It reads back what each load did, and removes a load as a whole
when one went wrong. The system is self-hosted and serves exactly one person. It still talks to
nothing outside itself except its own database.

**The trust boundary moves in this feature, and that is the point of the section.** Until now every
byte in the ledger was typed in by the Owner, so the only untrusted input was a malformed request.
From here on a file the Owner did not write — produced by a bank, in a shape the bank controls —
crosses into the system and its text is stored. The boundary is still drawn at the API: the file is
*data the Owner carries in*, not a system the product talks to. Nothing is fetched, no credential is
held, and no bank is called. That is why the bank does not appear as an external system below.

<!-- brownfield: src/ holds four AssemblyMarker.cs files and Program.cs. No entity, no endpoint, no
     migration. core-ledger owns the ledger this feature writes into and has no code either, so the
     only thing on disk that this feature can build on is the four projects and the architecture
     tests. This feature is the first to read anything from outside the process. -->

**External systems (in / out):**

| Actor or system | Type | Interaction |
|---|---|---|
| Owner | Person | Hands over a statement or an export; confirms once what each category a statement shape carries means; reads back what a load recorded, found already there and set aside; undoes a load |
| PostgreSQL 18 | System (separate process, self-hosted alongside the API) | Stores the ledger `core-ledger` owns, plus this feature's own records: the import runs, the mapping profiles, the set-aside rows and the link from each recorded movement back to the load that brought it |

**Nothing else is here, and each absence is a decision.** There is no bank connection, no identity
provider, no object store and no message broker (PRD §3 Non-goals). In particular there is no file
store: §4 (Solution strategy) decides that the uploaded file is never kept, which is what keeps this
diagram at two external boxes instead of three.

PRD §3 also rules out every source other than a file the Owner hands over — chat messages, email
and receipt photographs are all named there as non-goals, and none of them is designed for,
scheduled or assumed anywhere in this document. If one is ever built it is its own feature with its
own SAD; what it would reuse is the write path drawn here, not this boundary. The React PWA is the
same case: PRD §3 says there is no screen in this feature, and `src/web` is an empty folder.

**C4 Context (L1):**

```mermaid
C4Context
    title csv-import — System Context

    Person(owner, "Owner", "The single person whose money this installation tracks")

    System(finnavis, "FinNavis API", "Reads a handed-over file, records the movements it names, keeps what it set aside with the reason, and can remove a whole load again")

    SystemDb(pg, "PostgreSQL 18", "Stores the ledger, the import runs and their set-aside rows, the mapping profiles, and the link from each recorded movement to the load that brought it")

    Rel(owner, finnavis, "Hands over a statement or an export; confirms the rules for a shape once; reads back and undoes a load", "HTTPS / JSON")
    Rel(finnavis, pg, "Reads and writes the ledger and the import records", "EF Core + Npgsql")
```

## 4. Solution strategy

<!-- Why: the 3-4 pillars every ADR grows from. The densest section — the ADR gate fires here most. -->
<!-- Write: target surface FIRST, then 3-4 strategic choices, each with 2-3 sentences of rationale. -->
<!-- Watch: mirror the surface list into the frontmatter. It gates §5 and every later stage. -->

**Target surface(s) (the first decision — what is being built):** `[backend-service]`

One REST API plus the database `core-ledger` already defines. PRD §3 is explicit — "No screen —
there is no client yet; this feature ships the load itself" — and `src/web` holds a README and
nothing else (`SPEC.md`, "Planned, not built yet"). There is no UI surface, so no UI-architecture
decision is taken here, and §5 (Building block view) draws one application container.

**Top strategic choices (the seeds for ADRs):**

1. **The uploaded file is never stored** (ADR-0001). Confirming the rules for a statement shape
   (AC-01) needs the file read before the first load, because the Owner can only name what each of
   the file's categories means once the system can show which categories it carries. That makes the
   first load of a new shape two calls — read, then load — and the file is handed over for both.
   The alternative, keeping the file under an upload id, buys one fewer upload per bank and costs
   an `IFileStorage` port, an adapter, a volume in §7 (Deployment view), a lifetime policy for
   abandoned uploads, and whole bank statements sitting on disk. §2 classifies that text
   confidential and PRD §6.1 names it the feature's main new exposure, so the cheapest way to
   protect it is not to have it. The price paid: the Owner uploads twice, once per bank, ever.

2. **A row is matched against the ledger before it is recorded — exactly first, by proximity
   second — and always against a snapshot taken before the run writes anything** (ADR-0002,
   ADR-0003). This is quality goal 1 and the feature's main risk: a false match deletes real
   spending silently, while a missed match leaves a visible row the Owner can remove. Two tiers,
   because neither covers the other. The exact tier uses `transactions.external_ref`, which
   ADR-0006 of `core-ledger` put there for this purpose and left unique-indexed, so a second load
   of a row the bank numbered is refused by the database rather than by a guess. The proximity tier
   — same account, date within a window, same amount — is unavoidable regardless, because AC-21
   requires a file row to match a movement the Owner typed by hand, and a hand-typed movement
   carries no `external_ref` at all.

   The two records are separate because the two halves are separable. ADR-0002 fixes the *shape* of
   the rule; its parameters — how wide the date window is, whether the file's text takes part, how
   that text is normalised — need a real statement and a real export opened first, and stay open
   questions in §11 (Risks and technical debt), carried from PRD §8. ADR-0003 fixes *how* the match
   is executed and holds whatever the parameters turn out to be: one query before the first write,
   then matching against that snapshot only. The deciding argument there is correctness, not speed.
   `CONTEXT.md` says two identical spends are two real spends and both belong in the ledger, so the
   check must not see rows this same run just added — which a snapshot taken up front gives for
   free, and a per-row check interleaved with inserts has to be made careful about.

3. **A load is one unit: one database transaction to record it, and one record of everything it
   did** (ADR-0004). AC-22 requires a load that fails part-way to leave the ledger exactly as it
   was, which is quality goal 2 — so the whole run commits through a single `IUnitOfWork`, the port
   `core-ledger` already declares. This is affordable because ADR-0005 of `core-ledger` generates
   UUID v7 keys in the Application layer: every row and every link between rows exists in memory
   before anything is written. The cost is a ceiling rather than a defect — one transaction holding
   a whole file stops being the right shape somewhere above the row count this product will ever
   see. ADR-0004 names that number, and §7 (Deployment view) repeats it as a scaling threshold.

4. **Both jobs run through one pipeline with two entry points** (ADR-0005). PRD §1 says the monthly
   statement and the one-time move of the old history share the write path and differ in exactly
   three policies: whether missing accounts and categories are created, whether a rules-confirmation
   step exists, and what a move between the Owner's own accounts becomes — set aside under AC-06,
   recorded as a Transfer under AC-11. Those three live in one policy object; everything else —
   reading the file, matching, setting rows aside, recording the run, undoing it — exists once. Two
   use cases with their own endpoints keep the two jobs honestly different at the edge, so loading a
   monthly statement can never create an account because of one wrong field in a request.

Each tactical decision in a later section should trace to one of these. A tactical decision that
contradicts one is a red flag — it goes to §11, not into silence.

## 5. Building block view

<!-- Why: the static topology — who may talk to whom. §6 has no vocabulary without it. -->
<!-- Write: the style paragraph + the folder tree + the aggregates and ports + the C4Container block. -->
<!-- Watch: render the tree from ARCHITECTURE.md § Folder layout — substitute this feature's areas -->
<!--        and operations, drop the branches it does not touch, and mark as new any folder that -->
<!--        does not exist yet. No Application/Features/, no central Presentation/Contracts/. -->

**Clean Architecture, four rings, no new project.** The rings were fixed by
`docs/adr/0001-initial-setup.md` and this feature fills them in. It touches all four: two new
aggregates and their repository ports in Domain, a new business area and two new outside-world
ports in Application, four kinds of adapter in Infrastructure, and two new endpoint resources in
Presentation. The conventions quoted below — areas at the root of Application, contracts inside
each operation's own folder, one `IEntityTypeConfiguration<T>` per entity — come from `CLAUDE.md`
and `ARCHITECTURE.md`. They are inherited here, not decided.

**Two new aggregates, two new repository ports.** `CONTEXT.md` already separates them:

- **`ImportRun`** — one load of one file taken as a whole. It holds the name the file came with, the
  day it was handed over, which of the two jobs it was, the account it was loaded onto when the file
  names only one, and its status. Three kinds of child entity live inside it and have no life
  outside it: the **set-aside rows** with the reason each was not recorded; the **movement links**
  saying which Transaction or Transfer this run recorded, each carrying the content fingerprint of
  ADR-0008; and the **creation links** saying which Account or Category this run created, which is
  what AC-17 needs to undo them.
- **`MappingProfile`** — the rules remembered for one shape of file, keyed by the shape itself
  (ADR-0009). Each **rule** inside it says what one of the file's own categories means: one of the
  Owner's categories, a move between the Owner's own accounts, or recorded with no category. A
  profile outlives every run that uses it, and AC-04 is why it is a separate aggregate — changing a
  rule must leave recorded rows exactly as they were, which is only true if no recorded row points
  at a rule.

So the ports are `IImportRunRepository` and `IMappingProfileRepository`, both in
`Domain/Abstractions`. **None of the child entities gets a port of its own.** A port is added per
aggregate when a use case needs one, never because a table exists (`CLAUDE.md`) — `core-ledger`
made the same call for `CategoryGroup` and this follows it. Reading the set-aside rows of a run
(AC-13) and walking its movements to undo them (AC-16) are both reads of one `ImportRun`, served by
its own repository.

The four aggregates this feature writes into — Account, Category, Transaction, Transfer — belong to
`core-ledger` and are reached through the repository ports it already declares. **This feature adds
no column to any of them** (ADR-0007): the links above are rows in its own tables, pointing at
core-ledger's by id.

**The read path is separate, and this feature adds one query port of its own.** `core-ledger`
ADR-0004 settled that reads which aggregate go behind a port in `Application/Abstractions` rather
than through a repository. Two of this feature's reads qualify, and neither belongs to a single
aggregate:

- the candidate snapshot of ADR-0003 — every ledger movement that could match a row in the file,
  bounded by the accounts the file names and the date range it covers, fetched in one query before
  the run writes anything;
- the AC-17 check on undo — whether any movement from outside this run now names an Account or
  Category the run created.

Both go into a new `IImportQueries`, not into `core-ledger`'s `ILedgerQueries`. That record
anticipated the split ("may later split into one port per report"), and the alternative would make
the ledger's own reporting port know about imports. The remaining reads from PRD §9 need no new
port: the rules for a shape come from `IMappingProfileRepository`, and everything one load brought
comes from `IImportRunRepository`.

**One new outside-world port: `IStatementReader`** (ADR-0006). It turns a stream of bytes into a
stream of rows and knows nothing about the ledger, the rules or the matching. `ARCHITECTURE.md`
uses the name `IStatementImporter` as its illustration of an Application port; the real port is
narrower than that name suggests, because a port that *imports* would pull the mapping rules into
Infrastructure.

**Internal decomposition:**

Folders marked `NEW` do not exist today. Branches this feature does not touch are left out.

```
src/FinNavis.Domain/
├── Entities/              ImportRun (aggregate root) with its set-aside rows, movement links and
│                          creation links; MappingProfile (aggregate root) with its rules
├── Enums/                 ImportRunKind, ImportRunStatus, SetAsideReason, MappingRuleTarget
│                          — all stored as strings (CLAUDE.md)
└── Abstractions/          IImportRunRepository, IMappingProfileRepository

src/FinNavis.Application/
├── Import/           NEW  business area, at the project root — the shared pipeline and the policy
│                          object of ADR-0005, plus the use cases: InspectFile,
│                          ConfirmMappingProfile, LoadStatement, MigrateHistory, GetImportRun,
│                          UndoImportRun
└── Abstractions/          IStatementReader NEW, IImportQueries NEW. IUnitOfWork is core-ledger's
                           and is reused unchanged — ADR-0004 needs no new commit abstraction

src/FinNavis.Infrastructure/
├── Persistence/
│   ├── Configurations/    one IEntityTypeConfiguration<T> per new entity
│   └── Migrations/        one new migration. It adds tables and alters none, so `SPEC.md`'s
│                          safe-schema-change rule holds without a backfill (ADR-0007)
├── Repositories/          EfImportRunRepository, EfMappingProfileRepository
├── Queries/               EfImportQueries — the candidate snapshot and the AC-17 check
└── Import/           NEW  the CSV adapter behind IStatementReader

src/FinNavis.Presentation/
└── Endpoints/
    ├── ImportRuns/   NEW  LoadStatement/, MigrateHistory/, Get/, Undo/
    └── MappingProfiles/ NEW  Inspect/, Confirm/
```

Two names worth a line. The Application area is `Import/` — singular, because it is a business area
like `Reporting/`, not a resource. The Presentation resources are `ImportRuns/` and
`MappingProfiles/` — plural, and named from `CONTEXT.md`, because they are what the API exposes. The
two confirm-step operations sit under `MappingProfiles/` rather than under `ImportRuns/`: `Inspect/`
reads a file and reports the categories it carries without recording anything, and it exists only to
feed `Confirm/`.

Each leaf folder under `Endpoints/` holds one operation: the endpoint plus the request and response
records only that operation uses. There is no central `Contracts/` folder.

**C4 Container (L2):**

> The four boxes inside the boundary are the four rings. They compile into **one** ASP.NET Core
> process and are not separately deployable — they are drawn as containers because the boundary
> between them is the architecture here, and `tests/FinNavis.ArchitectureTests` enforces it.
> `target_surfaces` declares one surface, so there is one application boundary and no client box.

```mermaid
C4Container
    title csv-import — Containers

    Person(owner, "Owner", "Hands over a file, confirms the rules for a shape once, reads back and undoes a load")

    Container_Boundary(api, "FinNavis API — one ASP.NET Core process") {
        Container(pres, "Presentation", "ASP.NET Core Minimal APIs", "Maps HTTP to use cases, owns request and response contracts, is the composition root")
        Container(app, "Application", "C# use cases", "The import pipeline and its policy object; declares IStatementReader and IImportQueries; uses core-ledger's IUnitOfWork")
        Container(dom, "Domain", "C# entities and rules", "ImportRun and MappingProfile; declares IImportRunRepository and IMappingProfileRepository")
        Container(inf, "Infrastructure", "EF Core + Npgsql, CSV reader", "Adapters for every port, DbContext, entity configurations, migrations")
    }

    ContainerDb(pg, "PostgreSQL 18", "PostgreSQL", "core-ledger's accounts, categories, transactions and transfers, plus this feature's import runs, movement links, creation links, set-aside rows and mapping profiles")

    Rel(owner, pres, "Hands over a file; confirms the rules; reads back and undoes a load", "HTTPS / JSON")
    Rel(pres, app, "Calls use-case classes directly — there is no driving port")
    Rel(app, dom, "Builds ImportRun and MappingProfile; calls the repository ports")
    Rel(inf, dom, "Implements IImportRunRepository and IMappingProfileRepository")
    Rel(inf, app, "Implements IStatementReader and IImportQueries, and core-ledger's IUnitOfWork")
    Rel(pres, inf, "Registers every adapter in DI at startup — composition root only")
    Rel(inf, pg, "Reads and writes", "EF Core + Npgsql")
```

## 6. Runtime view

<!-- Why: boxes without flows are not architecture. This is where the refusals become visible. -->
<!-- Write: one sequenceDiagram per critical flow, participants named from §5, messages semantic. -->
<!-- Watch: §6 is CLOSED here — no later stage adds flows. Cite the AC ids in each heading. -->

Participants are the §5 rings. Messages are semantic — request methods, paths and status codes
belong to the API-contract stage, not here.

Six flows cover all 22 acceptance criteria. **A statement load takes the account from the Owner, not
from the file** — a Statement covers one account by definition (`CONTEXT.md`), so the Owner names it
when handing the file over and no column in the file is read for it. PRD AC-03 and AC-09 are written
against the other model, where a statement row names its own account; §11 (Risks and technical debt)
carries that as a wording gap in the PRD rather than hiding it here. A history migration does read
accounts per row, which is what AC-10 asks for.

**Critical flow 1: confirm the rules for a statement shape never seen before** (US-01 — AC-01,
AC-02, AC-03, AC-04)

```mermaid
sequenceDiagram
    actor Owner
    participant Pres as Presentation
    participant App as Application
    participant Dom as Domain
    participant Inf as Infrastructure
    participant PG as PostgreSQL

    Owner->>Pres: hand over a file to see what is in it
    Pres->>App: InspectFile use case
    App->>Inf: read the file through IStatementReader
    Inf-->>App: the header row, then the rows one at a time
    alt the file names no date or no amount
        App-->>Pres: refuse and say which of them is missing
        Pres-->>Owner: nothing is remembered and nothing is recorded
    else the file can be read
        App->>Inf: look for a profile whose shape key matches this header
        Inf->>PG: read one mapping profile
        PG-->>Inf: the profile, or nothing
        Inf-->>App: the profile, or nothing
        App-->>Pres: the categories this file carries, and whether rules already exist for its shape
        Pres-->>Owner: the list of the file's own categories
    end
    Owner->>Pres: say what each of those categories means
    Pres->>App: ConfirmMappingProfile use case
    App->>Inf: load the categories the rules point at
    Inf->>PG: read categories
    PG-->>Inf: rows
    Inf-->>App: the categories that exist
    alt a rule points at a category the ledger does not have
        App-->>Pres: refuse and name the unknown category
        Pres-->>Owner: no rule is remembered and no category is created
    else every rule points at something real
        App->>Dom: build the MappingProfile and its rules
        Dom-->>App: a valid profile
        App->>Inf: add the profile and commit the unit of work
        Inf->>PG: insert the profile and its rules
        PG-->>Inf: committed
        Inf-->>App: done
        App-->>Pres: the profile
        Pres-->>Owner: the rules are remembered for this shape
    end
    Note over Owner,PG: no recorded movement points at a rule, so changing a rule later cannot change anything already recorded (AC-04)
```

The file is handed over twice here, and that is ADR-0001: nothing is stored between the two steps.
The shape key comes from the header row (ADR-0009), so the next file of this shape skips both steps.

**Critical flow 2: load a monthly statement** (US-02 — AC-05, AC-06, AC-07, AC-08, AC-22)

```mermaid
sequenceDiagram
    actor Owner
    participant Pres as Presentation
    participant App as Application
    participant Dom as Domain
    participant Inf as Infrastructure
    participant PG as PostgreSQL

    Owner->>Pres: hand over the month's file and name the account it covers
    Pres->>App: LoadStatement use case, carrying the statement policy
    App->>Inf: load the named account and the profile for this file's shape
    Inf->>PG: read the account and the mapping profile
    PG-->>Inf: rows
    Inf-->>App: the account and the profile, or nothing
    alt the named account is not in the ledger, or no rules exist for this shape
        App-->>Pres: refuse and say which one is missing
        Pres-->>Owner: nothing is recorded and no account is created
    else both exist
        App->>Inf: ask IImportQueries for the candidate snapshot
        Inf->>PG: one query over that account across the dates the file covers
        PG-->>Inf: every movement that could match
        Inf-->>App: the snapshot
        Note over App,Inf: taken before the first write, so it can never hold a row this run adds
        loop every row the reader yields
            App->>Dom: classify the row against the profile and the snapshot
            alt its category is a move between the Owner's own accounts
                Dom-->>App: set aside — a statement names only one side of it
            else it is in a currency the ledger does not keep
                Dom-->>App: set aside — another currency
            else no rule covers its category
                Dom-->>App: set aside — no rule covers it
            else the snapshot already holds this movement
                Dom-->>App: already there, not recorded again
            else it is covered and new
                Dom-->>App: a Transaction on the named account, with the category the rule gives
            end
        end
        App->>Dom: build the ImportRun with its movement links, creation links and set-aside rows
        Dom-->>App: a valid run
        App->>Inf: add everything and commit the unit of work
        Inf->>PG: one transaction — the movements, the run, its links and its set-aside rows
        alt the commit fails
            PG-->>Inf: rolled back
            Inf-->>App: failure
            App-->>Pres: report that the load did not happen
            Pres-->>Owner: nothing is recorded and every balance is unchanged
        else the commit succeeds
            PG-->>Inf: committed
            Inf-->>App: done
            App-->>Pres: the run and its counts
            Pres-->>Owner: how many recorded, how many already there, and every set-aside row with its reason
        end
    end
    Note over Owner,PG: a set-aside row changes no balance, so the month's spending is never raised by money that only moved or by a row nobody filed
```

AC-06 and AC-07 are the first two set-aside branches; AC-08 is the third, and it names the uncovered
category back so the Owner can add a rule and hand the same file over again — which is flow 3. AC-22
is the failing-commit branch, and it costs nothing to guarantee because ADR-0004 commits the whole
run at once.

**Critical flow 3: hand over a file that is already in the ledger, in whole or in part** (US-05,
US-07 — AC-15, AC-20, AC-21)

```mermaid
sequenceDiagram
    actor Owner
    participant Pres as Presentation
    participant App as Application
    participant Inf as Infrastructure
    participant PG as PostgreSQL

    Owner->>Pres: hand over a file that was already loaded, or loaded only in part
    Pres->>App: LoadStatement use case
    App->>Inf: ask IImportQueries for the candidate snapshot
    Inf->>PG: one query over the named account across the dates the file covers
    PG-->>Inf: every movement that could match, whoever recorded it
    Inf-->>App: the snapshot
    Note over App,PG: the snapshot holds movements the Owner typed by hand as well as ones an earlier run recorded, and treats neither differently
    loop every row the reader yields
        alt the file gives this row an identifier and a movement already carries it as its external_ref
            App->>App: already there — an exact match, held by the unique index rather than by a judgement
        else a movement in the snapshot is on the same account, inside the date window, for the same amount
            App->>App: already there — a proximity match
        else nothing in the snapshot matches
            App->>App: record it
        end
    end
    App-->>Pres: the counts, including the rows an added rule has now let through
    Pres-->>Owner: how many were recorded and how many were already there
    Note over Owner,PG: two identical rows inside one file are both recorded — the snapshot was taken before this run wrote anything, so it can never contain the run's own work
```

The two tiers are ADR-0002 and the snapshot is ADR-0003. AC-15 is this flow run a second time after
a rule was added: the rows that were set aside now find a rule, and the rows the earlier load
recorded are found in the snapshot and counted as already there. AC-21 is the case where the
matching movement was typed by hand — it has no `external_ref`, so only the proximity tier can find
it, which is why ADR-0002 keeps that tier.

**Critical flow 4: move the old history across** (US-03 — AC-03, AC-10, AC-11, AC-12)

```mermaid
sequenceDiagram
    actor Owner
    participant Pres as Presentation
    participant App as Application
    participant Dom as Domain
    participant Inf as Infrastructure
    participant PG as PostgreSQL

    Owner->>Pres: hand over the old tracker's export
    Pres->>App: MigrateHistory use case, carrying the migration policy
    Note over Pres,App: no profile and no confirmation step — the export already names accounts and categories in the Owner's own words
    App->>Inf: read the file through IStatementReader
    Inf-->>App: the header row
    alt the export does not name a date, an amount, or the account a row belongs to
        App-->>Pres: refuse and say which of the three is missing
        Pres-->>Owner: nothing is recorded
    else the export can be read
        App->>Inf: ask IImportQueries for the candidate snapshot
        Inf->>PG: one query over every account the file names, across the whole range it covers
        PG-->>Inf: every movement that could match
        Inf-->>App: the snapshot
        loop every row the reader yields
            alt the snapshot already holds this movement
                App->>App: already there, not recorded again
            else the row names two of the Owner's own accounts
                App->>Dom: build a Transfer, creating either account the ledger lacks
                Dom-->>App: a Transfer — no category, and the Owner's total unchanged
            else the row names one account and one category
                App->>Dom: build a Transaction keeping the file's own date, creating the account or category the ledger lacks
                Dom-->>App: a Transaction
            end
        end
        App->>Inf: add everything and commit the unit of work
        Inf->>PG: one transaction — created accounts and categories, movements, the run and all its links
        PG-->>Inf: committed
        Inf-->>App: done
        App-->>Pres: the run and its counts
        Pres-->>Owner: what was recorded, what was already there, and what was set aside
    end
    Note over Owner,PG: every write is an insert, so a month the Owner already filled in by hand gains the loaded rows and loses nothing (AC-12)
```

This is flow 2 with a different policy object, which is ADR-0005. The three differences are all
visible: no profile and no confirmation step, accounts and categories created rather than refused,
and a move between the Owner's own accounts recorded as a Transfer instead of set aside. The
creation links the run keeps are what lets AC-17 undo those creations later.

**Critical flow 5: read what a load did** (US-04 — AC-13, AC-14)

```mermaid
sequenceDiagram
    actor Owner
    participant Pres as Presentation
    participant App as Application
    participant Inf as Infrastructure
    participant PG as PostgreSQL

    Owner->>Pres: ask what a load did
    Pres->>App: GetImportRun use case
    App->>Inf: load the run through IImportRunRepository
    Inf->>PG: read the run with its movement links, creation links and set-aside rows
    PG-->>Inf: rows
    Inf-->>App: the ImportRun
    Note over App,Inf: no query port is used here — this is one aggregate read through its own repository, not an aggregation across the ledger
    App-->>Pres: how many were recorded, how many were already there, every set-aside row with its reason, and whether the run has been undone
    Pres-->>Owner: the run report
    Note over Owner,PG: a set-aside row is kept with its reason and is never recorded on its own — only a later load can record it (AC-14)
```

**Critical flow 6: undo a load** (US-06 — AC-16, AC-17, AC-18, AC-19)

```mermaid
sequenceDiagram
    actor Owner
    participant Pres as Presentation
    participant App as Application
    participant Dom as Domain
    participant Inf as Infrastructure
    participant PG as PostgreSQL

    Owner->>Pres: undo a load
    Pres->>App: UndoImportRun use case
    App->>Inf: load the run with its links, and the movements those links point at
    Inf->>PG: read the run, then its transactions and transfers
    PG-->>Inf: rows
    Inf-->>App: the run and its movements
    alt the run was already undone
        App-->>Pres: refuse and say it was undone already
        Pres-->>Owner: nothing changes
    else a movement no longer matches the fingerprint stored when the load wrote it
        App-->>Pres: refuse and name the rows that were changed by hand
        Pres-->>Owner: the load cannot be undone as a whole
    else every movement still present is as the load wrote it
        App->>Inf: ask IImportQueries which of the run's created accounts and categories are now named from outside it
        Inf->>PG: one query over movements that belong to no link of this run
        PG-->>Inf: the ones still in use
        Inf-->>App: the list
        App->>Dom: mark the run Undone
        Dom-->>App: a valid run
        App->>Inf: delete the run's movements and the created accounts and categories nobody else names, then commit
        Inf->>PG: one transaction — the deletes, then the run's new status
        PG-->>Inf: committed
        Inf-->>App: done
        App-->>Pres: the run, now Undone
        Pres-->>Owner: every row the load recorded is gone and every balance is back to what it would have been
    end
    Note over Owner,PG: the run record stays with status Undone but its movements do not, so handing the same file over again records everything afresh (AC-19)
```

The fingerprint check is ADR-0008 and it is what makes AC-18 independent of `core-ledger`, which
keeps no audit trail (its ADR-0003). A movement the Owner **deleted** by hand is simply absent and
blocks nothing — there is nothing left to remove. The deletes are real deletes rather than reversing
entries, inherited from the same `core-ledger` record and not decided here.

## 7. Deployment view

<!-- Why: the topology a future reader needs without opening compose files. -->
<!-- Write: topology + what this feature creates + backups + monitoring + scaling thresholds. -->
<!-- Watch: "no metrics" is a decision with a reason, not a blank row. Numbers, not "later we'll see". -->

**The topology is `core-ledger`'s, unchanged.** One Docker Compose stack on the Owner's own machine
or home server: one API container, one PostgreSQL 18 container, one named volume for the database,
one replica of each. The two containers share a private Docker network and only the API port is
published, and only on the local network — with no authentication, that network boundary is the
only access control there is (`core-ledger/sad.md` §7, PRD §6.1).

**This feature adds nothing to the deployment unit, and that is a result rather than a coincidence.**
No second container, no background worker, no queue, no scheduled job. In particular **no volume for
uploaded files**: ADR-0001 decided the file is never stored, so there is nothing to mount, nothing
to clean up and no bank statement on disk. A load is one synchronous request that either commits or
does not (ADR-0004), so there is also no job state to keep anywhere.

**What it does depend on, and what does not exist yet.** `docker-compose.yml`, the Testcontainers
integration harness and a CI job are all still unbuilt (`SPEC.md`, "Planned, not built yet").
`core-ledger/sad.md` §7 schedules all three as its own first tasks. For this feature they are
prerequisites, not deliverables — and so is `core-ledger` itself, which today is a PRD and a SAD
with no code. Both PRD §6 numbers are unmeasurable until they exist. Carried into §11.

**Test fixtures, and the file that must never be committed.** PRD §9 asks for a real statement and a
real export to be kept as sample files. A real statement is one person's complete month of spending
with the shop names attached — §2 classifies it confidential, and the repository has no rule today
that would stop it being committed. So:

- **Committed:** synthetic sample files under the test projects. They copy the *shape* — the header
  row, the column order, the date and amount formats — and carry invented merchants and amounts.
  The whole suite runs off them, including both PRD §6 measurements, so CI needs nothing private.
- **Not committed, ever:** the real statement and the real export. They stay on the Owner's own
  machine. They are needed once, to answer the three PRD §8 questions that can only be answered by
  opening a real file — does it carry a stable operation number, does it name a currency, does the
  export name both sides of a move between accounts — and to build the synthetic files from.
- `CLAUDE.md` § Safety gains a line saying so, because a commit of a real statement cannot be taken
  back without rewriting history.

The residual risk is that a synthetic file is wrong about the real shape, so the suite passes
against a format no bank produces. §11 carries it.

**Backups.** The nightly `pg_dump` into a host folder that the Owner's own machine backup already
covers, inherited from `core-ledger/sad.md` §7. This feature changes what it is *for*. Undo (US-06)
is the in-product way back from a load that went wrong, so the dump is the backstop for the two
cases undo cannot cover: a load whose rows the Owner has since corrected by hand, where AC-18 blocks
the undo on purpose; and a load whose rules were wrong but plausible, where the damage is noticed
only after more work has been built on top of it. The same residual gap applies as before — anything
recorded after the last nightly dump is lost on a restore.

**Monitoring.** Deliberately none, and for one reason that is specific to this feature: there is
nothing here that can fail while nobody is looking. A load is a request the Owner made and is
waiting on; if it fails, it fails in front of them with nothing recorded (AC-22).

- **Logs** — structured logs to stdout through `Microsoft.Extensions.Logging`, read with
  `docker compose logs`. One line per run: which of the two jobs, the name the file came with, and
  the counts. **Not the rows.** A statement's rows are the confidential part (§2), and stdout with
  no retention policy is the wrong place for them — the set-aside rows the Owner needs to act on are
  in the run report (AC-13), which is behind the API.
- **Metrics** — none. Both PRD §6 numbers are measured in the integration suite against seeded data,
  not in production, and there is no concurrent load to observe.
- **Alerts** — none. There is nobody to page, and no background job whose silent failure an alert
  would catch.
- **Tracing** — none. One process, four rings, no network hop to correlate.

**Scaling thresholds.**

- **~1500 rows in one file** — the working size, from PRD §6, for the one-time move of the old
  history. A monthly statement is far smaller. The design has a wide margin here: ADR-0003 replaced
  about 1500 lookups with one query, and ADR-0004's single commit is routine at this size.
- **~50 000 rows in one file** — the ceiling on ADR-0004. Above it, one transaction holding the
  whole run and the change tracker holding every entity both stop being comfortable, and the
  revisit is the resumable design that record rejected. For scale: `core-ledger` sizes the entire
  ledger at about 20 000 movements over three years, so a file past this ceiling would hold more
  than all the history this product is designed for.
- **The candidate snapshot grows with the date range, not with the file.** This is the easy one to
  miss, because it is invisible if you only watch file sizes. ADR-0003 loads every ledger movement
  that could match, bounded by the accounts named and the dates covered — so a three-year history
  migration pulls close to the whole ledger into memory at once, about 20 000 movements by
  `core-ledger`'s own sizing. That is this feature's peak memory, and it is reached by the
  **first** load rather than by growth. Look again if the ledger passes roughly 200 000 movements,
  or if a single file is ever allowed to cover more than a few years.

## 8. Crosscutting concepts

<!-- Why: patterns that cross rings — logging, errors, ids, time, validation, indexes. -->
<!-- Write: one row per concept: concept / convention / where it is defined. -->
<!-- Watch: a repo-wide convention belongs in CLAUDE.md. N/A still says why. -->

| Concept | Convention | Where defined |
|---|---|---|
| Logging | Structured logs to stdout. One line per run — which job, the file's name, the counts. **Never the rows**: a statement's lines are the confidential part and stdout has no retention policy | §7 |
| Authentication | None. One owner, no sign-in. Network isolation is the only control, and this feature makes the gap wider — anyone who reaches the installation can now load a file, not only read | PRD §6.1 → §11 |
| Authorization | None. There is no second actor to deny (PRD §5 N/A note) | PRD §5 → §11 |
| Error handling — refusals | A refusal that stops the whole load is a domain error, mapped by Presentation to an RFC 9457 Problem Details response, with the wording taken from PRD §10 and not invented. Covers AC-02, AC-03, AC-18 and the AC-22 rollback | `core-ledger/sad.md` §8, PRD §10 |
| Error handling — partial success | **A load that finished is a success, even when some rows were set aside.** The response body carries the counts and every set-aside row with its reason (AC-08, AC-13); a set-aside row is a normal outcome of a working load, not a fault. This is a repo-wide convention for any batch operation, recorded in `CLAUDE.md` § API | `CLAUDE.md`, here |
| ID strategy | UUID v7 generated in the Application layer with `Guid.CreateVersion7()`. This feature is the caller that record was written for: a whole run and the links between its rows exist in memory before the single commit | `core-ledger` ADR-0005, ADR-0004 here |
| External reference | `transactions.external_ref`, nullable with a unique index, stops being unused. It carries the file's own identifier for a row when the file supplies one, and it is the exact tier of the duplicate match | `core-ledger` ADR-0006, ADR-0002 here |
| Money | `decimal`, single currency, no currency code stored. A row the file names in another currency is set aside rather than converted (AC-07) | `CLAUDE.md`, PRD §3 |
| Time | `DateTimeOffset` stored UTC. **The file gives a calendar date with no time**, and how that becomes a stored instant is `core-ledger`'s open question, inherited unresolved. It is a blocker for US-03 and not for US-02 — see §11 | `CLAUDE.md`, `core-ledger/sad.md` §11 → §11 |
| Validation | Shape in Presentation, use-case checks in Application, invariants inside Domain entities. Plus one check that belongs to neither: the row ceiling, enforced while the file streams, so a file that is not a statement is refused before anything is recorded (ADR-0006, PRD §6.1) | `core-ledger/sad.md` §8, ADR-0006 |
| Concurrency | None. One owner, one client, no optimistic concurrency token. ADR-0003 names the assumption it rests on: the candidate snapshot cannot see a movement written while the run is in flight | → §11 |
| Indexes | Five, all from PRD §9's new read paths: mapping profiles by shape key, **unique** (ADR-0009, one lookup per load); `transactions` and `transfers` by account and date (ADR-0003, the candidate snapshot — these two belong to `core-ledger` but become load-bearing for PRD §6's ≤ 60 s here); the movement and creation links by run id (reading a run and undoing it); set-aside rows by run id. `transactions.external_ref` is already unique and needs nothing added | §10, §11 |
| Set-aside reasons | A domain enum stored as a string (`CLAUDE.md`): a move between the Owner's own accounts, another currency, no rule covers it, the named account is unknown. The Owner-facing wording for each comes from PRD §10 | PRD §10, §6 |
| Internationalisation | N/A — English only. Separately: the file's own text is stored and shown back as plain text and **never interpreted** (PRD §6.1). The date and number formats *inside* a file are a parsing concern of the adapter, not a product language setting | ADR-0006 |
| Observability | N/A — no metrics, alerts or tracing. A load is a synchronous request the Owner is waiting on, so nothing here can fail unobserved. See §7 | §7 |
| Outbox / events | N/A — one process, one transaction per run, no module-to-module messaging | ADR-0004 |

## 9. Architecture decisions

<!-- Why: the reverse index into adr/. `ls adr/` gives files; §9 gives the reason they exist. -->
<!-- Write: one row per ADR, then how they relate, then what did NOT become an ADR and why. -->
<!-- Watch: closed in both directions — a row for every file, a file for every row. -->

| # | Title | Status | Section |
|---|---|---|---|
| 0001 | Never store the uploaded file; have it handed over again for the load | Accepted | §4 |
| 0002 | Match an imported row on an exact external reference first, then on a date-and-amount window | Accepted | §4 |
| 0003 | Take one snapshot of candidate movements before an import run writes anything | Accepted | §4 |
| 0004 | Commit an entire import run in one database transaction | Accepted | §4 |
| 0005 | Run both import jobs through one pipeline with two entry points and a policy object | Accepted | §4 |
| 0006 | Make the file reader a thin port that streams rows and knows nothing about the ledger | Accepted | §5 |
| 0007 | Keep import bookkeeping in this feature's own tables instead of adding columns to the ledger | Accepted | §5 |
| 0008 | Detect post-import hand edits with a content fingerprint stored at import time | Accepted | §5 |
| 0009 | Recognise a statement shape by its header row, with no name the Owner has to pick | Accepted | §5 |

ADR files live under `docs/features/csv-import/adr/NNNN-<title>.md`. Numbering restarts at 0001
inside this feature's folder; `docs/adr/0001-initial-setup.md` is project-wide and is not renumbered
from here.

**Nine records, where the size class L expects 10 to 15.** The gap has a reason rather than a
missing record: five decisions an import feature would normally have to take were already taken by
`core-ledger` and are inherited unchanged (§2) — the identity strategy, the split between the read
and write paths, whether the ledger is mutable or append-only, the two-table shape of movements, and
the `external_ref` column that was added specifically so this feature would need no backfill. That
last one is the clearest case: ADR-0006 of `core-ledger` exists *because of* csv-import, so the
decision is on record already and repeating it here would only dilute the folder.

**How they relate.** They are not nine independent choices.

- **0002 and 0003 are one contract, split on purpose.** 0002 fixes the *shape* of the duplicate
  rule and leaves its parameters open; 0003 fixes *how* the rule is executed and holds whatever
  those parameters turn out to be. Splitting them is what lets the rest of the design be finished
  while PRD §8's questions are still open.
- **0003 depends on 0004.** Its central argument — that a per-row check would see the run's own
  inserts — is only true because the whole run commits at once. If 0004 were ever reversed, 0003's
  reasoning would need rereading rather than merely its conclusion.
- **0004 is what makes 0007 trustworthy.** The links live in separate tables from the movements they
  describe, so the only thing stopping them disagreeing is that both land in the same commit.
- **0007 gives 0008 a home.** The fingerprint is import bookkeeping, not ledger data, so it belongs
  on the movement link rather than on the transaction.
- **0001 and 0009 together shape the confirm step.** Nothing is kept between the two calls, and the
  identity of a shape is derived from the file itself — which is the only way the first can work.
- **0005 is the umbrella over all of them.** One pipeline is why 0002, 0003, 0004, 0007 and 0008
  exist once rather than twice.
- **0006 is the edge**, and the only one of the nine that would survive unchanged if the feature
  were redesigned around a different file format.

**What did not become an ADR, and why.** This paragraph exists so the next reader does not
re-litigate something already settled.

- **The target surface.** PRD §3 excludes the alternative outright, so there was never a second
  option to weigh.
- **Which aggregates get a repository port.** `CLAUDE.md` already says one port per aggregate, added
  when a use case needs it. `core-ledger` applied the same rule to `CategoryGroup`.
- **A separate `IImportQueries` rather than extending `ILedgerQueries`.** `core-ledger` ADR-0004
  already decided that aggregating reads sit behind a query port, and anticipated the split. Which
  port holds which read is cohesion, and moving a method between two interfaces is an afternoon.
- **Keeping the run record after an undo, with status `Undone`.** One column and one enum value;
  reversible, and nothing downstream depends on which way it went.
- **How a partially-successful load reports its set-aside rows.** This outgrew the feature during
  the pass and went into `CLAUDE.md` § API, which is where a repo-wide rule belongs. An ADR would
  have hidden it from every feature that is not this one.
- **That undo deletes rather than posts reversing entries.** Decided by `core-ledger` ADR-0003 for
  the whole ledger. Deciding it again here would be a second record saying the same thing.
- **The CSV library, and whether rows are inserted with `AddRange` or a bulk path.** Package and
  adapter details, invisible outside Infrastructure, replaceable in an afternoon against the PRD §6
  numbers.
- **Everything in §2 Constraints.** Those are inputs. An ADR for one of them would fail the gate by
  construction, because the alternative is already excluded by a constraint.

## 10. Quality requirements

<!-- Why: without §10, §1 is a manifesto. Each goal has to map to something provable. -->
<!-- Write: per §1 goal — When / Then / How verify. Test names in Method_Scenario_ExpectedResult form. -->
<!-- Watch: every number comes from PRD §6 verbatim. A goal with no number says so honestly. -->

Each goal from §1 expanded into a full scenario. Every number is copied from PRD §6 verbatim.

**QG-1. <quality goal>**

- **When:** <the trigger condition>
- **Then:** <the expected result, with the PRD §6 number>
- **How verify:** <test name, where it runs, and what makes it fail>

**QG-2. <quality goal>**

- **When:** <…>
- **Then:** <…>
- **How verify:** <…>

**QG-3. <quality goal>**

- **When:** <…>
- **Then:** <…>
- **How verify:** <…>

<Any quality the PRD deliberately sets no target for, and the reason — so §7 reads as a consequence
rather than as neglect.>

## 11. Risks and technical debt

<!-- Why: collects everything that can break, plus the debt taken on purpose. -->
<!-- Write: the table, then accepted debt, then the open questions this pass closed. -->
<!-- Watch: severity is Low / Medium / High, or the literal "Open question" with owner AND due. -->

| Risk / debt | Severity | Mitigation | Owner |
|---|---|---|---|
| <risk> | <Low\|Medium\|High> | <mitigation, pointing at the section or test that catches it> | <name> |
| Open question: <the undecided thing. Default now: <…>> | Open question | Resolve before <stage or date>; <where it came from> | <name> |

**Accepted debt (acceptable now, plan to fix later):**

- <what is wrong on purpose, and why that is the right call today>

**Open questions closed during this pass** (they were due at this stage, so they are no longer open):

- <the question> → <the answer>. See <ADR or section>.

## 12. Glossary

<!-- Why: stops the argument a year from now about what a word meant. -->
<!-- Write: the CONTEXT.md terms this document uses, then the technical terms it introduces. -->
<!-- Watch: CONTEXT.md is canonical. A new *domain* word belongs there, not here. -->

Domain terms come from `CONTEXT.md`, which is canonical — if anything in this document contradicts
it, `CONTEXT.md` wins. Only the terms this SAD actually uses are listed.

| Term | Meaning |
|---|---|
| <term> | <definition with its boundary — what it is NOT> |

**Terms this document introduces that are not in `CONTEXT.md`.** They are technical rather than
domain words, so they do not need an entry there — recorded here for the reader:

| Term | Meaning |
|---|---|
| <term> | <definition> |
