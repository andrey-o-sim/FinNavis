---
id: T03
title: "Add the CI job that builds the solution and runs every test"
layer: "wiring"
deps: ["T02"]
acs: []
files_hint: [".github/workflows/ci.yml"]
owner: "Andrey Simonenko"
estimate: "S"
status: "todo"
---

# T03 — Add the CI job that builds the solution and runs every test

## Why

[PRD §6](../PRD.md) states the measurement prerequisite directly: every NFR row except Availability
is measured by an integration harness **and a CI pipeline that do not exist yet**.
[sad.md §7](../sad.md) schedules the CI job alongside the compose stack and the harness, before any
ledger code.

## What

- A workflow that runs on push and on pull request: `./build.sh build`, then `./build.sh test`.
- Docker must be available on the runner, so the Testcontainers suite from
  [T02](./T02-setup-testcontainers-harness.md) actually runs rather than being skipped.
- The architecture tests in `tests/FinNavis.ArchitectureTests` run as part of `test` and are not
  excluded — they are what guards the dependency rule (`CLAUDE.md`).

## Definition of Done

- [ ] the workflow runs `./build.sh build` and `./build.sh test` and both steps are required
- [ ] a deliberately failing test turns the job red
- [ ] the Testcontainers integration tests run (not skipped) on the runner
- [ ] no secret, connection string or key appears in the workflow file (`CLAUDE.md` Safety)

## Notes

- Throughput (≥ 10 requests/s, PRD §6) is measured by a CI smoke test added later in
  [T32](./T32-tests-qg2-latency-and-explain.md). That task edits this same file, so the two share
  a lane.
- `spectral lint` is deliberately **not** wired here. It was parked at the api-forge gate and left
  out of this epic.
