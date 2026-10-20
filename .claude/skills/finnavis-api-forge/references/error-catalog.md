# Error responses and the code catalog

Used in protocol step 3.

## The envelope

Flat, by `docs/adr/0002`. Not RFC 9457 Problem Details.

```json
{
  "code": "account.has_recorded_history",
  "message": "The account has 42 movements and cannot be deleted.",
  "details": { "movement_count": 42 }
}
```

- `code` — `module.error_name`, snake_case both sides. The only part a client branches on.
- `message` — the refusal sentence the **owner reads**. It comes from the PRD (§10 in `csv-import`,
  the acceptance criteria elsewhere) and is never invented here.
- `details` — optional, free-form, for structured context: the failing fields, a conflicting id, a
  count. Omit it rather than send an empty object.

One consequence to carry into the report every run until `src/` handles it: **ASP.NET Core returns
`application/problem+json` by default** — routing 404s, model-binding failures,
`Results.ValidationProblem`. That default has to be turned off, or the API answers with two shapes.
`docs/adr/0002` names this as the decision's real cost.

## Status codes in a project with no sign-in

| Status | Means here | Example code |
|---|---|---|
| `200` | Done. Also a batch that finished with items skipped | — |
| `201` | Created. `Location` header to the new resource | — |
| `204` | Done, nothing to return (a delete) | — |
| `400` | The request shape or a value is wrong | `transaction.amount_out_of_range` |
| `404` | It does not exist. **Plain not-found, not existence-hiding** — there is nobody to hide it from | `transaction.category_not_found` |
| `409` | It exists but the state or a uniqueness rule refuses | `account.has_recorded_history` |
| `5xx` | The server broke. No code catalogued; it is not a domain outcome | — |

**No `401`, no `403`, ever.** `PRD.md` §3, `sad.md` §8 and `.claude/rules/openapi.md` §1 all say
there is no sign-in and no second actor. A status that denies an actor who cannot exist is a lie in
the contract, and it makes the later OAuth feature harder, not easier, because nobody knows which
`403` was real.

`429` only when an acceptance criterion asks for a rate limit. None does today.

Prefer `409` over `400` when the request was well-formed and the **world** refused it. "Delete an
account that has movements" is 409. "Amount has five decimal places" is 400.

## Where a code comes from

Every code traces to something. In order of strength:

| Source | Shape | Example |
|---|---|---|
| A `UNIQUE` constraint in `data-model.md` | `<entity>.duplicate_<field>` | `account.duplicate_name` |
| A foreign key | `<entity>.<ref>_not_found` | `transaction.category_not_found` |
| A domain invariant named by an AC | named after the rule, not the code path | `account.has_recorded_history` |
| An `alt`-branch label in `sad.md` §6 | named after the branch | `import.unknown_account` |
| A validation bound (`varchar(N)`, a range) | `<entity>.<field>_invalid` or `_out_of_range` | `transaction.amount_out_of_range` |

`module` is the aggregate in the **singular**, from `CONTEXT.md`: `account`, `transaction`,
`transfer`, `category`, `import`. Not the table name, not the C# class name, not the path segment.

A code with no source in that table is invented. Ask the user where the refusal comes from, or drop
the response.

## One code, one definition

`.claude/rules/openapi.md`: one error `code` in the contract means one definition in the repo
returning the **same string**. Two definitions for one refusal is how a frontend `switch` goes stale
without anyone noticing.

`sad.md` §8 says a broken domain rule produces a **sentinel domain error** that Presentation maps to
a response. That sentinel is the natural carrier of the code. Where exactly the sentinels live is
`implement-tasks`' call — this stage does not pick a file.

Until `src/` has them, drift point 2 is **deferred, not failed**: list every code the contract
advertises in the report, so the registry is a transcription job rather than a re-derivation.

## The same refusal in two features

A code an earlier feature already defined is reused with the same spelling and the same status. If
this feature needs the same refusal with a different status, that is a conflict for the user, not
a second code.

## Validation details

When one request can fail several field checks at once, say so in `details` rather than returning
the first one:

```json
{
  "code": "transaction.invalid_request",
  "message": "The entry could not be recorded.",
  "details": { "fields": { "amount": "must have at most 4 decimal places", "occurred_on": "is required" } }
}
```

Use one `invalid_request` code per resource for shape failures, and a specific code for each
*business* refusal. A client branches on business refusals; it only displays shape failures.
