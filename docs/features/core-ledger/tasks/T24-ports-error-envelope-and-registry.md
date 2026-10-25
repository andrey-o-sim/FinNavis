---
id: T24
title: "Build the error registry and route every failure through the flat envelope"
layer: "ports"
deps: ["T05", "T04"]
acs: ["AC-25"]
files_hint: ["src/FinNavis.Presentation/Errors/", "src/FinNavis.Presentation/Middleware/", "src/FinNavis.Presentation/Program.cs", "tests/FinNavis.Presentation.UnitTests/Errors/"]
owner: "Andrey Simonenko"
estimate: "M"
status: "todo"
---

# T24 — Build the error registry and route every failure through the flat envelope

## Why

Derives from [`docs/adr/0002`](../../../adr/0002-return-a-flat-error-envelope-instead-of-problem-details.md),
[`docs/adr/0003`](../../../adr/0003-spell-an-error-code-in-pascalcase.md),
[sad.md §8](../sad.md) Error handling and
[`.claude/rules/openapi.md`](../../../../.claude/rules/openapi.md) §5 and §7.

`api-sync-report.md` drift point 2 is **deferred because no registry exists**: *"`implement-tasks`
creates the registry from the table in point 2, one class per module. Then `--reconcile` verifies
it."* Every endpoint task depends on this.

## What

- **The registry.** One static class per module, member per name:
  `CategoryErrors.GroupNotFound` returns `"Category.GroupNotFound"` — class for module, member for
  name, no translation rule between the contract and the registry. All **27** codes from
  `contracts/openapi.yaml`.
- **The envelope.** Map a sentinel domain error and a use-case refusal to
  `{code, message, details?}`. `message` is the refusal wording from the PRD acceptance criteria,
  not invented. `details` carries field-level validation only.
- **Turn off `application/problem+json`.** ASP.NET Core returns it by default for routing 404s,
  model-binding failures, `Results.ValidationProblem` and anything under `AddProblemDetails`. That
  default has to go, and **every** failure routed through the one envelope — otherwise the API
  answers with two shapes depending on how far into the pipeline the request got, and a client that
  switches on `code` breaks on exactly the requests that never reached a handler.
- The status classes from `.claude/rules/openapi.md`: `400` validation, `404` not found, `409`
  state or uniqueness conflict, `5xx` server. **No `401` and no `403`** — there is no sign-in and
  no second actor (deviation 1).

## Definition of Done

- [ ] all 27 codes in the registry match `contracts/openapi.yaml` string for string, and each
      matches `^[A-Z][A-Za-z0-9]*\.[A-Z][A-Za-z0-9]*$`
- [ ] a test enumerates the registry and the contract and fails if either side has a code the other
      does not
- [ ] a request to an unmapped route returns the **envelope**, not `application/problem+json`
- [ ] a model-binding failure returns the envelope, not `application/problem+json`
- [ ] JSON keys are snake_case everywhere, **including inside `details`** — the code is the one
      PascalCase value in the body
- [ ] an unknown id returns the matching `*.NotFound` with the PRD's wording (AC-25)
- [ ] no response anywhere carries `type`, `instance` or `title`

## Notes

- Blocked by [T04](./T04-adr-notfound-module-convention.md): the `*.NotFound` module convention
  must be settled first, or all four account codes and both category codes get written twice.
- Shares a lane with [T17](./T17-wiring-dependency-injection.md) on `Program.cs`.
- The `FinNavis.Presentation.UnitTests` project does not exist yet — create it, and add it to
  `FinNavis.slnx` and to `tests/FinNavis.ArchitectureTests`' allowed references if needed.
