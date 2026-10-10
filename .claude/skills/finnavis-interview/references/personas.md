# Review lenses — Builder / Future-me / User-me

Used in Phase 6. Three sub-agents in one message. Each sees all three approaches from §7 and
returns 3-5 bullets **per approach**.

## Why not Engineer / Executive / UX

The generic trio assumes three different people with three different stakes. Here they are one
person. Two of the three hats are borrowed, and a sub-agent wearing a borrowed hat invents the
stakes to go with it — in `core-ledger` the Executive sub-agent had to make up its own frame
("the currency is evenings") because the business it was asked about does not exist.

These three lenses split the same person by **time horizon** instead of by role. That split is
real on a solo project: the person building tonight, the person maintaining it in six months,
and the person using it on a Tuesday genuinely disagree, and their disagreement is the useful
signal.

---

## Builder — tonight

What it costs to build and to keep correct, starting now.

```
You are reviewing three strategic approaches for a feature of a single-owner, self-hosted
personal finance project. You are the person who will build it, starting this week, in the
evenings.

<the three approaches, verbatim>

For EACH approach return 3-5 bullets covering:
- What the real cost is, and whether it is build cost or correctness cost.
- Which parts are shared between approaches and which are genuinely extra work. If two
  approaches share a write path and differ only in what can be read back, say so - it
  changes the comparison completely.
- Where the testing burden grows faster than the feature does.
- Which approach is hardest to extend later without breaking what already exists.

HARD RULES
- No datastore, framework or library names. Talk about shape, surface and correctness, not
  about tools.
- Be concrete about cost in relative terms ("C costs one extra grouping concept over A, not
  a second subsystem"), never in hours.
- You are allowed to say an approach is cheap. Do not manufacture concerns to look thorough.
```

## Future-me — six months out

The person who comes back after a break and has to live with the decision.

```
You are reviewing three strategic approaches for a feature of a single-owner, self-hosted
personal finance project. You are the owner six months from now, coming back after a gap,
looking at what was built.

<the three approaches, verbatim>

For EACH approach return 3-5 bullets covering:
- What you would have to re-open, and why. Which approach makes the next feature start with
  a migration or a backfill over data that already exists?
- What was built and never used. Which approach spends evenings on capability nobody has
  demanded yet?
- What you would no longer remember. Which approach relies on knowledge that lives only in
  the builder's head tonight?
- Whether the thing is still being used at all, or the old tool quietly won.

HARD RULES
- No datastore, framework or library names.
- The currency is evenings and attention, not money or headcount. Opportunity cost here
  means "the other feature that did not get built".
- Judge a foundation by what it unlocked, not by how it looked on launch day.
```

## User-me — Tuesday evening

The same person, using it, with a receipt in hand and ten minutes of patience.

```
You are reviewing three strategic approaches for a feature of a single-owner, self-hosted
personal finance project. You are the owner USING it on a Tuesday evening: tired, holding a
receipt, with about ten minutes of patience.

<the three approaches, verbatim>

For EACH approach return 3-5 bullets covering:
- How many steps it takes to do the one thing you came to do.
- Whether the answer arrives ready to use, or you have to do arithmetic, re-sort or
  cross-check it yourself afterwards.
- What is obvious without remembering anything, and what needs you to recall how it works.
- What makes you stop trusting the numbers, even once.

HARD RULES
- No datastore, framework or library names.
- "Interface" here is whatever the owner touches - a response, a screen, a command. Judge
  the shape of the answer, not the styling.
- Trust is a usability property. A number you cannot explain is worse than a missing number.
```

---

## Building the synthesis matrix

After all three return, build the §8 matrix: three lenses by three approaches, `+` / `0` / `−`
in each cell, with a **six-word justification** inside the cell. Not a separate paragraph — in
the cell, so the table can be read on its own.

| | Builder | Future-me | User-me |
|---|:---:|:---:|:---:|
| App. A | **+** smallest surface, trivial to test | **0** fast now, repays later | **−** caller re-aggregates group totals |

Rules:

- **No lens may give the same symbol to all three approaches.** That means the lens did not
  discriminate, and the prompt needs re-running with the three approaches made explicit.
- **An all-plus approach is a finding, not a formality.** If one approach is `+` across all
  three lenses, §13 must cite that cell — it is the strongest single argument available.
- **A `−` needs a reason that a reader can check**, not a mood. "Combinatorial correctness,
  unbounded results" is checkable. "Feels heavy" is not.
