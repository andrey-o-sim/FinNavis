---
name: finnavis-pr-create
description: >
  Opens a pull request into master for the current work branch of FinNavis (a solo, self-hosted
  .NET pet project on GitHub). Owns the PR title convention — a Conventional Commit line that
  becomes the squash commit, with the task id as the scope: "feat(t14): match a row on its
  external reference", or "chore: bump npgsql" when there is no task — the fixed body template,
  and the link to the task file under docs/features/<slug>/tasks/. Takes the task id first from
  the branch name (feature/T14 means task T14), then from the arguments, the commit messages and
  tracker.md, and checks it against tasks.json. One branch carries out one task. Picks the commit
  type from the diff and asks only when the code does not make it clear. Keeps the two approval gates of .claude/rules/git.md: one before git push, one before
  gh pr create. Use this whenever the user wants to open, create, prepare or draft a PR, or push a
  branch for review in this repo — including phrasings like "создай PR", "открой пулл реквест",
  "сделай PR для T14", "подготовь описание PR", "запушь и открой PR", "/finnavis-pr-create" —
  even when they do not say the words pull request. Prefer it over a generic PR flow in this repo.
  Refuses on master, with a dirty tree, on a branch with no commits ahead of master, and on a
  branch that carries out more than one task.
argument-hint: "[task id, e.g. T14 or csv-import/T14] [--dry-run]"
allowed-tools: Bash, Read, Grep, Glob, Write, AskUserQuestion
---

# Create a pull request — FinNavis

The PR title becomes the one commit that lands on `master` (squash merge). So the title is the
long-lived history of the repo, and the body is the record of why the change was made. Both are
read later by the owner, who reads English as a second language. Write simply: short sentences,
one idea per sentence, plain words. Keep technical terms as they are.

Base, merge style and branch cleanup are in `.claude/rules/git.md` §Pull requests. Commit rules,
including the checks that run before every commit, are in `.claude/rules/git.md` §Commits. This
skill does not repeat them. It owns only the title, the body and the task reference.

Talk to the user in the language they wrote in. The title and body are always English.

## Arguments

- Task id, optional: `T14`, or with the feature slug `csv-import/T14`. Usually not needed — the
  branch name already carries it.
- `--dry-run`: build the title and body, write them to a file, and stop. No push, no PR. Write to
  the path the user gives, or else to `pr-draft.md` in the scratchpad directory. The file holds the
  title on the first line, an empty line, then the body.

## Step 1 — Refuse early

Stop with a clear message, and do nothing else, when:

- the current branch is `master`;
- `git status --porcelain` is not empty — uncommitted work would not be in the PR, and committing
  it is a separate step with its own checks;
- `git log master..HEAD` is empty — there is nothing to open a PR for.

## Step 2 — Collect the facts

```bash
git branch --show-current
git log --format='%h %s%n%b' master..HEAD
git diff --stat master...HEAD
git diff master...HEAD
```

For a large diff, read the stat first and then only the files that decide the type and the
subject. Also check `gh pr list --head <branch>`: if a PR already exists for this branch, tell the
user and stop — this skill opens PRs, it does not edit them.

## Step 3 — Find the task

A task is an internal one: a `T<NN>` entry in `docs/features/<slug>/tasks/tasks.json`, with its
file `docs/features/<slug>/tasks/T<NN>-<name>.md`. There is no external tracker.

One branch carries out one task. So the result of this step is exactly one task, or none.

**The branch name comes first.** A branch named `<prefix>/T<NN>` — for example `feature/T01` or
`bugfix/t07` — is the work of task `T<NN>`. The id is the part right after the prefix; match it
without regard to case. When the branch name gives an id, use it and treat the sources below only
as a cross-check.

When the branch name gives no id, look in this order:

1. The skill arguments.
2. The commit messages on the branch.
3. The diff of any `docs/features/<slug>/tasks/tracker.md` — a row whose status changed to `done`
   or `review` on this branch is the task this branch carries out.

Then check the id:

- The branch name does not carry the feature slug, and the same `T<NN>` can exist in several
  features. Find the slug from the argument, the tracker path, the feature folder the diff
  touches, or the commit scope. Check `tasks.json` of every feature. If the id exists in more than
  one feature and nothing on the branch tells which one, ask.
- The id must exist in that feature's `tasks.json`. An id that does not exist is a mistake —
  tell the user and ask, do not drop it silently and do not invent a task.
- Find the task file with `ls docs/features/<slug>/tasks/T<NN>-*.md`.

Stop and ask when the sources disagree — the branch says `T01` and a commit or the tracker says
`T02` — or when the branch clearly carries out more than one task. One branch per task is the
rule; tell the user and suggest splitting the branch. Do not pick one task yourself.

No task found is a normal outcome — a `chore/` or `docs/` branch often has none. Then the title has
no scope and the body has no `## Task` section. Do not ask the user for a task in that case.

## Step 4 — Pick the type

The type list is closed: `feat`, `fix`, `docs`, `refactor`, `test`, `chore`, `build`, `ci`,
`perf`. Pick by what the squashed change does to `master` as a whole, not by the first commit:

- new behaviour or an extended one → `feat`;
- a defect fixed in work already on `master` → `fix`;
- only documents (`docs/`, `ARCHITECTURE.md`, `CLAUDE.md`, ADRs) → `docs`;
- only tests → `test`;
- the code changes shape and no behaviour changes → `refactor`;
- skills, dependencies, editor config, things around the code → `chore`;
- `build.sh`, `Directory.Build.props`, csproj build settings → `build`;
- the pipeline → `ci`; a measured speed-up → `perf`.

When the branch mixes kinds, the one that matters most to `master` wins: a `feature/` branch with
`feat`, `test` and `docs` commits is `feat`. The task's `layer` in `tasks.json` and the branch
prefix are hints, not the answer.

If the diff does not make the type clear — for example a change that could be a `fix` or a `feat`
depending on whether the old behaviour was intended — ask the user with `AskUserQuestion`. Give the
two candidate types and one line each on why. Do not guess.

Breaking change: a removed or renamed endpoint, field, or error code, or a changed meaning of an
existing one. Mark it with `!` right before the colon and add the footer in the body (see below).

## Step 5 — Write the title

The PR title has its own scope rule. It is not the commit scope from `git.md`: in a PR title the
scope is the task id, and nothing else.

With a task:

```
<type>(<task id>)<!>: <subject>
```

Without a task:

```
<type><!>: <subject>
```

- Task id: lowercase, exactly as `tasks.json` numbers it — `t01`, `t14`. No feature slug, no
  layer, no area.
- No task → no brackets at all. Do not put a slug or a layer there instead.
- Subject: imperative mood, lowercase, no full stop. It says what changed for the product or the
  repo, not which files changed.
- The whole title is 72 characters or fewer. If it is longer, shorten the subject. Never drop the
  task id to make it fit.

Examples:

```
feat(t14): match a row on its external reference first
fix(t07): keep the money scale at four decimals on read
feat(t31)!: move history under /api/v1/history
chore: add finnavis-pr-create for the pr title and body
fix: point the master push hook at the real script
```

## Step 6 — Write the body

Use this template. Leave out a section only where the note says so.

```markdown
## What changed

- <one bullet per visible change, in plain words>

## Why

<one to three short sentences: the reason, the problem it solves, or the decision it carries out>

## Task

- [csv-import/T14 — <task title from tasks.json>](https://github.com/andrey-o-sim/FinNavis/blob/master/docs/features/csv-import/tasks/T14-app-pipeline-policy-and-matcher.md)

## Docs

- [ADR-0002 — <title>](https://github.com/andrey-o-sim/FinNavis/blob/master/docs/features/csv-import/adr/0002-....md)
- [PRD](https://github.com/andrey-o-sim/FinNavis/blob/master/docs/features/csv-import/PRD.md) — AC-04, AC-07

BREAKING CHANGE: <what breaks, for whom>
```

Rules for each part:

- **What changed** — three to seven bullets. Describe the effect, not the diff: "an imported row
  with a known bank reference is matched before the date window is tried", not "edited
  Matcher.cs". The diff is already in the PR.
- **Why** — the reason, in full. If the reason is in an ADR, say it in one sentence and link the ADR
  under Docs.
- **Task** — one bullet, with the feature slug, the id and the title copied from `tasks.json`.
  The title does not carry the slug, so this bullet is where the feature is named. Link the task file with
  the absolute `blob/master` URL: the branch is deleted after the merge, so a branch link would
  break, and a relative link does not resolve in a PR body. Leave the section out when there is no
  task.
- **Docs** — the ADR, PRD, `sad.md`, `data-model.md` or contract the work follows, and the AC ids
  from the task's `acs` in `tasks.json`. Take them from the task file and the commit bodies; do not
  add a document that the work does not follow. Use `blob/master` URLs, as for the task. A
  document that this branch creates is not on `master` yet, but the link works after the merge.
  Leave the section out when there is none.
- **BREAKING CHANGE** — only with `!` in the title. It goes last, as a plain line, so it lands in
  the squash commit as the footer.

## Step 7 — Show, then the two gates

1. Show the user the title and the body exactly as they will be sent, plus the list of commits.
2. In `--dry-run`, write the file now, say its full path, and stop here.
3. Ask before `git push -u origin <branch>`. Always name the branch; never push `master`.
   If `origin` already has the branch and it is up to date, say so and skip the push.
4. After the push, ask again before `gh pr create`. Approval to push is not approval to open a PR.
5. Create the PR with the body from a file, so quotes and backticks are not broken by the shell:

```bash
gh pr create --base master --head <branch> --title "<title>" --body-file <path to body file>
```

6. Give the user the PR URL.

If the user changes the title or body after the preview, apply the change and show the result
again before the gate. If the change is a convention this skill does not cover yet, say so with
⚠️ — it may belong in this file.
