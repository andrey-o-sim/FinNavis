# The entry format

The committed entries in `docs/GLOSSARY.md` are the specification. **Read them before composing a
new one** — they are more precise than any description, including this file, and they do not go
stale the way a copy here would. This file states what they have in common and why.

## The line

```
- <Term> — <one-sentence canonical definition>. NOT <Other> (<how it differs, in the owner's terms>).
```

One bullet, one line, no wrapping in the source. Long is fine: the committed `Category Group`
entry runs to four clauses, because the word genuinely needs them.

## The definition half

- **One sentence.** Semicolons are allowed and used — `Account` has one. A second full sentence
  usually means the word is really two words.
- **Say what the thing *is*, then the one fact that makes it usable.** `Transaction` gives the
  shape ("a single income or expense recorded on exactly one account") and then the fields that
  matter. `Balance` gives the meaning and then how it is derived.
- **In the owner's words, not the system's.** "a place where the owner's money sits, such as cash,
  a card or a savings account" — not "an entity with a name and a currency".
- **No implementation.** No table, column, type or endpoint. `Balance` says it equals a sum; it
  does not say it is computed on demand, even though `core-ledger` ADR-0002 settles exactly that.
  The ADR is where that belongs.

## The NOT-boundary — mandatory

Every committed entry has one, and so does every new one. The question is never *whether*, only
*which* concept.

**Why it is the expensive half.** A definition without a boundary is a label: it tells you what the
word means in isolation, not where it stops. Most domain confusion is two words whose edges
overlap, and the boundary is the only part of the entry that resolves it. The definition is usually
easy and the author already has it; spend the time here.

- **Name the concept, then say how it differs**, in one parenthesis, in the owner's terms:
  `NOT Category (a category says what the money was for, an account says where the money is).`
- **Point at something that exists** — a glossary term, or a wrong mental model a reader will
  plausibly arrive with. A boundary against a word nobody uses teaches nothing.
- **No obvious homonym? Use the nearest neighbour.** There is always one. `Export` draws its line
  against `Statement`; `Set-aside row` against `Duplicate`. Both pairs are easy to confuse exactly
  because they sit close together.
- **Two boundaries when the word is confused with two things**, joined by `and`. `Category Group`
  and `Default group` both do this. The second is usually not another term but a **wrong mental
  model** — which is the more valuable of the two.

**Mutual boundaries.** `Transaction` and `Transfer` define each other from both sides, and that is
what makes the pair unambiguous. A new term that sits next to an existing one should do the same —
which means an `--amend` on the neighbour, not a one-sided entry. Say so rather than writing half
the pair.

## Term naming

- **Capitalise the first word only.** The rest is lowercase unless a proper noun: `Mapping
  profile`, `Import run`, `Set-aside row`, `Default group`.
- Single-word terms are capitalised: `Account`, `Balance`, `Duplicate`, `Owner`, `Shape`.
- **One committed exception: `Category Group`,** in title case. It is referenced by name from other
  entries' boundaries, so renaming it would touch several lines plus whatever quotes it in the PRDs
  and SADs. Leave it. New terms follow the rule.
- If the user asks for a case that breaks the rule, say so in one line and write it their way if
  they confirm. It is their glossary.

## Ordering

Alphabetical, case-insensitive, by the full term. `Category` before `Category Group` before
`Default group`.

Insert in place. **Never re-sort the file as a side effect** — a one-term commit that also reorders
twenty lines is invisible in review.

## Which words qualify

**Capture** a word when it names a thing in the owner's world that the code will have to model,
and two people could reasonably read it differently:

- `Statement` — a file from a bank covering a period. Confusable with `Export`.
- `Duplicate` — a record for money already recorded. Confusable with "two identical purchases on
  the same day", which is not a duplicate at all.
- `Balance` — confusable with "total", "available", and "balance as of a date".

**Skip** generic technical vocabulary and anything another file already owns →
[domain-vs-technical.md](domain-vs-technical.md).

**Skip** a word the user used once in passing with no modelling consequence.

## When the conversation did not settle it

Do **not** invent a definition to fill the glossary. An unresolved entry is worse than no entry,
because later stages trust it.

Put it where the caller keeps open points instead:

```
- [ ] "Period" — a calendar month, or 30 days from a date? Owner: Andrey, due: data-model stage.
```

`finnavis-interview` has §15 Open questions, `finnavis-prd` has §8. Say which one you put it in.

## A worked pass

Term: `Reconciliation`, surfaced in a review.

1. **Domain or technical?** The owner would say "I checked the app against my bank". Domain.
2. **Already there?** No. Nearest neighbours: `Statement`, `Import run`, `Duplicate`.
3. **Definition, confirmed with the user:** "comparing what the ledger holds for one account over
   a period against what the bank's statement says for the same account and period, to find what
   is missing on either side".
4. **Boundary.** The likely confusion is with importing — both start from a statement.
   `NOT an Import run (a run records movements into the ledger, while a reconciliation records
   nothing and only reports the difference).`
5. **Line:**

   ```
   - Reconciliation — comparing what the ledger holds for one account over a period against what the bank's statement says for the same account and period, to find what is missing on either side. NOT an Import run (a run records movements into the ledger, while a reconciliation records nothing and only reports the difference).
   ```

6. **Position:** after `Owner`, before `Set-aside row`.

Note what did not happen: no matching window, no fingerprint, no tolerance. Those are `csv-import`
ADR territory. The glossary says what the word means.
