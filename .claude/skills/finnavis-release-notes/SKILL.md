---
name: finnavis-release-notes
description: >
  Writes the release notes of one FinNavis release (a solo, self-hosted .NET pet project): a short,
  benefit-first story in plain English for the owner as a user of the app — "what you can do now" —
  built from that release's section in CHANGELOG.md, with the user's own wording taken from the
  feature's PRD §4 (User stories). Same release as the changelog, different reader: the changelog
  is the developer's list by section, the release notes are prose with no endpoint paths, error
  codes, table names, file names or commit hashes. Keeps only what a user notices: install steps
  such as a migration to apply stay in CHANGELOG.md.
  Prints the notes to the chat to seed a GitHub Release description; writes no file and commits
  nothing. Run by hand only: "/finnavis-release-notes", "/finnavis-release-notes 0.1.0".
  Refuses when CHANGELOG.md or the asked release is missing — run finnavis-changelog first.
argument-hint: "[version, default: the newest release in CHANGELOG.md]"
allowed-tools: Read, Grep, Glob
user-invocable: true
disable-model-invocation: true
---

# Write release notes — FinNavis

The changelog and the release notes describe the same release for two different readers.

- `CHANGELOG.md` is for the developer. It lists every notable change by Keep a Changelog section,
  with endpoint paths and error codes. `finnavis-changelog` writes it.
- The release notes are for the person who uses the app. They answer one question: *what can I do
  now that I could not do before, and why is that good for me?* They read as a short story, not a
  list by section.

The reader is the owner. They read English as a second language. Write simply: short sentences,
one idea per sentence, plain words. No idioms, no marketing language ("powerful", "seamless",
"we're excited").

Talk to the user in the language they wrote in. The release notes are always English.

## Arguments

- No argument: the newest release in `CHANGELOG.md` — the first `## [x.y.z] - YYYY-MM-DD` heading.
- A version, `0.1.0` or `v0.1.0`: that release.

## Step 1 — Find the release, or refuse

Read `CHANGELOG.md` at the repository root.

- No file, or no release heading in it — stop. Say that the notes are built from the changelog,
  and suggest `/finnavis-changelog` first.
- The asked version is not in the file — stop, and list the versions that are.
- The release section is everything from its heading to the next `## [` heading.

The changelog is the only source of *what* changed. It is already curated: internal work, refactors,
tests and `.claude/` are out of it. Do not read commits or the diff to add anything to it. If the
section looks wrong or incomplete, say so in the report — the fix belongs in the changelog, not in
the notes.

## Step 2 — Find the user's words

The changelog says what the API does. The PRD says why the owner wanted it. Use the PRD for the
benefit, so the notes speak the owner's language and not the code's.

- Find the feature from the entries: an endpoint path, an error module (`Import`,
  `MappingProfile`), a migration name. Match it to a folder in `docs/features/<slug>/`.
- Read `docs/features/<slug>/PRD.md` §4 (User stories). The *So that* line of each story is the
  benefit. US-06 of csv-import says "so that one bad rule does not cost me an evening" — that is
  the reason to mention undo.
- Read `docs/GLOSSARY.md` for the domain words the notes use (load, statement shape, set aside).
  Use a term as the glossary defines it. Do not invent a new name for a known thing.
- If no PRD matches, write from the changelog alone and say so in the report.

## Step 3 — Keep what a user notices

Group the entries by what the reader gets, not one sentence per entry. Five endpoints that together
let the owner import a statement are one capability.

**In:**

- a new capability — something the owner can now do;
- a change in how an existing capability behaves, when the owner would notice it;
- a fix of a defect the owner could have hit, described by the relief it brings;
- a security fix, in one plain sentence about what is now protected. Never describe how the
  problem could be used.
- a **breaking change**, a **removed** capability or a **deprecated** one. These go first, under
  "What you need to know", because the reader has to act on them.

**Out:**

- the error catalogue, status codes and request rules (strict JSON, paging parameters). A user meets
  a refusal message, not a code list. If one refusal really matters to the reader, say what the app
  now protects them from: "a load that stops part-way leaves nothing behind".
- table names, column names, endpoint paths, field names, file names, commit hashes, task ids, AC
  ids.
- anything only a developer reading the code would care about.
- install steps: a migration to apply, a new setting, a new environment variable, any command.
  They are work for whoever installs the release, not for the user of the app, and the changelog
  entry already carries them. Once a deploy applies migrations, a manual step in the notes would
  also be wrong.

## Step 4 — Write the notes

Follow `templates/release-notes.md`. Rules:

- Lead with the headline: the one change that matters most, in one or two sentences.
- Then one short paragraph per capability: what the reader can do, then why it helps them.
  Address the reader as "you".
- Prose, not bullets. If the result looks like an Added/Fixed list, you wrote a changelog again.
- Every sentence must make sense to someone who never saw the code.
- Short. A release with one feature fits in about 150–250 words. Do not pad a small release.
- No closing thank-you or sales line. End when the content ends.

## Step 5 — Print and report

Print the notes to the chat inside one fenced `markdown` block, so the user can copy them into a
GitHub Release description. Write no file and commit nothing.

After the block, in the user's language, list briefly:

1. The release used: version and date.
2. The PRD stories used for the benefit wording, or that none matched.
3. What was left out of the changelog section and why, grouped (error codes, request rules,
   storage details).
4. Any judgement call: entries merged into one paragraph, an entry put under "What you need to
   know", a gap in the changelog you noticed.

If the user changes the wording, apply it and print the full notes again. If the change is a rule
this skill does not cover yet, say so with ⚠️ — it may belong in this file.
