---
name: finnavis-changelog
description: >
  Builds the changelog of one branch of FinNavis (a solo, self-hosted .NET pet project) in the six
  sections of Keep a Changelog 1.1.0 — Added, Changed, Deprecated, Removed, Fixed, Security — and
  writes it under "## [Unreleased]" in CHANGELOG.md. Two inputs: the current work branch (its
  commits master..HEAD), or the merge commit that landed a branch on master (its commits
  <sha>^1..<sha>^2). Sorts by what the change does for a reader of the changelog, not by the commit
  type: a fix of code written on the same branch is not Fixed, a refactor with no visible effect is
  left out, and .claude/ skills, workflows and the task tracker never appear. Writes the file and
  stops; never commits. Use this whenever the user wants a changelog, a CHANGELOG entry, "what
  changed" in keepachangelog sections, or release-style notes for a branch or a merge in this repo —
  including phrasings like "собери changelog", "changelog для ветки", "changelog для мержа a1b2c3d",
  "что попадёт в CHANGELOG", "обнови CHANGELOG.md", "/finnavis-changelog" — even when they do not
  say the word changelog. Refuses on master without a merge commit, and on a commit that is not a
  merge (a squash or a fast-forward has no branch boundary to read).
argument-hint: "[merge commit sha] [--dry-run]"
allowed-tools: Bash, Read, Edit, Write, Grep, Glob
---

# Build a changelog — FinNavis

A changelog is for a person, not for a machine. Keep a Changelog 1.1.0 says it lists the *notable*
changes, grouped by what they do, newest first. `git log` already exists for everything else. So
the work of this skill is mostly judgement: which changes a reader of this repo cares about, and
which of the six sections each one belongs to.

The reader is the owner, in two roles: the person who calls the API, and the developer who builds
and runs the project. They read English as a second language. Write each entry simply: one short
sentence, one idea, plain words. Keep technical terms, endpoint paths and error codes as they are.

Talk to the user in the language they wrote in. `CHANGELOG.md` is always English.

## Arguments

- No argument: the current branch. Its commits are `master..HEAD`.
- A commit sha: the merge commit that landed a branch on `master`. Its commits are
  `<sha>^1..<sha>^2` — everything reachable from the branch tip and not from `master` before the
  merge.
- `--dry-run`: build the entries and show them. Do not touch `CHANGELOG.md`.

## Step 1 — Find the range, or refuse

**With a sha:**

```bash
git rev-parse --verify "<sha>^{commit}"
git rev-list --parents -n 1 <sha>      # the sha, then its parents
```

- It must have exactly two parents. One parent means a squash or a fast-forward: the branch
  boundary is gone, so there is nothing reliable to read. Stop and say so. `.claude/rules/git.md`
  §Pull requests requires a merge commit for exactly this reason. Do not guess a range from dates
  or commit subjects.
- More than two parents (an octopus merge) — stop and ask which parent is the branch.
- Base is `<sha>^1`, tip is `<sha>^2`. Take the branch name from the merge subject when it has one
  (`Merge pull request #12 from owner/feature/T14`, `Merge branch 'feature/T14'`).
- If `git merge-base --is-ancestor <sha> master` fails, the merge is not on local `master`. Say so
  and continue — the user may be ahead of or behind `origin`.

**Without a sha:**

- On `master`, stop. Ask for the sha of the merge commit; offer `git log --merges --oneline -10
  master` so the user can pick one.
- Base is `master`, tip is `HEAD`. If `git log master..HEAD` is empty, stop: nothing to describe.
- Uncommitted changes are not in the range. Mention them in one line and continue; the changelog
  covers commits.

## Step 2 — Collect the facts

```bash
git log --no-merges --reverse --format='--- %h %s%n%b' <base>..<tip>
git diff --stat <base>...<tip>
```

The three-dot diff starts from the merge base, so it shows what the branch did, even when `master`
moved on. Read the full diff only for the files that decide an entry — `src/`, migrations,
contracts, `build.sh`, `.github/workflows/`. For a large branch, read the stat first.

Commit messages are hints, not the answer. A `feat:` commit can carry no visible change; a `test:`
commit can reveal what the feature really does. The diff is the evidence.

**A changelog already on the branch.** If the range itself changes `CHANGELOG.md`, the branch
already has entries — from an earlier run of this skill, or written by hand. Do not count
`CHANGELOG.md` as a change, and do not add a second copy. Rebuild the branch's entries from all of
its commits, then replace the entries the branch added with the new set. Leave entries that came
from other branches alone.

## Step 3 — Decide what is notable

Group commits by the change they make, not one entry per commit. A TDD triple — `test:`, `feat:`,
`refactor:` for one behaviour — is one entry. Five commits that build one endpoint are one entry.

**In** — something the reader sees or has to act on:

- the HTTP surface: a new or changed endpoint, request or response field, status code, error
  `code` or error message;
- data: a migration, a new table or column, a change to how money, dates or enums are stored.
  If the migration also changes what happens to rows that already exist — a new foreign key with
  `ON DELETE CASCADE` or `SET NULL` on an existing table, a backfill, a dropped column — say that
  in the entry. It is the part the reader can lose data over;
- configuration: a new or renamed setting in `launchSettings.example.json`, a new environment
  variable;
- running and building: a task in `build.sh` / `build.ps1`, `scripts/`, `docker-compose.yml`,
  `Dockerfile`, CI in `.github/workflows/`, the .NET or a package version;
- behaviour without a new surface: a rule that now refuses a request it used to accept, a
  performance change the reader would notice.

**Out** — leave these out, and list them in the report so the user can see nothing was lost:

- anything under `.claude/` — skills, rules, workflows, hooks, settings;
- the task tracker: `docs/features/<slug>/tasks/` (`tracker.md`, task files, run reports);
- design documents: PRD, `sad.md`, `data-model.md`, staged SQL specs, ADRs, `ARCHITECTURE.md`,
  `CLAUDE.md`. They explain *why*; the changelog says *what*. The one exception: a document change
  that tells the reader to do something, for example a new required setting — then the setting
  is the entry, not the document;
- a refactor with no visible effect, and a change to tests only;
- new code that nothing reachable calls yet: a Domain entity, a use case, a port, a repository or
  an EF configuration with no endpoint and no build task in front of it. It becomes notable on the
  branch that makes it reachable, and that branch's entry describes it. Its migration is the
  exception — a migration changes the database the moment it is applied, so it is an entry;
- a change to `docs/features/<slug>/contracts/openapi.yaml` with no handler that serves it. The
  contract states what the code *must* do; until a handler does it, no client can see it. When the
  handler lands, the endpoint, its fields and its error codes go into that branch's entry;
- a commit that undoes another commit on the same branch — both cancel out.

If nothing is left after this step, say so, show the out-list, and do not create or change
`CHANGELOG.md`.

## Step 4 — Pick the section

Every entry goes into exactly one section. The question is always *what does this do for the
reader*, measured against `master` before the branch — not against the branch's own first commit.

| Section | Use it when | FinNavis example |
|---|---|---|
| **Added** | Something new exists that did not exist on `master` before. | A new endpoint `POST /api/v1/imports`. A new error code `Import.FileEmpty`. A new `./build.sh migrate` task. |
| **Changed** | Something that already existed behaves differently. | A response field renamed. A `400` that is now a `409`. A different refusal message. A new default. |
| **Deprecated** | Something still works, but is marked to go away later. | An endpoint kept for now with a replacement next to it. |
| **Removed** | Something that existed is gone. | An endpoint, a field, a build task or a setting deleted. |
| **Fixed** | A defect that is **on `master` before this branch** no longer happens. | A balance off by one cent. A cursor that skipped a row. |
| **Security** | A change that closes a vulnerability or a data-exposure risk. | Input that reached a query unchecked. A package bump for a known CVE. A secret removed from a file. |

Rules that are easy to get wrong:

- **A fix of the branch's own work is not Fixed.** If the broken code was written on this branch,
  `master` never had the bug. Fold the fix into the Added or Changed entry it belongs to. To check,
  look at the code at the base: `git show <base>:<path>`. If the path or the code is not there,
  the bug never shipped.
- **A security fix goes to Security, not Fixed.** Keep a Changelog keeps it separate so a reader
  can find it fast.
- **A breaking change** — one that makes an existing client or script fail — goes to Changed or
  Removed and starts with `**BREAKING:**`. A commit with `!` or a `BREAKING CHANGE:` footer is
  breaking. A change can also be breaking without the marker: a removed field is, whatever the
  commit says.
- When an entry fits two sections, it is usually two changes. Split it, or pick the one that
  matters more to the reader and say why in the report.

## Step 5 — Write the entries

One line per entry. Describe the effect, not the code change: what the reader can now do, or what
now happens differently. Name the endpoint, field, error code, table or command in backticks.

- Good: ``Import a bank statement with `POST /api/v1/imports`. Rows that match an existing movement are set aside, not loaded twice.``
- Good: ``**BREAKING:** `GET /api/v1/transactions` no longer returns transfers. Use `GET /api/v1/history`.``
- Bad: `Add ImportRunConfiguration and EfImportQueries` — that is the diff, not the effect.
- Bad: `Fix T14 tests` — process noise. No task ids, no AC ids, no "finding N".
- Do not start with the section's own verb. Under `### Added`, write `` `POST /api/v1/imports` imports a bank statement``
  or `Import a bank statement …`, not `Added …` or `Add support for …`.

## Step 6 — Write CHANGELOG.md

The file is at the repository root. If it does not exist, create it with this header:

```markdown
# Changelog

All notable changes to this project are documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/).
The project has no version numbers yet, so every change is listed under [Unreleased].

## [Unreleased]
```

Then:

- Put the entries under `## [Unreleased]`. Sections go in this fixed order, each as
  `### <Section>`: Added, Changed, Deprecated, Removed, Fixed, Security.
- Leave out a section with no entries. Do not write an empty heading.
- If a section already exists under `[Unreleased]`, add to it. Do not reorder or rewrite entries
  that came from other branches.
- Never touch a released version below `[Unreleased]`.

Before the write, check the current branch with `git branch --show-current`. The file is written
into the working tree, so it lands on whatever branch is checked out:

- On `master` — write it, and say that the change has to move to a new branch before it is
  committed.
- On another branch in merge mode — that branch may have nothing to do with the merge. Say which
  branch it is, and write only after the user confirms.

In `--dry-run`, skip the write and show the block that would be written.

**Do not commit.** The user reviews the file and commits it. In branch mode the commit belongs on
the branch, so it lands with the branch. In merge mode the user is usually on `master`, where
`.claude/rules/git.md` forbids direct work — the user moves the change to a branch.

## Step 7 — Report

Show, in the user's language:

1. The range: base, tip, number of commits, and the branch name if known.
2. The entries, by section, exactly as written to the file.
3. The out-list: what was left out and why, grouped by reason (`.claude/` workflows — 4 commits,
   the task tracker, inner-ring code not reachable yet). One commit often touches several groups,
   so count by commit only where it is clean; otherwise name the files or the change. Short, not one
   line per commit.
4. Any judgement call the user may disagree with: an entry that could be Fixed or Added, a change
   you marked breaking without a marker, a group of commits you merged into one entry.
5. Next step: `git diff CHANGELOG.md`, then commit it — `docs: add the changelog for <branch>`.

If the user changes an entry or a section after the report, apply it. If the change is a rule this
skill does not cover yet, say so with ⚠️ — it may belong in this file.
