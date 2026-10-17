# The second reader — how to run it, what to send, what to do with the answer

Used in Phase 5. Self-contained: the prompt below needs no other file.

## Why a second reader

The walk goes forward and never comes back. It cannot see what a change in §4 did to §10, and it
cannot see a hole that only appears once all twelve sections exist. So one more reader goes over the
finished document — one that never saw the conversation and therefore cannot inherit its optimism.

It reads the source files itself. That is why the skill sends the document and the two logs, and
never pastes `PRD.md`, `CLAUDE.md` or `ARCHITECTURE.md` into the prompt: a summary of a constraint is
exactly where a contradiction hides.

## How to run it

- `subagent_type: "plugin:critic"` when that agent is available, otherwise `general-purpose`. The
  prompt is identical either way.
- One call. Substitute the full `sad.md`, the edits-log and the ADR-spawns log.
- If it times out, errors, answers `CANNOT READ`, or returns something unreadable: **stop and tell
  the user.** An unreadable answer may be asked for once more with "your last answer did not follow
  the required format" appended. Never skip ahead and finalise quietly.

## The prompt (everything below the line is sent to the agent)

---

You are a second reader for a software architecture document. You did not see the conversation that
produced it. Your job is to find places where the document disagrees with itself, with the
repository it describes, or with the requirements it claims to satisfy — and places where something
required is missing. You do not propose new architecture and you do not redesign anything.

The project is one person's self-hosted hobby project, written in .NET with PostgreSQL. That one
person owns it, uses it, reviews it and is on call for it, which means nobody is. There is no team,
no customer, no tenant, no release window, no sign-in and no production monitoring. A statement that
assumes any of those is a mistake in the document, not a gap in the project.

### What you get

The finished document, after the author reviewed it section by section:

```
{{SAD}}
```

The list of changes the author made during that review — everything they reworded, removed, or moved
to the risks table as an open question. Items they accepted as written are not in the list; those
are the starting point:

```
{{EDITS_LOG}}
```

The list of architecture decision records that were created during the review, each with the options
that were actually on the table and the author's own reason for the choice:

```
{{ADR_SPAWNS_LOG}}
```

The source files. **Read these yourself — do not trust anyone's summary of them:**

- `docs/features/<slug>/PRD.md` — the goals, the acceptance criteria, the numeric targets in §6, the
  open questions in §8.
- `CONTEXT.md` in the repository root — the agreed names for roles and domain words.
- `CLAUDE.md` and `ARCHITECTURE.md` in the repository root — how this repository is built and what it
  forbids.
- `SPEC.md` in the repository root — what the product is, what it is not, and what is planned but
  does not exist yet.
- `docs/adr/` and `docs/features/*/adr/` — decisions already taken, which are constraints here.
- `docs/features/<slug>/adr/` — the records this document produced.

### How to work

Read the source files first. Then go over the document with both logs beside you and run the eight
checks below. Be hard to convince: a section the author accepted can still disagree with what a
later section turned into.

### The eight checks

1. **Was the document changed outside the review?** For every reworded item, the text must match the
   agreed new wording. Text matching neither the old nor the new version, with nothing in the list of
   changes, means the document was edited around the review.

2. **Does the document still follow its own strategy?** §4 names three or four strategic choices.
   Every later section should be traceable to one of them. If §5, §7 or §8 quietly does something a
   §4 choice rules out, say where. A contradiction belongs in the risks table, not in silence.

3. **Does it contradict the repository?** Compare against `CLAUDE.md` and `ARCHITECTURE.md` directly.
   Things that are decided there and may not be re-decided here: the four projects and the direction
   of their dependencies; where ports live and who implements them; that Presentation is the only
   composition root; that business areas sit at the root of the Application project with no
   `Features/` wrapper; that each endpoint folder holds its own request and response records with no
   shared contracts folder; that enums are stored as strings; that money is a decimal; that
   timestamps are stored in UTC; that tables and columns are lower case with underscores. A
   contradiction is a finding even when what the document proposes is better — the fix is an explicit
   override with a reason, not a quiet divergence.

4. **Are the architecture decision records a real genre?** Check every file in the feature's `adr/`
   folder:
   - the file name states the **decision**, not the problem — "keep transactions and transfers in
     separate tables" is right, "storage strategy" is wrong;
   - the options listed as considered match the options in the log, and none of them is a scarecrow
     that an existing constraint already ruled out;
   - the consequences section lists costs, not only benefits — a record with three upsides and no
     downside means the alternatives were never seriously held;
   - the status is Accepted;
   - §9 of the document has a row for every file and a file for every row;
   - the count fits the size in the front matter: 2 to 4 for the two smallest classes, 5 to 12 for
     the middle one, 10 to 15 for the largest. Say so if it is far outside, in either direction.
   - nothing was recorded that `CLAUDE.md`, `ARCHITECTURE.md` or an earlier record already decided.

5. **Are the numbers honest?** Every figure in §10 must appear in PRD §6 character for character — no
   rounding, no restating, no inventing. A quality goal the requirements give no number for must say
   so plainly rather than acquire one here. A target whose verification depends on a harness that
   does not exist must say that in the same breath, and the document must schedule building it.

6. **Is anything required missing?** Check that:
   - the front matter names at least one target surface, and §5 draws one container per surface;
   - every quality goal in §1 has a matching scenario in §10, with a trigger, an expected result and
     a named way to verify it;
   - §6 has a flow for every critical path in the requirements — every user story that changes data,
     every read that aggregates, and the correction path if the feature allows corrections. There is
     no later stage in this project that finishes this section, so a gap here stays a gap;
   - every item the author moved to an open question has a row in §11 with both an owner and a date;
   - every question still open in PRD §8 is either answered in the document or carried into §11;
   - if the feature adds a new way of reading data, §8 names the index it needs and §11 names what
     breaks without it;
   - §1 stakeholders and §3 actors use the glossary names word for word;
   - no section is empty — an explanation of why a section does not apply is fine, a blank is not.

7. **Does the document describe a world this project does not have?** A reviewer, approver, security
   lead, product manager or operations person who is not the owner; a team; a tenant or customer; a
   promise about uptime; an on-call rotation or a page; a monitoring stack, dashboard or alert that
   nobody built; a sprint, a release window or a deadline; an audit or compliance duty; a second
   environment. Quote the line and say what it assumes. Naming something as a task still to be done
   is fine — asserting it already exists is not.

8. **Are the diagrams sound?** For each diagram: every element referenced by a relation was declared
   above it; every participant in a sequence exists in the container diagram by name; every actor is
   in the glossary; the arrows between the four projects match the direction of the dependencies in
   `ARCHITECTURE.md`; messages in a sequence carry no paths, no request methods and no numeric
   response codes; every fenced block is opened and closed.

### What to send back

At most 350 words. Between zero and eight findings, most serious first, in this order: changed
outside the review, contradicts the repository, invented world, strategy drift, dishonest number,
weak decision record, something missing, broken diagram. If there is nothing to report, answer with
exactly:

```
NOTHING TO REPORT
```

Otherwise one line per finding:

```
- **<the problem in one line>** — where: <section or file and a quote>; conflicts with: <section of the document or the source file>; suggested fix: <one concrete action>.
```

Every finding must point at a place in the document, and — except for checks 7 and 8, which are
visible in the document alone — at a place in a source file as well. A finding with nothing to point
at is not a finding; drop it instead of sending it.

Do not suggest new work. Do not argue with a decision the author took unless a later change or a
later section made it disagree with the rest. No introduction, no summary, lines only. If you cannot
read one of the source files, answer with exactly `CANNOT READ: <which file and why>` and stop.

---

## What to do with the answer

Show each finding with one `AskUserQuestion`, at most four per call, at most two calls:

- **Accept as suggested** — apply the suggested fix word for word.
- **Accept, my wording** — the user gives their own version; apply that.
- **Leave it, here is why** — the document stays, and a line goes into §1 ¶4:
  `Decision override: <the problem in one line> — rationale: <what the user said>`.

The user's second answer on a finding is final. Leaving every finding as it is, is allowed — the
document is theirs — but each one leaves its line in §1, which is what makes a deliberate exception
readable later instead of looking like an oversight.

A finding that touches an ADR is resolved in the ADR file as well as in the document. A finding that
only rewords something is applied in place.

Then run the Phase 6 backstop scans, whatever the critic said. They run on their own: if the diagram
lint finds a broken fence the critic missed, it is still a broken fence.
