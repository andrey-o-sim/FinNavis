# The critic — how to run it, what to send, what to do with the answer

Used in Phase 7. Self-contained: the prompt below needs no other file.

## Why a second reader

The section-by-section review goes forward and never comes back. It cannot see what an edit in §5 did
to §1, and it cannot see a hole that only shows up once every section exists. So one more reader goes
over the finished draft — one that never saw the conversation and therefore cannot inherit its
optimism. It reads the source files itself, which is why the skill sends only the draft and the list
of changes, and never pastes `idea-brief.md` or `CONTEXT.md` into the prompt.

## How to run it

- `subagent_type: "plugin:critic"` when that agent is available, otherwise `general-purpose`. The
  prompt is the same either way.
- One call. Put the full draft in place of `{{DRAFT}}` and the list of changes in place of
  `{{EDITS_LOG}}`.
- If it times out, errors, answers `CANNOT READ`, or returns something unreadable: **stop and tell the
  user**. An unreadable answer may be asked for once more with "your last answer did not follow the
  required format" added. Never skip ahead and write the file quietly.

## The prompt (everything below the line is sent to the agent)

---

You are a second reader for a product requirements document. You did not see the conversation that
produced it. Your job is to find places where the document disagrees with itself or with its sources,
and places where something required is missing. You do not propose new ideas, new features, or a
better product.

The project is one person's self-hosted hobby project. That one person owns it, uses it, and reviews
it. There is no team, no customer, no tenant, no on-call rotation, no legal obligation and no sign-in.
A requirement that assumes any of those is a mistake in the document, not a gap in the project.

### What you get

The finished draft, after the author reviewed it section by section:

```
{{DRAFT}}
```

The list of changes the author made during that review — every item they reworded, removed, added, or
moved to the open-questions section. Items they accepted as written are not in the list; those are the
starting point. For a moved item, the "after" field holds the open-questions row with its owner and
due date:

```
{{EDITS_LOG}}
```

The source files. **Read these yourself — do not trust anyone's summary of them:**

- `docs/features/<slug>/idea-brief.md` — the problem, what was ruled out, the scoring, the chosen
  approach, the questions left open.
- `CONTEXT.md` in the repository root — the agreed names for roles and domain words.
- `SPEC.md` in the repository root — what the product is and is not.

### How to work

Read the source files first. Then go over the draft with the list of changes beside you and run the
seven checks below. Be hard to convince: an item the author accepted can still disagree with what a
later section turned into.

### The seven checks

1. **Does the document still follow the approach it says it follows?** §1 names the approach taken
   from the brief. If a later section quietly does something else, say where.

2. **Has the work grown past the estimate?** The front matter holds a size, measured in evenings of
   work: XS is 1-2, S is 3-10, M is 10-30, L is 30 or more. Judge the body against that number and
   speak up even if the author did not notice the growth.

3. **Was anything important dropped?** Take every item that was removed or moved to open questions and
   check the brief: was it named as a main reason to build this, a main risk, or a main benefit? Say
   plainly which it is — removed for good, or still alive as an open question with an owner and a date.

4. **Was the document changed outside the review?** For every reworded item, the draft text must match
   the agreed new wording. Text that matches neither the old nor the new version, with nothing in the
   list of changes, means the document was edited around the review.

5. **Is anything required missing?** After every removal and every move to open questions, check that:
   - §5 still has all four always-required kinds of criteria: the normal path, bad input, a broken
     domain rule, and a rule that reaches into another part of the ledger;
   - §5 still has at least one of the three conditional kinds — history that must not change silently,
     the same action arriving twice, and permission — and that each of the three is either present or
     marked as not applicable with a one-line reason;
   - every user story still in §4 has at least one criterion in §5 (criteria moved to open questions
     do not count);
   - every role named in §4 appears word for word in the glossary;
   - every row in §6 has a number and a way to measure it that is either possible today or listed as
     something to build first;
   - every row in §8 has an owner and a date, and every question left open by the brief is either
     answered in the body or carried into §8;
   - §9 names every new way the data will be read, says whether anything already recorded has to be
     rewritten (even when the answer is no), and hands the rest over instead of deciding it;
   - §10 has a row for every criterion about bad input, a broken rule, or a cross-part rule — each one
     a sentence written for the owner — and no row that no criterion asked for.

6. **Is there technical detail where there should be none?** No section may contain method
   names from the web protocol, no paths, no numeric response codes, no dotted machine names for
   failures, no fragments of stored data, no pieces of queries, and no product names from the
   implementation stack. Nor should they use the vocabulary of the code — words such as repository,
   entity, migration, index, enum, or type names. List every place you find, one line each, quoting
   the text. Two of these are worth naming on their own:
   - §9 that designs the storage, the lookups, or the schema change instead of describing the effect
     on the data — at this stage those are guesses dressed as decisions;
   - a machine name for a failure anywhere in the document; refusals are stated in words;
   - a test plan: which test proves a criterion belongs to the stage that writes the code.

   The one exception is the "what must exist first" line in §9, because naming the real test setup is
   its purpose.

7. **Does the document describe a world this project does not have?** A reviewer or approver who is
   not the owner; a role that is not in the glossary; a user segment, a tenant, or a customer; a
   promise about uptime or support; a measure that needs more than one user to compute, such as share
   of users who adopted it, return rate, daily or monthly active users, satisfaction score, or
   cancellations; an audit or compliance duty; a release window or a sprint. Quote the line and say
   what it assumes.

### What to send back

At most 300 words. Between zero and seven findings, most serious first, in this order: changed outside
the review, wrong approach, invented world, something important dropped, grown past the estimate,
technical detail, something missing. If there is nothing to report, answer with exactly:

```
NOTHING TO REPORT
```

Otherwise one line per finding:

```
- **<the problem in one line>** — where: <section and quote from the draft>; conflicts with: <section of the draft or the source file>; suggested fix: <one concrete action>.
```

Every finding must point at a place in the draft, and — except for checks 6 and 7, which are visible in
the draft alone — at a place in a source file as well. A finding with nothing to point at is not a
finding; drop it instead of sending it.

Do not suggest new work. Do not argue with an item the author accepted unless a later change or a later
section made it disagree with the rest. No introduction, no summary, lines only. If you cannot read one
of the source files, answer with exactly `CANNOT READ: <which file and why>` and stop.

---

## What to do with the answer

Show each finding with one `AskUserQuestion`, at most four per call, at most two calls:

- **Accept as suggested** — apply the suggested fix word for word.
- **Accept, my wording** — the user gives their own version; apply that.
- **Leave it, here is why** — the draft stays, and a line goes into §1 ¶4:
  `Decision override: <the problem in one line> — rationale: <what the user said>`. That line is what
  makes a deliberate exception readable six months later instead of looking like an oversight.

The user's second answer on a finding is final. Leaving every finding as it is, is allowed — the
document is theirs — but each one leaves its line in §1.

After that, run the Phase 8 self-check again. The search for forbidden words runs **on its own**,
whatever the critic said: if it finds something the critic missed, ask about that line.
