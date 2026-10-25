---
id: T29
title: "Add the three transfer endpoints"
layer: "ports"
deps: ["T22", "T24", "T17"]
acs: ["AC-04", "AC-05", "AC-06", "AC-23"]
files_hint: ["src/FinNavis.Presentation/Endpoints/Transfers/"]
owner: "Andrey Simonenko"
estimate: "M"
status: "todo"
---

# T29 — Add the three transfer endpoints

## Why

Derives from [`contracts/openapi.yaml`](../contracts/openapi.yaml) operations `recordTransfer`,
`editTransfer`, `removeTransfer`, and [PRD US-02, US-08](../PRD.md).

## What

- `Endpoints/Transfers/Record/` — `POST /api/v1/transfers`
- `Endpoints/Transfers/Edit/` — `PATCH /api/v1/transfers/{transfer_id}`
- `Endpoints/Transfers/Remove/` — `DELETE /api/v1/transfers/{transfer_id}`

Same per-operation rules as [T25](./T25-ports-accounts-endpoints.md).

## Definition of Done

- [ ] every shape matches the contract field for field; **no request or response carries a
      category field** (AC-23 is unrepresentable, not validated)
- [ ] `POST` moves the amount: the source balance falls, the destination rises, and the total
      across all accounts is unchanged on the next read (AC-04)
- [ ] source equal to destination returns `400` with `Transfer.SameAccount` and the AC-05 wording
      (AC-05)
- [ ] a failed commit leaves neither account changed and returns `Transfer.NotRecorded` (AC-06)
- [ ] an unknown source or destination returns `404` with `Transfer.SourceAccountNotFound` or
      `Transfer.DestinationAccountNotFound`
- [ ] attempting to set a category on `PATCH` returns the contract's status with
      `Transfer.CategoryNotAllowed` and the AC-23 wording (AC-23)
- [ ] every `4xx` carries the flat envelope
- [ ] no response record is shared with another operation folder

## Notes

- The contract returns both account `*.NotFound` codes even though
  [sad.md §6 flow 2](../sad.md) draws no not-found branch — a known diagram gap recorded in
  `api-sync-report.md` Section D. **Follow the contract.**
