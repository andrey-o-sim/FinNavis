# Deriving operations and schemas

Used in protocol steps 1 and 2.

## A user story becomes an operation

PRD §4 is the list. Read each story as "who wants to do what", and the verb picks the method:

| The story says | Method | `operationId` |
|---|---|---|
| see / list / review | `GET` | `getAccountBalances`, `getHistory` |
| add / record / create | `POST` | `createAccount`, `recordTransaction` |
| change / correct / rename | `PUT` for a whole replace, `PATCH` for one field | `updateTransaction`, `renameCategory` |
| remove / delete | `DELETE` | `deleteAccount` |
| load / import a file | `POST` to a sub-resource | `importStatement` |

`operationId` is camelCase, unique across the whole repo — not just this file. An earlier feature's
contract already took some of them.

Paths are plural, lowercase, hyphenless: `/api/v1/accounts`, `/api/v1/category-groups` only if the
glossary itself hyphenates. Nouns come from `CONTEXT.md`, verbatim. If `CONTEXT.md` calls it a
*movement*, the path is not `/transactions-and-transfers`.

## One operation, one endpoint folder

`CLAUDE.md` §API fixes the layout: one folder per operation under
`Presentation/Endpoints/<Resource>/<Operation>/`. Write that folder into the operation's
`description`, as its last line:

```yaml
  post:
    operationId: createAccount
    description: |
      Creates an account with an opening balance.

      Endpoint folder: `Presentation/Endpoints/Accounts/Create/`
```

This is the whole point of the line: `implement-tasks` reads it and does not invent a layout, and a
reviewer can check the tree against the contract without opening both.

Resource folder is plural and PascalCase (`Accounts`), operation folder is the verb (`Create`,
`List`, `Delete`), matching `CLAUDE.md`'s own example.

## `$ref` in the contract, a separate record in C#

These two rules look contradictory and are not — they live at different levels:

- `.claude/rules/openapi.md`: shared types via `$ref` only, no inline duplicated schemas.
- `CLAUDE.md` §API: do not share response records between operations, even when two look identical.

The `$ref` rule keeps the **document** from drifting against itself. The no-sharing rule keeps
**one operation's change** from silently reshaping another endpoint. So: one `$ref`'d schema in the
contract, one record per operation folder generated from it. Note this once in the report rather
than per operation.

Never surface a Domain entity shape as-is. The schema is the API's view of the entity, and the two
diverge the first time a column is internal — `transactions.external_ref` is already that case.

## A story that gets no operation

Three legitimate reasons, and each one is recorded in the report rather than left silent:

1. **An earlier feature already exposes it.** Link that contract's `operationId`. Do not re-declare
   the path with a different shape.
2. **It is internal.** A use case SAD §5 declares with no outside caller. Say which flow consumes it.
3. **The PRD put it out of scope** (§6.1). Quote the line.

Anything else is drift point 4, not a judgement call to make alone.

## Query parameters come from acceptance criteria

A filter exists because an AC asks for it. "The owner filters history by category" is a
`category_id` query parameter; nothing else gets one. An AC that pulls two ways — a category filter
over a history that includes transfers, which carry no category — is a question for the user, and
the answer goes into the parameter's `description` so the next reader does not re-ask it.

## Lists: the cursor page wrapper

Every list response is wrapped, never a bare array:

```yaml
    required: [items, has_next, has_prev]
    properties:
      items: { type: array, items: { $ref: "..." } }
      has_next: { type: boolean }
      has_prev: { type: boolean }
      next_cursor: { type: [string, "null"] }
      prev_cursor: { type: [string, "null"] }
```

The cursor is an **opaque string**, not a UUID. History is a `UNION ALL` of two tables ordered by
`(occurred_on, id)` (ADR-0001), so one id cannot locate a boundary. The server mints the token; the
client passes it back unread. Both cursors are present because `before` is unusable without
`prev_cursor` — `.claude/rules/openapi.md` §3.

`limit` needs a minimum, a maximum and a default, and the default is justified by a PRD §6 latency
budget, not picked round.

## Batch operations answer 200

`CLAUDE.md` §API: a batch that finished is a success even when it skipped items. So the response is
a `200` carrying counts plus one entry per skipped item with its reason:

```yaml
    required: [imported, skipped, set_aside]
    properties:
      imported: { type: integer }
      skipped:  { type: integer }
      set_aside:
        type: array
        items:
          type: object
          required: [row, reason]
          properties:
            row:    { type: integer }
            reason: { type: string, enum: [own_transfer, other_currency, no_rule] }
```

The reason is a closed `enum` the PRD names, never free text. The error envelope is only for a
refusal that **stopped** the whole load.
