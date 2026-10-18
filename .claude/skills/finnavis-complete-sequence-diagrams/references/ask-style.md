# Ask style — questions for the person who wrote the repo

Every `AskUserQuestion` in this skill follows these rules, on top of the project language rule in
[`.claude/rules/language.md`](../../../rules/language.md).

1. **Explain the trade-off, not the vocabulary.** Do not gloss `idempotency`, `unit of work`,
   `query port`, `aggregate`. The reader uses these daily. Do explain what the choice costs: which
   block changes, what a future reader will conclude, what stays wrong if this is skipped.
2. **An option label is the next mechanical step, not a name.** «Add a branch to flow 2» says what
   will happen. «Option A» does not.

## The one thing this skill must never do

**Never paste `sequenceDiagram` source as the body of a question.** Mermaid read out of a terminal
prompt is unreadable, and the user approves it without reading. An approval nobody read is exactly
what this whole pass exists to prevent.

Instead: write the block into `sad.md` first, lint it, then describe it in prose and ask about the
description. The file renders properly in an editor; the chat gets words.

## Describing a flow in prose

Three short paragraphs, no bullet soup:

1. **The happy path in one sentence**, in product words. «The owner hands over a file, the reader
   turns each row into a movement using the remembered rules, and everything that matched is recorded
   in one commit.»
2. **Every branch, named.** «It refuses in three cases: the account named does not exist, a row is in
   another currency, and no rule covers a row. The first stops the whole load; the other two set the
   row aside and keep going.» If a branch quotes PRD §10, say that it does.
3. **What the block implies for later.** «It reads movements by account and by date, which is the
   index hint going to the data-model stage.»

Then the question. One flow per question, always.

## The four confirm states

Every per-flow question offers exactly these, in this order:

- **Accept** — the block stays as written.
- **Fix: `<the specific thing>`** — one round. Regenerate that block, overwrite it, re-lint,
  re-describe. The second answer is final; a third round means the problem is in the PRD, not in the
  diagram, and should be said out loud.
- **Save as an open question** — remove the block, add a row to §11 (Risks and technical debt) with
  **owner and due**. Missing either one downgrades this to Drop, with a warning. A §11 row with no
  owner is a note nobody will ever action.
- **Drop** — remove the block, and record in the matrix which acceptance criteria are now uncovered
  and why that is acceptable. Dropping without recording leaves a silent hole.

## The targeted-edit question

The one place this skill touches a committed block. It needs more detail than the others, because
the user is approving a change to something they already reviewed:

- **What is being changed** — name the flow by number and heading.
- **Why** — which AC the heading claims and which line fails to show it.
- **Exactly what changes** — «one `else` branch after the account check, four lines, nothing else
  moves».
- **The alternative** — a new flow that repeats most of this one, and why that is worse here.

Options: *Add the branch to flow `<N>`* (recommended) / *Draw a separate flow instead* / *Record as
non-runtime N/A* / *Leave it, here is why*.

## The Phase 2 question

The matrix is already on screen, so the question is short. Do not re-list the gaps in the options —
name them by id.

- *Draw all three* (recommended when every gap is real)
- *Draw AC-07 and AC-19, record AC-15 as non-runtime*
- *Nothing is missing, close the pass*

**When the audit found nothing, say so before the question**, in one plain sentence, and offer only
the header lines and the Notes block. Do not manufacture an option that invents work.

## Worked rewrite

❌ «Should I add error handling to the flow?»

Vague, unanswerable, and invites a yes that means nothing.

✅ «Flow 2's heading cites AC-07 — a row in another currency is set aside — but the block never
branches on currency, so a reader sees the criterion listed and nothing that shows it. The fix is one
`else` branch after the row is read, using the PRD §10 wording («This row is in another currency, so
it was set aside»). The alternative is a separate flow, which would repeat eight of flow 2's ten
lines to show four of them.»

Options: *Add the branch to flow 2* / *Draw a separate flow* / *Record AC-07 as non-runtime N/A* /
*Leave it, here is why*.
