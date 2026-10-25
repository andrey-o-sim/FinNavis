---
id: T34
title: "Diff the served OpenAPI document against the contract and close the deferrals"
layer: "docs"
deps: ["T25", "T26", "T27", "T28", "T29", "T30"]
acs: []
files_hint: ["docs/features/core-ledger/contracts/api-sync-report.md", "CLAUDE.md", "docs/features/core-ledger/tasks/tracker.md"]
owner: "Andrey Simonenko"
estimate: "S"
status: "todo"
---

# T34 — Diff the served OpenAPI document against the contract and close the deferrals

## Why

[`.claude/rules/openapi.md`](../../../../.claude/rules/openapi.md) and
[`CLAUDE.md`](../../../../CLAUDE.md) §API: the generated document at `/openapi/v1.json` is **not**
the contract — it is produced from the code, and it is **diffed against** the contract.

`api-sync-report.md` Section D parks two drift points on "no endpoint exists yet". After the six
endpoint tasks they can be checked for the first time.

## What

- Start the API in Development, fetch `/openapi/v1.json`, and diff it against
  [`contracts/openapi.yaml`](../contracts/openapi.yaml): 21 operations, 33 schemas, 27 error codes.
  Any difference is a code bug or a contract bug — resolve it, do not paper over it.
- Re-run `/finnavis-api-forge core-ledger --reconcile` so `api-sync-report.md` is true again:
  - **drift point 2** moves from deferred to checked — the registry now exists
    ([T24](./T24-ports-error-envelope-and-registry.md))
  - **drift point 6** moves from N/A to checked — `src/FinNavis.Presentation/Endpoints/` now holds
    the 21 folders
- Update `CLAUDE.md` "Not set up yet": `docker-compose.yml` and the CI pipeline now exist
  ([T01](./T01-setup-compose-stack.md), [T03](./T03-setup-ci-build-and-test.md)). The React PWA and
  the Playwright suite stay on the list.
- Set every row in [`tracker.md`](./tracker.md) to `done`.

## Definition of Done

- [ ] the served document and the contract agree on all 21 operations and 27 error codes; any
      difference is resolved in the code or in the contract before this task closes
- [ ] `api-sync-report.md` is re-run and its **Verdict** is `PASS` or `PASS (with deferrals)`, with
      drift points 2 and 6 no longer deferred
- [ ] `CLAUDE.md` "Not set up yet" lists only what is still missing
- [ ] `tracker.md` matches reality
- [ ] `info.version` is **not** bumped silently — only with an explicit CHANGELOG line
      (`.claude/rules/openapi.md`)

## Notes

- Two deferrals stay open on purpose and are **not** this task's job: `spectral lint` is not wired
  (no `check` target in `build.sh`), and [sad.md §6 flow 2](../sad.md) still has no not-found
  branch. Both were deliberately left out of this epic.
- If [T04](./T04-adr-notfound-module-convention.md) changed any `*.NotFound` code, this diff is
  where a missed one shows up.
