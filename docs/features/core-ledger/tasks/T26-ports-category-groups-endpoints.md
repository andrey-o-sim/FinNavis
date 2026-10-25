---
id: T26
title: "Add the four category-group endpoints"
layer: "ports"
deps: ["T19", "T24", "T17"]
acs: ["AC-26", "AC-27", "AC-28", "AC-29", "AC-31"]
files_hint: ["src/FinNavis.Presentation/Endpoints/CategoryGroups/"]
owner: "Andrey Simonenko"
estimate: "M"
status: "todo"
---

# T26 — Add the four category-group endpoints

## Why

Derives from [`contracts/openapi.yaml`](../contracts/openapi.yaml) operations
`createCategoryGroup`, `listCategoryGroups`, `renameCategoryGroup`, `deleteCategoryGroup`, and
[PRD US-09](../PRD.md). These are the operations whose absence made the first api-forge run fail.

## What

- `Endpoints/CategoryGroups/Create/` — `POST /api/v1/category-groups`
- `Endpoints/CategoryGroups/List/` — `GET /api/v1/category-groups`
- `Endpoints/CategoryGroups/Rename/` — `PATCH /api/v1/category-groups/{category_group_id}`
- `Endpoints/CategoryGroups/Delete/` — `DELETE /api/v1/category-groups/{category_group_id}`

Same per-operation rules as [T25](./T25-ports-accounts-endpoints.md): own request and response
records per folder, no Domain entity returned, snake_case JSON keys.

## Definition of Done

- [ ] every shape matches the contract field for field, `kind` using the contract's enum
- [ ] `POST` creates a group on a named side and returns it holding no categories (AC-26)
- [ ] `GET` returns every group with its name and side, empty groups included (AC-27)
- [ ] `PATCH` renames; a later breakdown read shows the new name and unchanged category figures
      (AC-28)
- [ ] `DELETE` on a group still holding categories returns **`204` with no body**, and the
      categories survive with all their history (AC-29)
- [ ] a duplicate name on the same side returns `409` with `CategoryGroup.DuplicateName`; the same
      name on the other side is accepted (AC-31)
- [ ] every `4xx` carries the flat envelope
- [ ] no response record is shared with another operation folder

## Notes

- **`deleteCategoryGroup` returns `204` with no body** — settled at the api-forge gate. The owner
  is not told how many categories the removal affected; they read the new grouping back with
  AC-33. Flip it only by changing the contract first.
- No `Default` group is ever created, returned or named here (sad.md §12).
