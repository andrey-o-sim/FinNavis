# erDiagram lint

Run in Phase 4, right after the diagram is written and before anything else happens.

If `mmdc` is installed, render-parse the file and fix whatever fails. It usually is not, so run
these structurally.

1. **Fence and type.** The block opens with ` ```mermaid ` and its first non-empty line is
   `erDiagram`.
2. **Every fence is closed.** Count the ` ``` ` markers in the whole file: the total must be even.
   An unclosed fence swallows the rest of the document — this has happened in this repo.
3. **Cardinality glyphs are valid.** `||--o{`, `||--||`, `}o--o{`, `||--|{` and their mirrors. A
   typo here parses as a relationship with no cardinality and renders wrong rather than failing.
4. **Entity names match the table names**, uppercased. `TRANSACTIONS` for `transactions`. A diagram
   naming `TRANSACTION` while the table is `transactions` sends the reader looking for two things.
5. **Attribute lines are `type name [PK|FK|UK] ["comment"]`**, in that order. A comment that is not
   quoted breaks the block.
6. **Every entity in a relationship is declared** with its own attribute block.
7. **Two relationships between the same pair are labelled differently.** Two accounts columns on one
   table produce two lines between the same boxes; without distinct labels — "sends from",
   "receives into" — the diagram says less than the table it describes.
8. **No `<placeholder>` or `TBD` inside the block.**

A failed check is fixed before the commit, not after. A broken diagram in a committed document is
worse than no diagram: it renders as an error box, and nobody fixes an error box in a file they are
only reading.
