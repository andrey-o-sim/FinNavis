# Hand checks for the contract

Used in protocol step 4.

`spectral` is not wired into `build.sh` yet (`CLAUDE.md` "Not set up yet"), so nothing automatic
catches a broken `$ref` or a 3.0-style `nullable`. Run these before writing the report. They take a
minute and they are the only gate the document gets.

Let `C=docs/features/<slug>/contracts/openapi.yaml`.

## Must be empty

| Check | Command | Why |
|---|---|---|
| 3.0-style nullability | `grep -n "nullable: true" $C` | 3.1 uses `type: [string, "null"]`. |
| Auth that does not exist | `grep -n "securitySchemes\|BearerAuth\|\"401\"\|\"403\"" $C` | No sign-in, no second actor (`sad.md` §8). |
| Open response shapes | `grep -n "additionalProperties: true" $C` | Only by ADR, and then noted in the report. |
| Offset paging | `grep -n "offset:\|page:\|per_page" $C` | Cursor only. |
| Query versioning | `grep -n "?v=" $C` | Version lives in the path. |
| Money as a number | `grep -n -B2 "amount\|balance\|total" $C \| grep "type: number"` | A JSON number loses the fourth decimal. |

## Must be present

| Check | How |
|---|---|
| The spec header | First line is the `REFERENCE SPEC` comment. |
| `openapi: 3.1.0` | Exactly this version. |
| `security: []` at the root | Not omitted — an empty list is the explicit statement. |
| Every `$ref` resolves | Collect `grep -o '#/components/[a-zA-Z/]*' $C \| sort -u`, then confirm each name is defined. |
| `operationId` on every operation, unique | `grep -c operationId` against the number of methods; compare with earlier features' contracts too. |
| Endpoint folder in every `description` | One `Presentation/Endpoints/<Resource>/<Operation>/` line per operation. |
| Every `4xx` references the error schema | No inline error object anywhere. |
| `limit` has minimum, maximum and default | A default with no latency budget behind it is a guess. |
| Three examples per operation | Request, success, error. |

## Read with your eyes, not grep

- **No real data in an example.** A merchant name, a real amount or a real account in an example is
  the one failure in this list that cannot be undone by an edit — it is in git from then on
  (`CLAUDE.md` Safety). Placeholders only: `Test User`, `<name>@example.test`.
- **Schema names are the glossary's words.** `Transaction`, not `TransactionDto`, not `TxnModel`.
- **Snake_case JSON keys throughout**, including inside `details` and inside examples.
- **Each error `message` is the sentence the owner reads**, lifted from the PRD, not paraphrased.

A failure found here is fixed before the report is written, not recorded in it. The report is for
disagreements between documents; this list is for the document being wrong on its own terms.
