---
id: T27
title: "Add the five category endpoints"
layer: "ports"
deps: ["T20", "T24", "T17"]
acs: ["AC-11", "AC-12", "AC-30", "AC-32", "AC-33", "AC-34", "AC-36"]
files_hint: ["src/FinNavis.Presentation/Endpoints/Categories/"]
owner: "Andrey Simonenko"
estimate: "M"
status: "todo"
---

# T27 — Add the five category endpoints

## Why

Derives from [`contracts/openapi.yaml`](../contracts/openapi.yaml) operations `createCategory`,
`listCategories`, `renameCategory`, `deleteCategory`, `setCategoryGroup`, and
[PRD US-04, US-10](../PRD.md).

## What

- `Endpoints/Categories/Create/` — `POST /api/v1/categories`
- `Endpoints/Categories/List/` — `GET /api/v1/categories` (optional `category_group_id` filter)
- `Endpoints/Categories/Rename/` — `PATCH /api/v1/categories/{category_id}`
- `Endpoints/Categories/Delete/` — `DELETE /api/v1/categories/{category_id}`
- `Endpoints/Categories/MoveToGroup/` — `PUT /api/v1/categories/{category_id}/group`

Same per-operation rules as [T25](./T25-ports-accounts-endpoints.md).

## Definition of Done

- [ ] every shape matches the contract field for field
- [ ] `POST` creates a category with or without a group, and it can then be named on a transaction
      (AC-11)
- [ ] `DELETE` on a category a transaction names returns `409` with
      `Category.HasRecordedHistory` (AC-12)
- [ ] placing a category into a group on the other side returns the contract's status with
      `Category.GroupKindMismatch` (AC-30)
- [ ] naming a group that is not in the ledger returns `404` with `CategoryGroup.NotFound`, and no
      group is created (AC-32)
- [ ] `GET` returns each category with name, side and current group; the `category_group_id` filter
      narrows it (AC-33)
- [ ] `PATCH` renames and recorded history keeps naming the same category (AC-34)
- [ ] a duplicate name on the same side returns `409` with `Category.DuplicateName`; the same name
      on the other side is accepted (AC-36)
- [ ] every `4xx` carries the flat envelope
- [ ] no response record is shared with another operation folder

## Notes

- **`setCategoryGroup` is the one operation whose `404` carries two different refusals** — the
  category and the group. Its `404` is written out instead of `$ref`-ing the shared response; the
  error *schema* is still one `$ref`, so nothing is duplicated (`api-sync-report.md` Section D).
- `Category.GroupNotFound` no longer exists; use `CategoryGroup.NotFound`.
