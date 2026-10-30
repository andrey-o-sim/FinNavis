---
id: T28
title: "Add the three transaction endpoints"
layer: "ports"
deps: ["T21", "T24", "T17"]
acs: ["AC-01", "AC-02", "AC-03", "AC-21", "AC-22", "AC-24"]
files_hint: ["src/FinNavis.Presentation/Endpoints/Transactions/"]
owner: "Andrey Simonenko"
estimate: "M"
status: "todo"
---

# T28 — Add the three transaction endpoints

## Why

Derives from [`contracts/openapi.yaml`](../contracts/openapi.yaml) operations `recordTransaction`,
`editTransaction`, `removeTransaction`, and [PRD US-01, US-08](../PRD.md).

## What

- `Endpoints/Transactions/Record/` — `POST /api/v1/transactions`
- `Endpoints/Transactions/Edit/` — `PATCH /api/v1/transactions/{transaction_id}`
- `Endpoints/Transactions/Remove/` — `DELETE /api/v1/transactions/{transaction_id}`

Same per-operation rules as [T25](./T25-ports-accounts-endpoints.md). Money is a **string** with
the positive-amount pattern `^\d{1,15}(\.\d{1,4})?$` — a JSON number is an IEEE double in most
clients and loses the fourth decimal place of `NUMERIC(19,4)` (deviation 2).

## Definition of Done

- [ ] every shape matches the contract field for field, including `note` at `maxLength: 500` and
      `occurred_on` as a date with no time
- [ ] `POST` records the expense, returns it with the note, and the balance read after it is lower
      by the amount (AC-01)
- [ ] an amount of zero or below returns `400` with `Transaction.AmountNotPositive` and the PRD's
      wording (AC-02)
- [ ] an unknown category returns `404` with `Category.NotFound` and creates no category
      (AC-03)
- [ ] `PATCH` on the amount changes every affected balance on the next read (AC-21)
- [ ] `DELETE` removes the entry from history and the balances return to what they would have been
      (AC-22)
- [ ] naming a second account on `PATCH` returns the contract's status with
      `Transaction.KindChangeNotAllowed` and the AC-24 wording (AC-24)
- [ ] `external_ref` is **not surfaced** in any request or response — intentionally internal
      (ADR-0006)
- [ ] every `4xx` carries the flat envelope
- [ ] no response record is shared with another operation folder

## Notes

- No `Idempotency-Key` on any of these. [sad.md §6](../sad.md) states it outright: *"No flow in
  this feature is asynchronous."* De-duplication of a repeated identical request is out of scope
  (PRD §6.1).
