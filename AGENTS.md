# AGENTS.md

## Review guidelines

Give every finding a priority. Always report P0 and P1. Report P2 only when the fix is small.

- **P0 — blocks the merge.** Security and data integrity.
  - SQL, command or path injection.
  - Untrusted input that reaches a query, a shell command or a file path without a check.
  - A hard-coded secret. A committed `launchSettings.json`, `.env`, `*.pem` or `*.key`.
  - A real bank statement or a real export in the diff. Test fixtures must be synthetic.
  - Data loss: an edited migration that is already applied, a column dropped without a
    backfill, money stored or computed as `double` or `float`.
- **P1 — fix before the merge.** Correctness.
  - Logic bugs, wrong boundaries, edge cases with no handling.
  - A regression in behaviour that is already on `master`.
  - New behaviour with no test.
  - A project rule that CI does not check: a new query without the index it needs, an enum
    stored as int, a new service missing from DI, an error response that is not
    `{code, message, details?}`, a handler that differs from
    `docs/features/<slug>/contracts/openapi.yaml`.
- **P2 — the author decides.** Duplication, readability, small style risks.

Every finding names the place as `file:line` and gives a concrete fix.

Do not report what the build already fails on: compiler warnings (they are errors here) and
dependency rule breaks (`tests/FinNavis.ArchitectureTests` checks them). The review adds to the
CI build and tests. It does not replace them.
