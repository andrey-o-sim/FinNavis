# `--amend` and `--audit`

Two modes beyond adding a term. They share nothing except the file they read.

---

# `--amend` — change a definition that has moved

The only irreversible thing this skill does: the old line disappears. Everything here exists to
make that a decision rather than an accident.

## Protocol

1. **Find the term.** Not in the glossary → stop, drop the flag, add it instead.
2. **Sweep the documents** for the word, so the user can see what the old sense is holding up:

   ```bash
   grep -rin "<term>" \
     docs/GLOSSARY.md \
     docs/features/*/CONTEXT.md \
     docs/features/*/PRD.md \
     docs/features/*/sad.md \
     docs/features/*/data-model.md \
     docs/features/*/idea-brief.md \
     docs/features/*/contracts/ \
     docs/features/*/adr/ \
     docs/features/*/tasks/ \
     docs/adr/ SPEC.md
   ```

   Case-insensitive, because the word appears mid-sentence in prose. Search the singular stem for a
   multi-word term. **Skip `src/` and `tests/`** — an identifier cannot contradict a definition; if
   the code's naming is wrong that is a review finding on the code.
3. **Show the diff of one line:** the current entry and the proposed one, one above the other.
4. **Show every document that carries the word**, grouped by file with line numbers.
5. **Ask for confirmation.** One `AskUserQuestion`, and name what is lost.
6. **Replace the one line.** Change nothing else — not the ordering, not a neighbouring entry.
7. **Report the affected documents in the handoff. Do not edit them.**

## Why step 7 does not edit

A PRD or a SAD is committed and reviewed. Rewriting one because a glossary line moved is a decision
with its own blast radius, and it belongs to `finnavis-prd` or `finnavis-architecture-design`.

The honest cost: the repo is briefly inconsistent — new definition, old prose. That is better than
a glossary edit that silently rewrites four documents, which is the failure this split prevents.

## The mutual-boundary case

If the amended term is half of a pair that defines itself against the other (`Transaction` /
`Transfer`), changing one side usually makes the other side wrong. Say so and offer to amend both
in the same run. A half-amended pair is worse than the original.

---

# `--audit` — check the glossary, write nothing

No term argument. Reports; never edits. Useful before a release, after a batch import of terms, or
when a skill downstream starts producing odd names.

## What it checks

**Inside `docs/GLOSSARY.md`:**

1. **Format.** Every line matches `- <Term> — <definition>. NOT <...>`. Report any line that does
   not.
2. **Boundary present.** Every entry has a `NOT`. This is the check that matters most — an entry
   without one is a label.
3. **Ordering.** Alphabetical, case-insensitive. Report the first break, not all of them.
4. **Case rule.** First word capitalised, rest lowercase. `Category Group` is the one committed
   exception — do not report it.
5. **No implementation words.** A definition naming a table, column, type, endpoint, status code
   or product name has leaked. Report with the line.
6. **Dangling boundaries.** A `NOT <Other>` pointing at a term that is neither in the glossary nor
   an obvious plain-English concept. Either the neighbour was never added or it was renamed.
7. **Duplicate senses.** Two entries whose definitions overlap enough that a reader would not know
   which to use.

**Between the glossary and the features:**

8. **Undeclared use.** A glossary term a feature's documents use often but which is missing from
   that feature's `**Terms this feature uses:**` line in `docs/features/<slug>/CONTEXT.md`.
9. **Declared but unused.** The reverse — a term on the list that the feature's documents never
   mention. Usually harmless; report it quietly.
10. **Undefined use.** A capitalised domain-looking noun that appears repeatedly across a feature's
    PRD and SAD and is in no glossary entry. This is the check that found `Ledger`, `Movement`,
    `History` and `Breakdown` — each used over fifty times with no definition.

Check 10 produces false positives. Rank by mention count, cap the list at the top handful, and
present them as candidates to review rather than as defects.

## Output

A short report in the chat, not a file — an audit is a moment-in-time read and a stale audit file
is worse than none.

```md
## Glossary audit — <N> terms

**Format** — <N>/<N> entries well-formed · <N> missing a NOT-boundary
**Ordering** — alphabetical ✓ | first break at `<Term>`
**Implementation leaks** — none | `<Term>`: names `<word>`
**Dangling boundaries** — none | `<Term>` → `<Other>` is not defined

**Per feature**
| Feature | Declared | Undeclared use | Declared but unused |
|---|---|---|---|
| core-ledger | 12 | — | — |

**Candidates for a definition** (used often, defined nowhere)
| Word | Mentions | Where |
|---|---|---|
| <Word> | <N> | <feature> PRD, SAD |

**Verdict:** PASS | PASS (with findings) | FAIL
```

`FAIL` is for a broken format or a missing boundary — things that make the file unreadable to the
skills that parse it by shape. Everything else is a finding.

End with the one next step, not a list: the single highest-value fix, and the command that makes
it — usually `/finnavis-fix-term <Word>` for the top candidate.
