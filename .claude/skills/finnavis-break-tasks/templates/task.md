---
id: T01
title: "<imperative, specific — names the thing, not the verb alone>"
layer: "domain|app|infra|migration|ports|wiring|tests|docs"
deps: []                       # task ids that must finish first — derived from the rings
acs: ["AC-01"]                 # PRD §5 ids this task satisfies; empty is allowed with a reason
files_hint: ["src/...", "tests/..."]
owner: "Andrey Simonenko"
estimate: "S"                  # S = 2h · M = half a day · L = a day · more splits
status: "todo"
---

# T01 — <title>

## Why

<!-- instruction: 1–2 sentences. LINK the upstream source, never paste it:
derives from [PRD §AC-01](../PRD.md), [sad.md §5](../sad.md), [ADR-0005](../adr/0005-....md),
[openapi.yaml](../contracts/openapi.yaml), [data-model.md](../data-model.md).
If `acs` is empty, say here why this task satisfies no acceptance criterion on its own. -->

## What

<!-- instruction: the concrete change, one ring, scoped to ≤1 working day. Name the files and
directories — the same set as files_hint. Per layer:

- domain    — the entity or value object, its invariants, and its repository port in
              Domain/Abstractions. One aggregate.
- app       — the use cases of one business area at the Application root. No EF Core.
- infra     — the adapters: repositories + IUnitOfWork together, or one read query. Name the
              index the query needs.
- migration — the IEntityTypeConfiguration<T> files, then `./build.sh migrate <Name>`, then the
              diff against the STAGED pair under ../migrations/. The staged SQL is a spec that is
              never applied.
- ports     — one resource: its operations, each in its own
              Presentation/Endpoints/<Resource>/<Operation>/ folder with its own records.
- wiring    — the layer's DependencyInjection entry point and the composition root.
- tests     — one SAD §10 quality gate, with its figure copied verbatim.
- docs      — an ADR, or the /openapi/v1.json diff against the contract.
-->

## Definition of Done

<!-- instruction: testable bullets. Each one names a command, a test, a PRD §5 outcome or a SAD §10
figure. Never a lint or vet command — this repo has none; TreatWarningsAsErrors makes
`./build.sh build` the lint gate. Patterns per layer live in
.claude/skills/finnavis-break-tasks/references/dod-catalog.md -->

- [ ] <the behaviour, with its AC id>
- [ ] <the test that proves it, in the test project this layer maps to>
- [ ] `./build.sh test` is green

## Notes

<!-- instruction: gotchas, the lane this shares with another task and on which file, and any Hard
rule from PRD §6 NFR / sad.md §11 / an ADR that this task must respect. Quote the rule, cite the
source. -->
