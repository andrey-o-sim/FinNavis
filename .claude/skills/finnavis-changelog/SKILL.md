---
name: finnavis-changelog
description: >
  Builds the changelog of one branch or one feature of FinNavis (a solo, self-hosted .NET pet project) in the six
  sections of Keep a Changelog 1.1.0 — Added, Changed, Deprecated, Removed, Fixed, Security — and
  writes it to CHANGELOG.md as a new Semantic Versioning release, "## [0.2.0] - 2026-11-21", with
  the bump picked from the entries (breaking or Removed → major, a new or changed feature → minor,
  only fixes → patch; below 1.0.0 a breaking change bumps minor). Three inputs: the current work
  branch (its commits master..HEAD), the merge commit that landed a branch on master (its commits
  <sha>^1..<sha>^2), or --scope <feature slug> for old history that has no merge commit. Sorts by what the change does for a reader of the changelog, not by the commit
  type: a fix of code written on the same branch is not Fixed, a refactor with no visible effect is
  left out, and .claude/ skills, workflows and the task tracker never appear. Writes the file and
  stops; never commits. Use this whenever the user wants a changelog, a CHANGELOG entry, "what
  changed" in keepachangelog sections, the next version number, or release-style notes for a
  branch, a merge or a feature in this repo —
  including phrasings like "собери changelog", "changelog для ветки", "changelog для мержа a1b2c3d",
  "что попадёт в CHANGELOG", "обнови CHANGELOG.md", "какая будет версия", "changelog для фичи
  csv-import", "/finnavis-changelog" — even when they do not say the word changelog. Never creates
  a git tag: the tag comes from a GitHub workflow after the merge. Refuses on master without a merge commit, and on a commit that is not a
  merge (a squash or a fast-forward has no branch boundary to read).
argument-hint: "[merge commit sha | --scope <feature slug>] [--dry-run]"
allowed-tools: Bash, Read, Edit, Write, Grep, Glob
---

# Build a changelog — FinNavis

A changelog is for a person, not for a machine. Keep a Changelog 1.1.0 says it lists the *notable*
changes, grouped by what they do, newest first. `git log` already exists for everything else. So
the work of this skill is mostly judgement: which changes a reader of this repo cares about, and
which of the six sections each one belongs to.

One run is one release. This project has one owner and no release windows, so what lands on
`master` is released: every branch, merge or feature this skill describes gets its own version
number. There is no `[Unreleased]` section to collect entries in.

The reader is the owner, in two roles: the person who calls the API, and the developer who builds
and runs the project. They read English as a second language. Write each entry simply: one short
sentence, one idea, plain words. Keep technical terms, endpoint paths and error codes as they are.

Talk to the user in the language they wrote in. `CHANGELOG.md` is always English.

## Arguments

- No argument: the current branch. Its commits are `master..HEAD`.
- A commit sha: the merge commit that landed a branch on `master`. Its commits are
  `<sha>^1..<sha>^2` — everything reachable from the branch tip and not from `master` before the
  merge.
- `--scope <feature slug>`: old history that landed on `master` without a merge commit, before
  `.claude/rules/git.md` required one. Its commits are the ones whose Conventional Commit scope is
  that slug — `feat(csv-import): …`. Use it only for that history; for anything merged since, the
  merge commit is the reliable boundary.
- `--dry-run`: build the entries and the version and show them. Do not touch `CHANGELOG.md`.

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
- Base is `<sha>^1`, tip is `<sha>^2`. The release date is the date of the merge commit
  (`git log -1 --format=%cs <sha>`). Take the branch name from the merge subject when it has one
  (`Merge pull request #12 from owner/feature/T14`, `Merge branch 'feature/T14'`).
- If `git merge-base --is-ancestor <sha> master` fails, the merge is not on local `master`. Say so
  and continue — the user may be ahead of or behind `origin`.

**With `--scope <slug>`:**

```bash
git log --reverse --format='%h %ad %s' --date=short master | grep -E '^[0-9a-f]+ \S+ [a-z]+\(<slug>\)!?:'
```

- The slug must be a folder in `docs/features/`. If it is not, stop and list the folders.
- Take the commits of every type. `docs(<slug>)` and `chore(<slug>)` commits are read too — Step 3
  drops most of them, but the report must show they were looked at.
- A commit with another scope can still be part of the feature — for example a `fix(presentation)`
  whose body says the feature's check found it. Look for the slug in the bodies of the commits
  between the first and the last one of the set (`git log --format='%h %s%n%b' <first>^..<last>`).
  Show each such commit to the user with one line on why it may belong, and add it only after
  they confirm. Do not add one silently.
- Base for Step 4 is the parent of the first commit in the set: `<first>^`. The release date is the
  date of the last commit in the set.
- Older history can carry commits with no scope or with the pre-convention form (`03: sad §4 …`).
  They are design documents almost every time. Mention how many there are and leave them out.

**Without a sha:**

- On `master`, stop. Ask for the sha of the merge commit; offer `git log --merges --oneline -10
  master` so the user can pick one.
- Base is `master`, tip is `HEAD`. If `git log master..HEAD` is empty, stop: nothing to describe.
  The release date is today.
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
`CHANGELOG.md` as a change, and do not add a second copy. In merge mode this is the normal case
— the branch wrote its release before the merge. Do not write it again: check that the release in
`CHANGELOG.md` covers the branch's changes, report any gap, and stop. There is nothing to write. In branch
mode, rebuild the branch's entries from all of
its commits, then replace the release section the branch added with the new one. Keep its version
number unless the new entries call for a different bump. Leave releases that came from other
branches alone.

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

## Step 6 — Pick the version

Versions follow [Semantic Versioning 2.0.0](https://semver.org/spec/v2.0.0.html): `MAJOR.MINOR.PATCH`.

**The current version** is the newest `## [x.y.z]` heading. Read it from two places and take the
higher one:

- `CHANGELOG.md` in the working tree;
- `git show master:CHANGELOG.md` — a branch cut before another branch was merged does not see that
  release yet, and two branches must not both claim the same number.

No version anywhere means this is the first release: it is `0.1.0`, whatever the entries are.

**The bump** comes from the strongest entry:

| The entries contain | From 1.0.0 on | Below 1.0.0 |
|---|---|---|
| a `**BREAKING:**` entry, or anything in Removed | MAJOR — `1.4.2` → `2.0.0` | MINOR — `0.3.2` → `0.4.0` |
| anything in Added, Changed or Deprecated | MINOR — `1.4.2` → `1.5.0` | MINOR — `0.3.2` → `0.4.0` |
| only Fixed and Security | PATCH — `1.4.2` → `1.4.3` | PATCH — `0.3.2` → `0.3.3` |

Below 1.0.0 semver says the API is not stable yet, so a breaking change does not force `1.0.0`.
Going to `1.0.0` is the owner's decision, not a side effect of a removed field — this skill never
picks it. If the user asks for it, use it.

A bump resets the parts to its right: a MINOR bump sets PATCH to `0`, a MAJOR bump sets both.

**An older release.** If the file already has a release dated after this one — for example a
merge from last week described after today's — stop and ask. Inserting it in the middle would
renumber every release above it.

## Step 7 — Write CHANGELOG.md

The file is at the repository root. If it does not exist, create it with this header:

```markdown
# Changelog

All notable changes to this project are documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).
```

Then:

- Add the release as `## [x.y.z] - YYYY-MM-DD` right under the header, above every older
  release. The newest release is always first.
- Sections go in this fixed order, each as `### <Section>`: Added, Changed, Deprecated, Removed,
  Fixed, Security.
- Leave out a section with no entries. Do not write an empty heading.
- Never change an older release. It describes what was already shipped.
- No link references at the bottom (`[0.2.0]: https://…/compare/…`). They need the tags on
  `origin`, and the tags come from a GitHub workflow, not from this skill.

Before the write, check the current branch with `git branch --show-current`. The file is written
into the working tree, so it lands on whatever branch is checked out:

- On `master` — write it, and say that the change has to move to a new branch before it is
  committed.
- On another branch in merge or scope mode — that branch may have nothing to do with the change.
  Say which branch it is, and write only after the user confirms.

In `--dry-run`, skip the write and show the block that would be written.

**Do not commit.** The user reviews the file and commits it. In branch mode the
commit belongs on the branch, so it lands with the branch. In merge and scope mode the user is usually on `master`, where
`.claude/rules/git.md` forbids direct work — the user moves the change to a branch.

## Step 8 — Report

Show, in the user's language:

1. The range: base, tip, number of commits, and the branch name if known. In scope mode, how the
   set was chosen and which other-scope commits the user added.
2. The version: the current one, the new one, and the entry that decided the bump.
3. The entries, by section, exactly as written to the file.
4. The out-list: what was left out and why, grouped by reason (`.claude/` workflows — 4 commits,
   the task tracker, inner-ring code not reachable yet). One commit often touches several groups,
   so count by commit only where it is clean; otherwise name the files or the change. Short, not one
   line per commit.
5. Any judgement call the user may disagree with: an entry that could be Fixed or Added, a change
   you marked breaking without a marker, a group of commits you merged into one entry.
6. Next step: `git diff CHANGELOG.md`, then commit it — `docs: add the changelog for <x.y.z>`.
   Do not create a tag. `.github/workflows/release.yml` tags the release after the merge into
   `master`. A local run is optional: when a branch has no release of its own, that workflow runs
   this skill on the merge and opens a Release-PR with the result.

If the user changes an entry or a section after the report, apply it. If the change is a rule this
skill does not cover yet, say so with ⚠️ — it may belong in this file.
