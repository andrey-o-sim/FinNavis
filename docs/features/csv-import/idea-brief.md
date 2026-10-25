---
status: Confirmed
owner: "Andrey Simonenko"
reviewers: []
updated_at: "2026-10-10"
feature_size: TBD
stage: "01"
depth: medium
ticket: "none"
value_score:
  rice: 0.33
  state: confirmed
  confirmed_at: "2026-10-10"
feasibility_state: confirmed
evenings_estimate: 30
---

<!-- Stage 01. Written by the finnavis-interview skill (.claude/skills/finnavis-interview). -->

# Idea Brief — CSV Import

## 1. Raw idea

One import path for two jobs: a one-time move of the old history out of the tracker the owner
is leaving, and a regular monthly load of a statement file. In his own words: «записываю
расстраты по ходу дела, но часть из них приходится записывать отдельно в конце месяца исходя
из банковской выписки». On what the file carries: «категории есть, но они не совпадают 1 в 1 с
моими и у меня их больше».

## 2. Problem

The owner records spending as it happens, up to 30 rows a month, and is content with that part.
The rest he cannot record that way, so at the end of the month he reads a bank statement and
re-types that batch by hand. The two streams land on the same accounts: a row typed on the day
and the statement line for the same purchase describe one movement of money. On hand already:
an export from the old tracker, a statement from his bank, and a spreadsheet he keeps himself.

The stated metric is **zero manual entry** of that batch. It is reachable for typing, not for
filing: the statement carries the bank's categories, he has more than the bank, and they do not
line up one to one, and no rule splits that without him. So this ends the typing and can propose
a category, but final filing of some rows stays manual until a later feature earns it. Of the
eight gaps listed against the old tracker in `core-ledger` §2, this closes gap #7 only.

## 3. Who it serves

One person: the owner, also the only user. He meets this feature at month end, filing rows from
a statement weeks after the fact. That is the hard moment — he no longer remembers what a line
was, so a row he cannot explain is worse here than anywhere else in the app.

## 4. Why now

The old tracker exports but cannot import, so nothing outside creates urgency. The trigger is
ordering. `core-ledger` §15 already asks whether a record should carry its own identifier from
day one, so that duplicate prevention, this feature and message capture need no later pass over
existing records. That cannot be answered without knowing what import needs, and import needs
it on the hand-entry path too.

## 5. Out of scope

- Capturing spending from chat messages, email or photographs of receipts.
- Reading free-form text ("продукты 450") instead of a file.
- Connecting to a bank directly. Files only, handed over by the owner.
- Learning categories from corrections over time (§14, approach B).
- Any screen. There is no client yet; what the owner touches here is a direct call.
- Multi-currency, budgets, forecasts, charts.

## 6. Competitive analysis

| # | Product · URL | Relevant features | Value | Gap |
|---|---|---|:---:|---|
| 1 | MoneyOk · https://apps.apple.com/us/app/money-ok-personal-finance/id606031670 | Export to file, backup and restore | 1 | No import at all — the migration is one way out |
| 2 | YNAB · https://support.ynab.com/en_us/file-based-import-a-guide-Bkj4Sszyo | Row identity from amount, date and occurrence; matches loaded rows against hand-typed ones within ten days | 4 | A deleted row can never be loaded again; files cannot carry categories |
| 3 | Actual Budget · https://actualbudget.org/docs/transactions/importing/ | Exact match on identity, else date, amount and similar payee within five days | 3 | Without an identity it guesses, and never explains the guess |
| 4 | Firefly III · https://github.com/firefly-iii/firefly-iii/issues/3421 | Self-hosted; saved mapping per file shape; identity hashed or read from a column | 3 | The hash covers names resolved on first load, so the load that **creates** the accounts is the one whose repeat is missed |
| 5 | GnuCash · https://www.gnucash.org/docs/v5/C/gnucash-guide/importing-from-files.html | Saved mapping presets; a matcher listing possible duplicates | 3 | Every row is confirmed by hand — the opposite of what he asked for |

Searched 2026-10-10; query per row was the product name plus "CSV import duplicate detection",
plus "import same file twice" for rows 1 and 4.

**Findings.** Everyone imports, and everyone solves re-loading by making up a stable row
identity. The owner's case is narrower: the file overlaps rows he already typed himself. Only
rows 2 and 3 match a loaded row against a hand-typed one, within ten and five days, and neither
is self-hosted or explains the match. Row 4 is self-hosted but recognises only rows it loaded
itself, and breaks on the load that creates the accounts — here, the migration. **Self-hosted,
matching against hand entry, and able to explain the skip: that combination is the wedge.**

## 7. Strategic approaches

### Approach A — Drop-in statement loader
- **Thesis**: hand over a file, name the account; every row becomes an entry, rows already
  present are skipped without asking.
- **For whom**: the owner on the first evening of the month, facing the closed month's statement.
- **Outcome metric**: rows re-typed by hand: ~30 per month → 0, over three months.
- **Key trade-off**: no category proposal, no matching against hand-typed rows, no pairing of
  transfers; filing stays manual forever.
- **Effort signal**: S · **Recommended?** ◯

### Approach B — Rules that learn from you
- **Thesis**: the import learns from every correction, so each statement needs less attention
  than the last.
- **For whom**: the owner wanting the file filed correctly without touching a row.
- **Outcome metric**: rows filed correctly without editing: ~0% at first load → 90% by the
  fourth statement.
- **Key trade-off**: a memory of naming decisions and of every raw row ever read; a wrong rule
  files quietly wrong until noticed.
- **Effort signal**: L · **Recommended?** ◯

### Approach C — Remembered import rules
- **Thesis**: one path for any file; the app proposes how each row maps, the owner confirms the
  mapping once per file shape, and every loaded record carries where it came from, so a second
  load lands silently.
- **For whom**: the owner at month end, and the same owner once, moving three years in one load.
- **Outcome metric**: rows typed by hand: ~30 per month → 0, the only action being "hand over
  the file" after the first confirmation for that shape.
- **Key trade-off**: a new file shape costs a few minutes of confirming, and the app is never
  smarter than that confirmation.
- **Effort signal**: M · **Recommended?** ◯ — deferred, see §13.

## 8. Three-lens review

### Builder — tonight
- Not three features: one write path with different amounts of confirmation in front of it.
  Only one decision cannot be deferred — what makes two rows the same row.
- A's cost is correctness, not build: a fingerprint of date, amount and text eats a real second
  identical purchase, and the step that would catch it was removed.
- A already holds two file shapes, so it grows C's stored concept by accident and buries it in
  code. C costs that one concept, not a second subsystem.

### Future-me — six months out
- A is the approach whose next feature starts by revisiting every existing record: nothing on a
  loaded row says it was loaded, and one-account-per-file never fits a multi-account export.
- C is the only foundation: origin sits on every record from the first load, which is what
  duplicate prevention, export and message capture all need.

### User-me — Tuesday evening
- A is one step tonight and wrong every month after: it does not match against rows typed on
  the day, so part of the month arrives twice and has to be cleaned by hand.
- A makes the migration harder — one file naming one account, against an export covering several
  accounts and three years.
- C is two steps the first time and one after, and the skip is a fact, not a guess.

### Synthesis matrix

|        | Builder | Future-me | User-me |
|--------|:-------:|:---------:|:-------:|
| App. A | **0** cheap build, hidden identity cost forever | **−** next feature revisits every existing record | **−** duplicates every row typed that month |
| App. B | **−** second subsystem, learning target not testable | **−** evenings spent on unrequested unfinished learning | **−** confident numbers nobody can explain afterwards |
| App. C | **+** one stored concept, mapping failures visible | **+** origin on day one unlocks later | **+** explainable skip, one confirmation per shape |

## 9. Trade-offs and edge cases

| Approach | Pros | Cons |
|---|---|---|
| A | Smallest build, fastest first load | Does not match rows typed on the day, so part of every month arrives twice; filing manual forever; next feature pays for the missing origin |
| B | Would eventually file rows unattended | Largest build, untestable target, contradicts "no review" and "nothing to learn" |
| C | Silent and explainable re-load; survives the load that creates everything | One mapping confirmation per file shape — a review step he did not ask for |

### Edge cases

- The same file loaded twice after the owner corrected an amount by hand.
- Two real identical purchases, same shop, same amount, same day, inside one file.
- The first load creates the accounts and categories, and is then re-run.
- A move between two of the owner's own accounts: leaving one statement, arriving in the other,
  counted as spending twice.
- A file whose shape changed: a new column shifts every value, and all of them stay plausible.

## 10. Risks

- **Top risk (devil's advocate).** This feature writes the shape of the first record before the
  ledger exists. If import decides what a record carries, the ledger's first contract is built
  around a monthly file rather than hand entry, which is 100% of use today. Signal: the record
  definition cannot be finished without first answering an import question.
- **The statement overlaps rows already typed by hand.** Both streams land on the same accounts,
  so a purchase recorded on the day appears again in the month-end file. Unless loaded rows are
  matched against hand-typed ones, every import doubles part of the month. Signal: the month
  total is visibly higher than the statement's, and the extra equals what he typed himself.
- **Silent skip on a key the bank may not provide.** Without a stable operation number, identity
  falls back to date, amount and text, which legitimately repeat, and the second coffee
  disappears silently. Signal: the month total is lower than the statement's.
- **Migration and monthly load are two features under one name.** One is several accounts,
  already categorised, run once; the other is one account, raw rows, recurring. Signal: the
  history lands and thousands of rows sit uncategorised.
- **Twelve runs a year against a format he does not control.** Signal: the bank changes its
  export, re-reading the old code costs more than typing the rows, so the importer stays unused.
- **The two streams drift apart.** The same purchase typed on the day and read from the
  statement can differ in date or amount — a card hold settles later, a rounding differs. Then
  the match fails and both survive. Signal: a balance that cannot be explained by any single row.

## 11. RICE — confirmed

- **Reach (R)**: 1 — one owner, one user. Fixed on this project, never varied.
- **Impact (I)**: 2 — closes the only recurring manual chore named in §2, and the Future-me lens
  (§8) finds it the only approach that is a foundation for three later features.
- **Confidence (C)**: 0.5 — no real file has been opened, and the silent-skip decision was made
  before anyone knew whether a statement carries a stable operation number (§15).
- **Effort (E)**: 3 person-weeks — signal M from Approach C. ≈ 30 evenings. The ledger itself is
  not included and does not exist yet.
- **RICE = 1 × 2 × 0.5 / 3 ≈ 0.33**
- **Honest caveat**: at Reach 1 the absolute score means nothing. It only ranks this against the
  other features of this project, which all share Reach 1 — here above `core-ledger` at 0.27,
  while still depending on it.
- **State**: confirmed

## 12. Feasibility — confirmed

- [☐] **Tech**: no adjacent module to cite. `Application/Features` is empty,
  `Infrastructure/Persistence/Migrations` is empty, and `core-ledger` exists as a brief, not as
  code. Confirmed by the owner as a **hard ordering constraint**: csv-import cannot start before
  `core-ledger`. That is sequence, not preference.
- [☐] **Evenings**: marked TBD by the owner. Nine commits, all scaffolding and documents, so 30
  evenings comes from the effort signal, not a measured pace.
- [☐] **Interest**: his own answer was "I want it to just work". No learning goal here.
- **State**: confirmed — 0 of 3 ☑, accepted as an honest reading. The empty boxes are why §13
  defers and why §5 is kept short.

## 13. Recommendation

**Selected: TBD — the choice between A and C is deliberately deferred.**

The standing recommendation is **Approach C — Remembered import rules**, and it holds unless the
files say otherwise. §6 shows the wedge no researched product offers: the two tools that match a
loaded row against a hand-typed one are not self-hosted and never explain the match, while the
self-hosted one breaks on the load that creates the accounts. §8 gives C a plus in all three
lenses — the only row that does — while A is marked
down by Future-me for making the next feature revisit every existing record. §11 puts it at 0.33,
above `core-ledger`, with Effort 3 against B's L for capability the owner explicitly does not
want. But §12 is 0 of 3 ☑, Confidence is 0.5 for one reason — no real file has been opened — and
the owner chose to wait rather than fix the approach on an assumption. That is the correct call:
four of the six risks in §10 are the same unresolved question about row identity, and one real
statement answers it in one evening.

**Locked-in pointer**: the next stage inherits the problem, the §5 boundaries, the RICE and the
feasibility reading as settled, plus one firm constraint — a loaded record carries where it came
from, and that decision belongs to `core-ledger`, not here. It does not inherit a chosen
approach, a file format, or a rule for what makes two rows the same.

## 14. Parked and rejected

| # | Approach | Status | Reason | Revisit trigger |
|---|---|:---:|---|---|
| A | Drop-in statement loader | parked | Does not match the file against rows typed on the day, so it doubles part of every month; the next feature also pays with a pass over every existing record | If the real files show a stable operation number that the hand-entry path can carry too |
| B | Rules that learn from you | rejected | Needs corrections to learn from, from an owner who asked for no review step and wants nothing to learn here | — |
| — | Confirming every row in a matcher | rejected | Settled in the interview: already-entered rows are skipped without asking | — |

## 15. Open questions

- [ ] Open a real bank statement and a real export from the old tracker. Does
      either carry a stable operation number? Everything below depends on it. Owner: Andrey,
      due: before the approach is chosen.
- [ ] Should a record carry its own identifier from day one? Raised in `core-ledger` §15; this
      interview turns it into a constraint, and it must be answered there. Owner: Andrey, due:
      core-ledger data-model stage.
- [ ] How does a bank category become one of his, given he has more and they do not line up one
      to one? Owner: Andrey, due: before the approach is chosen.
- [ ] Should a load have a handle that allows undoing it as a whole? Owner: Andrey, due:
      architecture-design.
- [ ] A move between two of the owner's own accounts appears in both statements. Is pairing
      those rows in scope here, or later? Owner: Andrey, due: before the approach is chosen.
- [ ] "Import run" surfaced as a word for a single load but was not settled, so it is not in
      `GLOSSARY.md`. Owner: Andrey, due: when the approach is chosen.

## 16. What this unlocks

Named in `SPEC.md`, all dependent on the origin decision this interview forces:

- **Preventing duplicate entries from repeated requests** — the same row-identity question.
- **Capturing spending from messages or email** — the same write path, a different source.
- **Portable export of all data** — must state each record's origin to be a true copy.
- **Closing the old tracker** — impossible until the history moves across, and it cannot import.

Not a pure enabler: it gives relief in the first month, which is why §11 prices Impact above
`core-ledger`.

## 17. Learning goal

None, and he said so plainly: "I want it to just work." That argues directly against Approach B,
whose value is a mechanism he has no appetite to tune. On a project with no deadline, boredom
ends features more often than complexity does.

This feature is an obligation, not a pleasure. The one part that could carry interest — making a
repeated load safe and explainable — is the hard part the next stage must not quietly skip.

## Related

- `GLOSSARY.md` — glossary, canonical. Terms added by this interview: `Duplicate`, `Statement`.
- `SPEC.md` — scope and the draft requirement list.
- `ARCHITECTURE.md`, `docs/adr/0001-initial-setup.md` — layering and the dependency rule.
- `docs/features/core-ledger/idea-brief.md` — this feature depends on it, and answers its §15
  question about record identity.

## DoD self-check

- [x] 17 sections present, plus Related
- [x] No banned terms in the body (regex per references/anti-patterns.md, word boundaries on)
- [~] Length — 2488 words of prose, 2904 counting the four tables. Over the skill's 2200-word
      target by about 13%; compressed six times, further cuts would remove findings, not padding
- [x] Frontmatter status: Confirmed
- [x] §11 states Reach = 1 and carries the caveat
- [x] RICE confirmed (state: confirmed)
- [x] Feasibility confirmed (state: confirmed)
- [x] §13 rationale cites §6, §8, §11, §12
- [x] §16 filled — four later features named
- [x] §17 filled
- [x] New domain terms appended to GLOSSARY.md, each with its NOT-boundary
