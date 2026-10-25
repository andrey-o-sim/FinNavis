# Git rules

Applies to every change in this repo, by hand or by Claude. Lines marked **Claude** are gates that
apply to the agent only — the owner is free to work directly.

## Branches

`master` is the trunk. It is the only long-lived branch. There is no `develop` and no `release/*`
— this project has one owner, no release windows and no tags, so an integration branch would add a
merge step and protect nothing. Add them in this file when releases start, not before.

Everything else is a short-lived work branch, cut from `master` and merged back into `master` by
pull request.

### Prefix — pick by the task

| Prefix     | Use it for                                                              |
|------------|-------------------------------------------------------------------------|
| `feature/` | New behaviour, or an extension of existing behaviour.                     |
| `bugfix/`  | A defect in work that is already on `master`. Not urgent.                 |
| `hotfix/`  | A defect that has to ship now. Same shape as `bugfix/`, different urgency.|
| `docs/`    | Documents only — `docs/`, `ARCHITECTURE.md`, `CLAUDE.md`, ADRs, skills.   |
| `test/`    | Tests only — adding coverage, fixing a flaky test, test infrastructure.   |
| `chore/`   | Everything around the code — dependencies, `build.sh`, CI, editor config. |

These six plus `master` are the whole list. If a change does not fit one, it is usually two
changes.

`chore/` is the easiest one to abuse. Its boundary: the change adds no behaviour, fixes no defect,
adds no test and writes no document. The moment a "dependency bump" needs a code change to keep the
build green, it stops being a chore and becomes `feature/` or `bugfix/`.

### Name

```
<prefix>/<short-slug>
```

- Lowercase, dash-separated, no spaces, no underscores.
- Three or four words. The slug says what changes, not how: `feature/csv-import-endpoint`, not
  `feature/add-new-endpoint-for-the-csv-import-feature`.
- No issue number. This repo has no issue tracker.

Good: `feature/transfer-rounding`, `docs/git-rules`, `bugfix/balance-off-by-one`,
`test/account-repository-integration`, `chore/bump-npgsql`.

Bad: `feature/1`, `feature/2-skill`, `my-branch`, `fix_stuff`.

Branches created before this rule keep their names. The rule applies to new branches.

**Claude:** create the branch before the first edit, without asking. Never commit to `master`.

## Commits

[Conventional Commits 1.0.0](https://www.conventionalcommits.org/en/v1.0.0/).

```
<type>(<scope>): <subject>

<body>
```

### Type — the closed list

`feat`, `fix`, `docs`, `refactor`, `test`, `chore`, `build`, `ci`, `perf`.

Nothing else. The repo's older commits use `skills:`, `api:`, `sequences:`, `tasks:` and bare
numbers like `06:` — those predate this rule and stay as they are. New commits use the list above:
work on a feature's documents is `docs(<feature-slug>): …`, work on a skill is `chore(skills): …`.

The type describes the change. The branch prefix describes the task. They often differ, and that is
fine: a `feature/` branch ends with a `feat` commit, a `test` commit and a `docs` commit.

### Scope — optional, but use it

- A feature slug from `docs/features/`: `core-ledger`, `csv-import`.
- Otherwise a layer or an area: `domain`, `application`, `infrastructure`, `presentation`, `build`,
  `skills`.
- Leave it out when the change really is repo-wide.

### Subject

- Imperative mood, lowercase, no full stop: `add the category group side`, not `Added …` or
  `adds …`.
- 72 characters or fewer.
- Say what changed, not which files changed.

### Body

- Optional for a one-line change. Required when the *why* is not obvious from the subject.
- Wrap at 100 characters. Explain the reason, not the diff — the diff is already in the commit.
- Reference an ADR by path when the commit carries out a decision:
  `See docs/adr/0003-spell-an-error-code-in-pascalcase.md`.

### Breaking changes

`!` after the type or scope, plus a `BREAKING CHANGE:` footer naming what breaks.

```
feat(presentation)!: move history under /api/v1/history

BREAKING CHANGE: GET /api/v1/transactions no longer returns transfers.
```

### Language

English, like every other written artifact in this repo. See `.claude/rules/language.md`.

### Examples

```
feat(core-ledger): record a transfer between two accounts
fix(infrastructure): keep the money scale at four decimals on read
docs(csv-import): add the data model and the staged migrations
test(domain): cover the transfer rounding edge cases
chore(skills): add finnavis-fix-term as the one owner of the glossary
```

**Claude:** commit without asking. A commit is local and reversible — the gate that matters is the
push. One logical change per commit: do not bundle a refactor into a feature commit.

## Pull requests

Every change reaches `master` through a pull request. No direct push to `master`, by hand or by
agent.

- **Base** is always `master`.
- **Merge** is a squash merge. The branch is short-lived and its intermediate commits are noise;
  the squashed subject is the branch's one Conventional Commit line.
- **Delete** the branch after the merge, local and remote.
- **Title** follows the same Conventional Commit format as a commit subject — it becomes the
  squashed commit.
- **Body** states what changed and why in short bullets, and links the ADR, PRD or feature document
  the work follows.
- The PR is self-review. There is no second reviewer. Read your own diff before you open it.

### Before opening a PR

- `./build.sh build` passes. Warnings are errors here, so a warning is a failure.
- `./build.sh test` passes, architecture tests included.
- No `launchSettings.json`, `.env`, `*.pem`, `*.key` or real financial data in the diff
  (`CLAUDE.md` §Safety).

**Claude:** two gates, both explicit.

1. Ask before `git push`. Nothing reaches `origin` without approval.
2. Ask again before `gh pr create`. Approval to push is not approval to open a PR.

Branching, editing and committing are local and need no approval. Pushing and opening a PR each do.

## Things that need no rule

- Rebase or merge to update a branch — either is fine, the branch is short-lived and solo.
- Force-push to your own work branch is fine. Force-push to `master` is never fine.
- `.gitignore` covers the secret files. Check `git status` before staging anyway.
