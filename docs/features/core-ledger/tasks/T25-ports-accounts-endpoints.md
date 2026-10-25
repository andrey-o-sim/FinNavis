---
id: T25
title: "Add the four account endpoints"
layer: "ports"
deps: ["T18", "T24", "T17"]
acs: ["AC-07", "AC-08", "AC-09", "AC-10", "AC-35"]
files_hint: ["src/FinNavis.Presentation/Endpoints/Accounts/"]
owner: "Andrey Simonenko"
estimate: "M"
status: "todo"
---

# T25 — Add the four account endpoints

## Why

Derives from [`contracts/openapi.yaml`](../contracts/openapi.yaml) operations `createAccount`,
`getAccountBalances`, `renameAccount`, `deleteAccount`, and [sad.md §5](../sad.md). **Handlers
follow the contract, never the other way round** (`.claude/rules/openapi.md`).

## What

One folder per operation, each holding the endpoint plus its own request and response records —
the folder names are written into the contract:

- `Endpoints/Accounts/Create/` — `POST /api/v1/accounts`
- `Endpoints/Accounts/GetBalances/` — `GET /api/v1/accounts/balances`
- `Endpoints/Accounts/Rename/` — `PATCH /api/v1/accounts/{account_id}`
- `Endpoints/Accounts/Delete/` — `DELETE /api/v1/accounts/{account_id}`

Rules that apply to every endpoint in this epic:

- **Do not share response records between operations**, even when two look identical today
  (`CLAUDE.md`). One `$ref`'d schema in the contract still becomes one record per operation folder
  (`.claude/rules/openapi.md` deviation 4).
- **Never return a Domain entity.**
- **Money crosses the wire as a `string`**, pattern `^-?\d{1,15}(\.\d{1,4})?$` for a signed
  balance, without the `-?` for an amount the owner enters (deviation 2).
- JSON keys snake_case.

## Definition of Done

- [ ] every request and response shape matches the contract field for field, including the money
      patterns and `maxLength` bounds
- [ ] `POST /api/v1/accounts` returns the new account, and `GET .../balances` shows it at zero
      (AC-07)
- [ ] `PATCH` renames and history keeps naming the same account (AC-08)
- [ ] `DELETE` on an account with history returns `409` with `Account.HasRecordedHistory` (AC-09);
      on one with none it returns the contract's success status (AC-10)
- [ ] a duplicate name returns `409` with `Account.DuplicateName` on create and on rename (AC-35)
- [ ] every `4xx` carries the flat envelope, never `application/problem+json`
- [ ] no response record is shared with another operation folder

## Notes

- There is deliberately **no** plain "list accounts" operation — `getAccountBalances` covers it
  (`api-sync-report.md` Section D).
- `security: []` globally, so no `401` and no `403` on any operation (deviation 1).
