# The lint — nine checks, run before anything is shown or committed

Used in Phase 3 on each block as it is written, and in Phase 6 over the whole of §6.

If `mmdc` is available, render-parse the file and fix whatever fails. It usually is not, so run the
checks below structurally. Each one comes from a real break in this repository.

1. **Fence and type.** The block opens with ` ```mermaid ` and its first non-empty line is
   `sequenceDiagram`. A stray closing fence with no opener leaves the rest of the document inside a
   code block — this happened in `core-ledger` and needed a fix commit.
2. **Every fence is closed.** Count the ` ``` ` markers in the whole file: the total must be even.
   Do this on the file, not on the block, because a block can look fine while the file is broken.
3. **Participants are declared before their first arrow**, with `actor` or `participant`, and every
   alias used in an arrow exists. `App-->>Pres` with no `participant Pres` renders a diagram with an
   extra phantom column.
4. **`alt` / `else` / `opt` / `loop` are closed with `end`.** Count the openers and the `end`s. One
   missing `end` swallows everything after it.
5. **Aliases are the repo's.** `Owner`, `Pres`, `App`, `Dom`, `Inf`, `PG`, `Web` — and nothing else
   unless §5 declared it → [participants.md](participants.md).
6. **No generic placeholder survived.** No `<client>`, `<service>`, `<data-store>`, `<external-system>`,
   `<message-bus>`, `<ui>`. These come from the generic skill's templates and are the single most
   likely thing to leak.
7. **No `<…>` stub, `TODO` or `NNNN`** anywhere in the block, including inside notes and headings.
   A `<what the owner asks for>` left in place is a visible admission that nobody read the diagram.
8. **No HTTP verb, path, status code or SQL** in any message. Grep the block for `GET `, `POST `,
   `PUT `, `DELETE `, `/`, `200`, `201`, `400`, `404`, `SELECT`, `INSERT`, `UPDATE`.
   A `/` inside ordinary prose is fine; a `/` in a path is not.
9. **Names check out.** The actor is in `CONTEXT.md`. Every domain noun in a message is a
   `CONTEXT.md` term or a §5 name — not a synonym invented while drafting. `Statement`, `Import run`
   and `Set-aside row` mean specific things here.

A block that fails any check is fixed **before it is described to the user**, not after. Showing a
broken block and asking about its content wastes the user's turn on the wrong question.

A broken diagram in a committed document is worse than no diagram: it renders as an error box, and
nobody fixes an error box in a file they are only reading.
