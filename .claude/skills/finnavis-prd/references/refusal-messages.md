# §10 — the refusal messages

Used in Phase 5 (draft §10) and Phase 8 (self-check).

## What this section is, and what it is not

Every §5 criterion that ends in a refusal contains a sentence the owner reads: "a transfer needs two
different accounts". §10 collects those sentences in one place, so they can be read together and made
to sound like one product rather than twelve different moods.

**No codes.** `finnavis.transfers.same_account` is a name for a machine, and naming it is a decision of
the stage that writes the code — not of a document that describes the product. A code written here
would be a guess that later gets read as a decision, and it would drag status numbers and payload
shapes in behind it. The PRD states the refusals; it does not name them.

That handover is explicit: §9's "Left to the implementation stage" bullet says that naming the
failures belongs there.

## The table

| AC | What the owner is told |
|---|---|
| AC-02 | The amount must be above zero. |
| AC-03 | There is no category with that name, and none was created. |
| AC-05 | A transfer needs two different accounts. |
| AC-09 | This account has recorded movements, so it cannot be deleted. |

Two columns, nothing else. Do **not** restate the Given and the When — they are in §5, and copying
them creates two versions of the same criterion that drift apart.

## The coverage rule (checked in Phase 8)

**Every §5 criterion of type `error`, `domain invariant` or `cross-context` has a row here.** A
happy-path criterion has no refusal to state. A history-integrity or duplicate-safety criterion gets a
row only when it ends in a refusal rather than in silent correctness — "the second load adds nothing
and tells the owner the file was already loaded" is a row; "the figure stays as it was" is not.

The reverse check matters as much: a row with no criterion behind it means a refusal was invented here
that nothing in §5 asked for. Delete it, or add the missing criterion.

## How to write the message

Same voice as §5 — the owner is reading it:

- say what is wrong and what must be true instead: "A transfer needs two different accounts";
- do not name a field the way code would ("source account id must differ from destination");
- do not apologise, do not say "an error occurred", do not suggest contacting support — there is
  nobody to contact;
- one sentence. If it needs two, it is probably two rules, and §5 is missing one of them.
