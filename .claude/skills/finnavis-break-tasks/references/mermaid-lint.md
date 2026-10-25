# flowchart lint

Run in step 5, right after `_epic.md` is written and before the commit question.

If `mmdc` is installed, render-parse the file and fix whatever fails. It usually is not, so run
these structurally.

1. **Fence and type.** The block opens with ` ```mermaid ` and its first non-empty line is
   `flowchart LR`.
2. **Every fence is closed.** Count the ` ``` ` markers in the whole file: the total must be even.
   An unclosed fence swallows the rest of the document — this has happened in this repo.
3. **Every node is declared once, with a quoted label**, before any edge uses it:
   `T08["T08 Transaction"]`. A node first seen inside an edge renders with its id as the label and
   the reader loses the title.
4. **Labels are quoted.** A bare label breaks on a bracket, a slash or a dot — `Program.cs`,
   `GET /accounts` and `UNION ALL` all appear in these titles.
5. **Edges are `-->` only.** No labelled edges, no `-.->`, no `==>`. A dependency has one meaning
   here, so a second arrow style says something the model cannot express.
6. **Node ids match the task ids exactly**, zero-padded: `T08`, not `T8` and not `t08`. The
   diagram, `tasks.json`, the table and the file names must all use one spelling.
7. **Every edge is a real `deps` entry, and every `deps` entry is an edge.** The diagram and
   `tasks.json` render the same graph. Count both sides.
8. **No cycle.** Walk the edges; a task that reaches itself is a defect that the implement stage
   will hit as a deadlock, not as an error message.
9. **At least one node has no incoming edge, and at least two of them do not.** One root means the
   graph is a chain and the ring cut collapsed.
10. **No `<placeholder>` or `TBD` inside the block.**

A failed check is fixed before the commit, not after. A broken diagram in a committed document is
worse than no diagram: it renders as an error box, and nobody fixes an error box in a file they are
only reading.
