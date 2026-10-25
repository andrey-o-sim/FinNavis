---
id: T27
title: "Diff the served document against the contract and close the deferrals"
layer: "docs"
deps: ["T21", "T22", "T23"]
acs: []
files_hint: ["docs/features/csv-import/contracts/openapi.yaml", "docs/features/csv-import/contracts/api-sync-report.md"]
owner: "Andrey Simonenko"
estimate: "M"
status: "todo"
---

# T27 — Diff the served document against the contract and close the deferrals

## Why

Where [`.claude/rules/openapi.md`](../../../../.claude/rules/openapi.md) closes the loop:
*"The contract is not the served document."* ASP.NET Core generates its own OpenAPI document at
`/openapi/v1.json` from the code; [openapi.yaml](../contracts/openapi.yaml) states what the code
**must** produce. Drift point 6 in [api-sync-report.md](../contracts/api-sync-report.md) is marked
N/A today only because `Endpoints/` held a `.gitkeep`.

`acs` is empty: a diff satisfies no acceptance criterion. It is what stops the contract and the code
disagreeing silently.

## What

Start the app in Development, fetch `/openapi/v1.json`, and diff it against
[openapi.yaml](../contracts/openapi.yaml) — all six operations, every request and response field,
every status code, every error code. Resolve each difference in the **handler** or in the contract,
never by editing the generated document.

Then close the three deferrals this epic actually closed, in
[api-sync-report.md](../contracts/api-sync-report.md) Section D:

- the twelve error codes have no definition in `src/` — closed by T20;
- `application/problem+json` is still on — asserted off by T20;
- drift point 6 unchecked — closed by this task.

Finally re-run the report with `--reconcile` so its Verdict reflects the code.

## Definition of Done

- [ ] `/openapi/v1.json` and `contracts/openapi.yaml` agree on all **six** operations and all
      **twelve** error codes
- [ ] Every difference found is resolved in the handler or in the contract, and the resolution is
      recorded in the report
- [ ] The three deferrals above are struck from `api-sync-report.md` Section D
- [ ] Drift point 6 moves from N/A to ✓
- [ ] The report's `**Verdict:**` is `PASS` or `PASS (with deferrals)`
- [ ] `info.version` is bumped only if a CHANGELOG line says so — never silently
- [ ] `spectral lint docs/features/csv-import/contracts/openapi.yaml` passes, if the tool is
      installed; this repo has no `check` target to put it in

## Notes

- **Four deferrals stay open on purpose**, and the report must still say so: the file-size ceiling
  (PRD §8), the five responses with no §6 `alt`-branch, the three drafted refusal messages if T02
  reworded rather than confirmed them, and the `finnavis-api-forge` `SKILL.md` spelling fix. Only
  strike what this epic closed.
- **The `SKILL.md` deferral is worth doing even though it is not feature work.**
  [api-sync-report.md](../contracts/api-sync-report.md): until it is fixed, a **first run** of
  `/finnavis-api-forge` on a new feature derives snake_case codes and contradicts `CLAUDE.md` §API
  and `.claude/rules/openapi.md` §7. It is parked in [`_epic.md`](./_epic.md), not here.
- **The generated document is produced from the code and is diffed against the contract, never the
  other way round.** If the generated document is nicer, change the contract deliberately and say
  why — do not let the code define the surface by accident.
- Depends on all three `ports` tasks, so it is the last task in the epic.
