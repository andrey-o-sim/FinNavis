---
id: T01
title: "Stand up the Docker Compose stack with PostgreSQL 18"
layer: "wiring"
deps: []
acs: []
files_hint: ["docker-compose.yml", "src/FinNavis.Presentation/Dockerfile", ".dockerignore"]
owner: "Andrey Simonenko"
estimate: "S"
status: "done"
attempts: 1
---

# T01 — Stand up the Docker Compose stack with PostgreSQL 18

## Why

[sad.md §7](../sad.md) says this feature **creates the deployment unit**, and schedules the compose
stack as one of the first tasks of core-ledger, before any ledger code. [PRD §6](../PRD.md) cannot
measure one NFR row without a database to run against. [sad.md §11](../sad.md) carries the absence
as a High risk.

## What

- `docker-compose.yml` — one API container, one PostgreSQL **18** container, one named volume for
  the data. The major version is pinned by §7: an unpinned tag would make a correctness-test
  failure impossible to attribute.
- The two containers share a private Docker network. Only the API port is published, and only on
  the local network — with no authentication in this feature, the network boundary is the only
  access control there is (PRD §6.1).
- A `Dockerfile` for `src/FinNavis.Presentation` and a `.dockerignore` that keeps `obj/`, `bin/`
  and `launchSettings.json` out of the build context.

## Definition of Done

- [ ] `docker compose up -d` starts both containers and the API answers on its published port
- [ ] only the API port is published; `docker compose port` shows no mapping for PostgreSQL
- [ ] the PostgreSQL image tag names major version 18, not `latest`
- [ ] `docker compose down -v` removes the named volume and leaves nothing behind
- [ ] no connection string, password or key is in the committed file — values come from the
      environment (`CLAUDE.md` Safety)

## Notes

- `docker-compose.yml` is listed under "Not set up yet" in `CLAUDE.md`. Removing it from that list
  belongs to [T34](./T34-docs-contract-diff-and-conventions.md), not here.
- Hard rule: never commit a real connection string or password (`CLAUDE.md` Safety).
